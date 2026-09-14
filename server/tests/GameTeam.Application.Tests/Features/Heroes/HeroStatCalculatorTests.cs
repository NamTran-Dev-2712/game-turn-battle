using System.Collections.Generic;
using FluentAssertions;
using GameTeam.Application.Features.Economy;
using GameTeam.Application.Features.Heroes;
using Xunit;

namespace GameTeam.Application.Tests.Features.Heroes;

/// <summary>
/// Phase 35 — công thức chỉ số theo cấp + Power Rating (<see cref="HeroStatCalculator"/>): tất định, integer
/// (ADR-011), data-driven (đường cong/tăng trưởng/trọng số từ config, ADR-004). Kết quả phải khớp bản mirror
/// client (<c>client/tests/combat/hero_stats_test.gd</c>).
/// </summary>
public class HeroStatCalculatorTests
{
    [Fact]
    public void ScaleStat_at_level_1_returns_base_regardless_of_growth()
    {
        HeroStatCalculator.ScaleStat(100, 1, 800).Should().Be(100);
        HeroStatCalculator.ScaleStat(1000, 1, 0).Should().Be(1000);
    }

    [Theory]
    // base + round_half_up(base * bp * (level-1) / 10000)
    [InlineData(100, 2, 800, 108)] // 100 + round(80000/10000=8) = 108
    [InlineData(100, 3, 800, 116)] // 100 + round(160000/10000=16) = 116
    [InlineData(7, 2, 800, 8)]     // 7 + round(5600/10000=0.56 → 1) = 8 (round-half-up)
    [InlineData(200, 5, 1000, 280)] // 200 + round(200*1000*4/10000=80) = 280
    public void ScaleStat_applies_config_growth_deterministically(int baseStat, int level, int growthBp, int expected)
        => HeroStatCalculator.ScaleStat(baseStat, level, growthBp).Should().Be(expected);

    [Fact]
    public void ScaleStat_is_deterministic_for_same_input()
    {
        int a = HeroStatCalculator.ScaleStat(137, 9, 850);
        int b = HeroStatCalculator.ScaleStat(137, 9, 850);
        a.Should().Be(b);
    }

    [Fact]
    public void Power_is_weighted_integer_sum_of_final_stats()
    {
        var weights = new PowerWeightsConfig { Hp = 1, Atk = 10, Def = 8, Spd = 6 };
        // 1000*1 + 200*10 + 100*8 + 120*6 = 1000 + 2000 + 800 + 720 = 4520
        HeroStatCalculator.Power(1000, 200, 100, 120, weights).Should().Be(4520);
    }

    [Fact]
    public void MaxLevel_is_one_plus_level_up_curve_length()
    {
        EconomyConfig economy = EconomyWith([100, 150, 220], growthBp: 800);
        HeroStatCalculator.MaxLevel(economy).Should().Be(4);
    }

    [Fact]
    public void MaxLevel_is_1_when_no_curve()
    {
        HeroStatCalculator.MaxLevel(EconomyWith([], growthBp: 0)).Should().Be(1);
    }

    [Fact]
    public void TryGetLevelUpCost_returns_curve_step_for_current_level()
    {
        EconomyConfig economy = EconomyWith([100, 150, 220], growthBp: 800);

        HeroStatCalculator.TryGetLevelUpCost(economy, 1, out long c1).Should().BeTrue();
        c1.Should().Be(100);
        HeroStatCalculator.TryGetLevelUpCost(economy, 3, out long c3).Should().BeTrue();
        c3.Should().Be(220);
    }

    [Fact]
    public void TryGetLevelUpCost_returns_false_at_max_level()
    {
        EconomyConfig economy = EconomyWith([100, 150, 220], growthBp: 800);

        // max level = 4; leveling from level 4 has no step.
        HeroStatCalculator.TryGetLevelUpCost(economy, 4, out long cost).Should().BeFalse();
        cost.Should().Be(0);
    }

    private static EconomyConfig EconomyWith(int[] levelUp, int growthBp)
        => new()
        {
            Id = "economy_default",
            CostCurves = new Dictionary<string, List<int>> { [EconomyConfig.LevelUpCurve] = [.. levelUp] },
            LevelStatGrowthBp = growthBp,
            PowerWeights = new PowerWeightsConfig { Hp = 1, Atk = 10, Def = 8, Spd = 6 },
        };
}
