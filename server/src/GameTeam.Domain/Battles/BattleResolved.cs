using GameTeam.Domain.Common;

namespace GameTeam.Domain.Battles;

/// <summary>
/// Raise khi một <see cref="BattleRecord"/> được ghi (trận đã quyết kết quả server-authoritative). Dispatch ở
/// SaveChanges (phase 11). Chưa có subscriber ở phase 30 — nền cho quest/progression (phase 41+) không cần sửa
/// lõi combat.
/// </summary>
public sealed record BattleResolved(Guid BattleId, Guid ProfileId, string Outcome) : IDomainEvent;
