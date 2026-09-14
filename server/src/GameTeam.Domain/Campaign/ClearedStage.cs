namespace GameTeam.Domain.Campaign;

/// <summary>
/// Một stage campaign đã được clear (bản ghi bất biến): id stage + thời điểm clear lần đầu (server-time).
/// Là phần tử của tập cleared trong <see cref="CampaignProgress"/> — dùng để chống cấp thưởng trùng
/// (first-clear only) và suy ra tiến độ. <see cref="StageId"/> tham chiếu id stage ở config (ADR-004).
/// </summary>
public sealed class ClearedStage
{
    /// <summary>Ctor không tham số cho hydration/ORM — không dùng trong logic nghiệp vụ.</summary>
    private ClearedStage()
    {
    }

    /// <summary>Dựng một bản ghi stage đã clear. Guard: stageId không rỗng.</summary>
    public ClearedStage(string stageId, DateTimeOffset clearedAt)
    {
        if (string.IsNullOrWhiteSpace(stageId))
        {
            throw new ArgumentException("StageId không được rỗng.", nameof(stageId));
        }

        StageId = stageId;
        ClearedAt = clearedAt;
    }

    /// <summary>Id stage đã clear (prefix <c>stage_</c>, ADR-004).</summary>
    public string StageId { get; private set; } = string.Empty;

    /// <summary>Thời điểm clear lần đầu (server-time, từ <c>IClock</c>).</summary>
    public DateTimeOffset ClearedAt { get; private set; }
}
