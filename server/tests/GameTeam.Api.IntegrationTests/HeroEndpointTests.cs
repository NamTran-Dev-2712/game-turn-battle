using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GameTeam.Application.Abstractions.Caching;
using GameTeam.Application.Abstractions.Configuration;
using GameTeam.Contracts.Auth;
using GameTeam.Contracts.Config;
using GameTeam.Contracts.Hero;
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
/// Phase 27 — hero system end-to-end over the real HTTP host + real PostgreSQL (Testcontainers):
/// guest login seeds owned heroes from config; <c>GET /api/v1/heroes</c> returns only the caller's heroes
/// (owner from token, server-authoritative); the endpoint is protected; and <c>GET
/// /api/v1/heroes/{id}/definition</c> serves the config-driven definition publicly. Requires Docker.
/// </summary>
public sealed class HeroEndpointTests : IClassFixture<HeroPostgresApiFactory>
{
    private readonly HeroPostgresApiFactory _factory;

    public HeroEndpointTests(HeroPostgresApiFactory factory) => _factory = factory;

    // Shared enums serialize as strings on the wire (JsonStringEnumConverter) — the client must too.
    private static readonly JsonSerializerOptions WireJson =
        new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

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
    public async Task Guest_login_seeds_heroes_from_config_and_get_returns_them_for_owner()
    {
        HttpClient client = _factory.CreateClient();
        string token = await LoginGuestAsync(client);
        Authenticated(client, token);

        HttpResponseMessage response = await client.GetAsync("/api/v1/heroes");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        MyHeroesResponse body = (await response.Content.ReadFromJsonAsync<MyHeroesResponse>())!;
        body.Heroes.Select(h => h.HeroId).Should().BeEquivalentTo(HeroPostgresApiFactory.SeededHeroIds);
        body.Heroes.Should().OnlyContain(h => h.Level == 1 && h.Stars == 1);
    }

    [Fact]
    public async Task Two_guests_each_own_their_own_seeded_heroes()
    {
        string tokenA = await LoginGuestAsync(_factory.CreateClient());
        string tokenB = await LoginGuestAsync(_factory.CreateClient());

        MyHeroesResponse heroesA = (await (await Authenticated(_factory.CreateClient(), tokenA)
            .GetAsync("/api/v1/heroes")).Content.ReadFromJsonAsync<MyHeroesResponse>())!;
        MyHeroesResponse heroesB = (await (await Authenticated(_factory.CreateClient(), tokenB)
            .GetAsync("/api/v1/heroes")).Content.ReadFromJsonAsync<MyHeroesResponse>())!;

        heroesA.Heroes.Select(h => h.HeroId).Should().BeEquivalentTo(HeroPostgresApiFactory.SeededHeroIds);
        heroesB.Heroes.Select(h => h.HeroId).Should().BeEquivalentTo(HeroPostgresApiFactory.SeededHeroIds);
    }

    [Fact]
    public async Task Get_heroes_without_token_returns_401()
    {
        HttpResponseMessage response = await _factory.CreateClient().GetAsync("/api/v1/heroes");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_hero_definition_is_public_and_reads_from_config()
    {
        HttpResponseMessage response = await _factory.CreateClient().GetAsync("/api/v1/heroes/hero_ignis/definition");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        HeroDefinitionDto dto = (await response.Content.ReadFromJsonAsync<HeroDefinitionDto>(WireJson))!;
        dto.HeroId.Should().Be("hero_ignis");
        dto.BaseStats.Atk.Should().Be(220);
        dto.Skills.Should().Contain("skill_ignis_strike");
    }

    [Fact]
    public async Task Get_hero_definition_for_unknown_id_returns_404()
    {
        HttpResponseMessage response = await _factory.CreateClient().GetAsync("/api/v1/heroes/hero_missing/definition");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

/// <summary>
/// Real-host factory backed by Testcontainers PostgreSQL, with a deterministic JSON-backed
/// <see cref="IConfigProvider"/> stub (so guest login seeds a known hero set and the definition endpoint has
/// data — independent of config-publish timing). Real unit of work; only Redis is stubbed. JWT config matches
/// <see cref="ApiTestFactory"/>.
/// </summary>
public sealed class HeroPostgresApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public static readonly string[] SeededHeroIds = ["hero_ignis", "hero_aqua"];

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

            // Deterministic config (no publish-timing dependency): seed hero_ignis + hero_aqua.
            services.RemoveAll<IConfigProvider>();
            services.AddSingleton<IConfigProvider>(StubHeroConfig());
        });
    }

    private static StubConfigProvider StubHeroConfig()
    {
        var stub = new StubConfigProvider();
        stub.Set("hero", "hero_ignis", """
            { "schema_version": 1, "id": "hero_ignis", "faction": "none", "class": "mage",
              "element": "fire", "role": "dps", "rarity": 5,
              "base_stats": { "hp": 900, "atk": 220, "def": 60, "spd": 110 },
              "skills": ["skill_ignis_strike"], "art": "res://assets/heroes/hero_ignis.png" }
            """);
        stub.Set("hero", "hero_aqua", """
            { "schema_version": 1, "id": "hero_aqua", "faction": "none", "class": "support",
              "element": "water", "role": "healer", "rarity": 4,
              "base_stats": { "hp": 1100, "atk": 90, "def": 80, "spd": 95 },
              "skills": ["skill_aqua_heal"], "art": "res://assets/heroes/hero_aqua.png" }
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

/// <summary>In-memory JSON-backed <see cref="IConfigProvider"/> for tests (mirrors RuntimeConfigProvider read semantics).</summary>
internal sealed class StubConfigProvider : IConfigProvider
{
    private static readonly JsonSerializerOptions ReadOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
    };

    private readonly Dictionary<string, Dictionary<string, string>> _data = new(StringComparer.Ordinal);

    public ConfigVersion CurrentVersion { get; } = new(1, 1);

    public void Set(string type, string id, string json)
    {
        if (!_data.TryGetValue(type, out Dictionary<string, string>? byId))
        {
            byId = new Dictionary<string, string>(StringComparer.Ordinal);
            _data[type] = byId;
        }

        byId[id] = json;
    }

    public T? Get<T>(string type, string id)
        where T : class
        => _data.TryGetValue(type, out Dictionary<string, string>? byId) && byId.TryGetValue(id, out string? json)
            ? JsonSerializer.Deserialize<T>(json, ReadOptions)
            : null;

    public IReadOnlyList<string> GetIds(string type)
        => _data.TryGetValue(type, out Dictionary<string, string>? byId) ? byId.Keys.ToList() : Array.Empty<string>();
}
