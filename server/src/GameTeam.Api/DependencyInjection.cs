using System.Text;
using System.Text.Json.Serialization;
using Asp.Versioning;
using GameTeam.Api.Auth;
using GameTeam.Api.Http;
using GameTeam.Api.OpenApi;
using GameTeam.Application.Abstractions.Security;
using GameTeam.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

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
            options =>
            {
                // Enum được DTO tham chiếu: làm giàu metadata (schema transformer).
                options.AddSchemaTransformer<ContractEnumSchemaTransformer>();
                // Enum chưa DTO nào tham chiếu: force-publish (document transformer).
                options.AddDocumentTransformer<ContractEnumsDocumentTransformer>();
            });

        // Xử lý lỗi tập trung (Phase 13): exception chưa bắt → 500 ErrorEnvelope (không lộ nội bộ).
        // AddProblemDetails() để framework có fallback chuẩn; handler của ta vẫn ghi ErrorEnvelope.
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        // ── Authentication / Authorization (Phase 18, ADR-008) ───────────────────────────────────
        // JWT bearer: validate CHỮ KÝ + issuer + audience + lifetime. Khoá/issuer/audience lấy từ
        // IOptions<JwtOptions> (Infrastructure đọc từ config/secret, fail-fast). 401/403 ghi ĐÚNG
        // ErrorEnvelope (AuthProblem) — cùng contract lỗi với phần còn lại.
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwt) =>
            {
                JwtOptions options = jwt.Value;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = options.Issuer,
                    ValidateAudience = true,
                    ValidAudience = options.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                };
                bearer.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        // Chặn body 401 rỗng mặc định — ghi ErrorEnvelope chuẩn.
                        context.HandleResponse();
                        await AuthProblem.WriteUnauthenticatedAsync(context.HttpContext);
                    },
                    OnForbidden = context => AuthProblem.WriteForbiddenAsync(context.HttpContext),
                };
            });

        // Authorization MẶC ĐỊNH: mọi endpoint yêu cầu authenticated user trừ khi opt-out .AllowAnonymous().
        // ⇒ API nghiệp vụ tạo về sau được bảo vệ mặc định (secure-by-default). Public whitelist khai báo
        // tường minh ở Program.cs (health/auth/swagger/openapi).
        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        // Current-user accessor (Phase 19): resolve the authenticated owner (JWT sub) from the request.
        // HttpContext-based ⇒ lives in the Api layer; Application depends only on the ICurrentUser port.
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        return services;
    }
}
