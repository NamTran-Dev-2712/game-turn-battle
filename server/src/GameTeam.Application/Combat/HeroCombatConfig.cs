namespace GameTeam.Application.Combat;

/// <summary>
/// Lát cắt config combat của một hero — đọc <b>trực tiếp từ gameplay hero config</b>
/// (<c>hero.schema.json</c>, phase 06/27) qua <see cref="Abstractions.Configuration.IConfigProvider"/>
/// (data-driven — ADR-004). Chỉ số nền lồng trong <c>base_stats</c>; <c>skills</c> là danh sách skill id.
/// Ánh xạ <c>skills[]</c> → basic/ultimate của trận thực hiện ở <see cref="CombatInputResolver"/> (phase 30).
/// </summary>
public sealed class HeroCombatConfig
{
    /// <summary>Chỉ số nền (integer, ADR-011) — lồng <c>base_stats</c> khớp schema hero.</summary>
    public HeroCombatStats BaseStats { get; init; } = new();

    /// <summary>Tham chiếu skill id (hero → skill). Rỗng ⇒ đơn vị dùng basic skill dùng chung của màn.</summary>
    public IReadOnlyList<string> Skills { get; init; } = Array.Empty<string>();
}

/// <summary>Chỉ số nền hero (integer, ADR-011) — khoá <c>hp/atk/def/spd</c> trong <c>base_stats</c>.</summary>
public sealed class HeroCombatStats
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
