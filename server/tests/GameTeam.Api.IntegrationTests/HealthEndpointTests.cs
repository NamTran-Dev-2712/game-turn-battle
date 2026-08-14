using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace GameTeam.Api.IntegrationTests;

/// <summary>
/// Integration test bootstrap — xác nhận host API khởi động & health endpoint trả OK.
/// Test API nghiệp vụ thêm ở phase Core Framework trở đi.
/// </summary>
public class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthEndpointTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Health_endpoint_returns_ok()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Phase 12 — graceful degradation: khi Redis KHÔNG truy cập được, API vẫn phục vụ (HTTP 200) và
    /// healthcheck báo <c>degraded</c> (không sập). Trỏ Redis vào endpoint chết để deterministic.
    /// </summary>
    [Fact]
    public async Task Health_reports_degraded_when_redis_unreachable()
    {
        using WebApplicationFactory<Program> factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting(
                "ConnectionStrings:Redis",
                "127.0.0.1:6399,abortConnect=false,connectRetry=0,connectTimeout=500");
        });

        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        string body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("degraded");
    }
}
