using GameTeam.Domain.Combat.Events;

namespace GameTeam.Domain.Combat;

/// <summary>
/// Đầu ra của combat sim: event log có thứ tự (seq = vị trí) + <see cref="BattleResult"/> (§18/§19).
/// Tái lập được: cùng <see cref="Model.BattleInput"/> ⇒ cùng output bit-for-bit (ADR-011).
/// </summary>
public sealed record BattleOutput(IReadOnlyList<CombatEvent> EventLog, BattleResult Result);
