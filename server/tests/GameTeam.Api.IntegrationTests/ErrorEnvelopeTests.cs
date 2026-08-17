using System.Net;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace GameTeam.Api.IntegrationTests;

/// <summary>
/// Phase 13 — the error body must match the Phase-05 contract EXACTLY: a single <c>error</c> envelope
/// wrapping <c>{ code, message, traceId }</c> and nothing else. No leaking/extra fields (no stack,
/// exception type, DB detail…). Guards against a second/ad-hoc error shape creeping in.
/// </summary>
public class ErrorEnvelopeTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;

    public ErrorEnvelopeTests(ApiTestFactory factory) => _factory = factory;

    [Fact]
    public async Task Error_body_is_exactly_the_error_envelope_contract()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/v1/ping?message=");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");

        using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        // Top level has ONLY "error".
        doc.RootElement.EnumerateObject().Select(p => p.Name).Should().Equal("error");

        // Inner object has EXACTLY code + message + traceId.
        JsonElement error = doc.RootElement.GetProperty("error");
        string[] fields = error.EnumerateObject().Select(p => p.Name).ToArray();
        fields.Should().HaveCount(3);
        fields.Should().Contain("code").And.Contain("message").And.Contain("traceId");
    }
}
