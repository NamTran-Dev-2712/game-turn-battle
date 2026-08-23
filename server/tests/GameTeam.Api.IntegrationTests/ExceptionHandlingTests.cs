using System.Net;
using System.Text.Json;
using FluentAssertions;
using GameTeam.Domain.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace GameTeam.Api.IntegrationTests;

/// <summary>
/// Phase 13 — an UNHANDLED exception thrown inside the pipeline must become a 500
/// <see cref="GameTeam.Contracts.Common.ErrorEnvelope"/> via <c>GlobalExceptionHandler</c>: generic
/// code, a traceId for log lookup, and NO leaked internals (no stack trace, exception type, or the
/// internal exception message). The exception is injected via a throwing <see cref="IClock"/> — a
/// real pipeline fault, with no production code modified.
/// </summary>
public class ExceptionHandlingTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;

    public ExceptionHandlingTests(ApiTestFactory factory) => _factory = factory;

    [Fact]
    public async Task Unhandled_exception_returns_500_error_envelope_without_leaking_internals()
    {
        using WebApplicationFactory<Program> factory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IClock>();
                services.AddSingleton<IClock, ThrowingClock>();
            }));

        HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        // /server-time is protected (Phase 18) — attach a valid token so the request reaches the
        // (throwing) handler and exercises the 500 path, not the 401 auth path.
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", ApiTestFactory.CreateGuestBearerToken());

        HttpResponseMessage response = await client.GetAsync("/api/v1/server-time");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

        string raw = await response.Content.ReadAsStringAsync();
        using JsonDocument doc = JsonDocument.Parse(raw);
        JsonElement error = doc.RootElement.GetProperty("error");
        error.GetProperty("code").GetString().Should().Be("INTERNAL_ERROR");
        error.GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();

        // No internal leakage.
        raw.Should().NotContain(ThrowingClock.SecretDetail);
        raw.Should().NotContain("InvalidOperationException");
        raw.Should().NotContain("at GameTeam."); // stack frame marker
    }
}
