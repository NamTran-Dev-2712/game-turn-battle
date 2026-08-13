using System.Collections.Generic;
using FluentAssertions;
using GameTeam.Domain.Common;
using Xunit;

namespace GameTeam.Domain.Tests;

public class ValueObjectTests
{
    private sealed class Money(int amount, string? currency) : ValueObject
    {
        public int Amount { get; } = amount;

        public string? Currency { get; } = currency;

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Amount;
            yield return Currency;
        }
    }

    [Fact]
    public void Same_components_are_equal_with_consistent_hash()
    {
        var a = new Money(100, "GOLD");
        var b = new Money(100, "GOLD");

        a.Should().Be(b);
        (a == b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Different_components_are_not_equal()
    {
        new Money(100, "GOLD").Should().NotBe(new Money(100, "GEM"));
        new Money(100, "GOLD").Should().NotBe(new Money(50, "GOLD"));
        (new Money(100, "GOLD") != new Money(50, "GOLD")).Should().BeTrue();
    }

    [Fact]
    public void Null_component_is_handled()
    {
        var a = new Money(100, null);
        var b = new Money(100, null);

        a.Should().Be(b);
        a.Should().NotBe(new Money(100, "GOLD"));
    }

    [Fact]
    public void Null_is_not_equal()
    {
        new Money(1, "GOLD").Equals(null).Should().BeFalse();
    }
}
