using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using GameTeam.Application.Features.Summon;
using Xunit;

namespace GameTeam.Application.Tests.Features.Summon;

/// <summary>
/// Phase 33 — the pure RNG + rate + pity core (<see cref="SummonRoller"/>). Deterministic by seed, data-driven
/// by config: rate distribution follows the configured weights (within tolerance); pity fires exactly at the
/// configured threshold and resets on the target; changing config changes behaviour with NO code change.
/// </summary>
public sealed class SummonRollerTests
{
    private static IReadOnlyDictionary<int, IReadOnlyList<string>> Heroes(params int[] rarities)
        => rarities.ToDictionary(r => r, r => (IReadOnlyList<string>)[$"hero_r{r}"]);

    private static GachaConfig Banner(
        IEnumerable<GachaRate> rates, GachaPityConfig? pity = null)
        => new()
        {
            Id = "gacha_test",
            Pool = ["hero_r3", "hero_r4", "hero_r5"],
            Rates = rates.ToList(),
            Pity = pity,
        };

    // ── A — rate distribution follows configured weights (data-driven) ─────────────────────────────
    [Theory]
    [InlineData(70, 25, 5)]
    [InlineData(50, 30, 20)]
    public void Distribution_matches_configured_weights_within_tolerance(int w3, int w4, int w5)
    {
        GachaConfig banner = Banner(
            [new GachaRate { Rarity = 3, Weight = w3 }, new GachaRate { Rarity = 4, Weight = w4 }, new GachaRate { Rarity = 5, Weight = w5 }]);
        const int n = 40000;

        SummonRoller.SummonRollOutcome outcome = SummonRoller.Roll(
            banner, Heroes(3, 4, 5), startingPity: 0, count: n, seed: 0xC0FFEEUL);

        double total = w3 + w4 + w5;
        foreach ((int rarity, int weight) in new[] { (3, w3), (4, w4), (5, w5) })
        {
            double observed = outcome.Pulls.Count(p => p.Rarity == rarity) / (double)n;
            observed.Should().BeApproximately(weight / total, 0.02,
                $"rarity {rarity} phải xấp xỉ weight config ({weight}/{total}) — không hardcode trong code");
        }
    }

    [Fact]
    public void Same_seed_is_deterministic()
    {
        GachaConfig banner = Banner([new GachaRate { Rarity = 3, Weight = 60 }, new GachaRate { Rarity = 5, Weight = 40 }]);

        SummonRoller.SummonRollOutcome a = SummonRoller.Roll(banner, Heroes(3, 5), 0, 20, 12345UL);
        SummonRoller.SummonRollOutcome b = SummonRoller.Roll(banner, Heroes(3, 5), 0, 20, 12345UL);

        a.Pulls.Select(p => p.Rarity).Should().Equal(b.Pulls.Select(p => p.Rarity));
        a.EndingPity.Should().Be(b.EndingPity);
    }

    // ── B — pity threshold boundary (t-1 not forced, t forced) + reset ─────────────────────────────
    [Theory]
    [InlineData(1, false)] // startingPity = threshold-2 ⇒ lần quay này CHƯA đảm bảo.
    [InlineData(2, true)]  // startingPity = threshold-1 ⇒ lần quay này (thứ 3 liên tiếp) ĐẢM BẢO.
    [InlineData(3, true)]  // startingPity = threshold   ⇒ vẫn đảm bảo.
    public void Pity_fires_exactly_at_threshold(int startingPity, bool expectForced)
    {
        // rates chỉ có rarity 3 ⇒ natural luôn ra 3; pity mục tiêu 5 (chỉ đạt được qua pity).
        var pity = new GachaPityConfig { Enabled = true, Threshold = 3, TargetRarity = 5 };
        GachaConfig banner = Banner([new GachaRate { Rarity = 3, Weight = 100 }], pity);

        SummonRoller.SummonRollOutcome outcome = SummonRoller.Roll(banner, Heroes(3, 5), startingPity, count: 1, seed: 1UL);

        SummonRoller.RolledPull pull = outcome.Pulls.Single();
        pull.WasPity.Should().Be(expectForced);
        pull.Rarity.Should().Be(expectForced ? 5 : 3);
        outcome.EndingPity.Should().Be(expectForced ? 0 : startingPity + 1, "trúng mục tiêu reset về 0; trượt tăng 1");
    }

    [Fact]
    public void Natural_target_hit_resets_pity_without_being_flagged_as_pity()
    {
        // rates chỉ có rarity 5 ⇒ natural luôn ra 5 (= mục tiêu) ⇒ reset, KHÔNG phải do pity.
        var pity = new GachaPityConfig { Enabled = true, Threshold = 10, TargetRarity = 5 };
        GachaConfig banner = Banner([new GachaRate { Rarity = 5, Weight = 100 }], pity);

        SummonRoller.SummonRollOutcome outcome = SummonRoller.Roll(banner, Heroes(5), startingPity: 7, count: 1, seed: 9UL);

        SummonRoller.RolledPull pull = outcome.Pulls.Single();
        pull.Rarity.Should().Be(5);
        pull.WasPity.Should().BeFalse("trúng tự nhiên, không phải do pity đảm bảo");
        outcome.EndingPity.Should().Be(0, "trúng mục tiêu (tự nhiên) cũng reset pity");
    }

    // ── B — 10-pull applies pity PER ROLL in sequence ─────────────────────────────────────────────
    [Fact]
    public void Ten_pull_applies_pity_per_roll_in_sequence()
    {
        var pity = new GachaPityConfig { Enabled = true, Threshold = 3, TargetRarity = 5 };
        GachaConfig banner = Banner([new GachaRate { Rarity = 3, Weight = 100 }], pity);

        SummonRoller.SummonRollOutcome outcome = SummonRoller.Roll(banner, Heroes(3, 5), startingPity: 0, count: 10, seed: 42UL);

        // Đảm bảo tại lần thứ 3/6/9 (index 2/5/8); các lần khác trượt (rarity 3).
        List<int> forcedIndices = outcome.Pulls
            .Select((p, i) => (p.WasPity, i)).Where(x => x.WasPity).Select(x => x.i).ToList();
        forcedIndices.Should().Equal(2, 5, 8);
        outcome.Pulls.Where(p => p.WasPity).Should().OnlyContain(p => p.Rarity == 5);
        outcome.Pulls.Where(p => !p.WasPity).Should().OnlyContain(p => p.Rarity == 3);
        outcome.EndingPity.Should().Be(1, "sau index 8 reset 0, index 9 trượt ⇒ 1");
    }

    [Fact]
    public void Pity_disabled_never_forces()
    {
        var pity = new GachaPityConfig { Enabled = false, Threshold = 3, TargetRarity = 5 };
        GachaConfig banner = Banner([new GachaRate { Rarity = 3, Weight = 100 }], pity);

        SummonRoller.SummonRollOutcome outcome = SummonRoller.Roll(banner, Heroes(3, 5), startingPity: 100, count: 5, seed: 7UL);

        outcome.Pulls.Should().OnlyContain(p => !p.WasPity && p.Rarity == 3);
    }
}
