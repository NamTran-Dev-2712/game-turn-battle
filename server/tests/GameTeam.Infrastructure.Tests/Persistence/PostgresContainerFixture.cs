using Testcontainers.PostgreSql;
using Xunit;

namespace GameTeam.Infrastructure.Tests.Persistence;

/// <summary>
/// Fixture khởi tạo một container <c>postgres:16-alpine</c> thật (Testcontainers) cho integration test
/// persistence — hành vi SQL đúng (ADR-007, docs/testing/backend-testing.md §4). Yêu cầu Docker runtime
/// (CI ubuntu-latest có sẵn; local chạy scripts/dev/up). Testcontainers cấp host-port ngẫu nhiên (không đụng 5432 dev).
/// </summary>
public sealed class PostgresContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    /// <summary>Connection string Npgsql tới container đã sẵn sàng.</summary>
    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
