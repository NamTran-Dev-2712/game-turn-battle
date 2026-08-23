using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using GameTeam.Application.Abstractions.Caching;
using GameTeam.Contracts.Auth;
using GameTeam.Contracts.Profile;
using GameTeam.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using Xunit;

namespace GameTeam.Api.IntegrationTests;

/// <summary>
/// Phase 19 — profile persistence end-to-end over the real HTTP host + real PostgreSQL (Testcontainers):
/// guest login creates the owner's profile; <c>GET /api/v1/profile</c> returns the caller's own profile;
/// retry is idempotent (one row per account); and one user can never read another's profile (ownership
/// from the token <c>sub</c> only). Requires Docker.
/// </summary>
public sealed class ProfileEndpointTests : IClassFixture<ProfilePostgresApiFactory>
{
    private readonly ProfilePostgresApiFactory _factory;

    public ProfileEndpointTests(ProfilePostgresApiFactory factory) => _factory = factory;

    private static string SubjectOf(string accessToken)
        => new JwtSecurityTokenHandler().ReadJwtToken(accessToken).Subject;

    private static async Task<string> LoginGuestAsync(HttpClient client)
    {
        HttpResponseMessage login = await client.PostAsJsonAsync("/api/v1/auth/guest", new AuthGuestRequest(null));
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        AuthGuestResponse body = (await login.Content.ReadFromJsonAsync<AuthGuestResponse>())!;
        return body.AccessToken;
    }

    private static HttpClient Authenticated(HttpClient client, string accessToken)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    [Fact]
    public async Task Guest_login_creates_profile_and_get_returns_it_for_the_owner()
    {
        HttpClient client = _factory.CreateClient();
        string token = await LoginGuestAsync(client);
        Authenticated(client, token);

        HttpResponseMessage response = await client.GetAsync("/api/v1/profile");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ProfileDto profile = (await response.Content.ReadFromJsonAsync<ProfileDto>())!;
        profile.PlayerId.Should().Be(SubjectOf(token));
        profile.SchemaVersion.Should().Be(1);
        profile.DisplayName.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Repeated_get_profile_is_idempotent_single_row_per_account()
    {
        HttpClient client = _factory.CreateClient();
        string token = await LoginGuestAsync(client);
        Authenticated(client, token);
        Guid accountId = Guid.Parse(SubjectOf(token));

        ProfileDto first = (await (await client.GetAsync("/api/v1/profile"))
            .Content.ReadFromJsonAsync<ProfileDto>())!;
        ProfileDto second = (await (await client.GetAsync("/api/v1/profile"))
            .Content.ReadFromJsonAsync<ProfileDto>())!;

        first.PlayerId.Should().Be(second.PlayerId);

        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        int rows = await db.PlayerProfiles.CountAsync(p => p.AccountId == accountId);
        rows.Should().Be(1, "login + repeated reads must never create a duplicate profile");
    }

    [Fact]
    public async Task User_cannot_read_another_users_profile()
    {
        // Two distinct guests, each with their own account + profile.
        string tokenA = await LoginGuestAsync(_factory.CreateClient());
        string tokenB = await LoginGuestAsync(_factory.CreateClient());
        string subA = SubjectOf(tokenA);
        string subB = SubjectOf(tokenB);
        subA.Should().NotBe(subB);

        HttpClient clientA = Authenticated(_factory.CreateClient(), tokenA);
        HttpClient clientB = Authenticated(_factory.CreateClient(), tokenB);

        ProfileDto profileA = (await (await clientA.GetAsync("/api/v1/profile"))
            .Content.ReadFromJsonAsync<ProfileDto>())!;
        ProfileDto profileB = (await (await clientB.GetAsync("/api/v1/profile"))
            .Content.ReadFromJsonAsync<ProfileDto>())!;

        profileA.PlayerId.Should().Be(subA);
        profileB.PlayerId.Should().Be(subB);
        profileB.PlayerId.Should().NotBe(subA, "B must never receive A's profile");

        // Attempt to override ownership via a query param — the server ignores it (owner is the token sub).
        ProfileDto bypass = (await (await clientB.GetAsync($"/api/v1/profile?accountId={subA}"))
            .Content.ReadFromJsonAsync<ProfileDto>())!;
        bypass.PlayerId.Should().Be(subB, "client-supplied accountId must not change the resolved owner");
    }

    [Fact]
    public async Task Get_profile_without_token_returns_401()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/v1/profile");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

/// <summary>
/// A real-host factory backed by a Testcontainers PostgreSQL instance (so login persists the account +
/// profile and <c>GET /profile</c> reads them back). Uses the real unit of work; only Redis is stubbed out
/// (<see cref="NoOpCacheService"/>). JWT config matches <see cref="ApiTestFactory"/> so login tokens validate.
/// </summary>
public sealed class ProfilePostgresApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _container =
        new PostgreSqlBuilder().WithImage("postgres:16-alpine").Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Jwt:SigningKey", ApiTestFactory.JwtSigningKey);
        builder.UseSetting("Jwt:Issuer", ApiTestFactory.JwtIssuer);
        builder.UseSetting("Jwt:Audience", ApiTestFactory.JwtAudience);
        builder.UseSetting("Jwt:AccessTokenMinutes", ApiTestFactory.JwtAccessTokenMinutes.ToString());
        builder.UseSetting("ConnectionStrings:Postgres", _container.GetConnectionString());

        builder.ConfigureTestServices(services =>
        {
            // Keep the real IUnitOfWork + AppDbContext (persist to the container); only avoid Redis.
            services.RemoveAll<ICacheService>();
            services.AddSingleton<ICacheService, NoOpCacheService>();
        });
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        using IServiceScope scope = Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _container.DisposeAsync();
        await base.DisposeAsync();
    }
}
