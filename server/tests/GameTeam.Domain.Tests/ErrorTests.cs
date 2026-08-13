using FluentAssertions;
using GameTeam.Domain.Common;
using Xunit;

namespace GameTeam.Domain.Tests;

public class ErrorTests
{
    [Fact]
    public void Stores_code_and_message()
    {
        var error = new Error("INSUFFICIENT_CURRENCY", "Không đủ tiền.");

        error.Code.Should().Be("INSUFFICIENT_CURRENCY");
        error.Message.Should().Be("Không đủ tiền.");
    }

    [Fact]
    public void Same_values_are_equal_with_consistent_hash()
    {
        var a = new Error("X", "m");
        var b = new Error("X", "m");

        a.Should().Be(b);
        (a == b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Different_code_is_not_equal()
    {
        new Error("A", "m").Should().NotBe(new Error("B", "m"));
    }

    [Fact]
    public void None_is_empty_code_and_message()
    {
        Error.None.Code.Should().BeEmpty();
        Error.None.Message.Should().BeEmpty();
    }
}
