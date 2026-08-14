using Testcontainers.Redis;
using Xunit;

namespace GameTeam.Infrastructure.Tests.Caching;

/// <summary>
/// Fixture khởi tạo một container <c>redis:7-alpine</c> thật (Testcontainers) cho integration test cache —
/// hành vi Redis đúng (Phase 12, docs/testing/backend-testing.md §4). Yêu cầu Docker runtime (CI ubuntu-latest
/// có sẵn; local chạy scripts/dev/up). Testcontainers cấp host-port ngẫu nhiên (không đụng 6379 dev).
/// </summary>
public sealed class RedisContainerFixture : IAsyncLifetime
{
    private readonly RedisContainer _container = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    /// <summary>Connection string StackExchange.Redis tới container đã sẵn sàng (host:port).</summary>
    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
