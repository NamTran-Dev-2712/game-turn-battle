using GameTeam.Contracts.Hero;
using GameTeam.Domain.Common;
using MediatR;

namespace GameTeam.Application.Features.Heroes.Queries;

/// <summary>
/// Đọc danh sách hero mà người gọi (đã xác thực) sở hữu — chủ sở hữu suy TỪ token <c>sub</c>
/// (<c>ICurrentUser</c>), KHÔNG nhận từ client (chống IDOR). Server-authoritative (ADR-007). Trả bản wire
/// tối giản (<see cref="OwnedHeroDto"/>); client ghép definition từ ConfigProvider. Không transactional,
/// không cacheable (state per-account là chân lý server).
/// </summary>
public sealed record GetMyHeroesQuery : IRequest<Result<MyHeroesResponse>>;
