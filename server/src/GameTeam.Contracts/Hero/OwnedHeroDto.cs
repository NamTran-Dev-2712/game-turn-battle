namespace GameTeam.Contracts.Hero;

/// <summary>
/// Một hero người chơi <b>sở hữu</b> (instance server-authoritative, ADR-007) — bản wire TỐI GIẢN: chỉ
/// trạng thái instance. Định nghĩa tĩnh (faction/class/element/role/stats/skills) đọc từ config qua
/// <c>ConfigProvider</c> ở client (ghép theo <see cref="HeroId"/>) — KHÔNG gửi trùng qua đây (data-driven,
/// ADR-004; xem <see cref="HeroDefinitionDto"/> cho definition từ config).
/// </summary>
/// <param name="HeroId">Id definition hero ở config (prefix <c>hero_</c>) — khoá ghép với definition.</param>
/// <param name="Level">Cấp hiện tại (bản nền — nâng cấp phase 35).</param>
/// <param name="Stars">Số sao hiện tại (bản nền — nâng sao phase 39).</param>
public sealed record OwnedHeroDto(string HeroId, int Level, int Stars);
