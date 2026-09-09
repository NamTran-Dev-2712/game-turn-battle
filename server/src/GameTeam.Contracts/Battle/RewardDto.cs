namespace GameTeam.Contracts.Battle;

/// <summary>
/// Một khoản thưởng đã cấp (server-authoritative, ADR-007) — bản wire của một entry trong reward table
/// (<c>reward.schema.json</c>). Phase 30 cấp tối giản loại <c>currency</c>; các loại khác (hero/fragment/item)
/// là phase 31–33.
/// </summary>
/// <param name="RewardType">Loại thưởng (<c>currency</c>/<c>hero</c>/<c>fragment</c>/<c>item</c>).</param>
/// <param name="RefId">Khoá tham chiếu thực thể thưởng (vd <c>gold</c>).</param>
/// <param name="Amount">Số lượng (integer, ADR-011).</param>
public sealed record RewardDto(string RewardType, string RefId, int Amount);
