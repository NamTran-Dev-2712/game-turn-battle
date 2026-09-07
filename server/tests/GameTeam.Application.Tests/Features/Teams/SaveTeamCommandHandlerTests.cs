using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GameTeam.Application.Abstractions.Configuration;
using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Application.Abstractions.Security;
using GameTeam.Application.Features.Teams;
using GameTeam.Application.Features.Teams.Commands;
using GameTeam.Contracts.Team;
using GameTeam.Domain.Common;
using GameTeam.Domain.Heroes;
using GameTeam.Domain.Profiles;
using GameTeam.Domain.Teams;
using NSubstitute;
using Xunit;
using DomainTeam = GameTeam.Domain.Teams.Team;

namespace GameTeam.Application.Tests.Features.Teams;

/// <summary>
/// Phase 29 — <see cref="SaveTeamCommandHandler"/>: server-authoritative validate (đúng số ô theo config,
/// slot hợp lệ, không trùng hero, hero thuộc sở hữu resolved TỪ token) rồi upsert. Client chỉ gửi intent.
/// </summary>
public sealed class SaveTeamCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 6, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid AccountId = Guid.NewGuid();

    private static IReadOnlyList<TeamSlotDto> Six(params string[] heroes)
        => heroes.Select((h, i) => new TeamSlotDto(i, h)).ToList();

    private static readonly string[] SixHeroes =
        { "hero_a", "hero_b", "hero_c", "hero_d", "hero_e", "hero_f" };

    [Fact]
    public async Task Saves_valid_team_and_returns_dto()
    {
        Harness h = Harness.Owning(SixHeroes);
        DomainTeam? captured = null;
        await h.Teams.AddAsync(Arg.Do<DomainTeam>(t => captured = t), Arg.Any<CancellationToken>());

        Result<TeamDto> result = await h.Handle(new SaveTeamCommand(Six(SixHeroes)));

        result.IsSuccess.Should().BeTrue();
        result.Value.Slots.Should().HaveCount(6);
        result.Value.Slots.Select(s => s.HeroId).Should().BeEquivalentTo(SixHeroes);
        await h.Teams.Received(1).AddAsync(Arg.Any<DomainTeam>(), Arg.Any<CancellationToken>());
        captured!.Slots.Should().HaveCount(6);
    }

    [Fact]
    public async Task Replaces_existing_team_without_adding()
    {
        Harness h = Harness.Owning(SixHeroes);
        DomainTeam existing = DomainTeam.Create(
            Guid.NewGuid(), h.Profile.Id,
            SixHeroes.Reverse().Select((hero, i) => new TeamSlot(i, hero)).ToList(),
            Now);
        h.Teams.GetByProfileIdAsync(h.Profile.Id, Arg.Any<CancellationToken>()).Returns(existing);

        Result<TeamDto> result = await h.Handle(new SaveTeamCommand(Six(SixHeroes)));

        result.IsSuccess.Should().BeTrue();
        existing.Slots.OrderBy(s => s.SlotIndex).Select(s => s.HeroId).Should().Equal(SixHeroes);
        await h.Teams.DidNotReceive().AddAsync(Arg.Any<DomainTeam>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rejects_wrong_size()
    {
        Harness h = Harness.Owning(SixHeroes);
        Result<TeamDto> result = await h.Handle(new SaveTeamCommand(Six("hero_a", "hero_b", "hero_c", "hero_d", "hero_e")));
        result.Error.Code.Should().Be("TEAM_INVALID_SIZE");
    }

    [Fact]
    public async Task Rejects_duplicate_hero()
    {
        Harness h = Harness.Owning(SixHeroes);
        IReadOnlyList<TeamSlotDto> dup = Six("hero_a", "hero_b", "hero_c", "hero_d", "hero_e", "hero_a");
        Result<TeamDto> result = await h.Handle(new SaveTeamCommand(dup));
        result.Error.Code.Should().Be("TEAM_DUPLICATE_HERO");
    }

    [Fact]
    public async Task Rejects_hero_not_owned()
    {
        Harness h = Harness.Owning("hero_a", "hero_b", "hero_c", "hero_d", "hero_e"); // thiếu hero_f
        Result<TeamDto> result = await h.Handle(new SaveTeamCommand(Six(SixHeroes)));
        result.Error.Code.Should().Be("TEAM_HERO_NOT_OWNED");
    }

    [Fact]
    public async Task Rejects_slot_out_of_grid()
    {
        Harness h = Harness.Owning(SixHeroes);
        // 6 ô nhưng một slot = 6 (ngoài lưới [0,6)); một ô thiếu (5).
        var slots = new List<TeamSlotDto>
        {
            new(0, "hero_a"), new(1, "hero_b"), new(2, "hero_c"),
            new(3, "hero_d"), new(4, "hero_e"), new(6, "hero_f"),
        };
        Result<TeamDto> result = await h.Handle(new SaveTeamCommand(slots));
        result.Error.Code.Should().Be("TEAM_INVALID_SLOT");
    }

    [Fact]
    public async Task Rejects_unauthenticated()
    {
        Harness h = Harness.Owning(SixHeroes);
        h.CurrentUser.AccountId.Returns((Guid?)null);
        Result<TeamDto> result = await h.Handle(new SaveTeamCommand(Six(SixHeroes)));
        result.Error.Code.Should().Be("UNAUTHENTICATED");
    }

    [Fact]
    public async Task Rejects_when_no_profile()
    {
        Harness h = Harness.Owning(SixHeroes);
        h.Profiles.GetByAccountIdAsync(AccountId, Arg.Any<CancellationToken>()).Returns((PlayerProfile?)null);
        Result<TeamDto> result = await h.Handle(new SaveTeamCommand(Six(SixHeroes)));
        result.Error.Code.Should().Be("PROFILE_NOT_FOUND");
    }

    [Fact]
    public async Task Rejects_when_formation_config_missing()
    {
        Harness h = Harness.Owning(SixHeroes);
        h.Config.Get<FormationConfig>(Arg.Any<string>(), Arg.Any<string>()).Returns((FormationConfig?)null);
        Result<TeamDto> result = await h.Handle(new SaveTeamCommand(Six(SixHeroes)));
        result.Error.Code.Should().Be("FORMATION_CONFIG_MISSING");
    }

    private sealed class Harness
    {
        public ITeamRepository Teams { get; } = Substitute.For<ITeamRepository>();
        public IOwnedHeroRepository OwnedHeroes { get; } = Substitute.For<IOwnedHeroRepository>();
        public IPlayerProfileRepository Profiles { get; } = Substitute.For<IPlayerProfileRepository>();
        public IConfigProvider Config { get; } = Substitute.For<IConfigProvider>();
        public ICurrentUser CurrentUser { get; } = Substitute.For<ICurrentUser>();
        public PlayerProfile Profile { get; } = PlayerProfile.CreateForAccount(Guid.NewGuid(), AccountId, Now);

        public static Harness Owning(params string[] ownedHeroIds)
        {
            var h = new Harness();
            h.CurrentUser.AccountId.Returns(AccountId);
            h.Profiles.GetByAccountIdAsync(AccountId, Arg.Any<CancellationToken>()).Returns(h.Profile);
            h.Config.Get<FormationConfig>("formation", "formation_default")
                .Returns(new FormationConfig { Id = "formation_default", Rows = 2, Cols = 3 });
            h.Teams.GetByProfileIdAsync(h.Profile.Id, Arg.Any<CancellationToken>()).Returns((DomainTeam?)null);
            h.OwnedHeroes.GetByProfileIdAsync(h.Profile.Id, Arg.Any<CancellationToken>())
                .Returns(ownedHeroIds.Select(id => OwnedHero.Grant(Guid.NewGuid(), h.Profile.Id, id, 1, 1, Now)).ToList());
            return h;
        }

        public Task<Result<TeamDto>> Handle(SaveTeamCommand command)
        {
            var clock = Substitute.For<IClock>();
            clock.UtcNow.Returns(Now);
            var handler = new SaveTeamCommandHandler(Teams, OwnedHeroes, Profiles, Config, CurrentUser, clock);
            return handler.Handle(command, CancellationToken.None);
        }
    }
}
