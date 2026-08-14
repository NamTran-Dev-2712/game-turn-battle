using GameTeam.Domain.Common;

namespace GameTeam.Infrastructure.Persistence;

/// <summary>
/// Neo <b>schema version</b> của persistence (ADR-007: "EF Core migrations + version field"). Một bảng
/// <c>schema_metadata</c> đơn hàng, seed <c>version = 1</c> ở migration khởi tạo — mốc để dữ liệu người chơi
/// (profile @19) migrate có kiểm soát về sau. Đây là entity hạ tầng nội bộ (không phải aggregate nghiệp vụ),
/// dùng định danh <see cref="Entity{TId}"/> để tái dùng equality/hydration đã chuẩn hoá ở Phase 09.
/// </summary>
public sealed class SchemaMetadata : Entity<short>
{
    /// <summary>Khoá singleton của hàng metadata duy nhất.</summary>
    public const short SingletonId = 1;

    /// <summary>Version schema persistence hiện tại.</summary>
    public int Version { get; private set; }

    private SchemaMetadata()
    {
    }

    public SchemaMetadata(short id, int version)
        : base(id)
    {
        Version = Guard.Positive(version);
    }
}
