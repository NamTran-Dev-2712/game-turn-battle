using System.Linq;
using FluentAssertions;
using GameTeam.Application.Combat;
using GameTeam.Domain.Combat.Model;
using GameTeam.Domain.Common;
using Xunit;

namespace GameTeam.Application.Tests.Combat;

/// <summary>
/// Phase 35 — combat integration của Level: <see cref="CombatInputResolver"/> tính lại chỉ số ally theo cấp
/// (data-driven từ economy config, ADR-004) trong khi địch giữ chỉ số nền (không chủ sở hữu). Đổi đường cong
/// tăng trưởng trong config ⇒ chỉ số vào trận đổi, KHÔNG sửa code (ADR-004); cấp 1 ⇒ chỉ số nền.
/// </summary>
public class CombatLevelScalingTests
{
    private const string StageId = "stage_sample_01";

    private static FakeConfigProvider SeedConfig(int? growthBp)
    {
        var config = new FakeConfigProvider();
        config.Set("hero", "hero_ally", """
            { "base_stats": { "hp": 1000, "atk": 200, "def": 100, "spd": 120 }, "skills": ["skill_basic"] }
            """);
        config.Set("hero", "hero_enemy", """
            { "base_stats": { "hp": 500, "atk": 150, "def": 80, "spd": 90 }, "skills": ["skill_basic"] }
            """);
        config.Set("skill", "skill_basic", """
            { "target": "single_enemy", "trigger": { "type": "cooldown", "value": 0 },
              "effects": [ { "effect_type": "damage", "params": { "coeff_fixed": 1000 } } ] }
            """);
        config.Set("stage", StageId, """
            {
              "max_rounds": 30,
              "basic_skill_id": "skill_basic",
              "combat_rules": {
                "def_constant_k": 300, "min_damage": 1, "crit_multiplier_fixed": 1500,
                "accuracy_bp": 10000, "crit_rate_bp": 0,
                "energy": { "initial": 0, "on_attack": 0, "on_hit": 0, "ultimate_cost": 100, "max": 100 }
              },
              "enemies": [ { "hero_id": "hero_enemy", "slot": 0 } ]
            }
            """);
        if (growthBp is not null)
        {
            config.Set("economy", "economy_default", $$"""
                { "cost_curves": { "level_up": [100, 150, 220] },
                  "level_stat_growth_bp": {{growthBp}},
                  "power_weights": { "hp": 1, "atk": 10, "def": 8, "spd": 6 } }
                """);
        }

        return config;
    }

    private static BattleRequest RequestAtLevel(int level)
        => new(12345UL, StageId, new[] { new CombatTeamMember("u_ally_01", "hero_ally", 0, level) });

    [Fact]
    public void Ally_stats_are_scaled_by_level_from_config()
    {
        Result<BattleInput> resolved = new CombatInputResolver(SeedConfig(growthBp: 800)).Resolve(RequestAtLevel(3));

        resolved.IsSuccess.Should().BeTrue();
        UnitSnapshot ally = resolved.Value.Ally.Single();
        // ScaleStat(base, level=3, bp=800) = base + round_half_up(base*800*2/10000)
        ally.Stats.Atk.Should().Be(232); // 200 + 32
        ally.Stats.Hp.Should().Be(1160); // 1000 + 160

        // Địch giữ chỉ số nền (không chủ sở hữu ⇒ cấp 1).
        resolved.Value.Enemy.Single().Stats.Atk.Should().Be(150);
    }

    [Fact]
    public void Ally_at_level_1_uses_base_stats_even_without_economy_config()
    {
        Result<BattleInput> resolved = new CombatInputResolver(SeedConfig(growthBp: null)).Resolve(RequestAtLevel(1));

        resolved.IsSuccess.Should().BeTrue();
        UnitSnapshot ally = resolved.Value.Ally.Single();
        ally.Stats.Atk.Should().Be(200);
        ally.Stats.Hp.Should().Be(1000);
    }

    [Fact]
    public void Changing_growth_curve_in_config_changes_combat_stats_without_code_change()
    {
        int scaledAtk = new CombatInputResolver(SeedConfig(growthBp: 800))
            .Resolve(RequestAtLevel(3)).Value.Ally.Single().Stats.Atk;
        int flatAtk = new CombatInputResolver(SeedConfig(growthBp: 0))
            .Resolve(RequestAtLevel(3)).Value.Ally.Single().Stats.Atk;

        scaledAtk.Should().Be(232);
        flatAtk.Should().Be(200); // growth 0 ⇒ chỉ số nền dù cấp 3
        scaledAtk.Should().BeGreaterThan(flatAtk);
    }
}
