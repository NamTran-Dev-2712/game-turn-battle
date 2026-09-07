namespace GameTeam.Contracts.Team;

/// <summary>
/// Một ô đội hình trên wire: cặp (vị trí lưới, hero). Dùng cho cả request lưu (<see cref="SaveTeamRequest"/>)
/// và response (<see cref="TeamDto"/>). Kích thước lưới (rows×cols) đọc từ config bundle ở client — KHÔNG
/// gửi trùng qua đây (data-driven, ADR-004).
/// </summary>
/// <param name="SlotIndex">Chỉ số ô 0-based (= <c>slot</c> combat; vị trí nhỏ hơn bị nhắm trước — §14).</param>
/// <param name="HeroId">Id definition hero ở config (prefix <c>hero_</c>) — phải thuộc sở hữu (validate server).</param>
public sealed record TeamSlotDto(int SlotIndex, string HeroId);
