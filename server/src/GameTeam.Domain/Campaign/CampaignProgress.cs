using GameTeam.Domain.Common;

namespace GameTeam.Domain.Campaign;

/// <summary>
/// Tiến độ campaign PvE của một người chơi — gắn 1-1 với gốc save <see cref="Profiles.PlayerProfile"/> qua
/// <see cref="ProfileId"/> (unique). Server-authoritative (ADR-007): tiến độ + "current AFK stage" do server
/// quyết, client chỉ hiển thị. Lưu tập stage đã clear (<see cref="ClearedStages"/>) để bảo đảm thưởng
/// <b>first-clear only</b> (clear lại không cấp lại, không lùi tiến độ) và suy ra tiến độ.
/// <para>
/// <b>Current AFK stage</b> (<see cref="CurrentAfkStageId"/>) = stage tiến độ hiện hành — được đọc bởi AFK
/// (phase 37) để tính rate. Thứ tự stage (stage nào "xa nhất") <b>phụ thuộc config chapter chain</b> nên
/// Domain KHÔNG tự suy — tầng Application tính rồi truyền vào <see cref="MarkStageCleared"/> (cùng nguyên
/// tắc tách Domain/Application như <see cref="Teams.Team"/>). Phase 34 KHÔNG hiện thực AFK accrual.
/// </para>
/// </summary>
public sealed class CampaignProgress : AggregateRoot<Guid>
{
    /// <summary>Phiên bản schema của bản ghi (ADR-007). Tăng kèm migration + test khi cấu trúc đổi.</summary>
    public const int CurrentSchemaVersion = 1;

    private readonly List<ClearedStage> _clearedStages = [];

    /// <summary>Ctor không tham số cho hydration/ORM — không dùng trong logic nghiệp vụ.</summary>
    private CampaignProgress()
    {
    }

    private CampaignProgress(
        Guid id,
        Guid profileId,
        IEnumerable<ClearedStage> clearedStages,
        string currentAfkStageId,
        int schemaVersion,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
        : base(id)
    {
        ProfileId = profileId;
        _clearedStages = clearedStages.ToList();
        CurrentAfkStageId = currentAfkStageId;
        SchemaVersion = schemaVersion;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    /// <summary>Profile sở hữu (khoá ngoại tới <c>player_profiles</c>, unique). Server-controlled.</summary>
    public Guid ProfileId { get; private set; }

    /// <summary>
    /// Stage AFK hiện hành = stage tiến độ (cho phase 37). Rỗng khi chưa clear stage nào. Server-controlled;
    /// giá trị do Application tính theo chapter chain rồi truyền vào <see cref="MarkStageCleared"/>.
    /// </summary>
    public string CurrentAfkStageId { get; private set; } = string.Empty;

    /// <summary>Phiên bản schema của bản ghi này (ADR-007). Server-controlled.</summary>
    public int SchemaVersion { get; private set; } = CurrentSchemaVersion;

    /// <summary>Thời điểm tạo (server-time, từ <see cref="IClock"/>).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Thời điểm cập nhật gần nhất (server-time).</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Các stage đã clear (chỉ đọc). Thứ tự chơi/tiến độ do chapter chain (Application) quyết.</summary>
    public IReadOnlyList<ClearedStage> ClearedStages => _clearedStages.AsReadOnly();

    /// <summary>
    /// Tạo tiến độ campaign rỗng cho một profile. <paramref name="id"/> do caller sinh (<c>Guid.NewGuid()</c>).
    /// Raise <see cref="CampaignProgressCreated"/>.
    /// </summary>
    public static CampaignProgress Create(Guid id, Guid profileId, DateTimeOffset nowUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("CampaignProgress id không được rỗng.", nameof(id));
        }

        if (profileId == Guid.Empty)
        {
            throw new ArgumentException("ProfileId không được rỗng.", nameof(profileId));
        }

        CampaignProgress progress = new(id, profileId, [], string.Empty, CurrentSchemaVersion, nowUtc, nowUtc);
        progress.RaiseDomainEvent(new CampaignProgressCreated(id, profileId));
        return progress;
    }

    /// <summary>True nếu <paramref name="stageId"/> đã clear (dùng chặn cấp thưởng trùng — first-clear only).</summary>
    public bool IsStageCleared(string stageId) =>
        _clearedStages.Any(s => string.Equals(s.StageId, stageId, StringComparison.Ordinal));

    /// <summary>
    /// Đánh dấu <paramref name="stageId"/> đã clear LẦN ĐẦU. Idempotent: đã clear ⇒ no-op, trả <c>false</c>
    /// (không cấp lại thưởng, không lùi tiến độ). Nếu là lần đầu: thêm vào tập cleared, đặt
    /// <see cref="CurrentAfkStageId"/> = <paramref name="currentAfkStageId"/> (Application tính = stage xa nhất
    /// theo chapter chain), cập nhật <see cref="UpdatedAt"/>, raise <see cref="CampaignStageCleared"/>, trả <c>true</c>.
    /// </summary>
    public bool MarkStageCleared(string stageId, string currentAfkStageId, DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(stageId))
        {
            throw new ArgumentException("StageId không được rỗng.", nameof(stageId));
        }

        if (string.IsNullOrWhiteSpace(currentAfkStageId))
        {
            throw new ArgumentException("CurrentAfkStageId không được rỗng khi clear.", nameof(currentAfkStageId));
        }

        if (IsStageCleared(stageId))
        {
            return false;
        }

        _clearedStages.Add(new ClearedStage(stageId, nowUtc));
        CurrentAfkStageId = currentAfkStageId;
        UpdatedAt = nowUtc;
        RaiseDomainEvent(new CampaignStageCleared(Id, ProfileId, stageId));
        return true;
    }

    /// <summary>Dựng lại từ trạng thái đã lưu — KHÔNG raise event (hydration/thử nghiệm).</summary>
    public static CampaignProgress Restore(
        Guid id,
        Guid profileId,
        IReadOnlyList<ClearedStage> clearedStages,
        string currentAfkStageId,
        int schemaVersion,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
        => new(id, profileId, clearedStages, currentAfkStageId, schemaVersion, createdAt, updatedAt);
}
