namespace GameTeam.Application.Abstractions.Messaging;

/// <summary>
/// Marker: a read query whose result may be cached. <c>CachingBehavior</c> caches ONLY requests
/// carrying this marker. The behavior builds the full cache key as
/// <c>"{RequestTypeName}:{CacheKey}:cfg{configBundleVersion}"</c> (query name + parameters + config
/// version) to avoid collisions and to invalidate on config rollout.
/// </summary>
public interface ICacheableQuery
{
    /// <summary>Parameter portion of the cache key — must be deterministic for equal inputs.</summary>
    string CacheKey { get; }

    /// <summary>Time-to-live for the cached result.</summary>
    TimeSpan CacheTtl { get; }
}
