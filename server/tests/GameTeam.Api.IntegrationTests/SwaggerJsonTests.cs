using System.Net;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace GameTeam.Api.IntegrationTests;

/// <summary>
/// Phase 13 — the OpenAPI/Swagger JSON (served first-party at <c>/openapi/v1.json</c>, the same doc
/// the dev Swagger UI renders) must return 200 and describe the v1 sample endpoints. This is the
/// contract the Swagger UI and client codegen consume.
/// </summary>
public class SwaggerJsonTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;

    public SwaggerJsonTests(ApiTestFactory factory) => _factory = factory;

    [Fact]
    public async Task Swagger_json_is_served_and_describes_v1_sample_endpoints()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/openapi/v1.json");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement paths = doc.RootElement.GetProperty("paths");

        paths.TryGetProperty("/api/v1/ping", out _).Should().BeTrue();
        paths.TryGetProperty("/api/v1/server-time", out _).Should().BeTrue();
    }
}
