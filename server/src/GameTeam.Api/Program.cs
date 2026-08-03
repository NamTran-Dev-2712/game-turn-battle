using GameTeam.Api;
using GameTeam.Application;
using GameTeam.Infrastructure;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Composition root: mỗi tầng tự đăng ký (docs/backend/solution-structure.md §2).
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApi();

WebApplication app = builder.Build();

// Health endpoint hạ tầng (KHÔNG phải API game) — xác nhận host chạy & dùng cho
// liveness/readiness. API nghiệp vụ thêm ở phase Core Framework trở đi.
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

await app.RunAsync();

// Lộ Program cho integration test (WebApplicationFactory<Program>).
public partial class Program { }
