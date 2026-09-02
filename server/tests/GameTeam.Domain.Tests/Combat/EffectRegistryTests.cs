using System.Collections.Generic;
using FluentAssertions;
using GameTeam.Domain.Combat.Effects;
using GameTeam.Domain.Combat.Events;
using GameTeam.Domain.Combat.Model;
using GameTeam.Domain.Combat.State;
using Xunit;

namespace GameTeam.Domain.Tests.Combat;

public class EffectRegistryTests
{
    private static readonly CombatRules Rules = new(
        DefConstantK: 300,
        MinDamage: 1,
        CritMultiplierFixed: 1500,
        AccuracyBp: 10000,
        CritRateBp: 0,
        MaxRounds: 30,
        Energy: new EnergyRules(0, 0, 0, 100, 100));

    private static UnitState Unit(string id, string team, int hp, int atk, int def) =>
        new(new UnitSnapshot(id, "hero_sample", team, 0, new UnitStats(hp, atk, def, 100)), 0);

    [Fact]
    public void Default_registry_resolves_damage_and_heal()
    {
        EffectRegistry registry = EffectRegistry.CreateDefault();
        registry.Resolve(DamageEffectHandler.TypeName).Should().BeOfType<DamageEffectHandler>();
        registry.Resolve(HealEffectHandler.TypeName).Should().BeOfType<HealEffectHandler>();
        registry.Has("damage").Should().BeTrue();
    }

    [Fact]
    public void Unknown_effect_type_throws_defined_contract()
    {
        EffectRegistry registry = EffectRegistry.CreateDefault();
        Action act = () => registry.Resolve("teleport");
        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void Duplicate_handler_registration_throws()
    {
        Action act = () => _ = new EffectRegistry(new IEffectHandler[]
        {
            new DamageEffectHandler(),
            new DamageEffectHandler(),
        });
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Damage_handler_applies_expected_damage_and_emits_events()
    {
        UnitState attacker = Unit("a", "ally", 1000, 200, 100);
        UnitState target = Unit("b", "enemy", 500, 150, 80);
        var skill = new SkillDef("skill_basic", 1000, "default", new[] { new EffectDef(DamageEffectHandler.TypeName) });
        var log = new List<CombatEvent>();
        var ctx = new EffectContext(attacker, target, skill, skill.Effects[0], Rules, isCrit: false, log);

        new DamageEffectHandler().Apply(ctx);

        target.Hp.Should().Be(342); // 500 - 158
        log.Should().ContainSingle(e => e is DamageApplied)
            .Which.Should().BeOfType<DamageApplied>()
            .Which.Amount.Should().Be(158);
    }

    [Fact]
    public void Damage_handler_emits_death_when_hp_reaches_zero()
    {
        UnitState attacker = Unit("a", "ally", 1000, 200, 100);
        UnitState target = Unit("b", "enemy", 100, 150, 80);
        var skill = new SkillDef("skill_basic", 1000, "default", new[] { new EffectDef(DamageEffectHandler.TypeName) });
        var log = new List<CombatEvent>();
        var ctx = new EffectContext(attacker, target, skill, skill.Effects[0], Rules, isCrit: false, log);

        new DamageEffectHandler().Apply(ctx);

        target.Hp.Should().Be(0);
        log.Should().Contain(e => e is Death);
    }

    [Fact]
    public void Heal_handler_restores_hp_from_config_param()
    {
        UnitState attacker = Unit("a", "ally", 1000, 200, 100);
        UnitState target = Unit("b", "ally", 1000, 150, 80);
        target.ApplyDamage(400); // hp = 600
        var effect = new EffectDef(
            HealEffectHandler.TypeName,
            new Dictionary<string, long>(StringComparer.Ordinal) { [HealEffectHandler.AmountFixedParam] = 100_000 });
        var skill = new SkillDef("skill_heal", 1000, "default", new[] { effect });
        var log = new List<CombatEvent>();
        var ctx = new EffectContext(attacker, target, skill, effect, Rules, isCrit: false, log);

        new HealEffectHandler().Apply(ctx);

        target.Hp.Should().Be(700); // +100
    }
}
