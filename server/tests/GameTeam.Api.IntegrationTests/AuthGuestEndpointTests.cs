using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using GameTeam.Contracts.Auth;
using GameTeam.Domain.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace GameTeam.Api.IntegrationTests;

/// <summary>
/// Phase 18 — guest auth + default authorization end-to-end (docs/roadmap/18-auth-jwt-guest.md):
/// (A) guest login returns a valid, correctly-claimed JWT; (B) a protected endpoint with no token
/// returns a 401 <see cref="GameTeam.Contracts.Common.ErrorEnvelope"/>; (C) a guest-login token
/// authorizes a protected endpoint; (D) tampered/expired tokens are rejected with 401.
/// </summary>
public class AuthGuestEndpointTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;

    public AuthGuestEndpointTests(ApiTestFactory factory) => _factory = factory;

    /// <summary>
    /// A factory whose server clock is ~now, so a token minted by the guest endpoint is valid under the
    /// real-time JWT lifetime check. (The shared <see cref="FixedClock"/> is intentionally in the past
    /// for the deterministic server-time test, which would otherwise make freshly-issued tokens expired.)
    /// </summary>
    private WebApplicationFactory<Program> RealClockFactory() =>
        _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IClock>();
            services.AddSingleton<IClock>(new FixedClock(DateTimeOffset.UtcNow));
        }));

    // ── Test A ────────────────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task Guest_login_returns_valid_jwt_with_expected_claims()
    {
        using WebApplicationFactory<Program> factory = RealClockFactory();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/guest", new AuthGuestRequest("device-abc"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        AuthGuestResponse? body = await response.Content.ReadFromJsonAsync<AuthGuestResponse>();
        body.Should().NotBeNull();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.RefreshToken.Should().NotBeNullOrWhiteSpace();
        body.ExpiresInSeconds.Should().Be(ApiTestFactory.JwtAccessTokenMinutes * 60);

        // Signature + issuer + audience + lifetime must all validate against the configured key.
        var handler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = ApiTestFactory.JwtIssuer,
            ValidateAudience = true,
            ValidAudience = ApiTestFactory.JwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ApiTestFactory.JwtSigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
        };

        ClaimsPrincipal principal = handler.ValidateToken(
            body.AccessToken, validationParameters, out SecurityToken validated);
        principal.Should().NotBeNull();

        var jwt = (JwtSecurityToken)validated;
        jwt.Subject.Should().NotBeNullOrWhiteSpace();
        Guid.TryParse(jwt.Subject, out _).Should().BeTrue("sub must be the account id");
        jwt.Claims.Should().Contain(c => c.Type == "type" && c.Value == "guest");
        jwt.ValidTo.Should().BeAfter(DateTime.UtcNow, "exp must be in the future");
    }

    // ── Test B ────────────────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task Protected_endpoint_without_token_returns_401_error_envelope()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/v1/server-time");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement error = doc.RootElement.GetProperty("error");
        error.GetProperty("code").GetString().Should().Be("UNAUTHENTICATED");
        error.GetProperty("message").GetString().Should().NotBeNullOrWhiteSpace();
        error.GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    // ── Test C ────────────────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task Guest_login_token_authorizes_protected_endpoint()
    {
        using WebApplicationFactory<Program> factory = RealClockFactory();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage login = await client.PostAsJsonAsync(
            "/api/v1/auth/guest", new AuthGuestRequest(null));
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        AuthGuestResponse body = (await login.Content.ReadFromJsonAsync<AuthGuestResponse>())!;

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body.AccessToken);

        HttpResponseMessage protectedResponse = await client.GetAsync("/api/v1/server-time");

        protectedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Test D ────────────────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task Tampered_token_returns_401()
    {
        HttpClient client = _factory.CreateClient();

        string[] parts = ApiTestFactory.CreateGuestBearerToken().Split('.');
        parts[2] = "invalidsignature"; // valid base64url chars, wrong signature
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", string.Join('.', parts));

        HttpResponseMessage response = await client.GetAsync("/api/v1/server-time");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Expired_token_returns_401()
    {
        HttpClient client = _factory.CreateClient();

        string expired = ApiTestFactory.CreateGuestBearerToken(lifetime: TimeSpan.FromMinutes(-5));
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", expired);

        HttpResponseMessage response = await client.GetAsync("/api/v1/server-time");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
