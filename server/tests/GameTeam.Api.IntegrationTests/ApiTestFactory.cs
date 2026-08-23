using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GameTeam.Application.Abstractions.Caching;
using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Domain.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace GameTeam.Api.IntegrationTests;

/// <summary>
/// Boots the real API host (versioning + MediatR + error pipeline) but swaps the Infrastructure ports
/// that would otherwise require live Postgres/Redis, so the Phase-13 sample endpoints run
/// deterministically in CI without external services:
/// <list type="bullet">
///   <item><see cref="IUnitOfWork"/> → no-op (PingCommand is transactional; must not hit Postgres).</item>
///   <item><see cref="ICacheService"/> → no-op (GetServerTimeQuery is cacheable; force a cache miss).</item>
///   <item><see cref="IClock"/> → <see cref="FixedClock"/> so server-time is assertable.</item>
/// </list>
/// This mirrors the established override pattern in <c>HealthEndpointTests</c> (WithWebHostBuilder).
/// </summary>
public class ApiTestFactory : WebApplicationFactory<Program>
{
    /// <summary>Deterministic server time returned by the fake clock.</summary>
    public static readonly DateTimeOffset FixedNow = new(2026, 8, 14, 12, 0, 0, TimeSpan.Zero);

    // ── JWT test configuration (Phase 18) ────────────────────────────────────────────────────────
    // A test-only signing key (≥ 256-bit, obviously non-production) injected via config, plus issuer/
    // audience. The JwtBearer scheme AND the guest-login token service both read these, and the token
    // helper below signs with the same values, so a helper-minted token validates end-to-end.
    /// <summary>Test-only HS256 signing key — NOT a production secret.</summary>
    public const string JwtSigningKey = "integration-test-signing-key-do-not-use-in-production-0123456789";

    /// <summary>Test token issuer.</summary>
    public const string JwtIssuer = "gameteam-api-test";

    /// <summary>Test token audience.</summary>
    public const string JwtAudience = "gameteam-client-test";

    /// <summary>Test access-token lifetime (minutes).</summary>
    public const int JwtAccessTokenMinutes = 60;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // JWT config (signing key from "secret" — here a test value via config, never committed as a real key).
        builder.UseSetting("Jwt:SigningKey", JwtSigningKey);
        builder.UseSetting("Jwt:Issuer", JwtIssuer);
        builder.UseSetting("Jwt:Audience", JwtAudience);
        builder.UseSetting("Jwt:AccessTokenMinutes", JwtAccessTokenMinutes.ToString());

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IUnitOfWork>();
            services.AddScoped<IUnitOfWork, NoOpUnitOfWork>();

            services.RemoveAll<ICacheService>();
            services.AddSingleton<ICacheService, NoOpCacheService>();

            services.RemoveAll<IClock>();
            services.AddSingleton<IClock>(new FixedClock(FixedNow));
        });
    }

    /// <summary>
    /// Mint a valid guest bearer token signed with the test key, at REAL wall-clock time (so it passes
    /// the JwtBearer lifetime check regardless of the deterministic <see cref="FixedClock"/> the server
    /// uses for business time). Used by tests that hit now-protected endpoints.
    /// </summary>
    /// <param name="accountId">Subject (defaults to a fresh guid).</param>
    /// <param name="lifetime">Access-token lifetime (defaults to <see cref="JwtAccessTokenMinutes"/>);
    /// pass a negative span to mint an already-expired token.</param>
    public static string CreateGuestBearerToken(Guid? accountId = null, TimeSpan? lifetime = null)
    {
        DateTime now = DateTime.UtcNow;
        TimeSpan life = lifetime ?? TimeSpan.FromMinutes(JwtAccessTokenMinutes);
        DateTime expires = now.Add(life);
        // Keep notBefore < expires even for an already-expired token (negative lifetime).
        DateTime notBefore = life < TimeSpan.Zero ? expires.AddMinutes(-5) : now;

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSigningKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: JwtIssuer,
            audience: JwtAudience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, (accountId ?? Guid.NewGuid()).ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("type", "guest"),
            ],
            notBefore: notBefore,
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>An <see cref="HttpClient"/> with a valid guest bearer token attached (authenticated).</summary>
    public HttpClient CreateAuthenticatedClient()
    {
        HttpClient client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", CreateGuestBearerToken());
        return client;
    }
}

/// <summary>No-op unit of work — transactional commands succeed without a real database.</summary>
public sealed class NoOpUnitOfWork : IUnitOfWork
{
    public Task BeginTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}

/// <summary>No-op cache — every read is a miss, so cacheable queries always run the handler.</summary>
public sealed class NoOpCacheService : ICacheService
{
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
        => Task.FromResult<T?>(null);

    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default) where T : class
        => Task.CompletedTask;

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

/// <summary>Deterministic clock for asserting server-time.</summary>
public sealed class FixedClock(DateTimeOffset now) : IClock
{
    public DateTimeOffset UtcNow { get; } = now;
}

/// <summary>Clock that throws — used to exercise the unhandled-exception → 500 path.</summary>
public sealed class ThrowingClock : IClock
{
    /// <summary>Sentinel that MUST NOT appear in any client-facing 500 body.</summary>
    public const string SecretDetail = "clock-internal-secret-detail";

    public DateTimeOffset UtcNow => throw new InvalidOperationException(SecretDetail);
}
