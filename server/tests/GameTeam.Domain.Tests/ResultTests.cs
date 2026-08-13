using System;
using FluentAssertions;
using GameTeam.Domain.Common;
using Xunit;

namespace GameTeam.Domain.Tests;

public class ResultTests
{
    [Fact]
    public void Success_is_successful_and_has_no_error()
    {
        Result result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().Be(Error.None);
    }

    [Fact]
    public void Failure_is_not_successful_and_carries_error()
    {
        var error = new Error("E", "m");

        Result result = Result.Failure(error);

        result.IsFailure.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void Failure_with_none_error_throws()
    {
        Action act = () => Result.Failure(Error.None);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Generic_success_exposes_value()
    {
        Result<int> result = Result.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Generic_failure_value_access_throws()
    {
        Result<int> result = Result.Failure<int>(new Error("E", "m"));

        result.IsFailure.Should().BeTrue();
        Action act = () => _ = result.Value;
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Error_implicitly_converts_to_failure()
    {
        Result<string> result = new Error("E", "m");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E");
    }
}
