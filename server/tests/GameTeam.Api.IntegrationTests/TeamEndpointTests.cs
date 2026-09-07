using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using GameTeam.Application.Abstractions.Caching;
using GameTeam.Application.Abstractions.Configuration;
using GameTeam.Contracts.Auth;
using GameTeam.Contracts.Team;
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
/// Phase 29 — team/formation end-to-end over the real HTTP host + real PostgreSQL (Testcontainers): guest
/// login seeds owned heroes; <c>POST /api/v1/team</c> saves a valid 6-hero formation (server-authoritative)
/// and <c>GET /api/v1/team</c> returns it for the owner; a wrong-size team is rejected (400); teams do not
/// leak across owners; both endpoints are protected. Requires Docker.
/// </summary>
public sealed class TeamEndpointTests : IClassFixture<TeamPostgresApiFactory>
{
    private readonly TeamPostgresApiFactory _factory;

    public TeamEndpointTests(TeamPostgresApiFactory factory) => _factory = factory;

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

    private static SaveTeamRequest TeamOf(params string[] heroes)
        => new(heroes.Select((h, i) => new TeamSlotDto(i, h)).ToList());

    [Fact]
    public async Task Save_valid_team_then_get_returns_the_saved_team()
    {
        HttpClient client = Authenticated(_factory.CreateClient(), await LoginGuestAsync(_factory.CreateClient()));

        HttpResponseMessage save = await client.PostAsJsonAsync("/api/v1/team", TeamOf(TeamPostgresApiFactory.SeededHeroIds));
        save.StatusCode.Should().Be(HttpStatusCode.OK);
        TeamDto saved = (await save.Content.ReadFromJsonAsync<TeamDto>())!;
        saved.Slots.Should().HaveCount(6);

        TeamDto fetched = (await (await client.GetAsync("/api/v1/team")).Content.ReadFromJsonAsync<TeamDto>())!;
        fetched.Slots.OrderBy(s => s.SlotIndex).Select(s => s.HeroId)
            .Should().Equal(TeamPostgresApiFactory.SeededHeroIds);
    }

    [Fact]
    public async Task Save_team_with_five_heroes_is_rejected()
    {
        HttpClient client = Authenticated(_factory.CreateClient(), await LoginGuestAsync(_factory.CreateClient()));

        HttpResponseMessage save = await client.PostAsJsonAsync(
            "/api/v1/team", TeamOf(TeamPostgresApiFactory.SeededHeroIds.Take(5).ToArray()));

        save.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Team_does_not_leak_across_owners()
    {
        // Owner A saves a team; owner B (a different guest) has not saved ⇒ empty.
        HttpClient ownerA = Authenticated(_factory.CreateClient(), await LoginGuestAsync(_factory.CreateClient()));
        (await ownerA.PostAsJsonAsync("/api/v1/team", TeamOf(TeamPostgresApiFactory.SeededHeroIds)))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        HttpClient ownerB = Authenticated(_factory.CreateClient(), await LoginGuestAsync(_factory.CreateClient()));
        TeamDto teamB = (await (await ownerB.GetAsync("/api/v1/team")).Content.ReadFromJsonAsync<TeamDto>())!;

        teamB.Slots.Should().BeEmpty("owner B chưa lưu đội — không thấy đội của owner A");
    }

    [Fact]
    public async Task Team_endpoints_require_authentication()
    {
        HttpResponseMessage get = await _factory.CreateClient().GetAsync("/api/v1/team");
        HttpResponseMessage post = await _factory.CreateClient()
            .PostAsJsonAsync("/api/v1/team", TeamOf(TeamPostgresApiFactory.SeededHeroIds));

        get.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        post.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

/// <summary>
/// Real-host factory backed by Testcontainers PostgreSQL, with a deterministic JSON-backed
/// <see cref="IConfigProvider"/> stub: seeds 6 heroes (guest login grants all as owned) + a
/// <c>formation_default</c> grid (2×3 = 6 slots). Real unit of work; only Redis is stubbed. JWT config
/// matches <see cref="ApiTestFactory"/>.
/// </summary>
public sealed class TeamPostgresApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public static readonly string[] SeededHeroIds =
        ["hero_a", "hero_b", "hero_c", "hero_d", "hero_e", "hero_f"];

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
            services.RemoveAll<ICacheService>();
            services.AddSingleton<ICacheService, NoOpCacheService>();

            services.RemoveAll<IConfigProvider>();
            services.AddSingleton<IConfigProvider>(StubConfig());
        });
    }

    private static StubConfigProvider StubConfig()
    {
        var stub = new StubConfigProvider();
        foreach (string heroId in SeededHeroIds)
        {
            stub.Set("hero", heroId, $$"""
                { "schema_version": 1, "id": "{{heroId}}", "faction": "none", "class": "warrior",
                  "element": "fire", "role": "dps", "rarity": 5,
                  "base_stats": { "hp": 100, "atk": 10, "def": 5, "spd": 10 }, "skills": ["skill_x"] }
                """);
        }

        stub.Set("formation", "formation_default", """
            { "schema_version": 1, "id": "formation_default", "rows": 2, "cols": 3 }
            """);
        return stub;
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
