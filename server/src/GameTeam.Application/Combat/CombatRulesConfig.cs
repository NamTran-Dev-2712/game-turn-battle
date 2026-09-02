namespace GameTeam.Application.Combat;

/// <summary>
/// Config luật/hằng số combat của một màn (§16/§17/§19). <b>Quyết định phase 24:</b> nguồn là
/// <b>stage config</b> — chưa hình thức hoá vào JSON Schema (Phase 06/07); việc chuẩn hoá schema
/// <c>combat_rules</c> là nợ theo dõi (follow-up). Toàn bộ là combat_int đọc từ config (ADR-004).
/// </summary>
public sealed class CombatRulesConfig
{
    /// <summary>Hằng K trong K/(K+def).</summary>
    public int DefConstantK { get; init; }

    /// <summary>Sàn sát thương tối thiểu.</summary>
    public int MinDamage { get; init; }

    /// <summary>Hệ số chí mạng fixed-point (1.5 → 1500).</summary>
    public int CritMultiplierFixed { get; init; }

    /// <summary>Tỉ lệ trúng (basis point).</summary>
    public int AccuracyBp { get; init; }

    /// <summary>Tỉ lệ chí mạng (basis point).</summary>
    public int CritRateBp { get; init; }

    /// <summary>Config năng lượng.</summary>
    public EnergyConfig Energy { get; init; } = new();
}
