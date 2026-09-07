using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using GameTeam.Application.Combat;
using GameTeam.Application.Features.Teams;
using GameTeam.Domain.Teams;
using Xunit;
using DomainTeam = GameTeam.Domain.Teams.Team;

namespace GameTeam.Application.Tests.Features.Teams;

/// <summary>
/// Phase 29 — <see cref="TeamSnapshotFactory"/>: đội hình đã lưu → snapshot bất biến (<see cref="CombatTeamMember"/>)
/// feed sim. Vị trí (slot) đi vào snapshot; snapshot là bản sao giá trị, KHÔNG dính state mutable của aggregate.
/// </summary>
public sealed class TeamSnapshotFactoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 6, 9, 0, 0, TimeSpan.Zero);

    private static DomainTeam TeamWith(params (int slot, string hero)[] slots)
        => DomainTeam.Create(Guid.NewGuid(), Guid.NewGuid(),
            slots.Select(s => new TeamSlot(s.slot, s.hero)).ToList(), Now);

    [Fact]
    public void Create_maps_slots_to_combat_members_ordered_by_slot()
    {
        DomainTeam team = TeamWith((1, "hero_b"), (0, "hero_a"));

        IReadOnlyList<CombatTeamMember> members = TeamSnapshotFactory.Create(team);

        members.Select(m => m.Slot).Should().Equal(0, 1);
        members.Select(m => m.HeroId).Should().Equal("hero_a", "hero_b");
        members.Select(m => m.ActorId).Should().Equal("ally_0", "ally_1");
    }

    [Fact]
    public void Different_formation_produces_different_combat_input()
    {
        IReadOnlyList<CombatTeamMember> a = TeamSnapshotFactory.Create(TeamWith((0, "hero_a"), (1, "hero_b")));
        IReadOnlyList<CombatTeamMember> b = TeamSnapshotFactory.Create(TeamWith((1, "hero_a"), (0, "hero_b")));

        // Cùng hero, khác vị trí ⇒ ánh xạ (hero → slot) khác ⇒ đầu vào sim khác (vị trí ảnh hưởng aggro).
        HeroAtSlot(a, "hero_a").Should().Be(0);
        HeroAtSlot(b, "hero_a").Should().Be(1);
        a.Should().NotBeEquivalentTo(b);
    }

    [Fact]
    public void Snapshot_is_a_value_copy_decoupled_from_mutable_team()
    {
        DomainTeam team = TeamWith((0, "hero_a"), (1, "hero_b"));
        IReadOnlyList<CombatTeamMember> snapshot = TeamSnapshotFactory.Create(team);

        // Đổi đội hình SAU khi tạo snapshot ⇒ snapshot cũ KHÔNG đổi (không giữ tham chiếu mutable).
        team.Replace(new List<TeamSlot> { new(0, "hero_c"), new(1, "hero_d") }, Now.AddMinutes(1));

        snapshot.Select(m => m.HeroId).Should().Equal("hero_a", "hero_b");
    }

    private static int HeroAtSlot(IReadOnlyList<CombatTeamMember> members, string heroId)
        => members.Single(m => m.HeroId == heroId).Slot;
}
