using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Domain.Common;
using GameTeam.Infrastructure.Persistence;
using GameTeam.Infrastructure.Persistence.Repositories;
using GameTeam.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameTeam.Infrastructure;

/// <summary>
/// Composition của tầng Infrastructure — hiện thực các port của Application (DIP): EF Core/PostgreSQL
/// persistence (Phase 11) + server-time (<see cref="IClock"/>). Redis/JWT/ConfigService/jobs ở phase sau
/// (12/18/21). Wiring thuần — KHÔNG logic gameplay. Canonical: docs/backend/infrastructure.md.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Khoá connection string PostgreSQL trong configuration (env: <c>ConnectionStrings__Postgres</c>).</summary>
    public const string PostgresConnectionName = "Postgres";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // ── Persistence: EF Core + Npgsql (ADR-007) ──────────────────────────────────────────────
        // Connection string LẤY TỪ CONFIG (env ConnectionStrings__Postgres) — không hardcode credential.
        string? connectionString = configuration.GetConnectionString(PostgresConnectionName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Thiếu connection string 'ConnectionStrings:{PostgresConnectionName}' (env ConnectionStrings__Postgres). " +
                "Cấu hình qua appsettings/biến môi trường — xem deploy/compose/docker-compose.yml.");
        }

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

        // Dispatcher domain event (MediatR bridge) do AppDbContext gọi ở SaveChanges.
        services.AddScoped<DomainEventDispatcher>();

        // Ports Phase 10 → hiện thực EF (scoped, cùng vòng đời DbContext/request).
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IRepository<,>), typeof(EfRepository<,>));

        // ── Server-time boundary (Domain port IClock) ────────────────────────────────────────────
        services.AddSingleton<IClock, SystemClock>();

        return services;
    }
}
