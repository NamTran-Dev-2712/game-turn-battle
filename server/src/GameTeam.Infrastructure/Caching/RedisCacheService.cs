using System.Text.Json;
using GameTeam.Application.Abstractions.Caching;
using GameTeam.Infrastructure.Serialization;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace GameTeam.Infrastructure.Caching;

/// <summary>
/// Hiện thực <see cref="ICacheService"/> trên Redis (StackExchange.Redis) — Phase 12.
/// <para>
/// GRACEFUL DEGRADATION (bắt buộc): Redis KHÔNG được là điểm chết đơn của request. Khi Redis lỗi
/// (kết nối/timeout) hoặc entry hỏng (JSON), service log cảnh báo và <b>degrade</b>: <see cref="GetAsync"/>
/// coi như cache miss (trả <c>null</c> ⇒ caller chạy nguồn thật), <see cref="SetAsync"/>/<see cref="RemoveAsync"/>
/// bỏ qua ghi. KHÔNG ném exception hạ tầng Redis lên người dùng; KHÔNG nuốt lỗi lập trình
/// (<c>ArgumentNullException</c>… vẫn ném). Key theo <see cref="RedisCacheKey"/>; TTL là absolute expiry.
/// Serialize dùng <see cref="CacheSerialization.Options"/> (round-trip được <c>Result&lt;T&gt;</c>).
/// </para>
/// </summary>
public sealed class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _multiplexer;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly string _environmentName;

    public RedisCacheService(
        IConnectionMultiplexer multiplexer,
        ILogger<RedisCacheService> logger,
        string environmentName)
    {
        ArgumentNullException.ThrowIfNull(multiplexer);
        ArgumentNullException.ThrowIfNull(logger);

        _multiplexer = multiplexer;
        _logger = logger;
        _environmentName = string.IsNullOrWhiteSpace(environmentName)
            ? RedisCacheKey.DefaultEnvironment
            : environmentName;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken)
        where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        cancellationToken.ThrowIfCancellationRequested();

        string fullKey = RedisCacheKey.ForCacheEntry(_environmentName, key);

        RedisValue payload;
        try
        {
            payload = await _multiplexer.GetDatabase().StringGetAsync(fullKey).ConfigureAwait(false);
        }
        catch (RedisException ex)
        {
            // Redis không sẵn sàng ⇒ degrade thành cache miss (caller chạy nguồn thật).
            _logger.LogWarning(ex, "Redis GET lỗi cho key {CacheKey}; degrade thành cache miss.", fullKey);
            return null;
        }

        if (payload.IsNullOrEmpty)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(payload.ToString(), CacheSerialization.Options);
        }
        catch (JsonException ex)
        {
            // Entry hỏng/không tương thích ⇒ coi như miss (không surface); config-version key sẽ tự thay thế.
            _logger.LogWarning(ex, "Không deserialize được cache entry key {CacheKey}; coi như miss.", fullKey);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken)
        where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);
        cancellationToken.ThrowIfCancellationRequested();

        string fullKey = RedisCacheKey.ForCacheEntry(_environmentName, key);
        string payload = JsonSerializer.Serialize(value, CacheSerialization.Options);

        try
        {
            await _multiplexer.GetDatabase()
                .StringSetAsync(fullKey, payload, ttl)
                .ConfigureAwait(false);
        }
        catch (RedisException ex)
        {
            // Ghi cache là best-effort ⇒ bỏ qua khi Redis lỗi, request vẫn tiếp tục.
            _logger.LogWarning(ex, "Redis SET lỗi cho key {CacheKey}; bỏ qua ghi cache.", fullKey);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        cancellationToken.ThrowIfCancellationRequested();

        string fullKey = RedisCacheKey.ForCacheEntry(_environmentName, key);

        try
        {
            await _multiplexer.GetDatabase().KeyDeleteAsync(fullKey).ConfigureAwait(false);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis REMOVE lỗi cho key {CacheKey}; bỏ qua.", fullKey);
        }
    }
}
