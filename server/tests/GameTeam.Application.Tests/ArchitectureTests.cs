using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace GameTeam.Application.Tests;

/// <summary>
/// Kiểm dependency rule của Clean Architecture (docs/architecture/dependency-graph.md).
/// Đây là hạt giống cho bộ architecture test đầy đủ ở phase sau.
/// </summary>
public class ArchitectureTests
{
    [Fact]
    public void Domain_should_not_depend_on_outer_layers()
    {
        TestResult result = Types.InAssembly(typeof(GameTeam.Domain.AssemblyMarker).Assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "GameTeam.Application",
                "GameTeam.Infrastructure",
                "GameTeam.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Domain phải thuần, không phụ thuộc tầng ngoài (ADR-003).");
    }

    [Fact]
    public void Application_should_not_depend_on_infrastructure()
    {
        TestResult result = Types.InAssembly(typeof(GameTeam.Application.AssemblyMarker).Assembly)
            .Should()
            .NotHaveDependencyOn("GameTeam.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Application chỉ phụ thuộc interface (DIP), không phụ thuộc Infrastructure cụ thể.");
    }

    [Fact]
    public void Contracts_should_not_depend_on_application_or_infrastructure()
    {
        // Hướng phụ thuộc hợp lệ: Contracts → Domain (enum/hằng) MÀ THÔI
        // (docs/architecture/dependency-graph.md §2/§6, Phase 05 completion criteria).
        TestResult result = Types.InAssembly(typeof(GameTeam.Contracts.AssemblyMarker).Assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "GameTeam.Application",
                "GameTeam.Infrastructure",
                "GameTeam.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Contracts là spine dùng chung, chỉ được phụ thuộc Domain — không App/Infra/Api.");
    }
}
