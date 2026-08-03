using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace GameTeam.Application;

/// <summary>
/// Composition của tầng Application (đăng ký gọn cho DI — docs/backend/solution-structure.md §2).
/// BOOTSTRAP: chỉ wiring MediatR + FluentValidation (chưa có handler/validator thật).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        Assembly assembly = typeof(AssemblyMarker).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);

        // TODO (phase Core Framework+): pipeline behaviors theo thứ tự
        //   Validation -> Logging -> Transaction -> Caching -> Idempotency
        // (docs/backend/domain-and-application.md). KHÔNG thêm logic gameplay ở đây.
        return services;
    }
}
