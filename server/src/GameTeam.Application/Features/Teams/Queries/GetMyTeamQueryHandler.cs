using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Application.Abstractions.Security;
using GameTeam.Contracts.Team;
using GameTeam.Domain.Common;
using DomainProfile = GameTeam.Domain.Profiles.PlayerProfile;
using DomainTeam = GameTeam.Domain.Teams.Team;
using MediatR;

namespace GameTeam.Application.Features.Teams.Queries;

/// <summary>
/// Handles <see cref="GetMyTeamQuery"/>: chủ sở hữu suy từ <see cref="ICurrentUser"/> → profile theo account →
/// đội theo profile → map DTO. Read-only. Chưa xác thực ⇒ lỗi; chưa có profile hoặc chưa lưu đội ⇒ đội rỗng
/// (client dựng lưới trống, không phải lỗi).
/// </summary>
public sealed class GetMyTeamQueryHandler : IRequestHandler<GetMyTeamQuery, Result<TeamDto>>
{
    private readonly ITeamRepository _teams;
    private readonly IPlayerProfileRepository _profiles;
    private readonly ICurrentUser _currentUser;

    public GetMyTeamQueryHandler(
        ITeamRepository teams,
        IPlayerProfileRepository profiles,
        ICurrentUser currentUser)
    {
        _teams = teams;
        _profiles = profiles;
        _currentUser = currentUser;
    }

    public async Task<Result<TeamDto>> Handle(GetMyTeamQuery request, CancellationToken cancellationToken)
    {
        Guid? accountId = _currentUser.AccountId;
        if (accountId is null)
        {
            return TeamErrors.Unauthenticated;
        }

        DomainProfile? profile = await _profiles.GetByAccountIdAsync(accountId.Value, cancellationToken);
        if (profile is null)
        {
            return Result.Success(TeamMapping.Empty());
        }

        DomainTeam? team = await _teams.GetByProfileIdAsync(profile.Id, cancellationToken);
        return team is null
            ? Result.Success(TeamMapping.Empty())
            : Result.Success(TeamMapping.ToDto(team));
    }
}
