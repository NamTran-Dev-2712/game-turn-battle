namespace GameTeam.Application.Combat;

/// <summary>
/// Lát cắt config combat của một hero (đọc qua <see cref="Abstractions.Configuration.IConfigProvider"/>,
/// data-driven — ADR-004). Khoá JSON là <c>snake_case</c> (<c>hp</c>/<c>atk</c>/<c>def</c>/<c>spd</c>).
/// </summary>
public sealed class HeroCombatConfig
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
