using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameTeam.Infrastructure;

/// <summary>
/// Composition của tầng Infrastructure (đăng ký EF/Redis/JWT/ConfigService/jobs).
/// BOOTSTRAP: stub rỗng — chưa đăng ký gì; hiện thực ở phase Core Framework
/// (docs/backend/infrastructure.md, docs/backend/cross-cutting.md).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // TODO:
        //   - AddDbContext<AppDbContext>(Npgsql, connection string từ configuration)
        //   - Redis (IConnectionMultiplexer / IDistributedCache)
        //   - JWT authentication + authorization
        //   - Configuration Service (IConfigProvider) — versioned bundle (ADR-005)
        //   - Background jobs
        // Wiring thuần — KHÔNG logic gameplay.
        _ = configuration;
        return services;
    }
}
