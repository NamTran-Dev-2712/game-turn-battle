using System.Text.Json.Nodes;
using FluentAssertions;
using GameTeam.Infrastructure.Caching;
using GameTeam.Infrastructure.Configuration;
using GameTeam.Infrastructure.Persistence;
using GameTeam.Infrastructure.Tests.Caching;
using GameTeam.Infrastructure.Tests.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Xunit;

namespace GameTeam.Infrastructure.Tests.Configuration;

/// <summary>
/// Configuration Service acceptance (Phase 21, ADR-005) on a REAL PostgreSQL + Redis (Testcontainers):
/// publish → provider reads by id; change a value → version bump, new served, old still served
/// (immutable/rollback foundation); validator-fail blocks publish leaving "current" unchanged; identical
/// config is idempotent (no version bump); each version is cached under its own immutable Redis key.
/// Requires Docker.
/// </summary>
public sealed class ConfigServiceIntegrationTests
    : IClassFixture<PostgresContainerFixture>, IClassFixture<RedisContainerFixture>, IAsyncLifetime
{
    private readonly PostgresContainerFixture _postgres;
    private readonly RedisContainerFixture _redis;
    private readonly RuntimeConfigProvider _provider = new();
    private readonly string _environment = "cfgtest-" + Guid.NewGuid().ToString("N"); // isolate Redis keys per test
    private readonly List<string> _tempDirs = [];

    private AppDbContext _db = null!;
    private IConnectionMultiplexer _multiplexer = null!;
    private RedisCacheService _cache = null!;

    public ConfigServiceIntegrationTests(PostgresContainerFixture postgres, RedisContainerFixture redis)
    {
        _postgres = postgres;
        _redis = redis;
    }

    public async Task InitializeAsync()
    {
        _db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(_postgres.ConnectionString).Options,
            new DomainEventDispatcher(new NoOpPublisher()));
        await _db.Database.EnsureCreatedAsync();

        // Per-test isolation on the shared container (bundle versions are sequential + the pointer is a singleton).
        await _db.ConfigCurrent.ExecuteDeleteAsync();
        await _db.ConfigBundles.ExecuteDeleteAsync();

        _multiplexer = await ConnectionMultiplexer.ConnectAsync(_redis.ConnectionString);
        _cache = new RedisCacheService(_multiplexer, NullLogger<RedisCacheService>.Instance, _environment);
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _multiplexer.DisposeAsync();
        foreach (string dir in _tempDirs)
        {
            try
            {
                Directory.Delete(dir, recursive: true);
            }
            catch (IOException)
            {
                // best-effort temp cleanup
            }
        }
    }

    [Fact]
    public async Task Publish_makes_the_provider_serve_config_by_id()
    {
        string config = TempConfig(dir => ConfigFixtures.WriteValidConfig(dir, heroHp: 42));

        PublishResult result = await Publisher(config).PublishAsync(CancellationToken.None);

        result.Published.Should().BeTrue();
        result.Version.Should().Be(1);
        _provider.CurrentVersion.Bundle.Should().Be(1);

        JsonNode? hero = _provider.Get<JsonNode>("hero", "hero_sample");
        hero.Should().NotBeNull();
        hero!["base_stats"]!["hp"]!.GetValue<int>().Should().Be(42);
        _provider.GetIds("hero").Should().ContainSingle().Which.Should().Be("hero_sample");
    }

    [Fact]
    public async Task Changing_a_value_publishes_a_new_version_and_keeps_the_old_one()
    {
        (await Publisher(TempConfig(d => ConfigFixtures.WriteValidConfig(d, 10))).PublishAsync(CancellationToken.None))
            .Version.Should().Be(1);

        PublishResult second = await Publisher(TempConfig(d => ConfigFixtures.WriteValidConfig(d, 20)))
            .PublishAsync(CancellationToken.None);

        second.Published.Should().BeTrue();
        second.Version.Should().Be(2);
        _provider.CurrentVersion.Bundle.Should().Be(2);
        _provider.Get<JsonNode>("hero", "hero_sample")!["base_stats"]!["hp"]!.GetValue<int>().Should().Be(20);

        // Old version is immutable + still served (same API, no client rebuild) — rollback foundation.
        StoredBundle? old = await Store().GetByVersionAsync(1, CancellationToken.None);
        old.Should().NotBeNull();
        old!.ConfigVersion.Should().Be("config@v1");
        HeroHpOf(old.Payload).Should().Be(10);
    }

    [Fact]
    public async Task Invalid_config_does_not_publish_and_leaves_current_unchanged()
    {
        (await Publisher(TempConfig(d => ConfigFixtures.WriteValidConfig(d, 5))).PublishAsync(CancellationToken.None))
            .Version.Should().Be(1);

        PublishResult blocked = await Publisher(TempConfig(ConfigFixtures.WriteInvalidConfig))
            .PublishAsync(CancellationToken.None);

        blocked.Published.Should().BeFalse();
        blocked.Reason.Should().Be("validation-failed");
        _provider.CurrentVersion.Bundle.Should().Be(1, "current must not change on validation failure");

        // No v2 persisted; the invalid bundle is never served; current still serves the good config.
        (await Store().GetByVersionAsync(2, CancellationToken.None)).Should().BeNull();
        _provider.Get<JsonNode>("hero", "hero_sample").Should().NotBeNull();
        _provider.Get<JsonNode>("hero", "hero_bad").Should().BeNull();
    }

    [Fact]
    public async Task Republishing_identical_config_does_not_bump_the_version()
    {
        (await Publisher(TempConfig(d => ConfigFixtures.WriteValidConfig(d, 7))).PublishAsync(CancellationToken.None))
            .Version.Should().Be(1);

        // Same content, different temp dir ⇒ same checksum ⇒ idempotent redeploy.
        PublishResult again = await Publisher(TempConfig(d => ConfigFixtures.WriteValidConfig(d, 7)))
            .PublishAsync(CancellationToken.None);

        again.Published.Should().BeFalse();
        again.Reason.Should().Be("unchanged");
        again.Version.Should().Be(1);
        _provider.CurrentVersion.Bundle.Should().Be(1);
        (await Store().GetByVersionAsync(2, CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task Each_version_is_cached_under_its_own_immutable_redis_key()
    {
        await Publisher(TempConfig(d => ConfigFixtures.WriteValidConfig(d, 1))).PublishAsync(CancellationToken.None);
        await Publisher(TempConfig(d => ConfigFixtures.WriteValidConfig(d, 2))).PublishAsync(CancellationToken.None);

        // v1's versioned Redis key still exists after v2 is published (not overwritten).
        RedisValue rawV1 = await _multiplexer.GetDatabase()
            .StringGetAsync($"{_environment}:cache:config-bundle:config@v1");
        rawV1.HasValue.Should().BeTrue("v1's immutable version key must survive a v2 publish");

        StoredBundle? v1 = await Store().GetByVersionAsync(1, CancellationToken.None);
        StoredBundle? v2 = await Store().GetByVersionAsync(2, CancellationToken.None);
        v1!.Checksum.Should().NotBe(v2!.Checksum);
        HeroHpOf(v1.Payload).Should().Be(1);
        HeroHpOf(v2.Payload).Should().Be(2);
    }

    private ConfigBundlePublisher Publisher(string configRoot) => new(
        new ConfigBundleStore(_db, _cache),
        _provider,
        new TestClock(DateTimeOffset.UnixEpoch),
        Options.Create(new ConfigServiceOptions { ConfigRoot = configRoot, SchemaRoot = ConfigFixtures.SchemaRoot }),
        NullLogger<ConfigBundlePublisher>.Instance);

    private ConfigBundleStore Store() => new(_db, _cache);

    private string TempConfig(Action<string> author)
    {
        string dir = ConfigFixtures.NewTempConfigDir();
        _tempDirs.Add(dir);
        author(dir);
        return dir;
    }

    private static int HeroHpOf(string payloadJson)
        => JsonNode.Parse(payloadJson)!["data"]!["hero"]!["hero_sample"]!["base_stats"]!["hp"]!.GetValue<int>();
}
