using System.Text.Json.Nodes;
using GameTeam.Contracts.Config;

namespace GameTeam.Infrastructure.Configuration;

/// <summary>
/// An immutable in-memory view of one published bundle: the <see cref="Version"/> plus
/// <c>type → id → entry</c>. <see cref="RuntimeConfigProvider"/> holds one of these and swaps it
/// atomically on publish, so reads are lock-free and never touch the filesystem. Entries are stored
/// as deep-cloned nodes; callers get their own copies via the provider's typed <c>Get&lt;T&gt;</c>.
/// </summary>
public sealed class ConfigSnapshot
{
    /// <summary>The empty snapshot (nothing published yet) — version <c>config@v0</c>, no entries.</summary>
    public static readonly ConfigSnapshot Empty = new(
        new ConfigVersion(0, 0),
        new Dictionary<string, IReadOnlyDictionary<string, JsonNode>>(StringComparer.Ordinal));

    private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, JsonNode>> _data;

    public ConfigSnapshot(ConfigVersion version, IReadOnlyDictionary<string, IReadOnlyDictionary<string, JsonNode>> data)
    {
        Version = version;
        _data = data;
    }

    /// <summary>Current version this snapshot represents.</summary>
    public ConfigVersion Version { get; }

    /// <summary>Look up an entry by type + id.</summary>
    public bool TryGet(string type, string id, out JsonNode? node)
    {
        node = null;
        if (_data.TryGetValue(type, out IReadOnlyDictionary<string, JsonNode>? byId)
            && byId.TryGetValue(id, out JsonNode? found))
        {
            node = found;
            return true;
        }

        return false;
    }

    /// <summary>Ordered ids present for a type (empty when the type is absent).</summary>
    public IReadOnlyList<string> Ids(string type)
        => _data.TryGetValue(type, out IReadOnlyDictionary<string, JsonNode>? byId)
            ? byId.Keys.OrderBy(k => k, StringComparer.Ordinal).ToArray()
            : [];

    /// <summary>Build a snapshot from an already-built data object + version (post-publish path).</summary>
    public static ConfigSnapshot FromData(ConfigVersion version, JsonObject data)
        => new(version, MapData(data));

    /// <summary>Parse a snapshot from a stored bundle payload (the envelope document).</summary>
    public static ConfigSnapshot FromPayload(string payloadJson)
    {
        if (JsonNode.Parse(payloadJson) is not JsonObject envelope)
        {
            return Empty;
        }

        int bundle = ConfigVersionLabel.Number(envelope["config_version"]?.GetValue<string>());
        int schema = envelope["schema_version"]?.GetValue<int>() ?? 0;
        JsonObject data = envelope["data"] as JsonObject ?? new JsonObject();

        return new ConfigSnapshot(new ConfigVersion(bundle, schema), MapData(data));
    }

    private static Dictionary<string, IReadOnlyDictionary<string, JsonNode>> MapData(JsonObject data)
    {
        Dictionary<string, IReadOnlyDictionary<string, JsonNode>> map = new(StringComparer.Ordinal);

        foreach (KeyValuePair<string, JsonNode?> typeEntry in data)
        {
            if (typeEntry.Value is not JsonObject entries)
            {
                continue;
            }

            Dictionary<string, JsonNode> byId = new(StringComparer.Ordinal);
            foreach (KeyValuePair<string, JsonNode?> entry in entries)
            {
                if (entry.Value is not null)
                {
                    byId[entry.Key] = entry.Value.DeepClone();
                }
            }

            map[typeEntry.Key] = byId;
        }

        return map;
    }
}
