using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Application.Abstractions.Security;
using GameTeam.Application.Features.Heroes.Queries;
using GameTeam.Contracts.Hero;
using GameTeam.Domain.Common;
using GameTeam.Domain.Heroes;
using GameTeam.Domain.Profiles;
using NSubstitute;
using Xunit;

namespace GameTeam.Application.Tests.Features.Heroes;

/// <summary>
/// Phase 27 — <see cref="GetMyHeroesQueryHandler"/>: owner resolved ONLY from the token (server-authoritative,
/// IDOR-safe), returns just the caller's owned heroes, empty when the caller has no profile.
/// </summary>
public sealed class GetMyHeroesQueryHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 5, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Returns_unauthenticated_when_no_account_on_token()
    {
        var handler = new GetMyHeroesQueryHandler(
            Substitute.For<IOwnedHeroRepository>(),
            Substitute.For<IPlayerProfileRepository>(),
            CurrentUser(null));

        Result<MyHeroesResponse> result =
            await handler.Handle(new GetMyHeroesQuery(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("UNAUTHENTICATED");
    }

    [Fact]
    public async Task Returns_empty_when_caller_has_no_profile()
    {
        Guid accountId = Guid.NewGuid();
        var profiles = Substitute.For<IPlayerProfileRepository>();
        profiles.GetByAccountIdAsync(accountId, Arg.Any<CancellationToken>()).Returns((PlayerProfile?)null);

        var handler = new GetMyHeroesQueryHandler(
            Substitute.For<IOwnedHeroRepository>(), profiles, CurrentUser(accountId));

        Result<MyHeroesResponse> result =
            await handler.Handle(new GetMyHeroesQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Heroes.Should().BeEmpty();
    }

    [Fact]
    public async Task Returns_only_the_owners_heroes_resolved_by_token_profile()
    {
        Guid accountId = Guid.NewGuid();
        PlayerProfile profile = PlayerProfile.CreateForAccount(Guid.NewGuid(), accountId, Now);

        var profiles = Substitute.For<IPlayerProfileRepository>();
        profiles.GetByAccountIdAsync(accountId, Arg.Any<CancellationToken>()).Returns(profile);

        var ownedHeroes = Substitute.For<IOwnedHeroRepository>();
        ownedHeroes.GetByProfileIdAsync(profile.Id, Arg.Any<CancellationToken>()).Returns(new List<OwnedHero>
        {
            OwnedHero.Grant(Guid.NewGuid(), profile.Id, "hero_ignis", 1, 1, Now),
            OwnedHero.Grant(Guid.NewGuid(), profile.Id, "hero_aqua", 2, 3, Now),
        });

        var handler = new GetMyHeroesQueryHandler(ownedHeroes, profiles, CurrentUser(accountId));

        Result<MyHeroesResponse> result =
            await handler.Handle(new GetMyHeroesQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Heroes.Should().BeEquivalentTo(new[]
        {
            new OwnedHeroDto("hero_ignis", 1, 1),
            new OwnedHeroDto("hero_aqua", 2, 3),
        });

        // Ownership is server-resolved: heroes are loaded ONLY for the token-owner's profile.
        await ownedHeroes.Received(1).GetByProfileIdAsync(profile.Id, Arg.Any<CancellationToken>());
    }

    private static ICurrentUser CurrentUser(Guid? accountId)
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.AccountId.Returns(accountId);
        return currentUser;
    }
}
