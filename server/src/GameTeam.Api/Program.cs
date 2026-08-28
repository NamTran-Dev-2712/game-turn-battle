using Asp.Versioning;
using Asp.Versioning.Builder;
using GameTeam.Api;
using GameTeam.Api.Http;
using GameTeam.Application;
using GameTeam.Application.Abstractions.Configuration;
using GameTeam.Application.Features.Auth.Commands;
using GameTeam.Application.Features.Diagnostics.Commands;
using GameTeam.Application.Features.Diagnostics.Queries;
using GameTeam.Application.Features.Profile.Commands;
using GameTeam.Contracts.Auth;
using GameTeam.Contracts.Common;
using GameTeam.Contracts.Config;
using GameTeam.Contracts.Profile;
using GameTeam.Domain.Common;
using GameTeam.Infrastructure;
using GameTeam.Infrastructure.Configuration;
using MediatR;
using StackExchange.Redis;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Composition root: mỗi tầng tự đăng ký (docs/backend/solution-structure.md §2).
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApi();

WebApplication app = builder.Build();

// Xử lý lỗi tập trung (Phase 13): mọi exception chưa bắt → 500 ErrorEnvelope qua GlobalExceptionHandler
// (không lộ stack/nội bộ; có traceId). Đặt SỚM trong pipeline để bọc toàn bộ request phía sau.
app.UseExceptionHandler();

// OpenAPI (nguồn shared/contracts): phục vụ /openapi/v1.json và là nguồn xuất spec (ADR-008).
// PUBLIC: spec phải đọc được không cần token (client codegen + Swagger UI + drift guard).
app.MapOpenApi().AllowAnonymous();

// Swagger UI CHỈ ở Development: render tài liệu OpenAPI first-party ở trên (KHÔNG dùng SwaggerGen —
// giữ single-source shared/contracts/openapi.json). Prod không expose UI.
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "GameTeam API v1"));
}

// ─────────────────────────────────────────────────────────────────────────────
// AUTHENTICATION / AUTHORIZATION (Phase 18 — JWT bearer, ADR-008). Scheme + authz mặc định đăng ký
// ở AddApi. Bật middleware ở ĐÂY (sau routing/swagger, trước khi map endpoint). Authorization MẶC ĐỊNH
// (FallbackPolicy) ⇒ mọi endpoint yêu cầu token trừ khi opt-out .AllowAnonymous() — public whitelist
// khai báo tường minh bên dưới (health + auth/guest; openapi/swagger đã anonymous ở trên).
// ─────────────────────────────────────────────────────────────────────────────
app.UseAuthentication();
app.UseAuthorization();

// Health endpoint hạ tầng (KHÔNG phải API game, KHÔNG versioned). PUBLIC (liveness). Ping Redis
// (Phase 12): "ok" khi Redis truy cập được, "degraded" khi không — vẫn HTTP 200 (giữ liveness
// semantics; full health checks vẫn ngoài scope). Kiểu hoá bằng HealthResponse; { "status": "..." }.
app.MapGet("/health", async (IConnectionMultiplexer redis) =>
{
    string status = await RedisIsReachableAsync(redis) ? "ok" : "degraded";
    return TypedResults.Ok(new HealthResponse(status));
}).WithName("Health").AllowAnonymous();

// ─────────────────────────────────────────────────────────────────────────────
// API v1 (Phase 13): versioning qua URL segment "/api/v{version:apiVersion}", default v1
// (Asp.Versioning). Đây là convention cho mọi feature endpoint về sau — endpoint nghiệp vụ mới
// (auth Phase 18, config Phase 21…) phải map vào version set này, KHÔNG tự tạo convention khác.
// Toàn bộ stub Phase 05 đã được các phase sở hữu reimplement vào version set (auth 18, profile 19,
// config 21) — không còn group literal "/api/v1".
// ─────────────────────────────────────────────────────────────────────────────
ApiVersionSet versionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1))
    .ReportApiVersions()
    .Build();

RouteGroupBuilder apiV1 = app.MapGroup("/api/v{version:apiVersion}").WithApiVersionSet(versionSet);

// Endpoint mẫu (Phase 13): HTTP → MediatR → Application handler → Result → ErrorEnvelope/200.
// Endpoint MỎNG — không nhét nghiệp vụ; mọi mapping lỗi tập trung ở ApiResults/ErrorHttpMapping.

// GET /api/v1/ping — qua PingCommand (Phase 10 sample). Không truyền ?message ⇒ "ping" (200);
// truyền rỗng "?message=" ⇒ validator fail ⇒ VALIDATION_FAILED ⇒ 400 ErrorEnvelope. Validation chạy
// TRƯỚC TransactionBehavior nên ping sai KHÔNG chạm DB.
apiV1.MapGet("/ping", (string? message, ISender sender, HttpContext httpContext) =>
        ApiResults.ToResponseAsync(sender.Send(new PingCommand(message ?? "ping")), httpContext))
    .WithName("Ping")
    .MapToApiVersion(1)
    .Produces(StatusCodes.Status200OK)
    .Produces<ErrorEnvelope>(StatusCodes.Status400BadRequest);

// GET /api/v1/server-time — qua GetServerTimeQuery (Phase 10 sample) + IClock (chứng minh wiring
// API → MediatR → Application → Infrastructure clock, KHÔNG gọi DateTime.UtcNow ở endpoint).
apiV1.MapGet("/server-time", (ISender sender, HttpContext httpContext) =>
        ApiResults.ToResponseAsync(sender.Send(new GetServerTimeQuery()), httpContext))
    .WithName("ServerTime")
    .MapToApiVersion(1)
    .Produces<ServerTimeResponse>(StatusCodes.Status200OK);
// LƯU Ý: /ping và /server-time KHÔNG .AllowAnonymous() ⇒ theo FallbackPolicy chúng YÊU CẦU token
// (secure-by-default). Đây là các endpoint "nghiệp vụ" mẫu để chứng minh bảo vệ mặc định (Phase 18).

// POST /api/v1/auth/guest (Phase 18): tạo tài khoản khách + cấp JWT. PUBLIC (.AllowAnonymous) — đây là
// điểm vào xác thực, chưa thể có token. HTTP → CreateGuestAccountCommand → Result<AuthGuestResponse>.
// Chuyển từ stub 501 (Phase 05, group literal) vào version set — giải quyết ngay tại đây, path KHÔNG đổi.
apiV1.MapPost("/auth/guest", (AuthGuestRequest request, ISender sender, HttpContext httpContext) =>
        ApiResults.ToResponseAsync(
            sender.Send(new CreateGuestAccountCommand(request.DeviceId)), httpContext))
    .WithName("AuthGuest")
    .MapToApiVersion(1)
    .AllowAnonymous()
    .Produces<AuthGuestResponse>(StatusCodes.Status200OK)
    .Produces<ErrorEnvelope>(StatusCodes.Status400BadRequest);

// GET /api/v1/profile (Phase 19): hồ sơ người chơi của CHÍNH mình — chủ sở hữu lấy từ token sub
// (GetOrCreateProfileCommand → ICurrentUser), KHÔNG nhận owner từ client ⇒ không thể đọc profile người
// khác (chống IDOR). Protected mặc định (KHÔNG .AllowAnonymous). Chuyển từ stub 501 (Phase 05, group
// literal) vào version set — path "/api/v1/profile" KHÔNG đổi.
apiV1.MapGet("/profile", (ISender sender, HttpContext httpContext) =>
        ApiResults.ToResponseAsync(sender.Send(new GetOrCreateProfileCommand()), httpContext))
    .WithName("GetProfile")
    .MapToApiVersion(1)
    .Produces<ProfileDto>(StatusCodes.Status200OK)
    .Produces<ErrorEnvelope>(StatusCodes.Status401Unauthorized);

// ─────────────────────────────────────────────────────────────────────────────
// CONFIGURATION SERVICE (Phase 21, ADR-005): phục vụ bundle config versioned bất biến. PUBLIC
// (.AllowAnonymous) — bundle là nội dung chung, không nhạy cảm, client cache theo version; tách khỏi
// token. Reimplement stub Phase 05 "/config/{version}" thành hai endpoint theo roadmap: bundle theo
// query param + endpoint current riêng.
//
// LƯU Ý: query param đặt tên "bundleVersion" (KHÔNG "version") — tên "version" TRÙNG token
// "{version:apiVersion}" của version set ⇒ ApiExplorer KHÔNG thay thế được, path spec kẹt ở
// "/api/v{version}/...". Đây chính là xung đột Phase 05 đã cảnh báo; đổi tên param giải quyết tại chỗ,
// path sạch "/api/v1/config/bundle".
// ─────────────────────────────────────────────────────────────────────────────

// GET /api/v1/config/current — version config hiện hành (con trỏ "current"). ConfigBundleDto mang
// ConfigVersion (bundle + schema) — giữ contract Phase 05 (KHÔNG đổi ⇒ không drift codegen).
apiV1.MapGet("/config/current", (IConfigProvider configProvider) =>
        Results.Ok(new ConfigBundleDto(configProvider.CurrentVersion)))
    .WithName("GetConfigCurrent")
    .MapToApiVersion(1)
    .AllowAnonymous()
    .Produces<ConfigBundleDto>(StatusCodes.Status200OK);

// GET /api/v1/config/bundle?bundleVersion=N — trả bundle bất biến theo version (thiếu ⇒ current),
// phục vụ NGUYÊN VĂN tài liệu bundle (checksum còn hiệu lực). Không tồn tại ⇒ 404 ErrorEnvelope.
apiV1.MapGet("/config/bundle", async (
        int? bundleVersion,
        IConfigProvider configProvider,
        ConfigBundleStore bundleStore,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
    {
        int requested = bundleVersion ?? configProvider.CurrentVersion.Bundle;
        StoredBundle? bundle = await bundleStore.GetByVersionAsync(requested, cancellationToken);

        return bundle is null
            ? ApiResults.Problem(
                new Error("CONFIG_BUNDLE_NOT_FOUND", $"Config bundle version {requested} not found."),
                httpContext)
            : Results.Content(bundle.Payload, "application/json");
    })
    .WithName("GetConfigBundle")
    .MapToApiVersion(1)
    .AllowAnonymous()
    .Produces<ConfigBundleDto>(StatusCodes.Status200OK)
    .Produces<ErrorEnvelope>(StatusCodes.Status404NotFound);

await app.RunAsync();

// Ping Redis với timeout ngắn — mọi lỗi ⇒ "degraded" (không bao giờ ném từ health probe).
static async Task<bool> RedisIsReachableAsync(IConnectionMultiplexer redis)
{
    try
    {
        await redis.GetDatabase().PingAsync().WaitAsync(TimeSpan.FromSeconds(2));
        return true;
    }
    catch
    {
        return false;
    }
}

// Lộ Program cho integration test (WebApplicationFactory<Program>).
public partial class Program { }
