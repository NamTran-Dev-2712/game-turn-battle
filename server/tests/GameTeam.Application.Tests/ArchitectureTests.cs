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
    public void Domain_should_not_depend_on_framework_packages()
    {
        // Phase 09: Domain là lõi THUẦN (ADR-003) — không package/framework nào.
        // Domain hiện không có PackageReference; test này khoá bất biến cho các phase sau.
        TestResult result = Types.InAssembly(typeof(GameTeam.Domain.AssemblyMarker).Assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore",
                "Microsoft.Extensions",
                "MediatR",
                "FluentValidation",
                "Npgsql",
                "StackExchange.Redis")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Domain phải thuần — không phụ thuộc EF/ASP.NET/MediatR/FluentValidation/Npgsql/Redis (ADR-003).");
    }

    [Fact]
    public void Application_should_not_depend_on_infrastructure_or_api()
    {
        // Phase 10: Application khai báo PORT (IUnitOfWork/IRepository/ICacheService/IConfigProvider)
        // và pipeline behaviors; Infrastructure hiện thực port (DIP). Application KHÔNG được ref
        // Infrastructure (cụ thể) hay Api (presentation) — hướng phụ thuộc vào trong (ADR-003).
        TestResult result = Types.InAssembly(typeof(GameTeam.Application.AssemblyMarker).Assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "GameTeam.Infrastructure",
                "GameTeam.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Application chỉ phụ thuộc interface (DIP), không phụ thuộc Infrastructure cụ thể hay Api.");
    }

    [Fact]
    public void Application_should_not_depend_on_efcore_or_npgsql()
    {
        // Phase 11: EF Core/Npgsql là chi tiết persistence — CHỈ Infrastructure được dùng. Application khai
        // báo port (IUnitOfWork/IRepository) và không được rò EF lên trên (ADR-003/007). Domain đã được canh
        // bởi Domain_should_not_depend_on_framework_packages; đây là gác cổng cho tầng Application.
        TestResult result = Types.InAssembly(typeof(GameTeam.Application.AssemblyMarker).Assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "Npgsql")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Application chỉ khai báo port; EF Core/Npgsql là chi tiết Infrastructure (ADR-003/007).");
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
