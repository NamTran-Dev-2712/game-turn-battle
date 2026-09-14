namespace GameTeam.Application.Combat;

/// <summary>
/// Một thành viên đội ally trong yêu cầu trận (định danh + hero + slot + <b>cấp</b>). Chỉ số nền từ hero
/// config; <see cref="Level"/> (Phase 35, mặc định 1) dùng để tính lại chỉ số theo cấp ở
/// <see cref="CombatInputResolver"/> (data-driven, ADR-004) ⇒ nâng cấp hero ảnh hưởng combat.
/// </summary>
public sealed record CombatTeamMember(string ActorId, string HeroId, int Slot, int Level = 1);
