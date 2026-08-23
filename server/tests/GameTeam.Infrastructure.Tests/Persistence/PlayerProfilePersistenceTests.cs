using FluentAssertions;
using GameTeam.Domain.Accounts;
using GameTeam.Domain.Profiles;
using GameTeam.Infrastructure.Persistence;
using GameTeam.Infrastructure.Persistence.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GameTeam.Infrastructure.Tests.Persistence;

/// <summary>
/// Phase 19 — the <see cref="PlayerProfile"/> aggregate persists on real PostgreSQL (Testcontainers):
/// round-trips through the repository, enforces one profile per account at the DB level (unique
/// <c>account_id</c> index — idempotency backstop), dispatches <see cref="PlayerProfileCreated"/> at
/// <c>SaveChanges</c>, and migrates a legacy record on read without losing data (ADR-007). Requires Docker.
/// </summary>
public sealed class PlayerProfilePersistenceTests : IClassFixture<PostgresContainerFixture>, IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 8, 23, 9, 0, 0, TimeSpan.Zero);

    private readonly PostgresContainerFixture _fixture;

    public PlayerProfilePersistenceTests(PostgresContainerFixture fixture) => _fixture = fixture;

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

    private async Task<Guid> SeedAccountAsync()
    {
        Guid accountId = Guid.NewGuid();
        await using TestDbContext context = NewContext(new NoOpPublisher());
        context.Accounts.Add(Account.CreateGuest(accountId, Now));
        await context.SaveChangesAsync(CancellationToken.None);
        return accountId;
    }

    [Fact]
    public async Task Profile_is_persisted_and_read_back_by_account()
    {
        Guid accountId = await SeedAccountAsync();
        Guid profileId = Guid.NewGuid();

        await using (TestDbContext writeContext = NewContext(new NoOpPublisher()))
        {
            var repository = new PlayerProfileRepository(writeContext);
            var unitOfWork = new UnitOfWork(writeContext);

            await unitOfWork.BeginTransactionAsync(CancellationToken.None);
            await repository.AddAsync(
                PlayerProfile.CreateForAccount(profileId, accountId, Now), CancellationToken.None);
            await unitOfWork.CommitAsync(CancellationToken.None);
        }

        await using TestDbContext readContext = NewContext(new NoOpPublisher());
        var readRepository = new PlayerProfileRepository(readContext);

        PlayerProfile? loaded = await readRepository.GetByAccountIdAsync(accountId, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(profileId);
        loaded.AccountId.Should().Be(accountId);
        loaded.SchemaVersion.Should().Be(PlayerProfile.CurrentSchemaVersion);
        loaded.Level.Should().Be(PlayerProfile.InitialLevel);
    }

    [Fact]
    public async Task Second_profile_for_same_account_violates_unique_index()
    {
        Guid accountId = await SeedAccountAsync();

        await using TestDbContext context = NewContext(new NoOpPublisher());
        context.PlayerProfiles.Add(PlayerProfile.CreateForAccount(Guid.NewGuid(), accountId, Now));
        context.PlayerProfiles.Add(PlayerProfile.CreateForAccount(Guid.NewGuid(), accountId, Now));

        Func<Task> act = () => context.SaveChangesAsync(CancellationToken.None);

        await act.Should().ThrowAsync<DbUpdateException>(
            "the unique account_id index guarantees at most one profile per account");
    }

    [Fact]
    public async Task Saving_new_profile_dispatches_PlayerProfileCreated()
    {
        Guid accountId = await SeedAccountAsync();

        var collector = new DispatchedEventsCollector();
        ServiceProvider provider = new ServiceCollection()
            .AddSingleton(collector)
            .AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(RecordingDomainEventHandler).Assembly))
            .BuildServiceProvider();

        Guid profileId = Guid.NewGuid();
        PlayerProfile profile = PlayerProfile.CreateForAccount(profileId, accountId, Now);

        await using (TestDbContext context = NewContext(provider.GetRequiredService<IPublisher>()))
        {
            context.PlayerProfiles.Add(profile);
            await context.SaveChangesAsync(CancellationToken.None);
        }

        collector.Events.Should().ContainSingle()
            .Which.Should().BeOfType<PlayerProfileCreated>()
            .Which.AccountId.Should().Be(accountId);

        profile.DomainEvents.Should().BeEmpty("event phải được clear sau khi dispatch.");
    }

    [Fact]
    public async Task Legacy_v0_profile_migrates_to_current_on_read_and_preserves_data()
    {
        // Persist a legacy record straight to the table: schema_version = 0, empty display name, Level = 7.
        Guid accountId = await SeedAccountAsync();
        Guid profileId = Guid.NewGuid();
        const int preservedLevel = 7;

        await using (TestDbContext seedContext = NewContext(new NoOpPublisher()))
        {
            seedContext.PlayerProfiles.Add(PlayerProfile.Restore(
                profileId, accountId, displayName: string.Empty, level: preservedLevel,
                schemaVersion: 0, createdAt: Now, updatedAt: Now));
            await seedContext.SaveChangesAsync(CancellationToken.None);
        }

        // Read-repair: load, Upgrade, persist (as the get-or-create command does inside its transaction).
        await using (TestDbContext migrateContext = NewContext(new NoOpPublisher()))
        {
            var repository = new PlayerProfileRepository(migrateContext);
            PlayerProfile legacy = (await repository.GetByAccountIdAsync(accountId, CancellationToken.None))!;
            legacy.SchemaVersion.Should().Be(0, "seed persisted a legacy record");

            legacy.Upgrade(Now.AddHours(1)).Should().BeTrue();
            await migrateContext.SaveChangesAsync(CancellationToken.None);
        }

        await using TestDbContext verifyContext = NewContext(new NoOpPublisher());
        PlayerProfile migrated =
            (await new PlayerProfileRepository(verifyContext).GetByAccountIdAsync(accountId, CancellationToken.None))!;

        migrated.SchemaVersion.Should().Be(PlayerProfile.CurrentSchemaVersion);
        migrated.Level.Should().Be(preservedLevel, "migration must preserve existing player data");
        migrated.DisplayName.Should().NotBeNullOrWhiteSpace("v0→v1 back-fills the display name");
    }
}
