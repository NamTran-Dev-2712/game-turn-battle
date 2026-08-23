using System.Net;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace GameTeam.Api.IntegrationTests;

/// <summary>
/// Phase 13 — the versioned sample endpoint proves HTTP → MediatR (PingCommand) → Result → HTTP.
/// Also proves the centralized Result-failure → ErrorEnvelope mapping (empty message ⇒ validation
/// failure ⇒ 400) and that versioned routing under <c>/api/v1</c> is actually in effect.
/// Phase 18 — <c>/ping</c> is now protected by the default authorization policy, so these tests
/// authenticate with a valid guest bearer token (missing-token behavior is covered by the auth tests).
/// </summary>
public class PingEndpointTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;

    public PingEndpointTests(ApiTestFactory factory) => _factory = factory;

    [Fact]
    public async Task Ping_on_versioned_route_returns_ok()
    {
        HttpClient client = _factory.CreateAuthenticatedClient();

        HttpResponseMessage response = await client.GetAsync("/api/v1/ping");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Ping_with_empty_message_returns_validation_error_envelope()
    {
        HttpClient client = _factory.CreateAuthenticatedClient();

        // Present-but-empty query value ⇒ PingCommand("") ⇒ NotEmpty validator fails ⇒ VALIDATION_FAILED.
        HttpResponseMessage response = await client.GetAsync("/api/v1/ping?message=");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement error = doc.RootElement.GetProperty("error");
        error.GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");
        error.GetProperty("message").GetString().Should().NotBeNullOrWhiteSpace();
        error.GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();
    }
}
