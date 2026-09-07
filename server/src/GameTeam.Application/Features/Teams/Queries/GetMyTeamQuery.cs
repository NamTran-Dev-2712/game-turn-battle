using GameTeam.Contracts.Team;
using GameTeam.Domain.Common;
using MediatR;

namespace GameTeam.Application.Features.Teams.Queries;

/// <summary>
/// Đọc đội hình của người gọi (đã xác thực) — chủ sở hữu suy TỪ token <c>sub</c> (<c>ICurrentUser</c>),
/// KHÔNG nhận từ client (chống IDOR). Server-authoritative (ADR-007). Chưa lưu đội ⇒ đội rỗng (lưới trống),
/// không lỗi. Không transactional (read-only).
/// </summary>
public sealed record GetMyTeamQuery : IRequest<Result<TeamDto>>;
