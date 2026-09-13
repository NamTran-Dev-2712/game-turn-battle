using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using GameTeam.Domain.Economy;
using GameTeam.Domain.Heroes;
using GameTeam.Domain.Profiles;
using GameTeam.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GameTeam.Api.IntegrationTests;

/// <summary>
/// Test-only DB seeding helpers over the real host (Testcontainers). Since Phase 33 removed the temporary
/// "guest login grants all heroes" seed (summon is now the real acquisition), integration tests that need a
/// profile to already own heroes / hold currency seed them directly in the DB, scoped to the caller's profile
/// (resolved from the JWT <c>sub</c>). This is a test convenience — it never bypasses the server-authoritative
/// command paths under test (summon/team/battle still run through MediatR).
/// </summary>
internal static class IntegrationTestSeeding
{
    /// <summary>Grant <paramref name="heroIds"/> as owned heroes to the profile of the token's owner (idempotent).</summary>
    public static async Task GrantHeroesAsync(
        WebApplicationFactory<Program> factory, string accessToken, params string[] heroIds)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        PlayerProfile profile = await ResolveProfileAsync(db, accessToken);

        DateTimeOffset now = DateTimeOffset.UtcNow;
        foreach (string heroId in heroIds)
        {
            bool owned = await db.OwnedHeroes.AnyAsync(h => h.ProfileId == profile.Id && h.HeroId == heroId);
            if (!owned)
            {
                db.OwnedHeroes.Add(OwnedHero.Grant(
                    Guid.NewGuid(), profile.Id, heroId, OwnedHero.InitialLevel, OwnedHero.InitialStars, now));
            }
        }

        await db.SaveChangesAsync();
    }

    /// <summary>Grant <paramref name="quantity"/> fragments of each hero in <paramref name="heroIds"/> to the token owner's inventory.</summary>
    public static async Task GrantFragmentsAsync(
        WebApplicationFactory<Program> factory, string accessToken, long quantity, params string[] heroIds)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        PlayerProfile profile = await ResolveProfileAsync(db, accessToken);

        DateTimeOffset now = DateTimeOffset.UtcNow;
        Domain.Inventory.Inventory? inventory = await db.Inventories.FirstOrDefaultAsync(i => i.ProfileId == profile.Id);
        if (inventory is null)
        {
            inventory = Domain.Inventory.Inventory.CreateFor(Guid.NewGuid(), profile.Id, now);
            db.Inventories.Add(inventory);
        }

        foreach (string heroId in heroIds)
        {
            inventory.Add("fragment", heroId, quantity, now);
        }

        await db.SaveChangesAsync();
    }

    /// <summary>Credit <paramref name="amount"/> of <paramref name="currency"/> to the token owner's wallet.</summary>
    public static async Task CreditCurrencyAsync(
        WebApplicationFactory<Program> factory, string accessToken, string currency, long amount)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        PlayerProfile profile = await ResolveProfileAsync(db, accessToken);

        DateTimeOffset now = DateTimeOffset.UtcNow;
        Wallet? wallet = await db.Wallets.FirstOrDefaultAsync(w => w.ProfileId == profile.Id);
        if (wallet is null)
        {
            wallet = Wallet.CreateFor(Guid.NewGuid(), profile.Id, now);
            db.Wallets.Add(wallet);
        }

        wallet.Credit(currency, amount, now);
        await db.SaveChangesAsync();
    }

    private static async Task<PlayerProfile> ResolveProfileAsync(AppDbContext db, string accessToken)
    {
        Guid accountId = Guid.Parse(new JwtSecurityTokenHandler().ReadJwtToken(accessToken).Subject);
        return await db.PlayerProfiles.FirstAsync(p => p.AccountId == accountId);
    }
}
