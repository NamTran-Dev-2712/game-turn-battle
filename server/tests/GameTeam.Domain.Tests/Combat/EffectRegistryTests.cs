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
    public void Default_registry_resolves_all_base_effect_handlers()
    {
        EffectRegistry registry = EffectRegistry.CreateDefault();
        registry.Resolve(DamageEffectHandler.TypeName).Should().BeOfType<DamageEffectHandler>();
        registry.Resolve(HealEffectHandler.TypeName).Should().BeOfType<HealEffectHandler>();
        registry.Resolve(ApplyBuffEffectHandler.TypeName).Should().BeOfType<ApplyBuffEffectHandler>();
        registry.Resolve(ApplyDebuffEffectHandler.TypeName).Should().BeOfType<ApplyDebuffEffectHandler>();
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

    [Fact]
    public void Heal_handler_emits_healed_event_with_actual_amount()
    {
        UnitState target = Unit("b", "ally", 1000, 150, 80);
        target.ApplyDamage(400); // hp = 600
        var effect = new EffectDef(
            HealEffectHandler.TypeName,
            new Dictionary<string, long>(StringComparer.Ordinal) { [HealEffectHandler.AmountFixedParam] = 100_000 });
        var skill = new SkillDef("skill_heal", 0, "single_ally", new[] { effect });
        var log = new List<CombatEvent>();
        var ctx = new EffectContext(target, target, skill, effect, Rules, isCrit: false, log);

        new HealEffectHandler().Apply(ctx);

        log.Should().ContainSingle(e => e is Healed).Which.Should().BeOfType<Healed>()
            .Which.Amount.Should().Be(100);
    }

    [Fact]
    public void Buff_handler_applies_positive_modifier_and_emits_signed_amount()
    {
        UnitState caster = Unit("a", "ally", 1000, 100, 100);
        UnitState target = Unit("b", "ally", 1000, 100, 100);
        var effect = new EffectDef(
            ApplyBuffEffectHandler.TypeName,
            new Dictionary<string, long>(StringComparer.Ordinal) { ["atk"] = 50, ["duration"] = 2 });
        var skill = new SkillDef("skill_buff", 0, "single_ally", new[] { effect });
        var log = new List<CombatEvent>();
        var ctx = new EffectContext(caster, target, skill, effect, Rules, isCrit: false, log);

        new ApplyBuffEffectHandler().Apply(ctx);

        target.Atk.Should().Be(150); // 100 + 50 (hiệu dụng)
        log.Should().ContainSingle(e => e is BuffApplied).Which.Should().BeOfType<BuffApplied>()
            .Which.Amount.Should().Be(50);
    }

    [Fact]
    public void Debuff_handler_applies_negative_modifier()
    {
        UnitState caster = Unit("a", "ally", 1000, 100, 100);
        UnitState target = Unit("b", "enemy", 1000, 100, 100);
        var effect = new EffectDef(
            ApplyDebuffEffectHandler.TypeName,
            new Dictionary<string, long>(StringComparer.Ordinal) { ["def"] = 40, ["duration"] = 2 });
        var skill = new SkillDef("skill_debuff", 0, "single_enemy", new[] { effect });
        var log = new List<CombatEvent>();
        var ctx = new EffectContext(caster, target, skill, effect, Rules, isCrit: false, log);

        new ApplyDebuffEffectHandler().Apply(ctx);

        target.Def.Should().Be(60); // 100 - 40
        log.Should().ContainSingle(e => e is BuffApplied).Which.Should().BeOfType<BuffApplied>()
            .Which.Amount.Should().Be(-40);
    }

    [Fact]
    public void Reapplying_same_source_and_stat_refreshes_instead_of_stacking()
    {
        UnitState u = Unit("a", "ally", 1000, 100, 100);

        u.ApplyStatModifier("skill_x", StatKind.Atk, 50, 2);
        u.ApplyStatModifier("skill_x", StatKind.Atk, 50, 2); // áp lại cùng khoá ⇒ refresh, KHÔNG chồng

        u.Atk.Should().Be(150); // 100 + 50 (không phải 200)
    }
}
