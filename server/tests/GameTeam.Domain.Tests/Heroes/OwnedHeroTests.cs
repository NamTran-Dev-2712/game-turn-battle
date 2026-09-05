using System;
using System.Linq;
using FluentAssertions;
using GameTeam.Domain.Heroes;
using Xunit;

namespace GameTeam.Domain.Tests.Heroes;

/// <summary>
/// Phase 27 — the <see cref="OwnedHero"/> aggregate: grant factory (identity/profile/hero + base level/star),
/// domain event, guards, and restore-without-event.
/// </summary>
public class OwnedHeroTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 5, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Grant_sets_identity_profile_hero_and_base_level_star()
    {
        Guid id = Guid.NewGuid();
        Guid profileId = Guid.NewGuid();

        OwnedHero hero = OwnedHero.Grant(id, profileId, "hero_ignis", OwnedHero.InitialLevel, OwnedHero.InitialStars, Now);

        hero.Id.Should().Be(id);
        hero.ProfileId.Should().Be(profileId);
        hero.HeroId.Should().Be("hero_ignis");
        hero.Level.Should().Be(OwnedHero.InitialLevel);
        hero.Stars.Should().Be(OwnedHero.InitialStars);
        hero.CreatedAt.Should().Be(Now);
    }

    [Fact]
    public void Grant_raises_OwnedHeroGranted_event()
    {
        Guid id = Guid.NewGuid();
        Guid profileId = Guid.NewGuid();

        OwnedHero hero = OwnedHero.Grant(id, profileId, "hero_aqua", 1, 1, Now);

        hero.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<OwnedHeroGranted>();
        var granted = (OwnedHeroGranted)hero.DomainEvents.Single();
        granted.OwnedHeroId.Should().Be(id);
        granted.ProfileId.Should().Be(profileId);
        granted.HeroId.Should().Be("hero_aqua");
    }

    [Fact]
    public void Grant_rejects_empty_ids_blank_hero_and_nonpositive_level_star()
    {
        Guid profileId = Guid.NewGuid();

        FluentActions.Invoking(() => OwnedHero.Grant(Guid.Empty, profileId, "hero_a", 1, 1, Now))
            .Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => OwnedHero.Grant(Guid.NewGuid(), Guid.Empty, "hero_a", 1, 1, Now))
            .Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => OwnedHero.Grant(Guid.NewGuid(), profileId, "  ", 1, 1, Now))
            .Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => OwnedHero.Grant(Guid.NewGuid(), profileId, "hero_a", 0, 1, Now))
            .Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => OwnedHero.Grant(Guid.NewGuid(), profileId, "hero_a", 1, 0, Now))
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Restore_rebuilds_state_without_raising_event()
    {
        Guid id = Guid.NewGuid();
        Guid profileId = Guid.NewGuid();

        OwnedHero hero = OwnedHero.Restore(id, profileId, "hero_ignis", 5, 3, Now);

        hero.Id.Should().Be(id);
        hero.ProfileId.Should().Be(profileId);
        hero.HeroId.Should().Be("hero_ignis");
        hero.Level.Should().Be(5);
        hero.Stars.Should().Be(3);
        hero.DomainEvents.Should().BeEmpty();
    }
}
