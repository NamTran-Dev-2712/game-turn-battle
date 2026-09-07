using GameTeam.Application.Abstractions.Messaging;
using GameTeam.Contracts.Team;
using GameTeam.Domain.Common;
using MediatR;

namespace GameTeam.Application.Features.Teams.Commands;

/// <summary>
/// Lưu (đè) đội hình của người gọi — <b>intent client</b>, server validate + quyết định (ADR-007/011).
/// Chủ sở hữu suy TỪ token <c>sub</c> (<c>ICurrentUser</c>), KHÔNG nhận trong payload (chống IDOR).
/// <see cref="ITransactionalRequest"/> ⇒ ghi được bao trong một transaction (commit khi <c>Result</c> thành công).
/// </summary>
/// <param name="Slots">Các ô đề xuất (hero + vị trí) — server validate đúng số, không trùng, thuộc sở hữu, hợp lệ.</param>
public sealed record SaveTeamCommand(IReadOnlyList<TeamSlotDto> Slots)
    : IRequest<Result<TeamDto>>, ITransactionalRequest;
