using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using GameTeam.Contracts.Auth;
using GameTeam.Contracts.Battle;
using GameTeam.Contracts.Hero;
using GameTeam.Contracts.Team;
using GameTeam.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GameTeam.Api.IntegrationTests;

/// <summary>
/// Phase 35 — nâng cấp hero end-to-end trên host HTTP thật + PostgreSQL thật (Testcontainers, tái dùng
/// <see cref="BattlePostgresApiFactory"/>): <c>POST /api/v1/heroes/{heroId}/level-up</c> server-authoritative +
/// atomic (ADR-004/007/011): thành công tiêu gold + tăng cấp + trả chỉ số/Power theo cấp; thiếu gold ⇒ 409 và
/// KHÔNG mutate; max cấp ⇒ 409; hero không sở hữu ⇒ 404; và <b>combat dùng chỉ số theo cấp</b> (đánh mạnh hơn
/// sau khi nâng). Cần Docker.
/// </summary>
public sealed class LevelUpHeroEndpointTests : IClassFixture<BattlePostgresApiFactory>
{
    private const string HeroId = "hero_a";

    private readonly BattlePostgresApiFactory _factory;

    public LevelUpHeroEndpointTests(BattlePostgresApiFactory factory) => _factory = factory;

    [Fact]
    public async Task LevelUp_requires_authentication()
    {
        HttpResponseMessage response = await _factory.CreateClient()
            .PostAsync($"/api/v1/heroes/{HeroId}/level-up", content: null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LevelUp_spends_gold_increments_level_and_returns_scaled_stats()
    {
        (HttpClient client, string token) = await AuthenticatedAsync();
        await IntegrationTestSeeding.GrantHeroesAsync(_factory, token, HeroId);
        await IntegrationTestSeeding.CreditCurrencyAsync(_factory, token, "gold", 100);

        LevelUpHeroResponse result = await LevelUpAsync(client, HeroId);

        // Cấp 1 → 2, tiêu 100 gold (level_up[0]); chỉ số theo công thức config (bp=800).
        result.HeroId.Should().Be(HeroId);
        result.Level.Should().Be(2);
        result.GoldSpent.Should().Be(100);
        result.GoldBalanceAfter.Should().Be(0);
        result.Stats.Atk.Should().Be(216); // 200 + round(200*800*1/10000=16)
        result.Stats.Hp.Should().Be(1080);  // 1000 + round(80)
        result.Power.Should().Be(4320);     // 1080*1 + 216*10 + 54*8 + 108*6

        // Server-authoritative persisted state.
        (await OwnedLevelAsync(token, HeroId)).Should().Be(2);
        (await GoldBalanceAsync(token)).Should().Be(0);
    }

    [Fact]
    public async Task LevelUp_insufficient_gold_returns_409_and_mutates_nothing()
    {
        (HttpClient client, string token) = await AuthenticatedAsync();
        await IntegrationTestSeeding.GrantHeroesAsync(_factory, token, HeroId);
        await IntegrationTestSeeding.CreditCurrencyAsync(_factory, token, "gold", 50); // cost is 100

        HttpResponseMessage response = await client.PostAsync($"/api/v1/heroes/{HeroId}/level-up", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await OwnedLevelAsync(token, HeroId)).Should().Be(1, "thiếu tiền ⇒ cấp không đổi");
        (await GoldBalanceAsync(token)).Should().Be(50, "thiếu tiền ⇒ số dư không đổi (rollback)");
    }

    [Fact]
    public async Task LevelUp_at_max_level_returns_409()
    {
        (HttpClient client, string token) = await AuthenticatedAsync();
        await IntegrationTestSeeding.GrantHeroesAsync(_factory, token, HeroId);
        await IntegrationTestSeeding.CreditCurrencyAsync(_factory, token, "gold", 1000);

        // Đường cong level_up có 3 bước ⇒ cấp tối đa = 4. Nâng 3 lần (1→2→3→4).
        await LevelUpAsync(client, HeroId);
        await LevelUpAsync(client, HeroId);
        await LevelUpAsync(client, HeroId);
        (await OwnedLevelAsync(token, HeroId)).Should().Be(4);

        HttpResponseMessage response = await client.PostAsync($"/api/v1/heroes/{HeroId}/level-up", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict, "đã ở cấp tối đa ⇒ HERO_MAX_LEVEL_CONFLICT");
        (await OwnedLevelAsync(token, HeroId)).Should().Be(4, "không nâng thêm");
    }

    [Fact]
    public async Task LevelUp_hero_not_owned_returns_404()
    {
        (HttpClient client, string token) = await AuthenticatedAsync();
        await IntegrationTestSeeding.CreditCurrencyAsync(_factory, token, "gold", 1000);
        // KHÔNG seed sở hữu HeroId.

        HttpResponseMessage response = await client.PostAsync($"/api/v1/heroes/{HeroId}/level-up", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task LevelUp_makes_the_hero_hit_harder_in_combat()
    {
        (HttpClient client, string token) = await AuthenticatedAsync();
        await IntegrationTestSeeding.GrantHeroesAsync(_factory, token, BattlePostgresApiFactory.SeededHeroIds);
        await IntegrationTestSeeding.CreditCurrencyAsync(_factory, token, "gold", 1000);
        Guid teamId = await SaveTeamAsync(client);

        // Trận trước khi nâng: sát thương đòn đầu của ally_0 (hero_a) ở cấp 1.
        int damageBefore = await FirstAllyDamageAsync(client, teamId);

        // Nâng hero_a vài cấp (server-authoritative).
        await LevelUpAsync(client, HeroId);
        await LevelUpAsync(client, HeroId);
        (await OwnedLevelAsync(token, HeroId)).Should().Be(3);

        int damageAfter = await FirstAllyDamageAsync(client, teamId);

        damageAfter.Should().BeGreaterThan(damageBefore,
            "team snapshot dùng chỉ số theo cấp mới ⇒ ally_0 đánh mạnh hơn sau khi nâng (ADR-011)");
    }

    // ── helpers ───────────────────────────────────────────────────────────────────────────────────
    private async Task<(HttpClient Client, string Token)> AuthenticatedAsync()
    {
        HttpResponseMessage login = await _factory.CreateClient()
            .PostAsJsonAsync("/api/v1/auth/guest", new AuthGuestRequest(null));
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        AuthGuestResponse body = (await login.Content.ReadFromJsonAsync<AuthGuestResponse>())!;

        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.AccessToken);
        return (client, body.AccessToken);
    }

    private static async Task<LevelUpHeroResponse> LevelUpAsync(HttpClient client, string heroId)
    {
        HttpResponseMessage response = await client.PostAsync($"/api/v1/heroes/{heroId}/level-up", content: null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<LevelUpHeroResponse>())!;
    }

    private static async Task<Guid> SaveTeamAsync(HttpClient client)
    {
        var request = new SaveTeamRequest(
            BattlePostgresApiFactory.SeededHeroIds.Select((h, i) => new TeamSlotDto(i, h)).ToList());
        HttpResponseMessage save = await client.PostAsJsonAsync("/api/v1/team", request);
        save.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await save.Content.ReadFromJsonAsync<TeamDto>())!.Id;
    }

    // Đánh stage "trâu" (địch sống hết vòng) rồi lấy sát thương đòn ĐẦU của ally_0 từ event log server-authoritative.
    private static async Task<int> FirstAllyDamageAsync(HttpClient client, Guid teamId)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/battles",
            new StartBattleRequest(teamId, BattlePostgresApiFactory.TankyStageId, Guid.NewGuid().ToString()));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        BattleResultDto result = (await response.Content.ReadFromJsonAsync<BattleResultDto>())!;

        using JsonDocument doc = JsonDocument.Parse(result.Log);
        foreach (JsonElement e in doc.RootElement.GetProperty("event_log").EnumerateArray())
        {
            if (e.GetProperty("type").GetString() == "DamageApplied"
                && e.GetProperty("actor").GetString() == "ally_0")
            {
                return e.GetProperty("amount").GetInt32();
            }
        }

        throw new InvalidOperationException("Không tìm thấy sát thương của ally_0 trong event log.");
    }

    private async Task<int> OwnedLevelAsync(string token, string heroId)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Guid accountId = Guid.Parse(new JwtSecurityTokenHandler().ReadJwtToken(token).Subject);
        Guid profileId = (await db.PlayerProfiles.FirstAsync(p => p.AccountId == accountId)).Id;
        return (await db.OwnedHeroes.FirstAsync(h => h.ProfileId == profileId && h.HeroId == heroId)).Level;
    }

    private async Task<long> GoldBalanceAsync(string token)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Guid accountId = Guid.Parse(new JwtSecurityTokenHandler().ReadJwtToken(token).Subject);
        Guid profileId = (await db.PlayerProfiles.FirstAsync(p => p.AccountId == accountId)).Id;
        Domain.Economy.Wallet? wallet = await db.Wallets.FirstOrDefaultAsync(w => w.ProfileId == profileId);
        return wallet?.BalanceOf("gold") ?? 0;
    }
}
