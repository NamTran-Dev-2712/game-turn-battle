using System.Linq;
using FluentAssertions;
using GameTeam.Application.Combat;
using GameTeam.Domain.Combat;
using GameTeam.Domain.Combat.Events;
using GameTeam.Domain.Combat.Model;
using GameTeam.Domain.Common;
using Xunit;

namespace GameTeam.Application.Tests.Combat;

/// <summary>
/// Chứng minh data-driven (ADR-004): đổi <b>một chỉ số trong config</b> (atk của hero) ⇒ kết quả sim đổi
/// tương ứng, <b>không sửa code combat</b>. Config đọc qua <see cref="IConfigProvider"/> (không hardcode).
/// </summary>
public class CombatDataDrivenTests
{
    private const string StageId = "stage_sample_01";

    private static readonly BattleRequest Request = new(
        Seed: 12345UL,
        StageId: StageId,
        Ally: new[] { new CombatTeamMember("u_ally_01", "hero_ally", 0) });

    private static FakeConfigProvider SeedConfig(int allyAtk)
    {
        var config = new FakeConfigProvider();
        config.Set("hero", "hero_ally", $$"""
            { "hp": 1000, "atk": {{allyAtk}}, "def": 100, "spd": 120 }
            """);
        config.Set("hero", "hero_enemy", """
            { "hp": 500, "atk": 150, "def": 80, "spd": 90 }
            """);
        config.Set("skill", "skill_basic", """
            { "coeff_fixed": 1000, "target_rule": "default", "effects": ["damage"] }
            """);
        config.Set("stage", StageId, """
            {
              "max_rounds": 30,
              "basic_skill_id": "skill_basic",
              "combat_rules": {
                "def_constant_k": 300,
                "min_damage": 1,
                "crit_multiplier_fixed": 1500,
                "accuracy_bp": 10000,
                "crit_rate_bp": 0,
                "energy": { "initial": 0, "on_attack": 0, "on_hit": 0, "ultimate_cost": 100, "max": 100 }
              },
              "enemies": [ { "actor_id": "u_enemy_01", "hero_id": "hero_enemy", "slot": 0 } ]
            }
            """);
        return config;
    }

    private static int FirstDamage(BattleInput input) =>
        new BattleSimulator().Simulate(input).EventLog.OfType<DamageApplied>().First().Amount;

    [Fact]
    public void Resolver_reads_stats_from_config_and_feeds_the_sim()
    {
        var resolver = new CombatInputResolver(SeedConfig(allyAtk: 200));

        Result<BattleInput> resolved = resolver.Resolve(Request);

        resolved.IsSuccess.Should().BeTrue();
        resolved.Value.ConfigVersion.Should().Be("config@v1");
        resolved.Value.Ally.Should().ContainSingle().Which.Stats.Atk.Should().Be(200);
        resolved.Value.Enemy.Should().ContainSingle().Which.ActorId.Should().Be("u_enemy_01");

        // atk=200, def=80, K=300 ⇒ 158 (khớp toán §17).
        FirstDamage(resolved.Value).Should().Be(158);
    }

    [Fact]
    public void Changing_hero_atk_in_config_changes_result_without_code_change()
    {
        BattleInput low = new CombatInputResolver(SeedConfig(allyAtk: 200)).Resolve(Request).Value;
        BattleInput high = new CombatInputResolver(SeedConfig(allyAtk: 400)).Resolve(Request).Value;

        int lowDamage = FirstDamage(low);
        int highDamage = FirstDamage(high);

        lowDamage.Should().Be(158);
        highDamage.Should().Be(316); // atk gấp đôi ⇒ sát thương tăng theo config
        highDamage.Should().BeGreaterThan(lowDamage);

        BattleResult lowResult = new BattleSimulator().Simulate(low).Result;
        BattleResult highResult = new BattleSimulator().Simulate(high).Result;

        // atk cao hơn ⇒ hạ địch nhanh hơn (ít vòng hơn) — kết quả đổi theo dữ liệu.
        highResult.Rounds.Should().BeLessThan(lowResult.Rounds);
        highResult.Outcome.Should().Be("VICTORY");
    }

    [Fact]
    public void Missing_stage_config_returns_failure()
    {
        var resolver = new CombatInputResolver(SeedConfig(allyAtk: 200));

        Result<BattleInput> resolved = resolver.Resolve(Request with { StageId = "stage_unknown" });

        resolved.IsFailure.Should().BeTrue();
        resolved.Error.Code.Should().Be("COMBAT_STAGE_CONFIG_NOT_FOUND");
    }

    [Fact]
    public void Missing_hero_config_returns_failure()
    {
        var resolver = new CombatInputResolver(SeedConfig(allyAtk: 200));

        Result<BattleInput> resolved = resolver.Resolve(
            Request with { Ally = new[] { new CombatTeamMember("u_ally_01", "hero_missing", 0) } });

        resolved.IsFailure.Should().BeTrue();
        resolved.Error.Code.Should().Be("COMBAT_HERO_CONFIG_NOT_FOUND");
    }
}
