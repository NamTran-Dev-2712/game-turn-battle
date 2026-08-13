using System.Reflection;
using FluentValidation;
using GameTeam.Application.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace GameTeam.Application;

/// <summary>
/// Composition of the Application layer (docs/backend/domain-and-application.md §2).
/// Registers MediatR + FluentValidation and the cross-cutting pipeline behaviors.
/// <para>
/// Behavior execution order (first registered = outermost):
/// <b>Logging → Validation → Transaction → Caching</b> (docs/backend/cross-cutting.md).
/// Logging wraps total time incl. validation; validation runs before a transaction opens;
/// transaction wraps only <c>ITransactionalRequest</c> commands; caching wraps only
/// <c>ICacheableQuery</c> reads (queries never enter a command transaction).
/// </para>
/// <para>
/// Ports (<c>IUnitOfWork</c>, <c>IRepository&lt;,&gt;</c>, <c>ICacheService</c>, <c>IConfigProvider</c>,
/// <c>IClock</c>) are declared in Application/Domain and implemented in Infrastructure (phases 11–12)
/// / Config Service (phase 21) — they are NOT registered here (DIP; Application stays Infra-free).
/// </para>
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        Assembly assembly = typeof(AssemblyMarker).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);

            // Order matters: outermost first.
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
            cfg.AddOpenBehavior(typeof(CachingBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
