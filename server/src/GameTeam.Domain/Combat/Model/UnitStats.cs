namespace GameTeam.Domain.Combat.Model;

/// <summary>Chỉ số cơ bản của một đơn vị (combat_int — số nguyên ≥ 0, đọc từ config; ADR-004).</summary>
public sealed record UnitStats(int Hp, int Atk, int Def, int Spd);
