using FluentAssertions;
using Xunit;

namespace GameTeam.Application.Tests;

/// <summary>Smoke test bootstrap — xác nhận wiring DI Application chạy được.</summary>
public class SmokeTests
{
    [Fact]
    public void AddApplication_registers_without_error()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        GameTeam.Application.DependencyInjection.AddApplication(services);
        services.Should().NotBeEmpty();
    }
}
