using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using GameTeam.Contracts.Auth;
using GameTeam.Contracts.Common;
using GameTeam.Contracts.Config;
using GameTeam.Contracts.Profile;
using Xunit;

namespace GameTeam.Contracts.Tests;

/// <summary>
/// Round-trip serialize/deserialize DTO nền (docs/roadmap/05-shared-contracts-skeleton.md).
/// Dùng cấu hình JSON khớp Api (Web defaults = camelCase + JsonStringEnumConverter) để
/// khẳng định đúng hình dạng dây (wire) mà client codegen sẽ thấy.
/// </summary>
public class SerializationTests
{
    private static readonly JsonSerializerOptions Options =
        new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    private static T RoundTrip<T>(T value)
    {
        string json = JsonSerializer.Serialize(value, Options);
        return JsonSerializer.Deserialize<T>(json, Options)!;
    }

    [Fact]
    public void AuthGuestRequest_round_trips()
    {
        AuthGuestRequest original = new("device-123");
        RoundTrip(original).Should().Be(original);

        JsonSerializer.Serialize(original, Options).Should().Contain("\"deviceId\"");
    }

    [Fact]
    public void AuthGuestResponse_round_trips()
    {
        AuthGuestResponse original = new("access", "refresh", 3600);
        RoundTrip(original).Should().Be(original);

        string json = JsonSerializer.Serialize(original, Options);
        json.Should().Contain("\"accessToken\"").And.Contain("\"expiresInSeconds\"");
    }

    [Fact]
    public void ProfileDto_round_trips()
    {
        ProfileDto original = new("player-1", "Nam", 42, 1);
        RoundTrip(original).Should().Be(original);

        JsonSerializer.Serialize(original, Options).Should().Contain("\"schemaVersion\"");
    }

    [Fact]
    public void ConfigVersion_round_trips()
    {
        ConfigVersion original = new(7, 2);
        RoundTrip(original).Should().Be(original);
    }

    [Fact]
    public void ConfigBundleDto_round_trips_nested_version()
    {
        ConfigBundleDto original = new(new ConfigVersion(7, 2));
        ConfigBundleDto result = RoundTrip(original);

        result.Should().Be(original);
        result.Version.Bundle.Should().Be(7);
        result.Version.Schema.Should().Be(2);
    }

    [Fact]
    public void ErrorResponse_round_trips()
    {
        ErrorResponse original = new("INSUFFICIENT_CURRENCY", "Không đủ gem để summon.", "trace-abc");
        RoundTrip(original).Should().Be(original);
    }

    [Fact]
    public void ErrorResponse_allows_null_trace_id()
    {
        ErrorResponse original = new("VALIDATION_ERROR", "Sai dữ liệu.", null);
        RoundTrip(original).Should().Be(original);
    }

    [Fact]
    public void ErrorEnvelope_wraps_error_in_camel_case()
    {
        ErrorEnvelope original = new(new ErrorResponse("NOT_FOUND", "Không thấy.", "t1"));
        RoundTrip(original).Should().Be(original);

        string json = JsonSerializer.Serialize(original, Options);
        // Hình dạng dây chuẩn hoá: { "error": { "code", "message", "traceId" } }.
        json.Should().Contain("\"error\"")
            .And.Contain("\"code\"")
            .And.Contain("\"message\"")
            .And.Contain("\"traceId\"");
    }

    [Fact]
    public void HealthResponse_round_trips()
    {
        HealthResponse original = new("ok");
        RoundTrip(original).Should().Be(original);

        JsonSerializer.Serialize(original, Options).Should().Be("{\"status\":\"ok\"}");
    }
}
