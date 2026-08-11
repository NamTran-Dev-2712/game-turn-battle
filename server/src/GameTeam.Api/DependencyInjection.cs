using System.Text.Json.Serialization;
using GameTeam.Api.OpenApi;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;

namespace GameTeam.Api;

/// <summary>
/// Composition của tầng Api/Presentation (versioning, OpenAPI, authz, SignalR…).
/// Phase 05: contract-first — OpenAPI sinh từ GameTeam.Contracts ra shared/contracts (ADR-008).
/// Endpoint nghiệp vụ thật (handler) thêm ở Phase 13.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        // Enum contract serialize dạng CHUỖI (canonical name) trong JSON & schema OpenAPI —
        // ổn định cho codegen client, dễ đọc hơn số (docs/backend/api-and-versioning.md §4).
        services.Configure<JsonOptions>(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        // OpenAPI .NET 9 first-party: document "v1" phục vụ /openapi/v1.json và là nguồn xuất
        // shared/contracts/openapi.json (build-time). Single source, không viết tay.
        // Transformer bổ sung: publish enum dùng chung vào components.schemas cho client codegen.
        services.AddOpenApi(
            GameTeam.Contracts.Common.ApiVersions.V1,
            options => options.AddDocumentTransformer<ContractEnumsDocumentTransformer>());

        // TODO (Phase 13+): controllers/minimal API handler thật, authorization policies,
        // SignalR (optional), health checks đầy đủ. Xem docs/backend/api-and-versioning.md.
        return services;
    }
}
