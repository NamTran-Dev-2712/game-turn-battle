using GameTeam.Domain.Common;

namespace GameTeam.Infrastructure.Persistence;

/// <summary>
/// Single-row pointer to the <b>current</b> published config version (Phase 21, ADR-005). The publish
/// pipeline flips this pointer <b>last</b>, inside the same transaction as the new bundle insert, so
/// "current" only ever names a fully-persisted bundle — never a half-built one (atomic publish).
/// Mirrors the <see cref="SchemaMetadata"/> singleton pattern (one row, <see cref="SingletonId"/>).
/// </summary>
public sealed class ConfigCurrentPointer : Entity<short>
{
    /// <summary>Singleton key of the single pointer row.</summary>
    public const short SingletonId = 1;

    /// <summary>The version number currently published as "current".</summary>
    public int CurrentVersion { get; private set; }

    private ConfigCurrentPointer()
    {
    }

    public ConfigCurrentPointer(short id, int currentVersion)
        : base(id)
    {
        CurrentVersion = Guard.Positive(currentVersion);
    }

    /// <summary>Point "current" at a newly published version (called inside the publish transaction).</summary>
    public void PointTo(int version) => CurrentVersion = Guard.Positive(version);
}
