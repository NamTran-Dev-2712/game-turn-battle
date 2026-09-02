namespace GameTeam.Application.Combat;

/// <summary>
/// Lát cắt config combat của một màn (data-driven — ADR-004): số vòng tối đa, skill đòn thường, luật
/// combat, và danh sách địch. <b>Nguồn của <see cref="CombatRules"/> là stage config (quyết định phase 24).</b>
/// </summary>
public sealed class StageCombatConfig
{
    /// <summary>Số vòng tối đa (đạt ⇒ DRAW).</summary>
    public int MaxRounds { get; init; }

    /// <summary>Id skill đòn thường (đọc coeff/effects từ skill config).</summary>
    public string BasicSkillId { get; init; } = "skill_basic";

    /// <summary>Luật/hằng số combat.</summary>
    public CombatRulesConfig CombatRules { get; init; } = new();

    /// <summary>Đội hình địch của màn.</summary>
    public IReadOnlyList<StageEnemyConfig> Enemies { get; init; } = new List<StageEnemyConfig>();
}
