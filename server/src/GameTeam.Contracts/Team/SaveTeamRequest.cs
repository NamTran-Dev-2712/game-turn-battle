namespace GameTeam.Contracts.Team;

/// <summary>
/// Body <c>POST /api/v1/team</c>: <b>intent</b> lưu đội hình từ client (KHÔNG phải chân lý). Server validate
/// tất cả (đúng số ô theo config, không trùng hero, hero thuộc sở hữu, slot hợp lệ) trước khi persist — client
/// chỉ đề xuất (ADR-007/011). Chủ sở hữu suy từ token, KHÔNG nhận trong body (chống IDOR).
/// </summary>
/// <param name="Slots">Các ô đề xuất (hero + vị trí). Server là bên quyết định cuối cùng.</param>
public sealed record SaveTeamRequest(IReadOnlyList<TeamSlotDto> Slots);
