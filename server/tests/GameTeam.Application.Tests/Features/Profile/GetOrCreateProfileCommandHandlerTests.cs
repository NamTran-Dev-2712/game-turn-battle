using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Application.Abstractions.Security;
using GameTeam.Application.Features.Profile.Commands;
using GameTeam.Application.Tests.TestSupport;
using GameTeam.Contracts.Profile;
using GameTeam.Domain.Common;
using GameTeam.Domain.Profiles;
using NSubstitute;
using Xunit;

namespace GameTeam.Application.Tests.Features.Profile;

/// <summary>
/// Phase 19 — <see cref="GetOrCreateProfileCommandHandler"/>: owner resolved from the token
/// (<see cref="ICurrentUser"/>) only, get-or-create semantics, and read-repair migration on a stale record.
/// </summary>
public sealed class GetOrCreateProfileCommandHandlerTests
{
    private static readonly FixedClock Clock =
        new(new DateTimeOffset(2026, 8, 23, 9, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task Returns_unauthenticated_when_no_account_on_token()
    {
        var profiles = Substitute.For<IPlayerProfileRepository>();
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.AccountId.Returns((Guid?)null);

        var handler = new GetOrCreateProfileCommandHandler(profiles, currentUser, Clock);

        Result<ProfileDto> result = await handler.Handle(new GetOrCreateProfileCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("UNAUTHENTICATED");
        await profiles.DidNotReceive().AddAsync(Arg.Any<PlayerProfile>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Creates_profile_for_authenticated_account_when_none_exists()
    {
        Guid accountId = Guid.NewGuid();
        var profiles = Substitute.For<IPlayerProfileRepository>();
        profiles.GetByAccountIdAsync(accountId, Arg.Any<CancellationToken>())
            .Returns((PlayerProfile?)null);
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.AccountId.Returns(accountId);

        var handler = new GetOrCreateProfileCommandHandler(profiles, currentUser, Clock);

        Result<ProfileDto> result = await handler.Handle(new GetOrCreateProfileCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PlayerId.Should().Be(accountId.ToString());
        result.Value.SchemaVersion.Should().Be(PlayerProfile.CurrentSchemaVersion);
        await profiles.Received(1).AddAsync(
            Arg.Is<PlayerProfile>(p => p.AccountId == accountId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Returns_existing_profile_of_owner_without_creating()
    {
        Guid accountId = Guid.NewGuid();
        PlayerProfile existing = PlayerProfile.CreateForAccount(Guid.NewGuid(), accountId, Clock.UtcNow);
        var profiles = Substitute.For<IPlayerProfileRepository>();
        profiles.GetByAccountIdAsync(accountId, Arg.Any<CancellationToken>()).Returns(existing);
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.AccountId.Returns(accountId);

        var handler = new GetOrCreateProfileCommandHandler(profiles, currentUser, Clock);

        Result<ProfileDto> result = await handler.Handle(new GetOrCreateProfileCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PlayerId.Should().Be(accountId.ToString());
        await profiles.DidNotReceive().AddAsync(Arg.Any<PlayerProfile>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Owner_is_read_only_from_token_never_from_repository_by_other_id()
    {
        // The handler must query the repository with the token's account id — never any other value.
        Guid tokenAccount = Guid.NewGuid();
        var profiles = Substitute.For<IPlayerProfileRepository>();
        profiles.GetByAccountIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((PlayerProfile?)null);
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.AccountId.Returns(tokenAccount);

        var handler = new GetOrCreateProfileCommandHandler(profiles, currentUser, Clock);

        await handler.Handle(new GetOrCreateProfileCommand(), CancellationToken.None);

        await profiles.Received(1).GetByAccountIdAsync(tokenAccount, Arg.Any<CancellationToken>());
    }
}
