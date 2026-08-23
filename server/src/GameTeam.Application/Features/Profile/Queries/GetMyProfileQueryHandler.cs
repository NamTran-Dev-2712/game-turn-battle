using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Application.Abstractions.Security;
using GameTeam.Contracts.Profile;
using GameTeam.Domain.Common;
using MediatR;
using DomainProfile = GameTeam.Domain.Profiles.PlayerProfile;

namespace GameTeam.Application.Features.Profile.Queries;

/// <summary>
/// Handles <see cref="GetMyProfileQuery"/>: resolves the owner from <see cref="ICurrentUser"/> and returns
/// that account's profile, or a <c>PROFILE_NOT_FOUND</c> failure. Read-only — never mutates or creates.
/// </summary>
public sealed class GetMyProfileQueryHandler
    : IRequestHandler<GetMyProfileQuery, Result<ProfileDto>>
{
    private readonly IPlayerProfileRepository _profiles;
    private readonly ICurrentUser _currentUser;

    public GetMyProfileQueryHandler(IPlayerProfileRepository profiles, ICurrentUser currentUser)
    {
        _profiles = profiles;
        _currentUser = currentUser;
    }

    public async Task<Result<ProfileDto>> Handle(
        GetMyProfileQuery request,
        CancellationToken cancellationToken)
    {
        Guid? accountId = _currentUser.AccountId;
        if (accountId is null)
        {
            return ProfileErrors.Unauthenticated;
        }

        DomainProfile? profile = await _profiles.GetByAccountIdAsync(accountId.Value, cancellationToken);

        return profile is null
            ? ProfileErrors.NotFound
            : Result.Success(ProfileMapping.ToDto(profile));
    }
}
