using System.Net;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace GameTeam.Api.IntegrationTests;

/// <summary>
/// Phase 13 — server-time proves HTTP → MediatR (GetServerTimeQuery) → Application → Infrastructure
/// clock (IClock) DI wiring. With the fake <see cref="FixedClock"/> the response is deterministic,
/// which also confirms the endpoint reads time through the port (not <c>DateTime.UtcNow</c>).
/// Phase 18 — <c>/server-time</c> is now protected, so the test authenticates with a valid guest token.
/// </summary>
public class ServerTimeEndpointTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;

    public ServerTimeEndpointTests(ApiTestFactory factory) => _factory = factory;

    [Fact]
    public async Task Server_time_returns_clock_value()
    {
        HttpClient client = _factory.CreateAuthenticatedClient();

        HttpResponseMessage response = await client.GetAsync("/api/v1/server-time");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        DateTimeOffset utcNow = doc.RootElement.GetProperty("utcNow").GetDateTimeOffset();
        utcNow.Should().Be(ApiTestFactory.FixedNow);
    }
}
