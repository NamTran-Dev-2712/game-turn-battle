using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Application.Abstractions.Security;
using GameTeam.Contracts.Profile;
using GameTeam.Domain.Common;
using MediatR;
using DomainProfile = GameTeam.Domain.Profiles.PlayerProfile;

namespace GameTeam.Application.Features.Profile.Commands;

/// <summary>
/// Handles <see cref="GetOrCreateProfileCommand"/>. Resolves the owner from <see cref="ICurrentUser"/>
/// (never from client input), loads the profile by account id, creates it when absent, and applies a
/// read-repair schema migration (<see cref="DomainProfile.Upgrade"/>) for a stale record. Any write is
/// committed atomically by <c>TransactionBehavior</c>.
/// </summary>
public sealed class GetOrCreateProfileCommandHandler
    : IRequestHandler<GetOrCreateProfileCommand, Result<ProfileDto>>
{
    private readonly IPlayerProfileRepository _profiles;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public GetOrCreateProfileCommandHandler(
        IPlayerProfileRepository profiles,
        ICurrentUser currentUser,
        IClock clock)
    {
        _profiles = profiles;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<ProfileDto>> Handle(
        GetOrCreateProfileCommand request,
        CancellationToken cancellationToken)
    {
        Guid? accountId = _currentUser.AccountId;
        if (accountId is null)
        {
            return ProfileErrors.Unauthenticated;
        }

        DomainProfile? profile = await _profiles.GetByAccountIdAsync(accountId.Value, cancellationToken);

        if (profile is null)
        {
            // First read for this account (e.g. account predating profiles): create it. The unique index on
            // account_id is the DB-level idempotency guarantee — a concurrent double-create loses at commit,
            // never producing a second row.
            profile = DomainProfile.CreateForAccount(Guid.NewGuid(), accountId.Value, _clock.UtcNow);
            await _profiles.AddAsync(profile, cancellationToken);
        }
        else
        {
            // Read-repair: migrate an older-schema record up to the current version, preserving data (ADR-007).
            // The upgraded aggregate is tracked, so the transaction persists it.
            profile.Upgrade(_clock.UtcNow);
        }

        return Result.Success(ProfileMapping.ToDto(profile));
    }
}
