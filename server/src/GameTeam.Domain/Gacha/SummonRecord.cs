using GameTeam.Domain.Common;

namespace GameTeam.Domain.Gacha;

/// <summary>
/// Bản ghi một lần triệu hồi — <b>server-authoritative</b> (ADR-011) và là <b>bản ghi idempotency</b> (ADR-007):
/// (<see cref="ProfileId"/>, <see cref="RequestId"/>) là <b>duy nhất</b> (unique index), nên gọi lại cùng
/// <see cref="RequestId"/> trả lại đúng kết quả đã lưu mà KHÔNG quay lại / tiêu tiền / cấp hero lần hai. Lưu đủ
/// để dựng lại kết quả (banner/count/pity/pulls) cho retry; <see cref="Seed"/> lưu để <b>audit</b> (không trả client).
/// </summary>
public sealed class SummonRecord : AggregateRoot<Guid>
{
    /// <summary>Phiên bản schema của bản ghi (ADR-007). Tăng kèm migration + test khi cấu trúc đổi.</summary>
    public const int CurrentSchemaVersion = 1;

    private readonly List<SummonPullLine> _pulls = [];

    /// <summary>Ctor không tham số cho hydration/ORM — không dùng trong logic nghiệp vụ.</summary>
    private SummonRecord()
    {
    }

    private SummonRecord(
        Guid id,
        Guid profileId,
        string requestId,
        string bannerId,
        int count,
        long seed,
        int pityAfter,
        IEnumerable<SummonPullLine> pulls,
        int schemaVersion,
        DateTimeOffset createdAt)
        : base(id)
    {
        ProfileId = profileId;
        RequestId = requestId;
        BannerId = bannerId;
        Count = count;
        Seed = seed;
        PityAfter = pityAfter;
        _pulls = pulls.ToList();
        SchemaVersion = schemaVersion;
        CreatedAt = createdAt;
    }

    /// <summary>Profile sở hữu (khoá ngoại tới <c>player_profiles</c>). Server-controlled.</summary>
    public Guid ProfileId { get; private set; }

    /// <summary>Khoá idempotency do client sinh cho một lần triệu hồi (unique theo profile). Server-controlled.</summary>
    public string RequestId { get; private set; } = string.Empty;

    /// <summary>Banner đã quay (id config <c>gacha_*</c>).</summary>
    public string BannerId { get; private set; } = string.Empty;

    /// <summary>Số lần quay của lượt này (1 hoặc 10).</summary>
    public int Count { get; private set; }

    /// <summary>Seed PRNG server đã dùng (Int64 không âm) — chỉ để AUDIT, KHÔNG trả client.</summary>
    public long Seed { get; private set; }

    /// <summary>Bộ đếm pity (profile, banner) sau lượt quay này.</summary>
    public int PityAfter { get; private set; }

    /// <summary>Kết quả từng lần quay (chỉ đọc, đúng thứ tự).</summary>
    public IReadOnlyList<SummonPullLine> Pulls => _pulls.AsReadOnly();

    /// <summary>Phiên bản schema của bản ghi này (ADR-007). Server-controlled.</summary>
    public int SchemaVersion { get; private set; } = CurrentSchemaVersion;

    /// <summary>Thời điểm ghi (server-time, từ <see cref="IClock"/>).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Ghi một bản ghi triệu hồi mới. <paramref name="id"/> do caller sinh (<c>Guid.NewGuid()</c>). Guard tham số.
    /// </summary>
    public static SummonRecord Create(
        Guid id,
        Guid profileId,
        string requestId,
        string bannerId,
        int count,
        long seed,
        int pityAfter,
        IReadOnlyList<SummonPullLine> pulls,
        DateTimeOffset nowUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("SummonRecord id không được rỗng.", nameof(id));
        }

        if (profileId == Guid.Empty)
        {
            throw new ArgumentException("ProfileId không được rỗng.", nameof(profileId));
        }

        if (string.IsNullOrWhiteSpace(requestId))
        {
            throw new ArgumentException("RequestId không được rỗng.", nameof(requestId));
        }

        if (string.IsNullOrWhiteSpace(bannerId))
        {
            throw new ArgumentException("BannerId không được rỗng.", nameof(bannerId));
        }

        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "Count phải dương.");
        }

        Guard.NotNull(pulls);

        return new SummonRecord(
            id, profileId, requestId, bannerId, count, seed, pityAfter, pulls,
            CurrentSchemaVersion, nowUtc);
    }

    /// <summary>Dựng lại từ trạng thái đã lưu — hydration/thử nghiệm.</summary>
    public static SummonRecord Restore(
        Guid id,
        Guid profileId,
        string requestId,
        string bannerId,
        int count,
        long seed,
        int pityAfter,
        IReadOnlyList<SummonPullLine> pulls,
        int schemaVersion,
        DateTimeOffset createdAt)
        => new(id, profileId, requestId, bannerId, count, seed, pityAfter, pulls, schemaVersion, createdAt);
}
