using FluentAssertions;
using GameTeam.Domain.Accounts;
using GameTeam.Domain.Heroes;
using GameTeam.Domain.Profiles;
using GameTeam.Infrastructure.Persistence;
using GameTeam.Infrastructure.Persistence.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GameTeam.Infrastructure.Tests.Persistence;

/// <summary>
/// Phase 27 — the <see cref="OwnedHero"/> aggregate persists on real PostgreSQL (Testcontainers): round-trips
/// through the repository (loaded by profile), enforces one grant per (profile, hero) at the DB level (unique
/// index), and dispatches <see cref="OwnedHeroGranted"/> at <c>SaveChanges</c>. Requires Docker.
/// </summary>
public sealed class OwnedHeroPersistenceTests : IClassFixture<PostgresContainerFixture>, IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 9, 5, 9, 0, 0, TimeSpan.Zero);

    private readonly PostgresContainerFixture _fixture;

    public OwnedHeroPersistenceTests(PostgresContainerFixture fixture) => _fixture = fixture;

    private TestDbContext NewContext(IPublisher publisher) =>
        new(
            new DbContextOptionsBuilder<TestDbContext>().UseNpgsql(_fixture.ConnectionString).Options,
            new DomainEventDispatcher(publisher));

    public async Task InitializeAsync()
    {
        await using TestDbContext context = NewContext(new NoOpPublisher());
        await context.Database.EnsureCreatedAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // Seed an account + profile (the FK owner) and return the profile id.
    private async Task<Guid> SeedProfileAsync()
    {
        Guid accountId = Guid.NewGuid();
        Guid profileId = Guid.NewGuid();
        await using TestDbContext context = NewContext(new NoOpPublisher());
        context.Accounts.Add(Account.CreateGuest(accountId, Now));
        context.PlayerProfiles.Add(PlayerProfile.CreateForAccount(profileId, accountId, Now));
        await context.SaveChangesAsync(CancellationToken.None);
        return profileId;
    }

    [Fact]
    public async Task Owned_heroes_are_persisted_and_read_back_by_profile()
    {
        Guid profileId = await SeedProfileAsync();

        await using (TestDbContext writeContext = NewContext(new NoOpPublisher()))
        {
            var repository = new OwnedHeroRepository(writeContext);
            var unitOfWork = new UnitOfWork(writeContext);

            await unitOfWork.BeginTransactionAsync(CancellationToken.None);
            await repository.AddAsync(OwnedHero.Grant(Guid.NewGuid(), profileId, "hero_ignis", 1, 1, Now), CancellationToken.None);
            await repository.AddAsync(OwnedHero.Grant(Guid.NewGuid(), profileId, "hero_aqua", 2, 3, Now), CancellationToken.None);
            await unitOfWork.CommitAsync(CancellationToken.None);
        }

        await using TestDbContext readContext = NewContext(new NoOpPublisher());
        IReadOnlyList<OwnedHero> loaded =
            await new OwnedHeroRepository(readContext).GetByProfileIdAsync(profileId, CancellationToken.None);

        loaded.Should().HaveCount(2);
        loaded.Select(h => h.HeroId).Should().Contain(new[] { "hero_ignis", "hero_aqua" });
        loaded.Should().OnlyContain(h => h.ProfileId == profileId);
    }

    [Fact]
    public async Task Second_grant_of_same_hero_to_same_profile_violates_unique_index()
    {
        Guid profileId = await SeedProfileAsync();

        await using TestDbContext context = NewContext(new NoOpPublisher());
        context.OwnedHeroes.Add(OwnedHero.Grant(Guid.NewGuid(), profileId, "hero_ignis", 1, 1, Now));
        context.OwnedHeroes.Add(OwnedHero.Grant(Guid.NewGuid(), profileId, "hero_ignis", 1, 1, Now));

        Func<Task> act = () => context.SaveChangesAsync(CancellationToken.None);

        await act.Should().ThrowAsync<DbUpdateException>(
            "the unique (profile_id, hero_id) index prevents duplicate grants");
    }

    [Fact]
    public async Task Granting_owned_hero_dispatches_OwnedHeroGranted()
    {
        Guid profileId = await SeedProfileAsync();

        var collector = new DispatchedEventsCollector();
        ServiceProvider provider = new ServiceCollection()
            .AddSingleton(collector)
            .AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(RecordingDomainEventHandler).Assembly))
            .BuildServiceProvider();

        OwnedHero hero = OwnedHero.Grant(Guid.NewGuid(), profileId, "hero_ignis", 1, 1, Now);

        await using (TestDbContext context = NewContext(provider.GetRequiredService<IPublisher>()))
        {
            context.OwnedHeroes.Add(hero);
            await context.SaveChangesAsync(CancellationToken.None);
        }

        collector.Events.Should().ContainSingle()
            .Which.Should().BeOfType<OwnedHeroGranted>()
            .Which.HeroId.Should().Be("hero_ignis");

        hero.DomainEvents.Should().BeEmpty("event phải được clear sau khi dispatch.");
    }
}
