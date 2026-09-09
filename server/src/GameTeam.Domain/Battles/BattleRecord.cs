using GameTeam.Domain.Common;

namespace GameTeam.Domain.Battles;

/// <summary>
/// Bản ghi kết quả một trận đấu — <b>server-authoritative</b> (ADR-011) và là <b>bản ghi idempotency</b>
/// (ADR-007): (<see cref="ProfileId"/>, <see cref="AttemptId"/>) là <b>duy nhất</b> (unique index), nên gọi lại
/// cùng <see cref="AttemptId"/> trả lại đúng kết quả đã lưu mà KHÔNG re-sim / cấp thưởng lần hai. Lưu đủ để dựng
/// lại <c>BattleResult</c> (seed/outcome/rounds/rewards/log) cho retry.
/// </summary>
public sealed class BattleRecord : AggregateRoot<Guid>
{
    /// <summary>Phiên bản schema của bản ghi (ADR-007). Tăng kèm migration + test khi cấu trúc đổi.</summary>
    public const int CurrentSchemaVersion = 1;

    private readonly List<BattleReward> _rewards = [];

    /// <summary>Ctor không tham số cho hydration/ORM — không dùng trong logic nghiệp vụ.</summary>
    private BattleRecord()
    {
    }

    private BattleRecord(
        Guid id,
        Guid profileId,
        string attemptId,
        Guid teamId,
        string stageId,
        long seed,
        string outcome,
        int rounds,
        IEnumerable<BattleReward> rewards,
        string log,
        int schemaVersion,
        DateTimeOffset createdAt)
        : base(id)
    {
        ProfileId = profileId;
        AttemptId = attemptId;
        TeamId = teamId;
        StageId = stageId;
        Seed = seed;
        Outcome = outcome;
        Rounds = rounds;
        _rewards = rewards.ToList();
        Log = log;
        SchemaVersion = schemaVersion;
        CreatedAt = createdAt;
    }

    /// <summary>Profile sở hữu (khoá ngoại tới <c>player_profiles</c>). Server-controlled.</summary>
    public Guid ProfileId { get; private set; }

    /// <summary>Khoá idempotency do client sinh cho một lần đánh (unique theo profile). Server-controlled.</summary>
    public string AttemptId { get; private set; } = string.Empty;

    /// <summary>Đội hình đã dùng (khoá tham chiếu — không snapshot chỉ số ở đây).</summary>
    public Guid TeamId { get; private set; }

    /// <summary>Màn chơi đã đánh (id config).</summary>
    public string StageId { get; private set; } = string.Empty;

    /// <summary>Seed PRNG đã dùng (Int64 không âm) — client replay cùng seed.</summary>
    public long Seed { get; private set; }

    /// <summary>Kết cục (<c>VICTORY</c>/<c>DEFEAT</c>/<c>DRAW</c>).</summary>
    public string Outcome { get; private set; } = string.Empty;

    /// <summary>Số vòng đã đánh.</summary>
    public int Rounds { get; private set; }

    /// <summary>Các khoản thưởng đã cấp (chỉ đọc). Rỗng nếu không thắng.</summary>
    public IReadOnlyList<BattleReward> Rewards => _rewards.AsReadOnly();

    /// <summary>Event log tất định dạng JSON chuỗi (<c>{event_log, result}</c>) — trả cho client vẽ + verify.</summary>
    public string Log { get; private set; } = string.Empty;

    /// <summary>Phiên bản schema của bản ghi này (ADR-007). Server-controlled.</summary>
    public int SchemaVersion { get; private set; } = CurrentSchemaVersion;

    /// <summary>Thời điểm ghi (server-time, từ <see cref="IClock"/>).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Ghi một bản ghi trận mới. <paramref name="id"/> do caller sinh (<c>Guid.NewGuid()</c>). Guard tham số.
    /// Raise <see cref="BattleResolved"/>.
    /// </summary>
    public static BattleRecord Create(
        Guid id,
        Guid profileId,
        string attemptId,
        Guid teamId,
        string stageId,
        long seed,
        string outcome,
        int rounds,
        IReadOnlyList<BattleReward> rewards,
        string log,
        DateTimeOffset nowUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("BattleRecord id không được rỗng.", nameof(id));
        }

        if (profileId == Guid.Empty)
        {
            throw new ArgumentException("ProfileId không được rỗng.", nameof(profileId));
        }

        if (string.IsNullOrWhiteSpace(attemptId))
        {
            throw new ArgumentException("AttemptId không được rỗng.", nameof(attemptId));
        }

        if (string.IsNullOrWhiteSpace(outcome))
        {
            throw new ArgumentException("Outcome không được rỗng.", nameof(outcome));
        }

        Guard.NotNull(rewards);

        BattleRecord record = new(
            id, profileId, attemptId, teamId, stageId, seed, outcome, rounds, rewards, log,
            CurrentSchemaVersion, nowUtc);
        record.RaiseDomainEvent(new BattleResolved(id, profileId, outcome));
        return record;
    }

    /// <summary>Dựng lại từ trạng thái đã lưu — KHÔNG raise event (hydration/thử nghiệm).</summary>
    public static BattleRecord Restore(
        Guid id,
        Guid profileId,
        string attemptId,
        Guid teamId,
        string stageId,
        long seed,
        string outcome,
        int rounds,
        IReadOnlyList<BattleReward> rewards,
        string log,
        int schemaVersion,
        DateTimeOffset createdAt)
        => new(id, profileId, attemptId, teamId, stageId, seed, outcome, rounds, rewards, log, schemaVersion, createdAt);
}
