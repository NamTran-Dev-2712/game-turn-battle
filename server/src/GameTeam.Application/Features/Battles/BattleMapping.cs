using GameTeam.Contracts.Battle;
using GameTeam.Domain.Battles;

namespace GameTeam.Application.Features.Battles;

/// <summary>
/// Map <see cref="BattleRecord"/> (nguồn chân lý đã lưu) → <see cref="BattleResultDto"/> wire. Dùng chung cho
/// cả trận mới lẫn retry idempotent ⇒ client luôn thấy đúng một kết quả cho một <c>attemptId</c>.
/// </summary>
internal static class BattleMapping
{
    public static BattleResultDto ToDto(BattleRecord record) => new(
        record.Seed,
        record.Outcome,
        record.Rounds,
        record.Rewards.Select(r => new RewardDto(r.RewardType, r.RefId, r.Amount)).ToList(),
        record.Log);
}
