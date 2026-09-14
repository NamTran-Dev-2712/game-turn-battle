using System;
using System.IdentityModel.Tokens.Jwt;
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
using GameTeam.Contracts.Battle;
using GameTeam.Contracts.Campaign;
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
/// Phase 34 — campaign PvE trên host HTTP thật + PostgreSQL thật (Testcontainers). Chứng minh
/// server-authoritative: clear stage → mở stage kế + thưởng + AFK stage (atomic); stage khoá bị chặn (403,
/// không mutation); tiến độ do server quyết (client không set được); thua ⇒ không tiến độ/thưởng; retry cùng
/// attemptId + re-clear ⇒ không thưởng/tiến độ lần hai (first-clear only + idempotent). Cần Docker.
/// </summary>
public sealed class CampaignEndpointTests : IClassFixture<CampaignPostgresApiFactory>
{
    private readonly CampaignPostgresApiFactory _factory;

    public CampaignEndpointTests(CampaignPostgresApiFactory factory) => _factory = factory;

    // ── Test 1 — clear → unlock next + reward + progression + AFK stage ────────────────────────────
    [Fact]
    public async Task Clearing_first_stage_unlocks_next_grants_reward_and_sets_afk_stage()
    {
        HttpClient client = await AuthenticatedClientAsync();
        Guid teamId = await SaveTeamAsync(client);

        BattleResultDto result = await StartCampaignAsync(client, teamId, "stage_ch01_01", Attempt(), HttpStatusCode.OK);
        result.Outcome.Should().Be("VICTORY");
        result.Rewards.Should().ContainSingle().Which.Should().BeEquivalentTo(new RewardDto("currency", "gold", 100));

        CampaignProgressDto progress = await GetProgressAsync(client);
        progress.CurrentAfkStageId.Should().Be("stage_ch01_01");
        Stage(progress, "stage_ch01_01").Cleared.Should().BeTrue();
        Stage(progress, "stage_ch01_02").Cleared.Should().BeFalse();
        Stage(progress, "stage_ch01_02").Unlocked.Should().BeTrue("clear stage 1 ⇒ mở khoá stage 2");
        Stage(progress, "stage_ch01_03").Unlocked.Should().BeFalse("stage 2 chưa clear");

        (long gold, int cleared, string afk) = await InspectAsync(client);
        gold.Should().Be(100);
        cleared.Should().Be(1);
        afk.Should().Be("stage_ch01_01");
    }

    // ── Test 2 — locked stage rejected, no mutation ────────────────────────────────────────────────
    [Fact]
    public async Task Locked_stage_is_rejected_without_mutation()
    {
        HttpClient client = await AuthenticatedClientAsync();
        Guid teamId = await SaveTeamAsync(client);

        // Người chơi mới: request stage 2 (chưa clear stage 1) ⇒ 403.
        HttpResponseMessage response = await PostCampaignAsync(client, teamId, "stage_ch01_02", Attempt());
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        CampaignProgressDto progress = await GetProgressAsync(client);
        progress.CurrentAfkStageId.Should().BeEmpty();
        progress.Stages.Should().OnlyContain(s => !s.Cleared);

        (long gold, int cleared, _) = await InspectAsync(client);
        gold.Should().Be(0, "stage khoá ⇒ không cấp thưởng");
        cleared.Should().Be(0, "stage khoá ⇒ không tiến độ");
    }

    // ── Test 3 — server authority: cannot skip to an arbitrary future stage ────────────────────────
    [Fact]
    public async Task Cannot_skip_to_arbitrary_future_stage()
    {
        HttpClient client = await AuthenticatedClientAsync();
        Guid teamId = await SaveTeamAsync(client);

        // Client "đòi" đánh thẳng stage cuối — server từ chối (tuần tự), tiến độ do server quyết.
        HttpResponseMessage response = await PostCampaignAsync(client, teamId, "stage_ch01_03", Attempt());
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        (long _, int cleared, _) = await InspectAsync(client);
        cleared.Should().Be(0);
    }

    // ── Test 5 — idempotency + first-clear only ────────────────────────────────────────────────────
    [Fact]
    public async Task Retry_and_replay_do_not_double_grant_or_double_advance()
    {
        HttpClient client = await AuthenticatedClientAsync();
        Guid teamId = await SaveTeamAsync(client);
        string attempt = Attempt();

        BattleResultDto first = await StartCampaignAsync(client, teamId, "stage_ch01_01", attempt, HttpStatusCode.OK);
        BattleResultDto retry = await StartCampaignAsync(client, teamId, "stage_ch01_01", attempt, HttpStatusCode.OK);
        retry.Seed.Should().Be(first.Seed, "retry cùng attemptId trả kết quả đã lưu");

        // Replay stage đã clear với attemptId MỚI ⇒ VICTORY nhưng KHÔNG thưởng lần hai (first-clear only).
        BattleResultDto replay = await StartCampaignAsync(client, teamId, "stage_ch01_01", Attempt(), HttpStatusCode.OK);
        replay.Outcome.Should().Be("VICTORY");
        replay.Rewards.Should().BeEmpty("first-clear only — clear lại không cấp thưởng");

        (long gold, int cleared, _) = await InspectAsync(client);
        gold.Should().Be(100, "thưởng cấp đúng MỘT lần");
        cleared.Should().Be(1, "stage clear một lần — không nhân đôi");
    }

    // ── Auth — protected ────────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task Campaign_endpoints_require_authentication()
    {
        HttpResponseMessage post = await PostCampaignAsync(_factory.CreateClient(), Guid.NewGuid(), "stage_ch01_01", Attempt());
        post.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        HttpResponseMessage get = await _factory.CreateClient().GetAsync("/api/v1/campaign/progress");
        get.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Test 4 — loss (config địch mạnh) ⇒ no progression / no reward ──────────────────────────────
    [Fact]
    public async Task Defeat_does_not_advance_progress_or_grant_reward()
    {
        // Host phụ: config có stage 1 địch cực mạnh ⇒ DEFEAT tất định (server-authoritative).
        WebApplicationFactory<Program> losing = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IConfigProvider>();
                services.AddSingleton<IConfigProvider>(CampaignPostgresApiFactory.BuildConfig(stage1Losing: true));
            }));

        HttpClient client = await AuthenticatedClientAsync(losing);
        Guid teamId = await SaveTeamAsync(client);

        BattleResultDto result = await StartCampaignAsync(client, teamId, "stage_ch01_01", Attempt(), HttpStatusCode.OK);
        result.Outcome.Should().Be("DEFEAT");
        result.Rewards.Should().BeEmpty();

        CampaignProgressDto progress = await GetProgressAsync(client);
        progress.CurrentAfkStageId.Should().BeEmpty();
        progress.Stages.Should().OnlyContain(s => !s.Cleared);
    }

    // ── helpers ───────────────────────────────────────────────────────────────────────────────────
    private static string Attempt() => Guid.NewGuid().ToString();

    private static CampaignStageDto Stage(CampaignProgressDto progress, string stageId) =>
        progress.Stages.Single(s => s.StageId == stageId);

    private async Task<HttpClient> AuthenticatedClientAsync(WebApplicationFactory<Program>? factory = null)
    {
        factory ??= _factory;
        HttpResponseMessage login = await factory.CreateClient()
            .PostAsJsonAsync("/api/v1/auth/guest", new AuthGuestRequest(null));
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        AuthGuestResponse body = (await login.Content.ReadFromJsonAsync<AuthGuestResponse>())!;

        await IntegrationTestSeeding.GrantHeroesAsync(factory, body.AccessToken, CampaignPostgresApiFactory.SeededHeroIds);

        HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.AccessToken);
        return client;
    }

    private static async Task<Guid> SaveTeamAsync(HttpClient client)
    {
        var request = new SaveTeamRequest(
            CampaignPostgresApiFactory.SeededHeroIds.Select((h, i) => new TeamSlotDto(i, h)).ToList());
        HttpResponseMessage save = await client.PostAsJsonAsync("/api/v1/team", request);
        save.StatusCode.Should().Be(HttpStatusCode.OK);
        TeamDto team = (await save.Content.ReadFromJsonAsync<TeamDto>())!;
        return team.Id;
    }

    private static Task<HttpResponseMessage> PostCampaignAsync(HttpClient client, Guid teamId, string stageId, string attemptId)
        => client.PostAsJsonAsync("/api/v1/campaign/battles", new StartCampaignBattleRequest(teamId, stageId, attemptId));

    private static async Task<BattleResultDto> StartCampaignAsync(
        HttpClient client, Guid teamId, string stageId, string attemptId, HttpStatusCode expected)
    {
        HttpResponseMessage response = await PostCampaignAsync(client, teamId, stageId, attemptId);
        response.StatusCode.Should().Be(expected);
        return (await response.Content.ReadFromJsonAsync<BattleResultDto>())!;
    }

    private static async Task<CampaignProgressDto> GetProgressAsync(HttpClient client)
    {
        HttpResponseMessage response = await client.GetAsync("/api/v1/campaign/progress");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<CampaignProgressDto>())!;
    }

    /// <summary>Đọc trạng thái DB của người gọi (scope theo token sub — không đọc nhầm profile test khác).</summary>
    private async Task<(long Gold, int ClearedCount, string AfkStageId)> InspectAsync(HttpClient client)
    {
        string token = client.DefaultRequestHeaders.Authorization!.Parameter!;
        Guid accountId = Guid.Parse(new JwtSecurityTokenHandler().ReadJwtToken(token).Subject);

        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var profile = await db.PlayerProfiles.FirstAsync(p => p.AccountId == accountId);
        var progress = await db.CampaignProgresses
            .Include(p => p.ClearedStages)
            .FirstOrDefaultAsync(p => p.ProfileId == profile.Id);
        var wallet = await db.Wallets.FirstOrDefaultAsync(w => w.ProfileId == profile.Id);

        long gold = wallet?.BalanceOf("gold") ?? 0;
        return (gold, progress?.ClearedStages.Count ?? 0, progress?.CurrentAfkStageId ?? string.Empty);
    }
}

/// <summary>
/// Host thật + Testcontainers PostgreSQL + <see cref="IConfigProvider"/> stub combat-capable cho campaign:
/// 6 hero đội + 1 chapter (3 stage tuần tự) + địch yếu (win tất định) + reward gold. Redis stub; real unit of
/// work. <see cref="BuildConfig"/> có biến thể "stage 1 địch mạnh" cho test thua.
/// </summary>
public sealed class CampaignPostgresApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
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
            services.AddSingleton<IConfigProvider>(BuildConfig(stage1Losing: false));
        });
    }

    /// <summary>Config campaign: 1 chapter, 3 stage tuần tự. <paramref name="stage1Losing"/> ⇒ stage 1 địch cực mạnh.</summary>
    public static IConfigProvider BuildConfig(bool stage1Losing)
    {
        var stub = new StubConfigProvider();

        for (int i = 0; i < SeededHeroIds.Length; i++)
        {
            stub.Set("hero", SeededHeroIds[i], $$"""
                { "schema_version": 1, "id": "{{SeededHeroIds[i]}}", "faction": "none", "class": "warrior",
                  "element": "fire", "role": "dps", "rarity": 5,
                  "base_stats": { "hp": 1000, "atk": 200, "def": 50, "spd": {{100 + i}} }, "skills": ["skill_basic"] }
                """);
        }

        stub.Set("hero", "hero_dummy", """
            { "schema_version": 1, "id": "hero_dummy", "faction": "none", "class": "warrior",
              "element": "fire", "role": "dps", "rarity": 3,
              "base_stats": { "hp": 100, "atk": 10, "def": 10, "spd": 50 }, "skills": ["skill_basic"] }
            """);

        // Địch cực mạnh cho test thua: đi trước (spd cao), một đòn hạ 1 ally, máu khổng lồ ⇒ DEFEAT trong max_rounds.
        stub.Set("hero", "hero_boss", """
            { "schema_version": 1, "id": "hero_boss", "faction": "none", "class": "warrior",
              "element": "fire", "role": "dps", "rarity": 5,
              "base_stats": { "hp": 100000, "atk": 5000, "def": 500, "spd": 999 }, "skills": ["skill_basic"] }
            """);

        stub.Set("skill", "skill_basic", """
            { "target": "single_enemy", "trigger": { "type": "cooldown", "value": 0 },
              "effects": [ { "effect_type": "damage", "params": { "coeff_fixed": 1000 } } ] }
            """);

        stub.Set("formation", "formation_default", """
            { "schema_version": 1, "id": "formation_default", "rows": 2, "cols": 3 }
            """);

        stub.Set("chapter", "chapter_01", """
            { "schema_version": 1, "id": "chapter_01", "order": 1,
              "stages": ["stage_ch01_01", "stage_ch01_02", "stage_ch01_03"] }
            """);

        string stage1Enemy = stage1Losing ? "hero_boss" : "hero_dummy";
        Stage("stage_ch01_01", stage1Enemy, "reward_c1");
        Stage("stage_ch01_02", "hero_dummy", "reward_c2");
        Stage("stage_ch01_03", "hero_dummy", "reward_c3");

        stub.Set("reward", "reward_c1", """{ "entries": [ { "reward_type": "currency", "ref_id": "gold", "amount": 100 } ] }""");
        stub.Set("reward", "reward_c2", """{ "entries": [ { "reward_type": "currency", "ref_id": "gold", "amount": 150 } ] }""");
        stub.Set("reward", "reward_c3", """{ "entries": [ { "reward_type": "currency", "ref_id": "gold", "amount": 200 } ] }""");

        return stub;

        void Stage(string id, string enemyId, string rewardId) => stub.Set("stage", id, $$"""
            {
              "max_rounds": 30,
              "basic_skill_id": "skill_basic",
              "combat_rules": {
                "def_constant_k": 300, "min_damage": 1, "crit_multiplier_fixed": 1500,
                "accuracy_bp": 10000, "crit_rate_bp": 0,
                "energy": { "initial": 0, "on_attack": 0, "on_hit": 0, "ultimate_cost": 100, "max": 100 }
              },
              "enemies": [ { "hero_id": "{{enemyId}}", "slot": 0 } ],
              "rewards": ["{{rewardId}}"]
            }
            """);
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
