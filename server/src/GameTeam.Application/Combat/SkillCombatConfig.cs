namespace GameTeam.Application.Combat;

/// <summary>
/// Lát cắt config combat của một skill (data-driven — ADR-004). <c>coeff_fixed</c> là hệ số sát thương
/// fixed-point (1.0 → 1000); <c>effects</c> là danh sách <c>effect_type</c> định tuyến tới handler.
/// </summary>
public sealed class SkillCombatConfig
{
    /// <summary>Hệ số sát thương fixed-point.</summary>
    public int CoeffFixed { get; init; }

    /// <summary>Chính sách chọn mục tiêu (§14).</summary>
    public string TargetRule { get; init; } = "default";

    /// <summary>Danh sách effect_type cấu thành skill.</summary>
    public IReadOnlyList<string> Effects { get; init; } = new[] { "damage" };
}
