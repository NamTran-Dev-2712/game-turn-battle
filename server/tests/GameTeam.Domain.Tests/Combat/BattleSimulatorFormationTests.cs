using System.Linq;
using FluentAssertions;
using GameTeam.Domain.Combat;
using GameTeam.Domain.Combat.Effects;
using GameTeam.Domain.Combat.Events;
using GameTeam.Domain.Combat.Model;
using GameTeam.Domain.Combat.Serialization;
using Xunit;

namespace GameTeam.Domain.Tests.Combat;

/// <summary>
/// Chứng minh <b>vị trí formation ảnh hưởng sim</b> (Phase 29, ADR-011): cùng hero/chỉ số/seed, chỉ đổi
/// <c>slot</c> đội hình ⇒ target/aggro khác ⇒ event log khác. Cơ chế đã có sẵn (combat-framework §14):
/// <c>ResolveByRule</c> chọn đơn vị sống slot nhỏ nhất (tie-break actor_id) — KHÔNG phát minh thuật toán mới.
/// </summary>
public class BattleSimulatorFormationTests
{
    private static CombatRules Rules() => new(
        DefConstantK: 300,
        MinDamage: 1,
        CritMultiplierFixed: 1500,
        AccuracyBp: 10000,
        CritRateBp: 0,
        MaxRounds: 1,
        Energy: new EnergyRules(0, 0, 0, 100, 100));

    private static SkillDef Basic => new("skill_basic", 1000, "default", new[] { new EffectDef(DamageEffectHandler.TypeName) });

    // Hai ally cùng hero/chỉ số, chỉ khác actor_id + slot; một enemy nhắm ally slot nhỏ nhất.
    private static BattleInput InputWithAllySlots(int slotA, int slotB) => new(
        "config@v1",
        12345UL,
        new StageInfo("stage_test", 1),
        new[]
        {
            new UnitSnapshot("ally_a", "hero_sample", "ally", slotA, new UnitStats(1000, 100, 50, 100)),
            new UnitSnapshot("ally_b", "hero_sample", "ally", slotB, new UnitStats(1000, 100, 50, 100)),
        },
        new[]
        {
            new UnitSnapshot("enemy_0", "hero_sample", "enemy", 0, new UnitStats(5000, 300, 50, 90)),
        },
        Rules(),
        Basic);

    [Fact]
    public void Enemy_targets_front_slot_ally_so_swapping_slots_changes_target()
    {
        // Đội hình 1: A ở tuyến đầu (slot 0). Đội hình 2: B ở tuyến đầu (slot 0). Chỉ khác vị trí.
        BattleOutput frontA = new BattleSimulator().Simulate(InputWithAllySlots(slotA: 0, slotB: 1));
        BattleOutput frontB = new BattleSimulator().Simulate(InputWithAllySlots(slotA: 1, slotB: 0));

        string EnemyTarget(BattleOutput o) => o.EventLog
            .OfType<TargetSelected>()
            .First(t => t.Actor == "enemy_0")
            .Target;

        EnemyTarget(frontA).Should().Be("ally_a", "enemy nhắm ally slot nhỏ nhất (tuyến đầu)");
        EnemyTarget(frontB).Should().Be("ally_b", "đổi vị trí ⇒ enemy nhắm ally khác");
    }

    [Fact]
    public void Same_heroes_same_seed_different_formation_produces_different_event_log()
    {
        // Cùng hero, cùng chỉ số, cùng seed — CHỈ khác slot ⇒ log phải khác (aggro theo vị trí).
        string logFrontA = CombatEventSerializer.Serialize(
            new BattleSimulator().Simulate(InputWithAllySlots(slotA: 0, slotB: 1)));
        string logFrontB = CombatEventSerializer.Serialize(
            new BattleSimulator().Simulate(InputWithAllySlots(slotA: 1, slotB: 0)));

        logFrontA.Should().NotBe(logFrontB, "vị trí formation đi vào target/aggro ⇒ input khác → kết quả khác");
    }

    [Fact]
    public void Identical_formation_is_deterministic()
    {
        // Đối chứng: cùng slot ⇒ log trùng (khác biệt ở test trên đến từ slot, không phải nhiễu).
        string first = CombatEventSerializer.Serialize(
            new BattleSimulator().Simulate(InputWithAllySlots(slotA: 0, slotB: 1)));
        string second = CombatEventSerializer.Serialize(
            new BattleSimulator().Simulate(InputWithAllySlots(slotA: 0, slotB: 1)));

        first.Should().Be(second);
    }
}
