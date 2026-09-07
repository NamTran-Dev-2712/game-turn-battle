using GameTeam.Domain.Common;

namespace GameTeam.Domain.Teams;

/// <summary>
/// Đội hình của một người chơi — <b>đội 6 hero + vị trí (formation)</b>, gắn 1-1 với gốc save
/// <see cref="Profiles.PlayerProfile"/> qua <see cref="ProfileId"/> (unique). Server-authoritative (ADR-007):
/// mọi thay đổi qua command server; client chỉ gửi intent. Là lựa chọn tactical duy nhất trong combat
/// full-auto — <see cref="TeamSlot.SlotIndex"/> đi thẳng vào <c>slot</c> của combat sim, ảnh hưởng
/// target/aggro (combat-framework §14, ADR-011).
/// <para>
/// <b>Bất biến cấu trúc (Domain):</b> slotIndex không âm, không trùng ô, không trùng hero, ≥1 slot.
/// Ràng buộc <b>phụ thuộc config</b> (đúng <c>rows*cols</c> ô, slotIndex &lt; số ô, hero thuộc sở hữu)
/// thuộc tầng Application (có <c>IConfigProvider</c>) — Domain không biết kích thước lưới.
/// </para>
/// </summary>
public sealed class Team : AggregateRoot<Guid>
{
    /// <summary>Phiên bản schema của bản ghi (ADR-007). Tăng kèm migration + test khi cấu trúc đổi.</summary>
    public const int CurrentSchemaVersion = 1;

    private readonly List<TeamSlot> _slots = [];

    /// <summary>Ctor không tham số cho hydration/ORM — không dùng trong logic nghiệp vụ.</summary>
    private Team()
    {
    }

    private Team(
        Guid id,
        Guid profileId,
        IEnumerable<TeamSlot> slots,
        int schemaVersion,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
        : base(id)
    {
        ProfileId = profileId;
        _slots = slots.ToList();
        SchemaVersion = schemaVersion;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    /// <summary>Profile sở hữu (khoá ngoại tới <c>player_profiles</c>, unique). Server-controlled.</summary>
    public Guid ProfileId { get; private set; }

    /// <summary>Phiên bản schema của bản ghi này (ADR-007). Server-controlled.</summary>
    public int SchemaVersion { get; private set; } = CurrentSchemaVersion;

    /// <summary>Thời điểm tạo (server-time, từ <see cref="IClock"/>).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Thời điểm lưu gần nhất (server-time).</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Các ô đội hình (chỉ đọc). Thứ tự lưu theo <see cref="TeamSlot.SlotIndex"/>.</summary>
    public IReadOnlyList<TeamSlot> Slots => _slots.AsReadOnly();

    /// <summary>
    /// Tạo đội hình mới cho một profile. <paramref name="id"/> do caller sinh (<c>Guid.NewGuid()</c>).
    /// Guard bất biến cấu trúc (xem <see cref="ValidateSlots"/>). Raise <see cref="TeamSaved"/>.
    /// </summary>
    public static Team Create(Guid id, Guid profileId, IReadOnlyList<TeamSlot> slots, DateTimeOffset nowUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Team id không được rỗng.", nameof(id));
        }

        if (profileId == Guid.Empty)
        {
            throw new ArgumentException("ProfileId không được rỗng.", nameof(profileId));
        }

        ValidateSlots(slots);

        Team team = new(id, profileId, slots, CurrentSchemaVersion, nowUtc, nowUtc);
        team.RaiseDomainEvent(new TeamSaved(id, profileId));
        return team;
    }

    /// <summary>
    /// Thay toàn bộ đội hình (lưu đè). Guard bất biến cấu trúc, cập nhật <see cref="UpdatedAt"/>,
    /// raise <see cref="TeamSaved"/>.
    /// </summary>
    public void Replace(IReadOnlyList<TeamSlot> slots, DateTimeOffset nowUtc)
    {
        ValidateSlots(slots);
        _slots.Clear();
        _slots.AddRange(slots);
        UpdatedAt = nowUtc;
        RaiseDomainEvent(new TeamSaved(Id, ProfileId));
    }

    /// <summary>Dựng lại từ trạng thái đã lưu — KHÔNG raise event (hydration/thử nghiệm).</summary>
    public static Team Restore(
        Guid id,
        Guid profileId,
        IReadOnlyList<TeamSlot> slots,
        int schemaVersion,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
        => new(id, profileId, slots, schemaVersion, createdAt, updatedAt);

    /// <summary>
    /// Bất biến cấu trúc (không phụ thuộc config): ≥1 slot, không trùng <see cref="TeamSlot.SlotIndex"/>,
    /// không trùng <see cref="TeamSlot.HeroId"/>. Vi phạm ⇒ lỗi lập trình (ném) — kiểm nghiệp vụ có thông
    /// điệp thân thiện nằm ở tầng Application trước khi tới đây.
    /// </summary>
    private static void ValidateSlots(IReadOnlyList<TeamSlot> slots)
    {
        Guard.NotNull(slots);
        if (slots.Count == 0)
        {
            throw new ArgumentException("Team phải có ít nhất một slot.", nameof(slots));
        }

        if (slots.Any(s => s is null))
        {
            throw new ArgumentException("Slot không được null.", nameof(slots));
        }

        if (slots.Select(s => s.SlotIndex).Distinct().Count() != slots.Count)
        {
            throw new ArgumentException("SlotIndex bị trùng trong đội hình.", nameof(slots));
        }

        if (slots.Select(s => s.HeroId).Distinct(StringComparer.Ordinal).Count() != slots.Count)
        {
            throw new ArgumentException("Hero bị trùng trong đội hình.", nameof(slots));
        }
    }
}
