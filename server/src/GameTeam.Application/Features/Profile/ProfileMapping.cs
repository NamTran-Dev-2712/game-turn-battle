using GameTeam.Contracts.Profile;
using DomainProfile = GameTeam.Domain.Profiles.PlayerProfile;

namespace GameTeam.Application.Features.Profile;

/// <summary>
/// Maps the <see cref="DomainProfile"/> aggregate to the wire <see cref="ProfileDto"/> (Phase 05 contract).
/// The EF entity is never returned from the API — only this read-model projection. <c>PlayerId</c> is the
/// owning account id (the JWT <c>sub</c>), so the client can correlate its own identity.
/// </summary>
internal static class ProfileMapping
{
    public static ProfileDto ToDto(DomainProfile profile)
        => new(
            profile.AccountId.ToString(),
            profile.DisplayName,
            profile.Level,
            profile.SchemaVersion);
}
