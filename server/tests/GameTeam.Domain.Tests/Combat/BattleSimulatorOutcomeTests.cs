using System.Linq;
using FluentAssertions;
using GameTeam.Domain.Combat;
using GameTeam.Domain.Combat.Effects;
using GameTeam.Domain.Combat.Events;
using GameTeam.Domain.Combat.Model;
using Xunit;

namespace GameTeam.Domain.Tests.Combat;

/// <summary>Xác minh các nhánh outcome §19 + hành vi miss §16 mà 2 golden vector không phủ.</summary>
public class BattleSimulatorOutcomeTests
{
    private static CombatRules Rules(int accuracyBp = 10000, int critRateBp = 0, int maxRounds = 30) => new(
        DefConstantK: 300,
        MinDamage: 1,
        CritMultiplierFixed: 1500,
        AccuracyBp: accuracyBp,
        CritRateBp: critRateBp,
        MaxRounds: maxRounds,
        Energy: new EnergyRules(0, 0, 0, 100, 100));

    private static UnitSnapshot Unit(string id, string team, int hp, int atk, int def, int spd) =>
        new(id, "hero_sample", team, 0, new UnitStats(hp, atk, def, spd));

    private static SkillDef Basic => new("skill_basic", 1000, "default", new[] { new EffectDef(DamageEffectHandler.TypeName) });

    private static BattleInput Input(UnitSnapshot ally, UnitSnapshot enemy, CombatRules rules) =>
        new("config@v1", 12345UL, new StageInfo("stage_test", rules.MaxRounds), new[] { ally }, new[] { enemy }, rules, Basic);

    [Fact]
    public void Defeat_when_ally_wiped_and_enemy_alive()
    {
        // Enemy nhanh hơn + rất mạnh, ally rất yếu ⇒ enemy giết ally trước.
        BattleInput input = Input(
            Unit("u_ally_01", "ally", hp: 10, atk: 10, def: 0, spd: 50),
            Unit("u_enemy_01", "enemy", hp: 10000, atk: 5000, def: 100, spd: 200),
            Rules());

        BattleResult result = new BattleSimulator().Simulate(input).Result;

        result.Outcome.Should().Be("DEFEAT");
        result.WinnerTeam.Should().Be("enemy");
    }

    [Fact]
    public void Draw_when_max_rounds_reached_with_both_alive()
    {
        // Cả hai quá trâu, 1 vòng không ai chết ⇒ DRAW.
        BattleInput input = Input(
            Unit("u_ally_01", "ally", hp: 100000, atk: 1, def: 100000, spd: 100),
            Unit("u_enemy_01", "enemy", hp: 100000, atk: 1, def: 100000, spd: 90),
            Rules(maxRounds: 1));

        BattleResult result = new BattleSimulator().Simulate(input).Result;

        result.Outcome.Should().Be("DRAW");
        result.WinnerTeam.Should().BeNull();
        result.Rounds.Should().Be(1);
    }

    [Fact]
    public void Miss_consumes_one_roll_and_deals_no_damage()
    {
        // accuracy_bp = 0 ⇒ luôn trượt (roll >= 0 luôn đúng) ⇒ không sát thương, không roll crit.
        BattleInput input = Input(
            Unit("u_ally_01", "ally", hp: 1000, atk: 200, def: 100, spd: 120),
            Unit("u_enemy_01", "enemy", hp: 500, atk: 150, def: 80, spd: 90),
            Rules(accuracyBp: 0, maxRounds: 1));

        BattleOutput output = new BattleSimulator().Simulate(input);

        output.EventLog.Should().Contain(e => e is Miss);
        output.EventLog.Should().NotContain(e => e is DamageApplied);
        // Mỗi hành động: đúng 1 RandomRoll "hit", KHÔNG có "crit".
        output.EventLog.OfType<RandomRoll>().Should().OnlyContain(r => r.Purpose == "hit");
        output.Result.Outcome.Should().Be("DRAW");
    }

    [Fact]
    public void Turn_order_is_by_speed_then_actor_id()
    {
        // Ally spd thấp hơn ⇒ enemy hành động trước trong vòng 1.
        BattleInput input = Input(
            Unit("u_ally_01", "ally", hp: 1000, atk: 10, def: 100, spd: 50),
            Unit("u_enemy_01", "enemy", hp: 1000, atk: 10, def: 100, spd: 200),
            Rules(maxRounds: 1));

        BattleOutput output = new BattleSimulator().Simulate(input);

        ActionStarted first = output.EventLog.OfType<ActionStarted>().First();
        first.Actor.Should().Be("u_enemy_01");
    }
}
