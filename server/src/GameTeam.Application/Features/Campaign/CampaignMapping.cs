using GameTeam.Contracts.Campaign;
using GameTeam.Domain.Campaign;

namespace GameTeam.Application.Features.Campaign;

/// <summary>
/// Map tiến độ campaign (aggregate + chuỗi config) → <see cref="CampaignProgressDto"/> wire, và các hằng số dùng
/// chung. Trạng thái mở/khoá/đã-clear của từng stage do server tính (chuỗi chapter + tập cleared) — client chỉ
/// hiển thị (ADR-007).
/// </summary>
internal static class CampaignMapping
{
    /// <summary>Nguồn (source) cho ledger tiền tệ khi cấp thưởng campaign (audit).</summary>
    public const string CampaignRewardSource = "campaign_reward";

    /// <summary>Tập id stage đã clear (rỗng nếu chưa có tiến độ).</summary>
    public static HashSet<string> ClearedSet(CampaignProgress? progress) =>
        progress is null
            ? new HashSet<string>(StringComparer.Ordinal)
            : new HashSet<string>(progress.ClearedStages.Select(c => c.StageId), StringComparer.Ordinal);

    /// <summary>Dựng DTO tiến độ từ chuỗi campaign + tiến độ đã lưu (đủ để client render mở/khoá/đã-clear).</summary>
    public static CampaignProgressDto ToDto(CampaignChain chain, CampaignProgress? progress)
    {
        HashSet<string> cleared = ClearedSet(progress);
        var stages = new List<CampaignStageDto>();
        foreach (CampaignStageRef stage in chain.OrderedStages())
        {
            stages.Add(new CampaignStageDto(
                stage.StageId,
                stage.ChapterId,
                stage.Order,
                cleared.Contains(stage.StageId),
                chain.IsUnlocked(stage.StageId, cleared)));
        }

        return new CampaignProgressDto(stages, progress?.CurrentAfkStageId ?? string.Empty);
    }
}
