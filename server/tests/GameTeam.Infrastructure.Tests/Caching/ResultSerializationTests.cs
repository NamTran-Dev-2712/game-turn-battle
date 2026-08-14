using System.Text.Json;
using FluentAssertions;
using GameTeam.Domain.Common;
using GameTeam.Infrastructure.Serialization;
using Xunit;

namespace GameTeam.Infrastructure.Tests.Caching;

/// <summary>
/// Unit test converter <see cref="ResultJsonConverterFactory"/> (Phase 12) — chứng minh <c>Result</c>/
/// <c>Result&lt;T&gt;</c> round-trip qua <see cref="CacheSerialization.Options"/> (STJ mặc định KHÔNG
/// deserialize được các kiểu bất biến này). Nhanh, không cần container.
/// </summary>
public sealed class ResultSerializationTests
{
    private static readonly JsonSerializerOptions Options = CacheSerialization.Options;

    [Fact]
    public void Result_success_round_trips()
    {
        Result original = Result.Success();

        string json = JsonSerializer.Serialize(original, Options);
        Result? restored = JsonSerializer.Deserialize<Result>(json, Options);

        restored.Should().NotBeNull();
        restored!.IsSuccess.Should().BeTrue();
        restored.Error.Should().Be(Error.None);
    }

    [Fact]
    public void Result_failure_round_trips_error()
    {
        Result original = Result.Failure(new Error("SOME_CODE", "human message"));

        string json = JsonSerializer.Serialize(original, Options);
        Result? restored = JsonSerializer.Deserialize<Result>(json, Options);

        restored.Should().NotBeNull();
        restored!.IsFailure.Should().BeTrue();
        restored.Error.Code.Should().Be("SOME_CODE");
        restored.Error.Message.Should().Be("human message");
    }

    [Fact]
    public void Result_of_T_success_round_trips_value()
    {
        Result<Payload> original = Result.Success(new Payload("alpha", 123));

        string json = JsonSerializer.Serialize(original, Options);
        Result<Payload>? restored = JsonSerializer.Deserialize<Result<Payload>>(json, Options);

        restored.Should().NotBeNull();
        restored!.IsSuccess.Should().BeTrue();
        restored.Value.Name.Should().Be("alpha");
        restored.Value.Number.Should().Be(123);
    }

    [Fact]
    public void Result_of_T_failure_round_trips_as_failure()
    {
        Result<Payload> original = Result.Failure<Payload>(new Error("BAD", "nope"));

        string json = JsonSerializer.Serialize(original, Options);
        Result<Payload>? restored = JsonSerializer.Deserialize<Result<Payload>>(json, Options);

        restored.Should().NotBeNull();
        restored!.IsFailure.Should().BeTrue();
        restored.Error.Code.Should().Be("BAD");
    }

    public sealed record Payload(string Name, int Number);
}
