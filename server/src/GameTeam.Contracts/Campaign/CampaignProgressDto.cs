namespace GameTeam.Contracts.Campaign;

/// <summary>
/// Tiến độ campaign của người chơi (server-authoritative, ADR-007) — đủ để client render màn campaign:
/// danh sách stage kèm trạng thái mở/khoá/đã clear (<paramref name="Stages"/>, sắp theo thứ tự chuỗi) và
/// <paramref name="CurrentAfkStageId"/> = stage tiến độ hiện hành (nguồn cho AFK phase 37; rỗng nếu chưa clear
/// stage nào). Client chỉ hiển thị — không tự quyết tiến độ.
/// </summary>
/// <param name="Stages">Các stage campaign kèm trạng thái, theo thứ tự chuỗi.</param>
/// <param name="CurrentAfkStageId">Stage AFK hiện hành (rỗng nếu chưa có tiến độ).</param>
public sealed record CampaignProgressDto(
    IReadOnlyList<CampaignStageDto> Stages,
    string CurrentAfkStageId);
