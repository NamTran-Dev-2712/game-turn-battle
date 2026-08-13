namespace GameTeam.Application.Abstractions.Caching;

/// <summary>
/// Cache store port (declared in Application, implemented in Infrastructure — DIP). Used by
/// <c>CachingBehavior</c> to read/write cached query results by key + TTL. Infrastructure-agnostic:
/// no Redis/StackExchange type leaks here — Phase 12 implements it (distributed cache).
/// </summary>
public interface ICacheService
{
    /// <summary>Read a cached value, or <c>null</c> on a cache miss.</summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken)
        where T : class;

    /// <summary>Write a value with an absolute time-to-live.</summary>
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken)
        where T : class;
}
