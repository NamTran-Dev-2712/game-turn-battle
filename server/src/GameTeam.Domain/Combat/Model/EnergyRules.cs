namespace GameTeam.Domain.Combat.Model;

/// <summary>
/// Luật năng lượng/ultimate (combat-framework.md §15, CB4 [ĐỀ XUẤT]). Vector mẫu phase 23 <b>tắt</b>
/// năng lượng (mọi giá trị gain = 0, ultimate không đạt ngưỡng) — cơ chế được nối sẵn nhưng no-op.
/// </summary>
public sealed record EnergyRules(int Initial, int OnAttack, int OnHit, int UltimateCost, int Max);
