using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GameTeam.Application.Abstractions.Caching;
using GameTeam.Application.Abstractions.Configuration;
using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Application.Combat;
using GameTeam.Contracts.Auth;
using GameTeam.Contracts.Battle;
using GameTeam.Contracts.Team;
using GameTeam.Domain.Battles;
using GameTeam.Domain.Combat;
using GameTeam.Domain.Combat.Serialization;
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
/// Phase 30 — luồng trận đầu-cuối trên host HTTP thật + PostgreSQL thật (Testcontainers): guest login → lưu đội 6
/// hero → <c>POST /api/v1/battles</c> re-sim server quyết VICTORY + cấp thưởng gold; BattleResult chứa seed/outcome/
/// rewards/log; seed trả về đúng seed đã dùng (re-sim lại khớp log); retry cùng attemptId KHÔNG cấp thưởng lần hai
/// (idempotency); lỗi persistence giữa transaction ⇒ rollback (không partial state); endpoint protected. Cần Docker.
/// </summary>
public sealed class BattleEndpointTests : IClassFixture<BattlePostgresApiFactory>
{
    private readonly BattlePostgresApiFactory _factory;

    public BattleEndpointTests(BattlePostgresApiFactory factory) => _factory = factory;

    // ── A — re-sim authoritative: server quyết outcome ────────────────────────────────────────────
    [Fact]
    public async Task Post_battle_runs_server_resim_and_returns_victory_result()
    {
        HttpClient client = await AuthenticatedClientAsync();
        Guid teamId = await SaveTeamAsync(client);

        BattleResultDto result = await StartBattleAsync(client, teamId, Attempt(), HttpStatusCode.OK);

        result.Outcome.Should().Be("VICTORY"); // accuracy_bp=10000, crit=0 ⇒ 6 ally hạ 1 địch yếu, tất định.
        result.Rounds.Should().BeGreaterThan(0);
    }

    // ── B + E + F — BattleResult chứa seed/outcome/rewards/log; seed đúng là seed đã dùng (re-sim khớp) ──
    [Fact]
    public async Task Battle_result_contains_seed_outcome_rewards_log_and_seed_reproduces_the_log()
    {
        HttpClient client = await AuthenticatedClientAsync();
        Guid teamId = await SaveTeamAsync(client);

        BattleResultDto result = await StartBattleAsync(client, teamId, Attempt(), HttpStatusCode.OK);

        result.Seed.Should().BeGreaterThanOrEqualTo(0); // Int64 không âm — round-trip an toàn.
        result.Outcome.Should().Be("VICTORY");
        result.Log.Should().Contain("event_log").And.Contain("result");
        result.Rewards.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new RewardDto("currency", "gold", 100));

        // Test F — replay parity: re-sim server với ĐÚNG seed trả về + cùng config/đội ⇒ cùng log.
        var resolver = new CombatInputResolver(_factory.Config);
        var ally = BattlePostgresApiFactory.SeededHeroIds
            .Select((hero, i) => new CombatTeamMember($"{TeamSnapshotPrefix}{i}", hero, i))
            .ToList();
        BattleOutput replay = new BattleSimulator().Simulate(
            resolver.Resolve(new BattleRequest((ulong)result.Seed, BattlePostgresApiFactory.StageId, ally)).Value);
        CombatEventSerializer.Serialize(replay).Should().Be(result.Log);
    }

    // ── C — transaction rollback: lỗi ghi giữa chừng ⇒ không partial state ────────────────────────
    [Fact]
    public async Task Persistence_failure_rolls_back_reward_and_record()
    {
        string attempt = Attempt();

        // Host phụ: repo battle record ném khi AddAsync (sau khi ví đã được credit trong cùng transaction).
        WebApplicationFactory<Program> throwing = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IBattleRecordRepository>();
                services.AddScoped<IBattleRecordRepository, ThrowingBattleRecordRepository>();
            }));

        HttpClient client = await AuthenticatedClientAsync(throwing);
        Guid teamId = await SaveTeamAsync(client);

        HttpResponseMessage response = await PostBattleAsync(client, teamId, attempt);
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

        (int records, long gold) = await InspectAsync(attempt);
        records.Should().Be(0, "transaction phải rollback — không ghi record");
        gold.Should().Be(0, "credit ví phải rollback cùng transaction — không partial state");
    }

    // ── D — idempotency: retry cùng attemptId ⇒ không cấp thưởng lần hai ──────────────────────────
    [Fact]
    public async Task Retry_same_attempt_does_not_double_grant_reward()
    {
        HttpClient client = await AuthenticatedClientAsync();
        Guid teamId = await SaveTeamAsync(client);
        string attempt = Attempt();

        BattleResultDto first = await StartBattleAsync(client, teamId, attempt, HttpStatusCode.OK);
        BattleResultDto retry = await StartBattleAsync(client, teamId, attempt, HttpStatusCode.OK);

        retry.Seed.Should().Be(first.Seed, "retry trả kết quả ĐÃ LƯU (cùng seed)");
        retry.Log.Should().Be(first.Log);

        (int records, long gold) = await InspectAsync(attempt);
        records.Should().Be(1, "chỉ một bản ghi cho một attemptId");
        gold.Should().Be(100, "thưởng cấp đúng MỘT lần — không double-grant");
    }

    // ── Auth — protected ──────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task Battle_endpoint_requires_authentication()
    {
        HttpResponseMessage response = await PostBattleAsync(_factory.CreateClient(), Guid.NewGuid(), Attempt());
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── helpers ───────────────────────────────────────────────────────────────────────────────────
    private const string TeamSnapshotPrefix = "ally_";

    private static string Attempt() => Guid.NewGuid().ToString();

    private async Task<HttpClient> AuthenticatedClientAsync(WebApplicationFactory<Program>? factory = null)
    {
        factory ??= _factory;
        HttpResponseMessage login = await factory.CreateClient()
            .PostAsJsonAsync("/api/v1/auth/guest", new AuthGuestRequest(null));
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        AuthGuestResponse body = (await login.Content.ReadFromJsonAsync<AuthGuestResponse>())!;

        HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.AccessToken);
        return client;
    }

    private static async Task<Guid> SaveTeamAsync(HttpClient client)
    {
        var request = new SaveTeamRequest(
            BattlePostgresApiFactory.SeededHeroIds.Select((h, i) => new TeamSlotDto(i, h)).ToList());
        HttpResponseMessage save = await client.PostAsJsonAsync("/api/v1/team", request);
        save.StatusCode.Should().Be(HttpStatusCode.OK);
        TeamDto team = (await save.Content.ReadFromJsonAsync<TeamDto>())!;
        return team.Id;
    }

    private static Task<HttpResponseMessage> PostBattleAsync(HttpClient client, Guid teamId, string attemptId)
        => client.PostAsJsonAsync("/api/v1/battles", new StartBattleRequest(teamId, BattlePostgresApiFactory.StageId, attemptId));

    private static async Task<BattleResultDto> StartBattleAsync(
        HttpClient client, Guid teamId, string attemptId, HttpStatusCode expected)
    {
        HttpResponseMessage response = await PostBattleAsync(client, teamId, attemptId);
        response.StatusCode.Should().Be(expected);
        return (await response.Content.ReadFromJsonAsync<BattleResultDto>())!;
    }

    private async Task<(int Records, long Gold)> InspectAsync(string attemptId)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        List<BattleRecord> records = await db.BattleRecords.Where(r => r.AttemptId == attemptId).ToListAsync();
        long gold = 0;
        if (records.Count > 0)
        {
            var wallet = await db.Wallets.FirstOrDefaultAsync(w => w.ProfileId == records[0].ProfileId);
            gold = wallet?.BalanceOf("gold") ?? 0;
        }

        return (records.Count, gold);
    }
}

/// <summary>Repo battle record LUÔN ném ở AddAsync — mô phỏng lỗi persistence giữa transaction (test rollback).</summary>
internal sealed class ThrowingBattleRecordRepository : IBattleRecordRepository
{
    public Task<BattleRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => Task.FromResult<BattleRecord?>(null);

    public Task<BattleRecord?> GetByProfileAndAttemptAsync(Guid profileId, string attemptId, CancellationToken cancellationToken)
        => Task.FromResult<BattleRecord?>(null);

    public Task AddAsync(BattleRecord entity, CancellationToken cancellationToken)
        => throw new InvalidOperationException("Lỗi persistence mô phỏng (test rollback).");
}

/// <summary>
/// Host thật + Testcontainers PostgreSQL + <see cref="IConfigProvider"/> stub combat-capable: 6 hero (guest sở hữu
/// qua seed) + 1 địch yếu + skill damage + formation 2×3 + stage demo + reward gold. Redis stub; real unit of work.
/// </summary>
public sealed class BattlePostgresApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string StageId = "stage_demo";

    public static readonly string[] SeededHeroIds =
        ["hero_a", "hero_b", "hero_c", "hero_d", "hero_e", "hero_f"];

    /// <summary>Config đội-được-đọc-bởi-sim — expose để test re-sim replay parity.</summary>
    public IConfigProvider Config { get; } = BuildConfig();

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
            services.AddSingleton<IConfigProvider>(Config);
        });
    }

    private static StubConfigProvider BuildConfig()
    {
        var stub = new StubConfigProvider();

        // 6 hero mạnh (đội) — chỉ số combat thật; skill cơ bản gây damage.
        for (int i = 0; i < SeededHeroIds.Length; i++)
        {
            stub.Set("hero", SeededHeroIds[i], $$"""
                { "schema_version": 1, "id": "{{SeededHeroIds[i]}}", "faction": "none", "class": "warrior",
                  "element": "fire", "role": "dps", "rarity": 5,
                  "base_stats": { "hp": 1000, "atk": 200, "def": 50, "spd": {{100 + i}} }, "skills": ["skill_basic"] }
                """);
        }

        // Địch yếu (guest cũng sở hữu qua seed nhưng không đưa vào đội) — hạ nhanh ⇒ VICTORY tất định.
        stub.Set("hero", "hero_dummy", """
            { "schema_version": 1, "id": "hero_dummy", "faction": "none", "class": "warrior",
              "element": "fire", "role": "dps", "rarity": 3,
              "base_stats": { "hp": 100, "atk": 10, "def": 10, "spd": 50 }, "skills": ["skill_basic"] }
            """);

        stub.Set("skill", "skill_basic", """
            { "target": "single_enemy", "trigger": { "type": "cooldown", "value": 0 },
              "effects": [ { "effect_type": "damage", "params": { "coeff_fixed": 1000 } } ] }
            """);

        stub.Set("formation", "formation_default", """
            { "schema_version": 1, "id": "formation_default", "rows": 2, "cols": 3 }
            """);

        stub.Set("stage", StageId, """
            {
              "max_rounds": 30,
              "basic_skill_id": "skill_basic",
              "combat_rules": {
                "def_constant_k": 300, "min_damage": 1, "crit_multiplier_fixed": 1500,
                "accuracy_bp": 10000, "crit_rate_bp": 0,
                "energy": { "initial": 0, "on_attack": 0, "on_hit": 0, "ultimate_cost": 100, "max": 100 }
              },
              "enemies": [ { "hero_id": "hero_dummy", "slot": 0 } ],
              "rewards": ["reward_demo"]
            }
            """);

        stub.Set("reward", "reward_demo", """
            { "entries": [ { "reward_type": "currency", "ref_id": "gold", "amount": 100 } ] }
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
