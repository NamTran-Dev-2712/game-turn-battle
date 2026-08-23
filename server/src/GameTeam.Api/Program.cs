using Asp.Versioning;
using Asp.Versioning.Builder;
using GameTeam.Api;
using GameTeam.Api.Http;
using GameTeam.Application;
using GameTeam.Application.Features.Auth.Commands;
using GameTeam.Application.Features.Diagnostics.Commands;
using GameTeam.Application.Features.Diagnostics.Queries;
using GameTeam.Contracts.Auth;
using GameTeam.Contracts.Common;
using GameTeam.Contracts.Config;
using GameTeam.Contracts.Profile;
using GameTeam.Infrastructure;
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
//
// LƯU Ý (deviation có chủ đích): các stub Phase 05 giữ nguyên trên group literal "/api/v1" bên dưới,
// KHÔNG chuyển vào version set — vì "/config/{version}" có route param tên "version" TRÙNG với param
// "{version:apiVersion}" của prefix version set (RoutePatternFactory từ chối trùng tên ⇒ vỡ sinh
// OpenAPI). Đổi tên param sẽ phá path contract "/api/v1/config/{version}" (Phase 05). Khi các phase
// sở hữu reimplement (18/21), chúng map vào version set và giải quyết trùng tên tại chỗ.
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

// ─────────────────────────────────────────────────────────────────────────────
// CONTRACT SKELETON (Phase 05): khai báo HÌNH DẠNG endpoint nền cho OpenAPI — KHÔNG hiện thực
// nghiệp vụ (handler thật ở phase sở hữu: config=21; profile=19). Trả 501 Not Implemented. Giữ trên
// group LITERAL "/api/v1" (ApiVersions.V1Prefix) như Phase 05 — path contract KHÔNG đổi. (Xem lưu ý
// deviation ở version set phía trên về xung đột param "{version}".) auth/guest đã reimplement ở trên.
// Các stub này theo FallbackPolicy ⇒ mặc định yêu cầu token; phase sở hữu (19/21) quyết định public/authz.
// ─────────────────────────────────────────────────────────────────────────────
RouteGroupBuilder contractV1 = app.MapGroup(ApiVersions.V1Prefix);

// TODO Phase 19+: real handler — trả hồ sơ người chơi hiện tại.
contractV1.MapGet("/profile", () => Results.StatusCode(StatusCodes.Status501NotImplemented))
    .WithName("GetProfile")
    .Produces<ProfileDto>(StatusCodes.Status200OK)
    .Produces<ErrorEnvelope>(StatusCodes.Status401Unauthorized);

// TODO Phase 21: real handler — trả gói cấu hình theo version.
contractV1.MapGet("/config/{version}", (string version) => Results.StatusCode(StatusCodes.Status501NotImplemented))
    .WithName("GetConfigBundle")
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
