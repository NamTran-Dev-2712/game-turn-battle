using FluentAssertions;
using GameTeam.Domain.Combat.Rng;
using Xunit;

namespace GameTeam.Domain.Tests.Combat;

public class Pcg32Tests
{
    [Fact]
    public void Seed_12345_matches_golden_vector01_first_rolls()
    {
        var rng = new Pcg32(12345UL);
        rng.Bounded(10000).Should().Be(7329U); // hit
        rng.Bounded(10000).Should().Be(4605U); // crit
        rng.Bounded(10000).Should().Be(1261U); // next hit
        rng.Bounded(10000).Should().Be(2745U); // next crit
    }

    [Fact]
    public void Seed_999_matches_golden_vector02_first_rolls()
    {
        var rng = new Pcg32(999UL);
        rng.Bounded(10000).Should().Be(8003U); // hit
        rng.Bounded(10000).Should().Be(8884U); // crit
        rng.Bounded(10000).Should().Be(2400U); // next hit
        rng.Bounded(10000).Should().Be(33U);   // next crit
    }

    [Fact]
    public void Same_seed_produces_same_sequence()
    {
        var a = new Pcg32(42UL);
        var b = new Pcg32(42UL);
        for (int i = 0; i < 1000; i++)
        {
            a.NextUInt32().Should().Be(b.NextUInt32());
        }
    }

    [Fact]
    public void Different_seed_diverges()
    {
        var a = new Pcg32(1UL);
        var b = new Pcg32(2UL);
        bool anyDifferent = false;
        for (int i = 0; i < 20; i++)
        {
            if (a.NextUInt32() != b.NextUInt32())
            {
                anyDifferent = true;
            }
        }

        anyDifferent.Should().BeTrue("seed khác nhau phải cho stream khác nhau");
    }

    [Fact]
    public void Bounded_rejects_zero()
    {
        var rng = new Pcg32(7UL);
        Action act = () => rng.Bounded(0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Bounded_stays_within_range()
    {
        var rng = new Pcg32(123UL);
        for (int i = 0; i < 5000; i++)
        {
            rng.Bounded(10000).Should().BeInRange(0U, 9999U);
        }
    }
}
