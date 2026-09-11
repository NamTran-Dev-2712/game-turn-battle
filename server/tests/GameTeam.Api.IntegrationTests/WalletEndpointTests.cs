using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using FluentAssertions;
using GameTeam.Contracts.Auth;
using GameTeam.Contracts.Battle;
using GameTeam.Contracts.Economy;
using GameTeam.Contracts.Enums;
using GameTeam.Contracts.Team;
using GameTeam.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GameTeam.Api.IntegrationTests;

/// <summary>
/// Phase 31 — ví/tiền tệ end-to-end trên host HTTP thật + PostgreSQL thật (Testcontainers, tái dùng
/// <see cref="BattlePostgresApiFactory"/>): <c>GET /api/v1/wallet</c> protected + trả số dư server-authoritative;
/// khách mới ⇒ ví rỗng; thưởng trận (Phase 30) cấp gold QUA cơ chế giao dịch (ghi ledger) và hiện ở GET /wallet;
/// retry cùng attemptId KHÔNG double-credit (số dư + ledger không đổi). Cần Docker.
/// </summary>
public sealed class WalletEndpointTests : IClassFixture<BattlePostgresApiFactory>
{
    private readonly BattlePostgresApiFactory _factory;

    public WalletEndpointTests(BattlePostgresApiFactory factory) => _factory = factory;

    // Enum wire = chuỗi (JsonStringEnumConverter) — client/test phải khớp.
    private static readonly JsonSerializerOptions WireJson =
        new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    [Fact]
    public async Task Wallet_endpoint_requires_authentication()
    {
        HttpResponseMessage response = await _factory.CreateClient().GetAsync("/api/v1/wallet");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task New_guest_has_empty_wallet()
    {
        HttpClient client = await AuthenticatedClientAsync();

        WalletDto wallet = await GetWalletAsync(client);

        wallet.Balances.Should().BeEmpty("khách mới chưa có giao dịch ⇒ ví rỗng, không lỗi");
    }

    [Fact]
    public async Task Battle_reward_credits_wallet_and_is_visible_via_get_wallet_with_ledger_row()
    {
        HttpClient client = await AuthenticatedClientAsync();
        Guid teamId = await SaveTeamAsync(client);
        string attempt = Guid.NewGuid().ToString();

        await StartBattleAsync(client, teamId, attempt);

        WalletDto wallet = await GetWalletAsync(client);
        wallet.Balances.Should().Contain(b => b.Currency == Currency.Gold && b.Amount == 100,
            "thưởng trận cấp gold qua cơ chế giao dịch, hiện ở GET /wallet (server-authoritative)");
        (await LedgerCountAsync($"battle:{attempt}:gold")).Should().Be(1, "một dòng ledger audit cho lần cấp thưởng");
    }

    [Fact]
    public async Task Battle_retry_does_not_double_credit_wallet_or_ledger()
    {
        HttpClient client = await AuthenticatedClientAsync();
        Guid teamId = await SaveTeamAsync(client);
        string attempt = Guid.NewGuid().ToString();

        await StartBattleAsync(client, teamId, attempt);
        await StartBattleAsync(client, teamId, attempt); // retry cùng attemptId

        WalletDto wallet = await GetWalletAsync(client);
        wallet.Balances.Single(b => b.Currency == Currency.Gold).Amount.Should().Be(100, "không double-grant khi retry");
        (await LedgerCountAsync($"battle:{attempt}:gold")).Should().Be(1, "ledger không ghi lần hai khi retry");
    }

    // ── helpers ───────────────────────────────────────────────────────────────────────────────────
    private async Task<HttpClient> AuthenticatedClientAsync()
    {
        HttpResponseMessage login = await _factory.CreateClient()
            .PostAsJsonAsync("/api/v1/auth/guest", new AuthGuestRequest(null));
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        AuthGuestResponse body = (await login.Content.ReadFromJsonAsync<AuthGuestResponse>())!;

        HttpClient client = _factory.CreateClient();
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

    private static async Task StartBattleAsync(HttpClient client, Guid teamId, string attempt)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/battles", new StartBattleRequest(teamId, BattlePostgresApiFactory.StageId, attempt));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task<WalletDto> GetWalletAsync(HttpClient client)
    {
        HttpResponseMessage response = await client.GetAsync("/api/v1/wallet");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        string json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<WalletDto>(json, WireJson)!;
    }

    private async Task<int> LedgerCountAsync(string idempotencyKey)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.CurrencyTransactions.CountAsync(t => t.IdempotencyKey == idempotencyKey);
    }
}
