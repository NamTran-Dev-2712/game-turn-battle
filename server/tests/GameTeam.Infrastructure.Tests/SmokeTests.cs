using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GameTeam.Infrastructure.Tests;

/// <summary>
/// Smoke test bootstrap — xác nhận wiring DI Infrastructure chạy được.
/// Integration test thật (EF/Redis qua Testcontainers) thêm ở phase sau
/// (docs/testing/backend-testing.md).
/// </summary>
public class SmokeTests
{
    [Fact]
    public void AddInfrastructure_registers_without_error()
    {
        var services = new ServiceCollection();
        IConfiguration configuration = new ConfigurationBuilder().Build();

        GameTeam.Infrastructure.DependencyInjection.AddInfrastructure(services, configuration);

        services.Should().NotBeNull();
    }
}
