using GameTeam.Api;
using GameTeam.Application;
using GameTeam.Contracts.Auth;
using GameTeam.Contracts.Common;
using GameTeam.Contracts.Config;
using GameTeam.Contracts.Profile;
using GameTeam.Infrastructure;
using StackExchange.Redis;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Composition root: mỗi tầng tự đăng ký (docs/backend/solution-structure.md §2).
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApi();

WebApplication app = builder.Build();

// OpenAPI (nguồn shared/contracts): phục vụ /openapi/v1.json và là nguồn xuất spec (ADR-008).
app.MapOpenApi();

// Health endpoint hạ tầng (KHÔNG phải API game). Ping Redis (Phase 12): "ok" khi Redis truy cập được,
// "degraded" khi không — vẫn HTTP 200 (giữ nguyên liveness semantics; full health checks = Phase 13+).
// Kiểu hoá bằng HealthResponse để mô tả được trong OpenAPI; hình dạng { "status": "..." } không đổi.
app.MapGet("/health", async (IConnectionMultiplexer redis) =>
{
    string status = await RedisIsReachableAsync(redis) ? "ok" : "degraded";
    return TypedResults.Ok(new HealthResponse(status));
}).WithName("Health");

// ─────────────────────────────────────────────────────────────────────────────
// CONTRACT SKELETON (Phase 05): khai báo HÌNH DẠNG endpoint nền cho OpenAPI —
// KHÔNG hiện thực nghiệp vụ. Tất cả trả 501 Not Implemented; handler thật ở Phase 13.
// ─────────────────────────────────────────────────────────────────────────────
RouteGroupBuilder apiV1 = app.MapGroup(ApiVersions.V1Prefix);

// TODO Phase 13: real handler — cấp JWT guest.
apiV1.MapPost("/auth/guest", (AuthGuestRequest request) => Results.StatusCode(StatusCodes.Status501NotImplemented))
    .WithName("AuthGuest")
    .Produces<AuthGuestResponse>(StatusCodes.Status200OK)
    .Produces<ErrorEnvelope>(StatusCodes.Status400BadRequest);

// TODO Phase 13: real handler — trả hồ sơ người chơi hiện tại.
apiV1.MapGet("/profile", () => Results.StatusCode(StatusCodes.Status501NotImplemented))
    .WithName("GetProfile")
    .Produces<ProfileDto>(StatusCodes.Status200OK)
    .Produces<ErrorEnvelope>(StatusCodes.Status401Unauthorized);

// TODO Phase 13: real handler — trả gói cấu hình theo version.
apiV1.MapGet("/config/{version}", (string version) => Results.StatusCode(StatusCodes.Status501NotImplemented))
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
