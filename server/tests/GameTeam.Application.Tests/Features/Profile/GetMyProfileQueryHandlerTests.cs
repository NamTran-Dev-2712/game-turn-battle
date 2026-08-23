using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Application.Abstractions.Security;
using GameTeam.Application.Features.Profile.Queries;
using GameTeam.Contracts.Profile;
using GameTeam.Domain.Common;
using GameTeam.Domain.Profiles;
using NSubstitute;
using Xunit;

namespace GameTeam.Application.Tests.Features.Profile;

/// <summary>
/// Phase 19 — <see cref="GetMyProfileQueryHandler"/>: pure read of the caller's own profile (owner from
/// token), <c>PROFILE_NOT_FOUND</c> when absent, never creates.
/// </summary>
public sealed class GetMyProfileQueryHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 23, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Returns_unauthenticated_when_no_account_on_token()
    {
        var profiles = Substitute.For<IPlayerProfileRepository>();
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.AccountId.Returns((Guid?)null);

        var handler = new GetMyProfileQueryHandler(profiles, currentUser);

        Result<ProfileDto> result = await handler.Handle(new GetMyProfileQuery(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("UNAUTHENTICATED");
    }

    [Fact]
    public async Task Returns_not_found_when_owner_has_no_profile()
    {
        Guid accountId = Guid.NewGuid();
        var profiles = Substitute.For<IPlayerProfileRepository>();
        profiles.GetByAccountIdAsync(accountId, Arg.Any<CancellationToken>()).Returns((PlayerProfile?)null);
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.AccountId.Returns(accountId);

        var handler = new GetMyProfileQueryHandler(profiles, currentUser);

        Result<ProfileDto> result = await handler.Handle(new GetMyProfileQuery(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PROFILE_NOT_FOUND");
    }

    [Fact]
    public async Task Returns_owner_profile_dto()
    {
        Guid accountId = Guid.NewGuid();
        PlayerProfile profile = PlayerProfile.CreateForAccount(Guid.NewGuid(), accountId, Now);
        var profiles = Substitute.For<IPlayerProfileRepository>();
        profiles.GetByAccountIdAsync(accountId, Arg.Any<CancellationToken>()).Returns(profile);
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.AccountId.Returns(accountId);

        var handler = new GetMyProfileQueryHandler(profiles, currentUser);

        Result<ProfileDto> result = await handler.Handle(new GetMyProfileQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PlayerId.Should().Be(accountId.ToString());
        result.Value.SchemaVersion.Should().Be(PlayerProfile.CurrentSchemaVersion);
    }
}
