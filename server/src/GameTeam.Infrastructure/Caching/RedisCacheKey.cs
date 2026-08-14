namespace GameTeam.Infrastructure.Caching;

/// <summary>
/// Chuẩn hoá key cache theo quy ước <c>{env}:{domain}:{name}:{configVersion?}</c> — tập trung một chỗ
/// để tránh mỗi caller tự nối chuỗi khác nhau (chống collision, namespace rõ). <c>CachingBehavior</c>
/// (Phase 10) đã gấp <c>cfg{configVersion}</c> vào phần <c>name</c>; service chỉ thêm tiền tố
/// <c>{env}:{domain}</c>. Domain mặc định cho cache query là <c>cache</c>. Xem docs/backend/infrastructure.md.
/// </summary>
public static class RedisCacheKey
{
    /// <summary>Domain namespace cho các entry cache đọc-query (CachingBehavior).</summary>
    public const string CacheDomain = "cache";

    /// <summary>Môi trường mặc định khi không xác định được (local/test).</summary>
    public const string DefaultEnvironment = "dev";

    /// <summary>
    /// Ghép key đầy đủ: <c>{env}:{domain}:{rawKey}</c>. <paramref name="rawKey"/> đã chứa
    /// <c>{name}[:cfg{version}]</c> do CachingBehavior dựng. Env được chuẩn hoá lowercase/trim.
    /// </summary>
    public static string Compose(string environmentName, string domain, string rawKey)
        => $"{Normalize(environmentName, DefaultEnvironment)}:{Normalize(domain, CacheDomain)}:{rawKey}";

    /// <summary>Ghép key cho cache query (domain = <see cref="CacheDomain"/>).</summary>
    public static string ForCacheEntry(string environmentName, string rawKey)
        => Compose(environmentName, CacheDomain, rawKey);

    private static string Normalize(string? segment, string fallback)
    {
        if (string.IsNullOrWhiteSpace(segment))
        {
            return fallback;
        }

        return segment.Trim().ToLowerInvariant();
    }
}
