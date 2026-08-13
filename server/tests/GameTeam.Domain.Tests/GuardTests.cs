using System;
using FluentAssertions;
using GameTeam.Domain.Common;
using Xunit;

namespace GameTeam.Domain.Tests;

public class GuardTests
{
    [Fact]
    public void NotNull_returns_value_when_not_null()
    {
        var value = new object();

        Guard.NotNull(value).Should().BeSameAs(value);
    }

    [Fact]
    public void NotNull_throws_when_null()
    {
        Action act = () => Guard.NotNull<object>(null);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Positive_returns_value_when_positive()
    {
        Guard.Positive(5).Should().Be(5);
        Guard.Positive(1L).Should().Be(1L);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Positive_throws_when_not_positive(int value)
    {
        Action act = () => Guard.Positive(value);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void InRange_returns_value_when_in_range()
    {
        Guard.InRange(5, 1, 10).Should().Be(5);
        Guard.InRange(1, 1, 10).Should().Be(1);
        Guard.InRange(10, 1, 10).Should().Be(10);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void InRange_throws_when_out_of_range(int value)
    {
        Action act = () => Guard.InRange(value, 1, 10);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
