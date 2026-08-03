using Microsoft.Extensions.DependencyInjection;

namespace GameTeam.Api;

/// <summary>
/// Composition của tầng Api/Presentation (controllers, versioning, swagger, authz, SignalR).
/// BOOTSTRAP: tối thiểu — chỉ EndpointsApiExplorer; chưa có endpoint nghiệp vụ.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        // TODO (phase Core Framework+): API versioning (/api/vN), controllers/minimal API,
        // Swagger/OpenAPI (nguồn shared/contracts), authorization policies, SignalR (optional),
        // error contract, health checks đầy đủ. Xem docs/backend/api-and-versioning.md.
        return services;
    }
}
