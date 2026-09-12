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
using GameTeam.Contracts.Inventory;
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
/// Phase 32 — kho đồ end-to-end trên host HTTP thật + PostgreSQL thật (Testcontainers): <c>GET
/// /api/v1/inventory</c> protected + trả kho server-authoritative; khách mới có kho seed (vật phẩm + mảnh) và
/// hero sở hữu được chiếu kèm; lọc theo loại + phân trang; hai khách mỗi người thấy kho của CHÍNH mình (owner từ
/// token — chống IDOR). Không có endpoint thêm/bớt công khai (cấp/tiêu là command nội bộ). Cần Docker.
/// </summary>
public sealed class InventoryEndpointTests : IClassFixture<InventoryPostgresApiFactory>
{
    private readonly InventoryPostgresApiFactory _factory;

    public InventoryEndpointTests(InventoryPostgresApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Inventory_endpoint_requires_authentication()
    {
        HttpResponseMessage response = await _factory.CreateClient().GetAsync("/api/v1/inventory");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task New_guest_has_seeded_items_fragments_and_projected_heroes()
    {
        HttpClient client = await AuthenticatedClientAsync();

        InventoryDto inventory = await GetInventoryAsync(client);

        inventory.Items.Should().Contain(x => x.ItemType == "item" && x.ItemId == "item_potion" && x.Quantity > 0,
            "guest mới được seed vật phẩm catalog (tạm, Phase 32)");
        inventory.Items.Should().Contain(x => x.ItemType == "fragment" && x.ItemId == "hero_ignis" && x.Quantity > 0,
            "guest mới được seed mảnh cho mỗi hero");
        inventory.OwnedHeroes.Select(h => h.HeroId).Should().BeEquivalentTo(InventoryPostgresApiFactory.SeededHeroIds,
            "hero sở hữu được chiếu kèm (Phase 27)");
    }

    [Fact]
    public async Task Filter_by_item_type_returns_only_that_type()
    {
        HttpClient client = await AuthenticatedClientAsync();

        InventoryDto items = await GetInventoryAsync(client, "?itemType=item");
        InventoryDto fragments = await GetInventoryAsync(client, "?itemType=fragment");

        items.Items.Should().OnlyContain(x => x.ItemType == "item").And.NotBeEmpty();
        fragments.Items.Should().OnlyContain(x => x.ItemType == "fragment").And.NotBeEmpty();
    }

    [Fact]
    public async Task Pagination_limits_returned_stacks()
    {
        HttpClient client = await AuthenticatedClientAsync();

        InventoryDto page = await GetInventoryAsync(client, "?page=1&pageSize=1");

        page.Items.Should().HaveCount(1, "pageSize=1 ⇒ đúng một stack mỗi trang");
    }

    [Fact]
    public async Task Invalid_filter_returns_400()
    {
        HttpClient client = await AuthenticatedClientAsync();

        HttpResponseMessage response = await client.GetAsync("/api/v1/inventory?itemType=weapon");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Two_guests_each_see_their_own_inventory()
    {
        HttpClient a = await AuthenticatedClientAsync();
        HttpClient b = await AuthenticatedClientAsync();

        InventoryDto inventoryA = await GetInventoryAsync(a);
        InventoryDto inventoryB = await GetInventoryAsync(b);

        // Mỗi khách có kho riêng (owner suy từ token) — không rò kho người khác; nội dung seed giống nhau.
        inventoryA.Items.Should().NotBeEmpty();
        inventoryB.Items.Should().NotBeEmpty();
        inventoryA.OwnedHeroes.Select(h => h.HeroId).Should().BeEquivalentTo(InventoryPostgresApiFactory.SeededHeroIds);
        inventoryB.OwnedHeroes.Select(h => h.HeroId).Should().BeEquivalentTo(InventoryPostgresApiFactory.SeededHeroIds);
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

    private static async Task<InventoryDto> GetInventoryAsync(HttpClient client, string query = "")
    {
        HttpResponseMessage response = await client.GetAsync($"/api/v1/inventory{query}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<InventoryDto>())!;
    }
}

/// <summary>
/// Host thật + Testcontainers PostgreSQL + <see cref="IConfigProvider"/> stub: 2 hero (guest sở hữu + seed mảnh)
/// + 2 catalog item (seed vật phẩm). Redis stub; real unit of work. JWT khớp <see cref="ApiTestFactory"/>.
/// </summary>
public sealed class InventoryPostgresApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
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

            services.RemoveAll<IConfigProvider>();
            services.AddSingleton<IConfigProvider>(BuildConfig());
        });
    }

    private static StubConfigProvider BuildConfig()
    {
        var stub = new StubConfigProvider();
        // Hero (guest sở hữu + seed mảnh) — chỉ cần id hợp lệ cho seed/validation.
        stub.Set("hero", "hero_ignis", """
            { "schema_version": 1, "id": "hero_ignis", "faction": "none", "class": "mage",
              "element": "fire", "role": "dps", "rarity": 5,
              "base_stats": { "hp": 900, "atk": 220, "def": 60, "spd": 110 }, "skills": ["skill_ignis_strike"] }
            """);
        stub.Set("hero", "hero_aqua", """
            { "schema_version": 1, "id": "hero_aqua", "faction": "none", "class": "support",
              "element": "water", "role": "healer", "rarity": 4,
              "base_stats": { "hp": 1100, "atk": 90, "def": 80, "spd": 95 }, "skills": ["skill_aqua_heal"] }
            """);
        // Catalog item (seed vật phẩm) — chỉ cần id hợp lệ cho seed/validation.
        stub.Set("item", "item_potion", """{ "schema_version": 1, "id": "item_potion", "item_type": "item" }""");
        stub.Set("item", "item_ore", """{ "schema_version": 1, "id": "item_ore", "item_type": "item" }""");
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
