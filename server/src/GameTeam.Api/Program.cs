using Asp.Versioning;
using Asp.Versioning.Builder;
using GameTeam.Api;
using GameTeam.Api.Http;
using GameTeam.Application;
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
app.MapOpenApi();

// Swagger UI CHỈ ở Development: render tài liệu OpenAPI first-party ở trên (KHÔNG dùng SwaggerGen —
// giữ single-source shared/contracts/openapi.json). Prod không expose UI.
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "GameTeam API v1"));
}

// ─────────────────────────────────────────────────────────────────────────────
// AUTHENTICATION / AUTHORIZATION HOOK — Phase 18 (JWT bearer).
// Phase 13 CHỪA CHỖ, KHÔNG bật auth thật, KHÔNG fake user. Khi Phase 18 tới, bật đúng vị trí này
// (sau routing, trước khi map endpoint) + AddAuthentication/AddAuthorization trong AddApi:
//   TODO Phase 18: app.UseAuthentication();
//   TODO Phase 18: app.UseAuthorization();
// ─────────────────────────────────────────────────────────────────────────────

// Health endpoint hạ tầng (KHÔNG phải API game, KHÔNG versioned). Ping Redis (Phase 12): "ok" khi
// Redis truy cập được, "degraded" khi không — vẫn HTTP 200 (giữ liveness semantics; full health
// checks vẫn ngoài scope). Kiểu hoá bằng HealthResponse để mô tả trong OpenAPI; { "status": "..." }.
app.MapGet("/health", async (IConnectionMultiplexer redis) =>
{
    string status = await RedisIsReachableAsync(redis) ? "ok" : "degraded";
    return TypedResults.Ok(new HealthResponse(status));
}).WithName("Health");

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

// ─────────────────────────────────────────────────────────────────────────────
// CONTRACT SKELETON (Phase 05): khai báo HÌNH DẠNG endpoint nền cho OpenAPI — KHÔNG hiện thực
// nghiệp vụ (handler thật ở phase sở hữu: auth=18, config=21). Trả 501 Not Implemented. Giữ trên
// group LITERAL "/api/v1" (ApiVersions.V1Prefix) như Phase 05 — path contract KHÔNG đổi. (Xem lưu ý
// deviation ở version set phía trên về xung đột param "{version}".)
// ─────────────────────────────────────────────────────────────────────────────
RouteGroupBuilder contractV1 = app.MapGroup(ApiVersions.V1Prefix);

// TODO Phase 18: real handler — cấp JWT guest.
contractV1.MapPost("/auth/guest", (AuthGuestRequest request) => Results.StatusCode(StatusCodes.Status501NotImplemented))
    .WithName("AuthGuest")
    .Produces<AuthGuestResponse>(StatusCodes.Status200OK)
    .Produces<ErrorEnvelope>(StatusCodes.Status400BadRequest);

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
