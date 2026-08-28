using GameTeam.Domain.Common;

namespace GameTeam.Infrastructure.Persistence;

/// <summary>
/// Durable, <b>immutable</b> row of a published config bundle (Phase 21, ADR-005). One row per version
/// <c>config@vN</c> — rows are never mutated after insert; a config change publishes a NEW row/version.
/// Keeping historical versions is the rollback foundation. This is an infrastructure persistence entity
/// (not a gameplay aggregate); it uses <see cref="Entity{TId}"/> for identity equality only.
/// <para>
/// <see cref="Payload"/> is the <b>verbatim canonical bundle JSON document</b> (the envelope
/// <c>{ schema_version, config_version, checksum, generated_at, data }</c> per
/// <c>shared/config-schema/config-bundle.schema.json</c>) — served byte-for-byte so the
/// <see cref="Checksum"/> stays valid. Checksum is computed over the content only (<c>schema_version</c>
/// + <c>data</c>), excluding <c>generated_at</c>, so redeploying unchanged config is deduplicated.
/// </para>
/// </summary>
public sealed class ConfigBundleRecord : Entity<int>
{
    /// <summary>Bundle version number N (the <c>config@vN</c> ordinal). Also the primary key.</summary>
    public int Version => Id;

    /// <summary>Immutable version label, e.g. <c>config@v1</c>.</summary>
    public string ConfigVersion { get; private set; } = string.Empty;

    /// <summary>Config schema generation the bundle was built against (envelope <c>schema_version</c>).</summary>
    public int SchemaVersion { get; private set; }

    /// <summary>Deterministic SHA-256 (hex) of the bundle content; drives publish dedup + integrity.</summary>
    public string Checksum { get; private set; } = string.Empty;

    /// <summary>Server time the bundle was built (metadata only — not part of the checksum).</summary>
    public DateTimeOffset GeneratedAt { get; private set; }

    /// <summary>The canonical bundle JSON document, served verbatim.</summary>
    public string Payload { get; private set; } = string.Empty;

    private ConfigBundleRecord()
    {
    }

    public ConfigBundleRecord(
        int version,
        string configVersion,
        int schemaVersion,
        string checksum,
        DateTimeOffset generatedAt,
        string payload)
        : base(Guard.Positive(version))
    {
        ConfigVersion = Guard.NotNull(configVersion);
        SchemaVersion = Guard.Positive(schemaVersion);
        Checksum = Guard.NotNull(checksum);
        GeneratedAt = generatedAt;
        Payload = Guard.NotNull(payload);
    }
}
