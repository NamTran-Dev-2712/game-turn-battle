using System.Text.Json.Serialization;
using Asp.Versioning;
using GameTeam.Api.Http;
using GameTeam.Api.OpenApi;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;

namespace GameTeam.Api;

/// <summary>
/// Composition của tầng Api/Presentation (versioning, error handling, OpenAPI, authz hook…).
/// Phase 05: contract-first — OpenAPI sinh từ GameTeam.Contracts ra shared/contracts (ADR-008).
/// Phase 13: API versioning (/api/v{version:apiVersion}, default v1) + xử lý lỗi tập trung
/// (Result/exception → ErrorEnvelope). Chỉ đăng ký service của tầng Api — KHÔNG nhồi
/// Application/Infrastructure (mỗi tầng có Add&lt;Layer&gt;() riêng, docs/backend/solution-structure.md §2).
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

        // API versioning (Phase 13, ADR-008): version nằm ở URL segment "/api/v{version:apiVersion}",
        // mặc định v1, chấp nhận khi client không nêu version. ApiExplorer + SubstituteApiVersionInUrl
        // ⇒ OpenAPI render path đã resolve ("/api/v1/...") thay vì để lại token {version}.
        services
            .AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            })
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

        // OpenAPI .NET 9 first-party: document "v1" phục vụ /openapi/v1.json và là nguồn xuất
        // shared/contracts/openapi.json (build-time). Single source, không viết tay.
        // Transformer bổ sung: publish enum dùng chung vào components.schemas cho client codegen.
        services.AddOpenApi(
            GameTeam.Contracts.Common.ApiVersions.V1,
            options => options.AddDocumentTransformer<ContractEnumsDocumentTransformer>());

        // Xử lý lỗi tập trung (Phase 13): exception chưa bắt → 500 ErrorEnvelope (không lộ nội bộ).
        // AddProblemDetails() để framework có fallback chuẩn; handler của ta vẫn ghi ErrorEnvelope.
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        // TODO (Phase 18): authentication/authorization THẬT (JWT bearer) — xem Program.cs pipeline
        // hook. Phase 13 KHÔNG đăng ký scheme thật, KHÔNG fake user.
        return services;
    }
}
