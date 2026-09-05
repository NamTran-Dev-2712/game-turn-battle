namespace GameTeam.Contracts.Hero;

/// <summary>
/// Phản hồi <c>GET /api/v1/heroes</c>: danh sách hero người chơi sở hữu (server-authoritative). Bọc trong
/// một object (không trả mảng trần) để client parse nhất quán (một kênh Dictionary) và chừa chỗ thêm
/// metadata (paging…) về sau mà không phá contract.
/// </summary>
/// <param name="Heroes">Hero người chơi sở hữu (bản wire tối giản; definition ghép từ ConfigProvider ở client).</param>
public sealed record MyHeroesResponse(IReadOnlyList<OwnedHeroDto> Heroes);
