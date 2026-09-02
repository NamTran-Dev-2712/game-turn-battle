namespace GameTeam.Application.Combat;

/// <summary>Một thành viên đội ally trong yêu cầu trận (định danh + hero + slot; chỉ số từ hero config).</summary>
public sealed record CombatTeamMember(string ActorId, string HeroId, int Slot);
