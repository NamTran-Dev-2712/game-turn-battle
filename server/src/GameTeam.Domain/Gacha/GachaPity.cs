using GameTeam.Domain.Common;

namespace GameTeam.Domain.Gacha;

/// <summary>
/// Bộ đếm pity <b>server-side</b> theo (<see cref="ProfileId"/>, <see cref="BannerId"/>) — ADR-007/011: pity là
/// trạng thái server-authoritative, KHÔNG lưu/nhận từ client. (<c>profile_id</c>, <c>banner_id</c>) là duy nhất
/// (unique index). <see cref="Count"/> = số lần quay liên tiếp chưa trúng rarity mục tiêu; <see cref="Increment"/>
/// khi không trúng, <see cref="Reset"/> khi trúng (tự nhiên hoặc do pity). Ngưỡng/mục tiêu đọc từ config (data-driven).
/// </summary>
public sealed class GachaPity : AggregateRoot<Guid>
{
    /// <summary>Ctor không tham số cho hydration/ORM — không dùng trong logic nghiệp vụ.</summary>
    private GachaPity()
    {
    }

    private GachaPity(Guid id, Guid profileId, string bannerId, int count, DateTimeOffset createdAt, DateTimeOffset updatedAt)
        : base(id)
    {
        ProfileId = profileId;
        BannerId = bannerId;
        Count = count;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    /// <summary>Profile sở hữu (khoá ngoại tới <c>player_profiles</c>). Server-controlled.</summary>
    public Guid ProfileId { get; private set; }

    /// <summary>Banner áp dụng pity (id config <c>gacha_*</c>).</summary>
    public string BannerId { get; private set; } = string.Empty;

    /// <summary>Số lần quay liên tiếp chưa trúng rarity mục tiêu (server-side).</summary>
    public int Count { get; private set; }

    /// <summary>Thời điểm tạo (server-time, từ <see cref="IClock"/>).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Thời điểm cập nhật gần nhất (server-time).</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Tạo bộ đếm pity mới (Count = 0) cho một (profile, banner). Guard tham số.</summary>
    public static GachaPity CreateFor(Guid id, Guid profileId, string bannerId, DateTimeOffset nowUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("GachaPity id không được rỗng.", nameof(id));
        }

        if (profileId == Guid.Empty)
        {
            throw new ArgumentException("ProfileId không được rỗng.", nameof(profileId));
        }

        if (string.IsNullOrWhiteSpace(bannerId))
        {
            throw new ArgumentException("BannerId không được rỗng.", nameof(bannerId));
        }

        return new GachaPity(id, profileId, bannerId, 0, nowUtc, nowUtc);
    }

    /// <summary>Dựng lại từ trạng thái đã lưu — hydration/thử nghiệm.</summary>
    public static GachaPity Restore(
        Guid id, Guid profileId, string bannerId, int count, DateTimeOffset createdAt, DateTimeOffset updatedAt)
        => new(id, profileId, bannerId, count, createdAt, updatedAt);

    /// <summary>Ghi đè bộ đếm pity (server tính từ <c>SummonRoller</c>) — cập nhật <see cref="UpdatedAt"/>.</summary>
    public void SetCount(int count, DateTimeOffset nowUtc)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "Pity count không được âm.");
        }

        Count = count;
        UpdatedAt = nowUtc;
    }
}
