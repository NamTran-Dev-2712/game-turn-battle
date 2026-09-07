using FluentAssertions;
using GameTeam.Domain.Accounts;
using GameTeam.Domain.Profiles;
using GameTeam.Domain.Teams;
using GameTeam.Infrastructure.Persistence;
using GameTeam.Infrastructure.Persistence.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GameTeam.Infrastructure.Tests.Persistence;

/// <summary>
/// Phase 29 — the <see cref="Team"/> aggregate persists on real PostgreSQL (Testcontainers): round-trips
/// through the repository (loaded by profile, with owned <c>team_slots</c>), enforces one team per profile
/// (unique index), supports replace/upsert, and dispatches <see cref="TeamSaved"/> at <c>SaveChanges</c>.
/// Requires Docker.
/// </summary>
public sealed class TeamPersistenceTests : IClassFixture<PostgresContainerFixture>, IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 9, 6, 9, 0, 0, TimeSpan.Zero);

    private readonly PostgresContainerFixture _fixture;

    public TeamPersistenceTests(PostgresContainerFixture fixture) => _fixture = fixture;

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

    private static List<TeamSlot> Slots(params (int slot, string hero)[] items)
        => items.Select(i => new TeamSlot(i.slot, i.hero)).ToList();

    [Fact]
    public async Task Team_is_persisted_and_read_back_with_slots()
    {
        Guid profileId = await SeedProfileAsync();

        await using (TestDbContext writeContext = NewContext(new NoOpPublisher()))
        {
            var repository = new TeamRepository(writeContext);
            var unitOfWork = new UnitOfWork(writeContext);

            await unitOfWork.BeginTransactionAsync(CancellationToken.None);
            await repository.AddAsync(
                Team.Create(Guid.NewGuid(), profileId, Slots((0, "hero_a"), (1, "hero_b"), (2, "hero_c")), Now),
                CancellationToken.None);
            await unitOfWork.CommitAsync(CancellationToken.None);
        }

        await using TestDbContext readContext = NewContext(new NoOpPublisher());
        Team? loaded = await new TeamRepository(readContext).GetByProfileIdAsync(profileId, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.ProfileId.Should().Be(profileId);
        loaded.Slots.OrderBy(s => s.SlotIndex).Select(s => s.HeroId).Should().Equal("hero_a", "hero_b", "hero_c");
    }

    [Fact]
    public async Task Second_team_for_same_profile_violates_unique_index()
    {
        Guid profileId = await SeedProfileAsync();

        await using TestDbContext context = NewContext(new NoOpPublisher());
        context.Teams.Add(Team.Create(Guid.NewGuid(), profileId, Slots((0, "hero_a")), Now));
        context.Teams.Add(Team.Create(Guid.NewGuid(), profileId, Slots((0, "hero_b")), Now));

        Func<Task> act = () => context.SaveChangesAsync(CancellationToken.None);

        await act.Should().ThrowAsync<DbUpdateException>("the unique profile_id index allows one team per profile");
    }

    [Fact]
    public async Task Replace_upserts_slots_in_place()
    {
        Guid profileId = await SeedProfileAsync();
        Guid teamId = Guid.NewGuid();

        await using (TestDbContext writeContext = NewContext(new NoOpPublisher()))
        {
            var uow = new UnitOfWork(writeContext);
            await uow.BeginTransactionAsync(CancellationToken.None);
            await new TeamRepository(writeContext).AddAsync(
                Team.Create(teamId, profileId, Slots((0, "hero_a"), (1, "hero_b")), Now), CancellationToken.None);
            await uow.CommitAsync(CancellationToken.None);
        }

        await using (TestDbContext editContext = NewContext(new NoOpPublisher()))
        {
            var repo = new TeamRepository(editContext);
            var uow = new UnitOfWork(editContext);
            Team? team = await repo.GetByProfileIdAsync(profileId, CancellationToken.None);
            await uow.BeginTransactionAsync(CancellationToken.None);
            team!.Replace(Slots((0, "hero_b"), (1, "hero_a")), Now.AddMinutes(1));
            await uow.CommitAsync(CancellationToken.None);
        }

        await using TestDbContext readContext = NewContext(new NoOpPublisher());
        Team? reloaded = await new TeamRepository(readContext).GetByProfileIdAsync(profileId, CancellationToken.None);

        reloaded!.Id.Should().Be(teamId, "upsert cùng đội — không tạo đội mới");
        reloaded.Slots.OrderBy(s => s.SlotIndex).Select(s => s.HeroId).Should().Equal("hero_b", "hero_a");
    }

    [Fact]
    public async Task Saving_team_dispatches_TeamSaved()
    {
        Guid profileId = await SeedProfileAsync();

        var collector = new DispatchedEventsCollector();
        ServiceProvider provider = new ServiceCollection()
            .AddSingleton(collector)
            .AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(RecordingDomainEventHandler).Assembly))
            .BuildServiceProvider();

        Team team = Team.Create(Guid.NewGuid(), profileId, Slots((0, "hero_a")), Now);

        await using (TestDbContext context = NewContext(provider.GetRequiredService<IPublisher>()))
        {
            context.Teams.Add(team);
            await context.SaveChangesAsync(CancellationToken.None);
        }

        collector.Events.Should().ContainSingle().Which.Should().BeOfType<TeamSaved>()
            .Which.ProfileId.Should().Be(profileId);
        team.DomainEvents.Should().BeEmpty("event phải được clear sau khi dispatch.");
    }
}
