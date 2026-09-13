using System;
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
using GameTeam.Contracts.Summon;
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
/// Phase 33 — summon/gacha end-to-end over the real HTTP host + real PostgreSQL (Testcontainers), with a
/// deterministic single-hero single-rarity banner so outcomes are exact: SERVER decides RNG/rate/pity/hero
/// (client only sends an intent); currency spend + hero/fragment grant are ATOMIC + IDEMPOTENT (retry same
/// requestId ⇒ no double spend/grant); a duplicate hero converts to fragments (no second owned-hero row);
/// insufficient funds is rejected with no mutation. Requires Docker.
/// </summary>
public sealed class SummonEndpointTests : IClassFixture<SummonPostgresApiFactory>
{
    private readonly SummonPostgresApiFactory _factory;

    public SummonEndpointTests(SummonPostgresApiFactory factory) => _factory = factory;

    // ── C — single summon: server grants hero, spends currency exactly once ────────────────────────
    [Fact]
    public async Task Single_summon_grants_hero_and_spends_currency_once()
    {
        (HttpClient client, string token) = await AuthWithTicketsAsync(5);

        SummonResultDto result = await SummonAsync(client, SummonRequestOf(1, Req()));

        result.Count.Should().Be(1);
        result.Pulls.Should().ContainSingle();
        result.Pulls[0].HeroId.Should().Be(SummonPostgresApiFactory.PoolHeroId);
        result.Pulls[0].IsNew.Should().BeTrue("hero đầu tiên là hero mới");
        result.Pulls[0].Fragments.Should().Be(0);

        (await OwnedHeroCountAsync(token)).Should().Be(1, "một hero mới vào sở hữu");
        (await TicketBalanceAsync(token)).Should().Be(4, "tiêu đúng 1 ticket");
    }

    // ── C + F — 10-pull: atomic, cost = 10×, first new then dupes→fragments ────────────────────────
    [Fact]
    public async Task Ten_pull_is_atomic_costs_ten_and_converts_dupes_to_fragments()
    {
        (HttpClient client, string token) = await AuthWithTicketsAsync(20);

        SummonResultDto result = await SummonAsync(client, SummonRequestOf(10, Req()));

        result.Count.Should().Be(10);
        result.Pulls.Should().HaveCount(10);
        result.Pulls.Count(p => p.IsNew).Should().Be(1, "lần đầu là hero mới; 9 lần sau là trùng (banner 1 hero)");
        result.Pulls.Count(p => !p.IsNew && p.Fragments == 10).Should().Be(9, "mỗi lần trùng cấp 10 mảnh (rarity 3)");

        (await OwnedHeroCountAsync(token)).Should().Be(1, "chỉ một dòng sở hữu — trùng KHÔNG tạo hero thứ hai");
        (await FragmentQuantityAsync(token)).Should().Be(90, "9 lần trùng × 10 mảnh");
        (await TicketBalanceAsync(token)).Should().Be(10, "10-pull tiêu 10 ticket");
    }

    // ── E — duplicate hero across two summons converts to fragments ───────────────────────────────
    [Fact]
    public async Task Duplicate_summon_grants_fragments_not_a_second_hero()
    {
        (HttpClient client, string token) = await AuthWithTicketsAsync(5);

        SummonResultDto first = await SummonAsync(client, SummonRequestOf(1, Req()));
        SummonResultDto second = await SummonAsync(client, SummonRequestOf(1, Req()));

        first.Pulls[0].IsNew.Should().BeTrue();
        second.Pulls[0].IsNew.Should().BeFalse("hero đã sở hữu ⇒ trùng");
        second.Pulls[0].Fragments.Should().Be(10);

        (await OwnedHeroCountAsync(token)).Should().Be(1, "không tạo hero trùng");
        (await FragmentQuantityAsync(token)).Should().Be(10);
    }

    // ── D — idempotency: retry same requestId ⇒ no double spend/grant/pity ─────────────────────────
    [Fact]
    public async Task Retry_same_request_does_not_double_spend_or_grant()
    {
        (HttpClient client, string token) = await AuthWithTicketsAsync(5);
        string req = Req();

        SummonResultDto first = await SummonAsync(client, SummonRequestOf(1, req));
        SummonResultDto retry = await SummonAsync(client, SummonRequestOf(1, req));

        retry.Should().BeEquivalentTo(first, "retry trả kết quả ĐÃ LƯU");

        (await OwnedHeroCountAsync(token)).Should().Be(1, "không cấp hero lần hai");
        (await TicketBalanceAsync(token)).Should().Be(4, "không tiêu ticket lần hai");
        (await SummonRecordCountAsync(token)).Should().Be(1, "một bản ghi cho một requestId");
        (await SpendLedgerCountAsync(req)).Should().Be(1, "một dòng ledger tiêu tiền cho requestId");
    }

    // ── C — insufficient funds: rejected with no mutation ─────────────────────────────────────────
    [Fact]
    public async Task Insufficient_funds_is_rejected_and_grants_nothing()
    {
        (HttpClient client, string token) = await AuthWithTicketsAsync(0);

        HttpResponseMessage response = await PostSummonAsync(client, SummonRequestOf(1, Req()));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict, "thiếu tiền ⇒ CURRENCY_INSUFFICIENT_FUNDS (409)");
        (await OwnedHeroCountAsync(token)).Should().Be(0, "không cấp hero khi thiếu tiền");
        (await TicketBalanceAsync(token)).Should().Be(0, "không tiêu tiền");
    }

    [Fact]
    public async Task Unknown_banner_returns_404()
    {
        (HttpClient client, _) = await AuthWithTicketsAsync(5);

        HttpResponseMessage response = await PostSummonAsync(client, new SummonRequest("gacha_missing", 1, Req()));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Invalid_count_returns_400()
    {
        (HttpClient client, _) = await AuthWithTicketsAsync(5);

        HttpResponseMessage response = await PostSummonAsync(client, SummonRequestOf(5, Req()));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Summon_endpoint_requires_authentication()
    {
        HttpResponseMessage response = await PostSummonAsync(_factory.CreateClient(), SummonRequestOf(1, Req()));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── helpers ───────────────────────────────────────────────────────────────────────────────────
    private static string Req() => Guid.NewGuid().ToString();

    private static SummonRequest SummonRequestOf(int count, string requestId)
        => new(SummonPostgresApiFactory.BannerId, count, requestId);

    private async Task<(HttpClient Client, string Token)> AuthWithTicketsAsync(long tickets)
    {
        HttpResponseMessage login = await _factory.CreateClient()
            .PostAsJsonAsync("/api/v1/auth/guest", new AuthGuestRequest(null));
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        string token = (await login.Content.ReadFromJsonAsync<AuthGuestResponse>())!.AccessToken;

        if (tickets > 0)
        {
            await IntegrationTestSeeding.CreditCurrencyAsync(_factory, token, "ticket", tickets);
        }

        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (client, token);
    }

    private static Task<HttpResponseMessage> PostSummonAsync(HttpClient client, SummonRequest request)
        => client.PostAsJsonAsync("/api/v1/summon", request);

    private static async Task<SummonResultDto> SummonAsync(HttpClient client, SummonRequest request)
    {
        HttpResponseMessage response = await PostSummonAsync(client, request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<SummonResultDto>())!;
    }

    private async Task<Guid> ProfileIdAsync(string token)
    {
        Guid accountId = Guid.Parse(new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(token).Subject);
        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return (await db.PlayerProfiles.FirstAsync(p => p.AccountId == accountId)).Id;
    }

    private async Task<int> OwnedHeroCountAsync(string token)
    {
        Guid profileId = await ProfileIdAsync(token);
        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.OwnedHeroes.CountAsync(h => h.ProfileId == profileId);
    }

    private async Task<long> TicketBalanceAsync(string token)
    {
        Guid profileId = await ProfileIdAsync(token);
        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var wallet = await db.Wallets.FirstOrDefaultAsync(w => w.ProfileId == profileId);
        return wallet?.BalanceOf("ticket") ?? 0;
    }

    private async Task<long> FragmentQuantityAsync(string token)
    {
        Guid profileId = await ProfileIdAsync(token);
        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var inventory = await db.Inventories.FirstOrDefaultAsync(i => i.ProfileId == profileId);
        return inventory?.QuantityOf("fragment", SummonPostgresApiFactory.PoolHeroId) ?? 0;
    }

    private async Task<int> SummonRecordCountAsync(string token)
    {
        Guid profileId = await ProfileIdAsync(token);
        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.SummonRecords.CountAsync(r => r.ProfileId == profileId);
    }

    private async Task<int> SpendLedgerCountAsync(string requestId)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.CurrencyTransactions.CountAsync(t => t.IdempotencyKey == $"gacha:{requestId}:spend");
    }
}

/// <summary>
/// Real host + Testcontainers PostgreSQL + <see cref="IConfigProvider"/> stub with a DETERMINISTIC banner:
/// a single-hero (<c>hero_x</c>, rarity 3) single-rarity pool so every pull yields the same hero — making
/// new-vs-dupe, cost, and pity assertions exact. Cost = 1 ticket/pull; dupe (rarity 3) = 10 fragments. Redis
/// stubbed; real unit of work. JWT config matches <see cref="ApiTestFactory"/>.
/// </summary>
public sealed class SummonPostgresApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string BannerId = "gacha_test";
    public const string PoolHeroId = "hero_x";

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
            services.AddSingleton<IConfigProvider>(BuildConfig());
        });
    }

    private static StubConfigProvider BuildConfig()
    {
        var stub = new StubConfigProvider();

        stub.Set("hero", PoolHeroId, $$"""
            { "schema_version": 1, "id": "{{PoolHeroId}}", "faction": "none", "class": "warrior",
              "element": "fire", "role": "dps", "rarity": 3,
              "base_stats": { "hp": 100, "atk": 10, "def": 5, "spd": 10 }, "skills": ["skill_x"] }
            """);

        // Banner tất định: pool 1 hero, rate 1 rarity (3), cost 1 ticket, dupe rarity-3 = 10 mảnh, không pity.
        stub.Set("gacha", BannerId, $$"""
            {
              "schema_version": 1, "id": "{{BannerId}}",
              "pool": ["{{PoolHeroId}}"],
              "rates": [ { "rarity": 3, "weight": 100 } ],
              "cost": { "currency": "ticket", "amount": 1 },
              "dupe_fragments": [ { "rarity": 3, "amount": 10 } ]
            }
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
