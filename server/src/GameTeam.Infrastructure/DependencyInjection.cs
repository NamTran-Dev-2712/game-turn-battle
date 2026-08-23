using System.Text;
using GameTeam.Application.Abstractions.Caching;
using GameTeam.Application.Abstractions.Configuration;
using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Application.Abstractions.Security;
using GameTeam.Domain.Common;
using GameTeam.Infrastructure.Auth;
using GameTeam.Infrastructure.Caching;
using GameTeam.Infrastructure.Configuration;
using GameTeam.Infrastructure.Persistence;
using GameTeam.Infrastructure.Persistence.Repositories;
using GameTeam.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace GameTeam.Infrastructure;

/// <summary>
/// Composition của tầng Infrastructure — hiện thực các port của Application (DIP): EF Core/PostgreSQL
/// persistence (Phase 11) + Redis cache (<see cref="ICacheService"/>, Phase 12) + server-time
/// (<see cref="IClock"/>). JWT/ConfigService/jobs ở phase sau (18/21). Wiring thuần — KHÔNG logic
/// gameplay. Canonical: docs/backend/infrastructure.md.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Khoá connection string PostgreSQL trong configuration (env: <c>ConnectionStrings__Postgres</c>).</summary>
    public const string PostgresConnectionName = "Postgres";

    /// <summary>Khoá connection string Redis trong configuration (env: <c>ConnectionStrings__Redis</c>).</summary>
    public const string RedisConnectionName = "Redis";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // ── Persistence: EF Core + Npgsql (ADR-007) ──────────────────────────────────────────────
        // Connection string LẤY TỪ CONFIG (env ConnectionStrings__Postgres) — không hardcode credential.
        string? connectionString = configuration.GetConnectionString(PostgresConnectionName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Thiếu connection string 'ConnectionStrings:{PostgresConnectionName}' (env ConnectionStrings__Postgres). " +
                "Cấu hình qua appsettings/biến môi trường — xem deploy/compose/docker-compose.yml.");
        }

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

        // Dispatcher domain event (MediatR bridge) do AppDbContext gọi ở SaveChanges.
        services.AddScoped<DomainEventDispatcher>();

        // Ports Phase 10 → hiện thực EF (scoped, cùng vòng đời DbContext/request).
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IRepository<,>), typeof(EfRepository<,>));

        // Repository đặc thù feature: profile (Phase 19) — lookup theo account_id (unique).
        services.AddScoped<IPlayerProfileRepository, PlayerProfileRepository>();

        // ── Cache phân tán: Redis (ADR-003/005, Phase 12) ────────────────────────────────────────
        // Connection LẤY TỪ CONFIG (env ConnectionStrings__Redis) — không hardcode host/port/password.
        string? redisConnectionString = configuration.GetConnectionString(RedisConnectionName);
        if (string.IsNullOrWhiteSpace(redisConnectionString))
        {
            throw new InvalidOperationException(
                $"Thiếu connection string 'ConnectionStrings:{RedisConnectionName}' (env ConnectionStrings__Redis). " +
                "Cấu hình qua appsettings/biến môi trường — xem deploy/compose/docker-compose.yml.");
        }

        // Multiplexer là SINGLETON, thread-safe, tái dùng toàn tiến trình (khuyến nghị StackExchange.Redis).
        // AbortOnConnect=false: boot KHÔNG chặn/ném khi Redis down ⇒ nền tảng cho graceful degradation.
        ConfigurationOptions redisOptions = ConfigurationOptions.Parse(redisConnectionString);
        redisOptions.AbortOnConnectFail = false;
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisOptions));

        // Env cho namespace key {env}:{domain}:{name} — không xác định được thì mặc định "dev".
        string environmentName = configuration["ASPNETCORE_ENVIRONMENT"] ?? RedisCacheKey.DefaultEnvironment;
        services.AddSingleton<ICacheService>(sp => new RedisCacheService(
            sp.GetRequiredService<IConnectionMultiplexer>(),
            sp.GetRequiredService<ILogger<RedisCacheService>>(),
            environmentName));

        // ── Server-time boundary (Domain port IClock) ────────────────────────────────────────────
        services.AddSingleton<IClock, SystemClock>();

        // ── Config version provider (port IConfigProvider) ───────────────────────────────────────
        // Placeholder tối thiểu (config@v1) để composition root ĐỦ cho CachingBehavior — Phase 21
        // (Config Service) THAY THẾ bằng bundle loading/publishing thật. Không đọc balance ở đây.
        services.AddSingleton<IConfigProvider, DefaultConfigProvider>();

        // ── Auth: JWT token service (Phase 18, ADR-008) ──────────────────────────────────────────
        // JwtOptions đọc từ section "Jwt". SigningKey LẤY TỪ SECRET/env (Jwt__SigningKey) — không
        // hardcode/commit. Fail-fast khi thiếu/khoá quá ngắn (giống ConnectionStrings). Đăng ký LAZY
        // (factory) ⇒ không đọc/validate lúc sinh OpenAPI build-time (chưa có key) — chỉ khi resolve
        // thật (token service / JWT scheme). Việc bật scheme JwtBearer + authorization mặc định nằm ở
        // AddApi (tầng Web) — xem docs/backend/api-and-versioning.md.
        services.AddSingleton<IOptions<JwtOptions>>(_ => BuildJwtOptions(configuration));
        services.AddScoped<ITokenService, JwtTokenService>();

        return services;
    }

    /// <summary>
    /// Đọc + kiểm tra cấu hình JWT (fail-fast) rồi gói vào <see cref="IOptions{TOptions}"/>. Đọc tường minh
    /// qua <see cref="IConfiguration"/> (không cần gói Binder trong classlib), giữ đúng convention fail-fast
    /// của tầng Infrastructure.
    /// </summary>
    private static IOptions<JwtOptions> BuildJwtOptions(IConfiguration configuration)
    {
        IConfigurationSection section = configuration.GetSection(JwtOptions.SectionName);

        string issuer = section["Issuer"] ?? string.Empty;
        string audience = section["Audience"] ?? string.Empty;
        string signingKey = section["SigningKey"] ?? string.Empty;
        string accessTokenMinutesRaw = section["AccessTokenMinutes"] ?? string.Empty;

        if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience))
        {
            throw new InvalidOperationException(
                $"Thiếu cấu hình '{JwtOptions.SectionName}:Issuer'/'{JwtOptions.SectionName}:Audience'. " +
                "Cấu hình qua appsettings/biến môi trường.");
        }

        if (string.IsNullOrWhiteSpace(signingKey))
        {
            throw new InvalidOperationException(
                $"Thiếu '{JwtOptions.SectionName}:SigningKey' (env {JwtOptions.SectionName}__SigningKey). " +
                "Khoá ký JWT phải lấy từ secret/biến môi trường — KHÔNG hardcode/commit.");
        }

        if (Encoding.UTF8.GetByteCount(signingKey) < JwtOptions.MinSigningKeyLength)
        {
            throw new InvalidOperationException(
                $"'{JwtOptions.SectionName}:SigningKey' quá ngắn: cần tối thiểu {JwtOptions.MinSigningKeyLength} byte (256-bit) cho HS256.");
        }

        if (!int.TryParse(accessTokenMinutesRaw, out int accessTokenMinutes) || accessTokenMinutes <= 0)
        {
            throw new InvalidOperationException(
                $"'{JwtOptions.SectionName}:AccessTokenMinutes' phải là số nguyên dương.");
        }

        return Options.Create(new JwtOptions
        {
            Issuer = issuer,
            Audience = audience,
            SigningKey = signingKey,
            AccessTokenMinutes = accessTokenMinutes,
        });
    }
}
