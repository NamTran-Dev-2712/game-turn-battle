using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GameTeam.Infrastructure.Persistence;

/// <summary>
/// Factory <b>chỉ dùng lúc thiết kế</b> (design-time) cho tooling <c>dotnet ef</c> (migrations add / database update),
/// khi không có host DI để cấp <see cref="AppDbContext"/>. KHÔNG dùng ở runtime (runtime lấy DbContext qua
/// <c>AddInfrastructure</c> + connection string từ <see cref="Microsoft.Extensions.Configuration.IConfiguration"/>).
/// <para>
/// Connection string ưu tiên biến môi trường <c>ConnectionStrings__Postgres</c>; nếu thiếu, fallback về mặc định
/// dev-compose (khớp <c>deploy/compose/docker-compose.yml</c>) — chỉ để tooling dev chạy, không phải secret runtime.
/// </para>
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    // Mặc định dev-only (khớp deploy/compose/docker-compose.yml). Chỉ phục vụ tooling migration cục bộ;
    // runtime luôn đọc connection string từ configuration (không hardcode credential ở runtime).
    private const string DevFallbackConnectionString =
        "Host=localhost;Port=5432;Database=gameteam;Username=gameteam;Password=devpassword";

    public AppDbContext CreateDbContext(string[] args)
    {
        string? connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = DevFallbackConnectionString;
        }

        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        // Dispatcher design-time không bao giờ được gọi (tooling không SaveChanges nghiệp vụ) — dùng no-op publisher.
        return new AppDbContext(options, new DomainEventDispatcher(new NoOpPublisher()));
    }

    /// <summary>Publisher rỗng cho design-time: tooling migration không dispatch domain event.</summary>
    private sealed class NoOpPublisher : IPublisher
    {
        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification => Task.CompletedTask;
    }
}
