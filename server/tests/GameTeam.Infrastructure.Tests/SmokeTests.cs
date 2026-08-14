using FluentAssertions;
using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Domain.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GameTeam.Infrastructure.Tests;

/// <summary>
/// Smoke test bootstrap — xác nhận wiring DI Infrastructure chạy được (không mở kết nối DB thật;
/// <c>AddDbContext</c> lười). Integration test EF thật (Testcontainers Postgres) ở
/// <see cref="Persistence.PersistenceIntegrationTests"/>.
/// </summary>
public class SmokeTests
{
    private static IConfiguration ConfigurationWithPostgres() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Chuỗi giả — AddDbContext lười, không mở kết nối lúc đăng ký.
                ["ConnectionStrings:Postgres"] = "Host=localhost;Database=gameteam;Username=gameteam;Password=x",
            })
            .Build();

    [Fact]
    public void AddInfrastructure_registers_without_error()
    {
        var services = new ServiceCollection();

        GameTeam.Infrastructure.DependencyInjection.AddInfrastructure(services, ConfigurationWithPostgres());

        services.Should().NotBeNull();
    }

    [Fact]
    public void AddInfrastructure_registers_persistence_ports_and_clock()
    {
        var services = new ServiceCollection();

        GameTeam.Infrastructure.DependencyInjection.AddInfrastructure(services, ConfigurationWithPostgres());

        // Port Phase 10 phải được hiện thực (DIP): UoW, repository generic, clock.
        services.Should().Contain(d => d.ServiceType == typeof(IUnitOfWork));
        services.Should().Contain(d => d.ServiceType == typeof(IRepository<,>));
        services.Should().Contain(d => d.ServiceType == typeof(IClock));
    }

    [Fact]
    public void AddInfrastructure_throws_when_connection_string_missing()
    {
        var services = new ServiceCollection();
        IConfiguration emptyConfig = new ConfigurationBuilder().Build();

        Action act = () => GameTeam.Infrastructure.DependencyInjection.AddInfrastructure(services, emptyConfig);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ConnectionStrings*Postgres*");
    }
}
