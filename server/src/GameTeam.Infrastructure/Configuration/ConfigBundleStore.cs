using GameTeam.Application.Abstractions.Caching;
using GameTeam.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GameTeam.Infrastructure.Configuration;

/// <summary>A published bundle read back from storage (metadata + verbatim payload).</summary>
public sealed record StoredBundle(int Version, string ConfigVersion, int SchemaVersion, string Checksum, string Payload);

/// <summary>Cacheable projection of <see cref="StoredBundle"/> (a class for <see cref="ICacheService"/>).</summary>
public sealed class CachedBundle
{
    public int Version { get; set; }

    public string ConfigVersion { get; set; } = string.Empty;

    public int SchemaVersion { get; set; }

    public string Checksum { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;

    public static CachedBundle From(StoredBundle bundle) => new()
    {
        Version = bundle.Version,
        ConfigVersion = bundle.ConfigVersion,
        SchemaVersion = bundle.SchemaVersion,
        Checksum = bundle.Checksum,
        Payload = bundle.Payload,
    };

    public StoredBundle ToStored() => new(Version, ConfigVersion, SchemaVersion, Checksum, Payload);
}

/// <summary>
/// Durable + cached storage for published config bundles (Phase 21). Bundles persist immutably in
/// <c>config_bundles</c>; the current version is named by the <c>config_current</c> pointer. Reads go
/// through Redis first (immutable <c>config@vN</c> key — reuses the Phase-12 <see cref="ICacheService"/>,
/// which degrades gracefully) then fall back to the DB (re-warming the cache). Writes persist the new
/// bundle and flip "current" in ONE transaction (atomic publish), then warm the cache after commit.
/// </summary>
public sealed class ConfigBundleStore
{
    // Bundle is immutable per version ⇒ a long absolute TTL is safe; the version-keyed entry never goes stale.
    private static readonly TimeSpan BundleCacheTtl = TimeSpan.FromDays(30);

    private readonly AppDbContext _db;
    private readonly ICacheService _cache;

    public ConfigBundleStore(AppDbContext db, ICacheService cache)
    {
        _db = db;
        _cache = cache;
    }

    /// <summary>The bundle the "current" pointer names, or <c>null</c> when nothing is published yet.</summary>
    public async Task<StoredBundle?> GetCurrentAsync(CancellationToken cancellationToken)
    {
        ConfigCurrentPointer? pointer = await _db.ConfigCurrent
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == ConfigCurrentPointer.SingletonId, cancellationToken);

        return pointer is null ? null : await GetByVersionAsync(pointer.CurrentVersion, cancellationToken);
    }

    /// <summary>A specific version (Redis → DB fallback → re-warm), or <c>null</c> if it does not exist.</summary>
    public async Task<StoredBundle?> GetByVersionAsync(int version, CancellationToken cancellationToken)
    {
        if (version <= 0)
        {
            return null;
        }

        string cacheKey = CacheKey(ConfigVersionLabel.For(version));

        CachedBundle? cached = await _cache.GetAsync<CachedBundle>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached.ToStored();
        }

        ConfigBundleRecord? record = await _db.ConfigBundles
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == version, cancellationToken);

        if (record is null)
        {
            return null;
        }

        StoredBundle stored = ToStored(record);
        await _cache.SetAsync(cacheKey, CachedBundle.From(stored), BundleCacheTtl, cancellationToken);
        return stored;
    }

    /// <summary>
    /// Persist a new immutable bundle and flip "current" to it atomically (single transaction), then
    /// warm the version-keyed cache. On any persistence failure the transaction rolls back and "current"
    /// keeps pointing at the previous version.
    /// </summary>
    public async Task SaveAndPublishAsync(ConfigBundleRecord record, CancellationToken cancellationToken)
    {
        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction =
            await _db.Database.BeginTransactionAsync(cancellationToken);

        _db.ConfigBundles.Add(record);

        ConfigCurrentPointer? pointer = await _db.ConfigCurrent
            .FirstOrDefaultAsync(p => p.Id == ConfigCurrentPointer.SingletonId, cancellationToken);

        if (pointer is null)
        {
            _db.ConfigCurrent.Add(new ConfigCurrentPointer(ConfigCurrentPointer.SingletonId, record.Version));
        }
        else
        {
            pointer.PointTo(record.Version);
        }

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        // Warm the cache AFTER the durable commit (best-effort — the cache degrades gracefully).
        await _cache.SetAsync(CacheKey(record.ConfigVersion), CachedBundle.From(ToStored(record)), BundleCacheTtl, cancellationToken);
    }

    private static StoredBundle ToStored(ConfigBundleRecord record)
        => new(record.Version, record.ConfigVersion, record.SchemaVersion, record.Checksum, record.Payload);

    private static string CacheKey(string label) => $"config-bundle:{label}";
}
