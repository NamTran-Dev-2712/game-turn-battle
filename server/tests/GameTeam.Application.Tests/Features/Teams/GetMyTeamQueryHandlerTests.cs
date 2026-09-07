using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Application.Abstractions.Security;
using GameTeam.Application.Features.Teams.Queries;
using GameTeam.Contracts.Team;
using GameTeam.Domain.Common;
using GameTeam.Domain.Profiles;
using GameTeam.Domain.Teams;
using NSubstitute;
using Xunit;
using DomainTeam = GameTeam.Domain.Teams.Team;

namespace GameTeam.Application.Tests.Features.Teams;

/// <summary>
/// Phase 29 — <see cref="GetMyTeamQueryHandler"/>: owner resolved ONLY from token (IDOR-safe); chưa có
/// profile/đội ⇒ đội rỗng (không lỗi); có đội ⇒ trả đúng slot.
/// </summary>
public sealed class GetMyTeamQueryHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 6, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Returns_unauthenticated_when_no_account()
    {
        var handler = new GetMyTeamQueryHandler(
            Substitute.For<ITeamRepository>(),
            Substitute.For<IPlayerProfileRepository>(),
            CurrentUser(null));

        Result<TeamDto> result = await handler.Handle(new GetMyTeamQuery(), CancellationToken.None);

        result.Error.Code.Should().Be("UNAUTHENTICATED");
    }

    [Fact]
    public async Task Returns_empty_when_no_profile()
    {
        Guid accountId = Guid.NewGuid();
        var profiles = Substitute.For<IPlayerProfileRepository>();
        profiles.GetByAccountIdAsync(accountId, Arg.Any<CancellationToken>()).Returns((PlayerProfile?)null);

        var handler = new GetMyTeamQueryHandler(Substitute.For<ITeamRepository>(), profiles, CurrentUser(accountId));

        Result<TeamDto> result = await handler.Handle(new GetMyTeamQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Slots.Should().BeEmpty();
    }

    [Fact]
    public async Task Returns_empty_when_no_team_saved()
    {
        Guid accountId = Guid.NewGuid();
        PlayerProfile profile = PlayerProfile.CreateForAccount(Guid.NewGuid(), accountId, Now);
        var profiles = Substitute.For<IPlayerProfileRepository>();
        profiles.GetByAccountIdAsync(accountId, Arg.Any<CancellationToken>()).Returns(profile);
        var teams = Substitute.For<ITeamRepository>();
        teams.GetByProfileIdAsync(profile.Id, Arg.Any<CancellationToken>()).Returns((DomainTeam?)null);

        var handler = new GetMyTeamQueryHandler(teams, profiles, CurrentUser(accountId));

        Result<TeamDto> result = await handler.Handle(new GetMyTeamQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Slots.Should().BeEmpty();
    }

    [Fact]
    public async Task Returns_saved_team_slots_ordered()
    {
        Guid accountId = Guid.NewGuid();
        PlayerProfile profile = PlayerProfile.CreateForAccount(Guid.NewGuid(), accountId, Now);
        DomainTeam team = DomainTeam.Create(
            Guid.NewGuid(), profile.Id,
            new List<TeamSlot> { new(1, "hero_b"), new(0, "hero_a") },
            Now);

        var profiles = Substitute.For<IPlayerProfileRepository>();
        profiles.GetByAccountIdAsync(accountId, Arg.Any<CancellationToken>()).Returns(profile);
        var teams = Substitute.For<ITeamRepository>();
        teams.GetByProfileIdAsync(profile.Id, Arg.Any<CancellationToken>()).Returns(team);

        var handler = new GetMyTeamQueryHandler(teams, profiles, CurrentUser(accountId));

        Result<TeamDto> result = await handler.Handle(new GetMyTeamQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Slots.Select(s => s.SlotIndex).Should().Equal(0, 1);
        result.Value.Slots.Select(s => s.HeroId).Should().Equal("hero_a", "hero_b");
    }

    private static ICurrentUser CurrentUser(Guid? accountId)
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.AccountId.Returns(accountId);
        return currentUser;
    }
}
