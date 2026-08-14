using FluentAssertions;
using GameTeam.Application.Abstractions.Caching;
using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Domain.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Xunit;

namespace GameTeam.Infrastructure.Tests;

/// <summary>
/// Smoke test bootstrap — xác nhận wiring DI Infrastructure chạy được (không mở kết nối thật: <c>AddDbContext</c>
/// lười và multiplexer Redis được tạo trong factory singleton, chỉ connect khi resolve). Integration test thật
/// (Testcontainers Postgres/Redis) ở <see cref="Persistence.PersistenceIntegrationTests"/> và
/// <see cref="Caching.RedisCacheServiceTests"/>.
/// </summary>
public class SmokeTests
{
    private static IConfiguration ConfigurationWithConnections() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Chuỗi giả — đăng ký lười, không mở kết nối lúc AddInfrastructure.
                ["ConnectionStrings:Postgres"] = "Host=localhost;Database=gameteam;Username=gameteam;Password=x",
                ["ConnectionStrings:Redis"] = "localhost:6379",
            })
            .Build();

    [Fact]
    public void AddInfrastructure_registers_without_error()
    {
        var services = new ServiceCollection();

        GameTeam.Infrastructure.DependencyInjection.AddInfrastructure(services, ConfigurationWithConnections());

        services.Should().NotBeNull();
    }

    [Fact]
    public void AddInfrastructure_registers_persistence_cache_and_clock()
    {
        var services = new ServiceCollection();

        GameTeam.Infrastructure.DependencyInjection.AddInfrastructure(services, ConfigurationWithConnections());

        // Port Phase 10/12 phải được hiện thực (DIP): UoW, repository generic, cache, multiplexer, clock.
        services.Should().Contain(d => d.ServiceType == typeof(IUnitOfWork));
        services.Should().Contain(d => d.ServiceType == typeof(IRepository<,>));
        services.Should().Contain(d => d.ServiceType == typeof(ICacheService));
        services.Should().Contain(d => d.ServiceType == typeof(IConnectionMultiplexer));
        services.Should().Contain(d => d.ServiceType == typeof(IClock));
    }

    [Fact]
    public void AddInfrastructure_throws_when_postgres_connection_string_missing()
    {
        var services = new ServiceCollection();
        IConfiguration emptyConfig = new ConfigurationBuilder().Build();

        Action act = () => GameTeam.Infrastructure.DependencyInjection.AddInfrastructure(services, emptyConfig);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ConnectionStrings*Postgres*");
    }

    [Fact]
    public void AddInfrastructure_throws_when_redis_connection_string_missing()
    {
        var services = new ServiceCollection();
        IConfiguration postgresOnly = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = "Host=localhost;Database=gameteam;Username=gameteam;Password=x",
            })
            .Build();

        Action act = () => GameTeam.Infrastructure.DependencyInjection.AddInfrastructure(services, postgresOnly);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ConnectionStrings*Redis*");
    }
}
