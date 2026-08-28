using System.Text.Json;
using System.Text.Json.Nodes;
using GameTeam.Application.Abstractions.Configuration;
using GameTeam.Contracts.Config;

namespace GameTeam.Infrastructure.Configuration;

/// <summary>
/// The real <see cref="IConfigProvider"/> (Phase 21) — replaces the Phase-13 <c>DefaultConfigProvider</c>
/// placeholder. Serves the <b>current published immutable bundle</b> from an in-memory
/// <see cref="ConfigSnapshot"/>, swapped atomically by <see cref="ConfigBundlePublisher"/> at boot /
/// publish. Reads are synchronous, lock-free (volatile snapshot reference) and never touch the
/// filesystem, so Domain/Application read config only through this port (ADR-004/005).
/// </summary>
public sealed class RuntimeConfigProvider : IConfigProvider
{
    // Config JSON is snake_case; map into caller POCOs accordingly (harmless for JsonNode/JsonObject targets).
    private static readonly JsonSerializerOptions ReadOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
    };

    private volatile ConfigSnapshot _snapshot = ConfigSnapshot.Empty;

    public ConfigVersion CurrentVersion => _snapshot.Version;

    public T? Get<T>(string type, string id)
        where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        return _snapshot.TryGet(Normalize(type), id, out JsonNode? node) && node is not null
            ? node.Deserialize<T>(ReadOptions)
            : null;
    }

    public IReadOnlyList<string> GetIds(string type)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        return _snapshot.Ids(Normalize(type));
    }

    /// <summary>Atomically swap in a new current snapshot (publisher-only).</summary>
    public void Apply(ConfigSnapshot snapshot) => _snapshot = snapshot;

    private static string Normalize(string type) => type.Trim().ToLowerInvariant();
}
