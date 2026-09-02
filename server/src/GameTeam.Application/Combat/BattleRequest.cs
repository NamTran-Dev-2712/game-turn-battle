namespace GameTeam.Application.Combat;

/// <summary>
/// Yêu cầu dựng đầu vào trận (data-driven): seed server, màn, đội ally. Địch + luật combat lấy từ stage
/// config. <b>Không</b> phải endpoint (phase 30) — đây là dữ liệu cho <see cref="CombatInputResolver"/>.
/// </summary>
public sealed record BattleRequest(ulong Seed, string StageId, IReadOnlyList<CombatTeamMember> Ally);
