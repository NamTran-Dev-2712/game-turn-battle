using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using GameTeam.Domain.Teams;
using Xunit;

namespace GameTeam.Domain.Tests.Teams;

/// <summary>
/// Bất biến cấu trúc của aggregate <see cref="Team"/> (Phase 29). Ràng buộc phụ thuộc config (đúng số ô,
/// slot trong lưới, sở hữu) là tầng Application — không kiểm ở đây.
/// </summary>
public class TeamTests
{
    private static readonly Guid ProfileId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static List<TeamSlot> Slots(params (int slot, string hero)[] items)
        => items.Select(i => new TeamSlot(i.slot, i.hero)).ToList();

    [Fact]
    public void Create_sets_fields_and_raises_event()
    {
        Team team = Team.Create(Guid.NewGuid(), ProfileId, Slots((0, "hero_a"), (1, "hero_b")), Now);

        team.ProfileId.Should().Be(ProfileId);
        team.SchemaVersion.Should().Be(Team.CurrentSchemaVersion);
        team.CreatedAt.Should().Be(Now);
        team.UpdatedAt.Should().Be(Now);
        team.Slots.Select(s => s.HeroId).Should().Equal("hero_a", "hero_b");
        team.DomainEvents.OfType<TeamSaved>().Should().ContainSingle();
    }

    [Fact]
    public void Create_rejects_empty_slots()
    {
        Action act = () => Team.Create(Guid.NewGuid(), ProfileId, new List<TeamSlot>(), Now);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_rejects_duplicate_slot_index()
    {
        Action act = () => Team.Create(Guid.NewGuid(), ProfileId, Slots((0, "hero_a"), (0, "hero_b")), Now);
        act.Should().Throw<ArgumentException>().WithMessage("*SlotIndex*");
    }

    [Fact]
    public void Create_rejects_duplicate_hero()
    {
        Action act = () => Team.Create(Guid.NewGuid(), ProfileId, Slots((0, "hero_a"), (1, "hero_a")), Now);
        act.Should().Throw<ArgumentException>().WithMessage("*Hero*");
    }

    [Fact]
    public void TeamSlot_rejects_negative_index_and_empty_hero()
    {
        Action negative = () => _ = new TeamSlot(-1, "hero_a");
        Action empty = () => _ = new TeamSlot(0, "  ");
        negative.Should().Throw<ArgumentOutOfRangeException>();
        empty.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Replace_swaps_slots_and_bumps_updated_at()
    {
        Team team = Team.Create(Guid.NewGuid(), ProfileId, Slots((0, "hero_a"), (1, "hero_b")), Now);
        DateTimeOffset later = Now.AddMinutes(5);

        team.Replace(Slots((0, "hero_b"), (1, "hero_a")), later);

        team.Slots.OrderBy(s => s.SlotIndex).Select(s => s.HeroId).Should().Equal("hero_b", "hero_a");
        team.UpdatedAt.Should().Be(later);
        team.CreatedAt.Should().Be(Now);
    }

    [Fact]
    public void Replace_rejects_invalid_slots()
    {
        Team team = Team.Create(Guid.NewGuid(), ProfileId, Slots((0, "hero_a"), (1, "hero_b")), Now);
        Action act = () => team.Replace(Slots((0, "hero_a"), (1, "hero_a")), Now);
        act.Should().Throw<ArgumentException>();
    }
}
