namespace GameTeam.Contracts.Summon;

/// <summary>
/// Kết quả một lần triệu hồi <b>server-authoritative</b> (ADR-011): danh sách <see cref="Pulls"/> do server
/// quyết (RNG/rate/pity), <see cref="PityAfter"/> là bộ đếm pity của (profile, banner) sau lượt quay này.
/// Seed KHÔNG trả về client — gacha không replay ở client (khác combat); seed chỉ lưu server để audit.
/// </summary>
/// <param name="BannerId">Banner đã quay.</param>
/// <param name="Count">Số lần quay đã thực hiện.</param>
/// <param name="PityAfter">Bộ đếm pity (profile, banner) sau lượt quay này (server-side).</param>
/// <param name="Pulls">Kết quả từng lần quay (đúng thứ tự) — server-authoritative.</param>
public sealed record SummonResultDto(
    string BannerId,
    int Count,
    int PityAfter,
    IReadOnlyList<SummonPullDto> Pulls);
