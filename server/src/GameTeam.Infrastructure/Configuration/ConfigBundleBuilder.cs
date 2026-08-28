using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using GameTeam.ConfigValidator;

namespace GameTeam.Infrastructure.Configuration;

/// <summary>
/// Builds the immutable config bundle (Phase 21, ADR-005) from the Phase-07 loader's entities:
/// groups config by type/id into a <c>data</c> object, computes a <b>deterministic</b> SHA-256
/// checksum over the content, and composes the canonical bundle JSON document (the envelope per
/// <c>shared/config-schema/config-bundle.schema.json</c>).
/// <para>
/// <b>Determinism</b> is essential: the checksum drives publish dedup and integrity, so it must be
/// stable regardless of file discovery order or JSON key order. All serialization here sorts object
/// keys recursively, and the checksum is taken over <c>{ schema_version, data }</c> only — excluding
/// <c>generated_at</c> (wall-clock) and the <c>config_version</c> label (derived from the version).
/// </para>
/// </summary>
public static class ConfigBundleBuilder
{
    /// <summary>Type key used in the bundle <c>data</c> object (e.g. <c>Hero → "hero"</c>).</summary>
    public static string TypeKey(ConfigType type) => type.ToString().ToLowerInvariant();

    /// <summary>
    /// Group validated config entities into <c>{ "&lt;type&gt;": { "&lt;id&gt;": &lt;entry&gt; } }</c>.
    /// All 8 type keys are always present (empty object when a type has no entries) for a stable shape;
    /// entries are deep-cloned so the bundle is detached from the loader's nodes.
    /// </summary>
    public static JsonObject BuildData(IReadOnlyList<ConfigEntity> entities)
    {
        Dictionary<ConfigType, List<ConfigEntity>> byType = entities
            .Where(e => e.Root is not null && e.Id is not null)
            .GroupBy(e => e.Type)
            .ToDictionary(g => g.Key, g => g.ToList());

        JsonObject data = new();
        foreach (ConfigType type in Enum.GetValues<ConfigType>().OrderBy(TypeKey, StringComparer.Ordinal))
        {
            JsonObject entries = new();
            if (byType.TryGetValue(type, out List<ConfigEntity>? typeEntities))
            {
                foreach (ConfigEntity entity in typeEntities.OrderBy(e => e.Id, StringComparer.Ordinal))
                {
                    entries[entity.Id!] = entity.Root!.DeepClone();
                }
            }

            data[TypeKey(type)] = entries;
        }

        return data;
    }

    /// <summary>Deterministic SHA-256 (lower hex) over the canonical form of <c>{ schema_version, data }</c>.</summary>
    public static string ComputeChecksum(int schemaVersion, JsonObject data)
    {
        JsonObject content = new()
        {
            ["schema_version"] = schemaVersion,
            ["data"] = data.DeepClone(),
        };

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(Canonicalize(content)));
        return Convert.ToHexStringLower(hash);
    }

    /// <summary>Compose the full canonical bundle JSON document (envelope + data), served verbatim.</summary>
    public static string ComposeBundleJson(
        int schemaVersion,
        string configVersionLabel,
        string checksum,
        DateTimeOffset generatedAt,
        JsonObject data)
    {
        JsonObject envelope = new()
        {
            ["schema_version"] = schemaVersion,
            ["config_version"] = configVersionLabel,
            ["checksum"] = checksum,
            ["generated_at"] = generatedAt.ToUniversalTime().ToString("O"),
            ["data"] = data.DeepClone(),
        };

        return Canonicalize(envelope);
    }

    /// <summary>Serialize a node with object keys sorted ordinally at every level ⇒ stable byte output.</summary>
    private static string Canonicalize(JsonNode? node)
    {
        StringBuilder sb = new();
        WriteCanonical(node, sb);
        return sb.ToString();
    }

    private static void WriteCanonical(JsonNode? node, StringBuilder sb)
    {
        switch (node)
        {
            case null:
                sb.Append("null");
                break;

            case JsonObject obj:
                sb.Append('{');
                bool firstProp = true;
                foreach (KeyValuePair<string, JsonNode?> property in obj.OrderBy(p => p.Key, StringComparer.Ordinal))
                {
                    if (!firstProp)
                    {
                        sb.Append(',');
                    }

                    firstProp = false;
                    sb.Append(JsonSerializer.Serialize(property.Key));
                    sb.Append(':');
                    WriteCanonical(property.Value, sb);
                }

                sb.Append('}');
                break;

            case JsonArray arr:
                sb.Append('[');
                bool firstItem = true;
                foreach (JsonNode? item in arr)
                {
                    if (!firstItem)
                    {
                        sb.Append(',');
                    }

                    firstItem = false;
                    WriteCanonical(item, sb);
                }

                sb.Append(']');
                break;

            default:
                // Scalar (string/number/bool) — its own JSON representation is already canonical enough
                // (the parsed value round-trips stably for a given input).
                sb.Append(node.ToJsonString());
                break;
        }
    }
}
