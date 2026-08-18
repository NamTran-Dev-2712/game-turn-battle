using GameTeam.Application.Abstractions.Caching;
using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Domain.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
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
