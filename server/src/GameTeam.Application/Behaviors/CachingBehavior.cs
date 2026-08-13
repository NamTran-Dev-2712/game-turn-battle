using GameTeam.Application.Abstractions.Caching;
using GameTeam.Application.Abstractions.Configuration;
using GameTeam.Application.Abstractions.Messaging;
using GameTeam.Domain.Common;
using MediatR;

namespace GameTeam.Application.Behaviors;

/// <summary>
/// Caches read queries marked <see cref="ICacheableQuery"/>. On a cache hit the cached result is
/// returned and the handler is NOT invoked; on a miss the handler runs and a successful result is
/// stored with the query's TTL.
/// <para>
/// Cache key = <c>"{RequestTypeName}:{query.CacheKey}:cfg{configBundleVersion}"</c> (query name +
/// parameters + config version) — collision-safe and invalidated by a config rollout. Only
/// successful <see cref="Result"/> responses are cached (failures are not memoized).
/// </para>
/// </summary>
public sealed class CachingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICacheableQuery
    where TResponse : class
{
    private readonly ICacheService _cache;
    private readonly IConfigProvider _configProvider;

    public CachingBehavior(ICacheService cache, IConfigProvider configProvider)
    {
        _cache = cache;
        _configProvider = configProvider;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        string key = BuildKey(request);

        TResponse? cached = await _cache.GetAsync<TResponse>(key, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        TResponse response = await next();

        if (ShouldCache(response))
        {
            await _cache.SetAsync(key, response, request.CacheTtl, cancellationToken);
        }

        return response;
    }

    private string BuildKey(TRequest request)
    {
        int configVersion = _configProvider.CurrentVersion.Bundle;
        return $"{typeof(TRequest).Name}:{request.CacheKey}:cfg{configVersion}";
    }

    /// <summary>Cache only successful results; a non-<see cref="Result"/> response is cached as-is.</summary>
    private static bool ShouldCache(TResponse response) => response switch
    {
        Result result => result.IsSuccess,
        _ => true,
    };
}
