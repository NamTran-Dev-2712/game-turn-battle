namespace GameTeam.Domain.Combat.Model;

/// <summary>
/// Đầu vào <b>thuần, tự chứa</b> của combat sim (combat-framework.md §9). Cùng
/// (<see cref="ConfigVersion"/>, đội hình, <see cref="Stage"/>, <see cref="Seed"/>) ⇒ cùng output
/// bit-for-bit trên mọi máy (ADR-011). Không I/O, không đọc config trực tiếp — mọi chỉ số đã được
/// tầng Application (data-driven) hoặc golden vector nạp sẵn vào đây.
/// </summary>
public sealed record BattleInput(
    string ConfigVersion,
    ulong Seed,
    StageInfo Stage,
    IReadOnlyList<UnitSnapshot> Ally,
    IReadOnlyList<UnitSnapshot> Enemy,
    CombatRules Rules,
    SkillDef BasicSkill);
