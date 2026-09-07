namespace GameTeam.Contracts.Team;

/// <summary>
/// Phản hồi <c>GET/POST /api/v1/team</c>: đội hình hiện hành của người chơi (server-authoritative, ADR-007).
/// Chưa lưu đội nào ⇒ <see cref="Slots"/> rỗng (client dựng lưới trống). Bọc trong object (không trả mảng
/// trần) để client parse nhất quán và chừa chỗ thêm metadata về sau.
/// </summary>
/// <param name="Slots">Các ô đã đặt hero (có thể rỗng). Definition hero ghép từ ConfigProvider ở client.</param>
public sealed record TeamDto(IReadOnlyList<TeamSlotDto> Slots);
