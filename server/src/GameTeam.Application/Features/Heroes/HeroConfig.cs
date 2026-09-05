namespace GameTeam.Application.Features.Heroes;

/// <summary>
/// POCO đọc definition hero từ config qua <c>IConfigProvider.Get&lt;HeroConfig&gt;("hero", id)</c> (data-driven,
/// ADR-004). Khoá JSON là <c>snake_case</c> (map PascalCase↔snake_case bằng naming policy của
/// <c>RuntimeConfigProvider</c>). Bám schema hero (phase 06). KHÔNG chứa giá trị mặc định gameplay — thiếu
/// config ⇒ null (handler báo lỗi, không đoán).
/// </summary>
public sealed class HeroConfig
{
    /// <summary>Id definition (prefix <c>hero_</c>).</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Phe (chuỗi — GP2 chưa chốt).</summary>
    public string Faction { get; init; } = string.Empty;

    /// <summary>Lớp (khớp enum config: warrior/mage/ranger/support).</summary>
    public string Class { get; init; } = string.Empty;

    /// <summary>Nguyên tố (fire/water/earth/light/dark).</summary>
    public string Element { get; init; } = string.Empty;

    /// <summary>Vai trò (tank/dps/support/healer).</summary>
    public string Role { get; init; } = string.Empty;

    /// <summary>Độ hiếm (3/4/5).</summary>
    public int Rarity { get; init; }

    /// <summary>Chỉ số nền (integer, ADR-011).</summary>
    public HeroBaseStats BaseStats { get; init; } = new();

    /// <summary>Tham chiếu skill id (hero → skill).</summary>
    public IReadOnlyList<string> Skills { get; init; } = Array.Empty<string>();

    /// <summary>Tham chiếu art (id → path/atlas, ADR-009) — tuỳ chọn.</summary>
    public string? Art { get; init; }
}

/// <summary>Chỉ số nền hero (integer, ADR-011) — lát cắt của <see cref="HeroConfig"/>.</summary>
public sealed class HeroBaseStats
{
    /// <summary>Máu.</summary>
    public int Hp { get; init; }

    /// <summary>Tấn công.</summary>
    public int Atk { get; init; }

    /// <summary>Phòng thủ.</summary>
    public int Def { get; init; }

    /// <summary>Tốc độ.</summary>
    public int Spd { get; init; }
}
