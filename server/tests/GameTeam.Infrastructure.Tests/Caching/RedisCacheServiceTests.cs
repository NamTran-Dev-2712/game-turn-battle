using System.Diagnostics;
using FluentAssertions;
using GameTeam.Domain.Common;
using GameTeam.Infrastructure.Caching;
using StackExchange.Redis;
using Xunit;

namespace GameTeam.Infrastructure.Tests.Caching;

/// <summary>
/// Integration test <see cref="RedisCacheService"/> trên Redis THẬT (Testcontainers <c>redis:7-alpine</c>).
/// Phủ: set/get (kể cả <c>Result&lt;T&gt;</c> để chứng minh converter), TTL hết hạn, remove. Mỗi test dùng key
/// GUID để cô lập, không phụ thuộc thứ tự chạy (Phase 12 — docs/backend/infrastructure.md).
/// </summary>
public sealed class RedisCacheServiceTests : IClassFixture<RedisContainerFixture>, IDisposable
{
    private readonly ConnectionMultiplexer _multiplexer;
    private readonly RedisCacheService _cache;

    public RedisCacheServiceTests(RedisContainerFixture fixture)
    {
        ConfigurationOptions options = ConfigurationOptions.Parse(fixture.ConnectionString);
        options.AbortOnConnectFail = false;
        _multiplexer = ConnectionMultiplexer.Connect(options);
        _cache = new RedisCacheService(_multiplexer, new CapturingLogger<RedisCacheService>(), "test");
    }

    [Fact]
    public async Task Set_then_Get_returns_same_value()
    {
        string key = NewKey();
        await _cache.SetAsync(key, new SampleDto("hello", 42), TimeSpan.FromMinutes(5), CancellationToken.None);

        SampleDto? got = await _cache.GetAsync<SampleDto>(key, CancellationToken.None);

        got.Should().NotBeNull();
        got!.Name.Should().Be("hello");
        got.Number.Should().Be(42);
    }

    [Fact]
    public async Task Set_then_Get_round_trips_Result_of_T()
    {
        string key = NewKey();
        Result<SampleDto> success = Result.Success(new SampleDto("ok", 7));
        await _cache.SetAsync(key, success, TimeSpan.FromMinutes(5), CancellationToken.None);

        Result<SampleDto>? got = await _cache.GetAsync<Result<SampleDto>>(key, CancellationToken.None);

        got.Should().NotBeNull();
        got!.IsSuccess.Should().BeTrue();
        got.Value.Name.Should().Be("ok");
        got.Value.Number.Should().Be(7);
    }

    [Fact]
    public async Task Get_missing_key_returns_null()
    {
        SampleDto? got = await _cache.GetAsync<SampleDto>(NewKey(), CancellationToken.None);

        got.Should().BeNull();
    }

    [Fact]
    public async Task Set_with_short_ttl_expires()
    {
        string key = NewKey();
        await _cache.SetAsync(key, new SampleDto("temp", 1), TimeSpan.FromMilliseconds(300), CancellationToken.None);

        // Có ngay sau khi set.
        (await _cache.GetAsync<SampleDto>(key, CancellationToken.None)).Should().NotBeNull();

        // Poll tới khi hết hạn (không sleep dài cứng) — Redis expiry là absolute.
        SampleDto? afterExpiry = await PollUntilNullAsync(key, TimeSpan.FromSeconds(5));

        afterExpiry.Should().BeNull();
    }

    [Fact]
    public async Task Remove_deletes_entry()
    {
        string key = NewKey();
        await _cache.SetAsync(key, new SampleDto("x", 1), TimeSpan.FromMinutes(5), CancellationToken.None);
        (await _cache.GetAsync<SampleDto>(key, CancellationToken.None)).Should().NotBeNull();

        await _cache.RemoveAsync(key, CancellationToken.None);

        (await _cache.GetAsync<SampleDto>(key, CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task Remove_missing_key_is_noop()
    {
        Func<Task> act = () => _cache.RemoveAsync(NewKey(), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    private async Task<SampleDto?> PollUntilNullAsync(string key, TimeSpan timeout)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < timeout)
        {
            SampleDto? current = await _cache.GetAsync<SampleDto>(key, CancellationToken.None);
            if (current is null)
            {
                return null;
            }

            await Task.Delay(50);
        }

        return await _cache.GetAsync<SampleDto>(key, CancellationToken.None);
    }

    private static string NewKey() => $"probe:{Guid.NewGuid():N}";

    public void Dispose() => _multiplexer.Dispose();

    /// <summary>DTO mẫu (reference type) để round-trip qua cache.</summary>
    public sealed record SampleDto(string Name, int Number);
}
