using FluentAssertions;
using GameTeam.Domain.Combat.Numerics;
using Xunit;

namespace GameTeam.Domain.Tests.Combat;

public class FixedPointTests
{
    [Fact]
    public void ToFixed_scales_by_1000()
    {
        FixedPoint.FixedScale.Should().Be(1000L);
        FixedPoint.ToFixed(200).Should().Be(200_000L);
        FixedPoint.ToFixed(0).Should().Be(0L);
    }

    [Theory]
    [InlineData(0, 1000, 0)]
    [InlineData(157_800, 1000, 158)] // round-half-up: 157.8 → 158
    [InlineData(157_500, 1000, 158)] // đúng 0.5 → làm tròn LÊN
    [InlineData(157_499, 1000, 157)] // < 0.5 → xuống
    [InlineData(157_400, 1000, 157)]
    public void FromFixed_rounds_half_up(long fixedValue, long ignoredScaleSanity, long expected)
    {
        _ = ignoredScaleSanity;
        FixedPoint.FromFixed(fixedValue).Should().Be(expected);
    }

    [Fact]
    public void Mul_and_Div_match_spec_worked_example_vector01()
    {
        // atk=200, coeff=1.0 → raw=200000; ratio = K/(K+def) = 300/(300+80)
        long raw = FixedPoint.Mul(FixedPoint.ToFixed(200), 1000);
        raw.Should().Be(200_000L);

        long ratio = FixedPoint.Div(FixedPoint.ToFixed(300), FixedPoint.ToFixed(300) + FixedPoint.ToFixed(80));
        ratio.Should().Be(789L); // fixed_div(300000, 380000) = 789

        long dmgFixed = FixedPoint.Mul(200_000L, 789L);
        dmgFixed.Should().Be(157_800L); // fixed_mul(200000, 789)

        FixedPoint.FromFixed(dmgFixed).Should().Be(158L);
    }

    [Fact]
    public void Div_supports_crit_example_vector02()
    {
        // atk=260 → raw=260000; ratio=300/(300+50)=857; dmg=260000*857=222820 → crit ×1.5 → 334230 → 334
        long ratio = FixedPoint.Div(FixedPoint.ToFixed(300), FixedPoint.ToFixed(300) + FixedPoint.ToFixed(50));
        ratio.Should().Be(857L);

        long dmgFixed = FixedPoint.Mul(FixedPoint.ToFixed(260), ratio);
        dmgFixed.Should().Be(222_820L);

        long crit = FixedPoint.Mul(dmgFixed, 1500);
        crit.Should().Be(334_230L);
        FixedPoint.FromFixed(crit).Should().Be(334L);
    }

    [Fact]
    public void Clamp_applies_floor()
    {
        FixedPoint.Clamp(0, 1, long.MaxValue).Should().Be(1L);
        FixedPoint.Clamp(158, 1, 158).Should().Be(158L);
    }

    [Fact]
    public void RoundHalfUp_rejects_zero_denominator()
    {
        Action act = () => FixedPoint.RoundHalfUp(10, 0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void RoundHalfUp_rejects_negative_operand()
    {
        Action act = () => FixedPoint.RoundHalfUp(-1, 1000);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ToFixed_rejects_negative()
    {
        Action act = () => FixedPoint.ToFixed(-5);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Handles_large_values_without_overflow()
    {
        // atk cỡ lớn vẫn nằm trong long (không cần 128-bit ở MVP).
        long raw = FixedPoint.Mul(FixedPoint.ToFixed(1_000_000), 1500);
        raw.Should().Be(1_500_000_000L);
        FixedPoint.FromFixed(raw).Should().Be(1_500_000L);
    }
}
