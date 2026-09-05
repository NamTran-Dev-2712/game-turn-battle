using GameTeam.Contracts.Enums;

namespace GameTeam.Contracts.Hero;

/// <summary>
/// Định nghĩa tĩnh của một hero — đọc từ config bundle qua <c>IConfigProvider</c> (data-driven, ADR-004);
/// KHÔNG hardcode chỉ số ở code. Bám schema hero (phase 06).
/// <para>
/// <see cref="Faction"/> là <b>chuỗi</b> vì danh sách phe CHƯA chốt (open question GP2) — schema chỉ ràng
/// buộc chuỗi không rỗng; các trục đã chốt (class/element/role/rarity) dùng enum contract (phase 05, khớp
/// enum config). Quy tắc ổn định enum: additive-only.
/// </para>
/// </summary>
/// <param name="HeroId">Id definition (prefix <c>hero_</c>).</param>
/// <param name="Faction">Phe (chuỗi — GP2 chưa chốt).</param>
/// <param name="Class">Lớp (phong cách chiến đấu).</param>
/// <param name="Element">Nguyên tố (khắc chế).</param>
/// <param name="Role">Vai trò đội hình.</param>
/// <param name="Rarity">Độ hiếm (số sao gốc).</param>
/// <param name="BaseStats">Chỉ số nền (từ config).</param>
/// <param name="Skills">Tham chiếu skill id (hero → skill; tồn tại kiểm ở validator phase 07).</param>
/// <param name="Art">Tham chiếu art (id → path/atlas, ADR-009) — tuỳ chọn (thiếu ⇒ client placeholder).</param>
public sealed record HeroDefinitionDto(
    string HeroId,
    string Faction,
    Class Class,
    Element Element,
    Role Role,
    Rarity Rarity,
    HeroBaseStatsDto BaseStats,
    IReadOnlyList<string> Skills,
    string? Art);
