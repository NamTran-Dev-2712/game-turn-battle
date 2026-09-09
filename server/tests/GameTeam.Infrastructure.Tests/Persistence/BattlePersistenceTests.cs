using FluentAssertions;
using GameTeam.Domain.Accounts;
using GameTeam.Domain.Battles;
using GameTeam.Domain.Economy;
using GameTeam.Domain.Profiles;
using GameTeam.Infrastructure.Persistence;
using GameTeam.Infrastructure.Persistence.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GameTeam.Infrastructure.Tests.Persistence;

/// <summary>
/// Phase 30 — <see cref="BattleRecord"/> + <see cref="Wallet"/> persist trên PostgreSQL thật (Testcontainers):
/// round-trip cột JSON (rewards/balances), ràng buộc <b>unique (profile_id, attempt_id)</b> (idempotency tầng DB),
/// credit ví lưu qua transaction, và dispatch <see cref="BattleResolved"/> ở <c>SaveChanges</c>. Cần Docker.
/// </summary>
public sealed class BattlePersistenceTests : IClassFixture<PostgresContainerFixture>, IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 9, 7, 9, 0, 0, TimeSpan.Zero);

    private readonly PostgresContainerFixture _fixture;

    public BattlePersistenceTests(PostgresContainerFixture fixture) => _fixture = fixture;

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

    [Fact]
    public async Task Battle_record_round_trips_with_rewards_json()
    {
        Guid profileId = await SeedProfileAsync();
        var rewards = new[] { new BattleReward("currency", "gold", 100) };

        await using (TestDbContext write = NewContext(new NoOpPublisher()))
        {
            var repo = new BattleRecordRepository(write);
            var uow = new UnitOfWork(write);
            await uow.BeginTransactionAsync(CancellationToken.None);
            await repo.AddAsync(
                BattleRecord.Create(Guid.NewGuid(), profileId, "attempt-1", Guid.NewGuid(), "stage_demo_01",
                    12345, "VICTORY", 3, rewards, "{\"event_log\":[]}", Now),
                CancellationToken.None);
            await uow.CommitAsync(CancellationToken.None);
        }

        await using TestDbContext read = NewContext(new NoOpPublisher());
        BattleRecord? loaded = await new BattleRecordRepository(read)
            .GetByProfileAndAttemptAsync(profileId, "attempt-1", CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.Outcome.Should().Be("VICTORY");
        loaded.Seed.Should().Be(12345);
        loaded.Rewards.Should().ContainSingle().Which.RefId.Should().Be("gold");
        loaded.Rewards[0].Amount.Should().Be(100);
    }

    [Fact]
    public async Task Duplicate_profile_and_attempt_violates_unique_index()
    {
        Guid profileId = await SeedProfileAsync();

        await using TestDbContext context = NewContext(new NoOpPublisher());
        context.BattleRecords.Add(BattleRecord.Create(
            Guid.NewGuid(), profileId, "attempt-x", Guid.NewGuid(), "stage_demo_01", 1, "VICTORY", 1,
            Array.Empty<BattleReward>(), "{}", Now));
        context.BattleRecords.Add(BattleRecord.Create(
            Guid.NewGuid(), profileId, "attempt-x", Guid.NewGuid(), "stage_demo_01", 2, "DEFEAT", 1,
            Array.Empty<BattleReward>(), "{}", Now));

        Func<Task> act = () => context.SaveChangesAsync(CancellationToken.None);

        await act.Should().ThrowAsync<DbUpdateException>(
            "unique (profile_id, attempt_id) chống double-grant khi race (idempotency tầng DB)");
    }

    [Fact]
    public async Task Wallet_credit_round_trips_with_balances_json()
    {
        Guid profileId = await SeedProfileAsync();
        Guid walletId = Guid.NewGuid();

        await using (TestDbContext write = NewContext(new NoOpPublisher()))
        {
            var repo = new WalletRepository(write);
            var uow = new UnitOfWork(write);
            await uow.BeginTransactionAsync(CancellationToken.None);
            Wallet wallet = Wallet.CreateFor(walletId, profileId, Now);
            wallet.Credit("gold", 100, Now);
            await repo.AddAsync(wallet, CancellationToken.None);
            await uow.CommitAsync(CancellationToken.None);
        }

        // Credit thêm trên thực thể được track ⇒ lưu ở SaveChanges (cùng cách handler làm trong transaction).
        await using (TestDbContext edit = NewContext(new NoOpPublisher()))
        {
            var repo = new WalletRepository(edit);
            var uow = new UnitOfWork(edit);
            Wallet? wallet = await repo.GetByProfileIdAsync(profileId, CancellationToken.None);
            await uow.BeginTransactionAsync(CancellationToken.None);
            wallet!.Credit("gold", 50, Now.AddMinutes(1));
            await uow.CommitAsync(CancellationToken.None);
        }

        await using TestDbContext read = NewContext(new NoOpPublisher());
        Wallet? reloaded = await new WalletRepository(read).GetByProfileIdAsync(profileId, CancellationToken.None);

        reloaded!.Id.Should().Be(walletId, "credit cập nhật ví hiện có — không tạo ví mới");
        reloaded.BalanceOf("gold").Should().Be(150);
    }

    [Fact]
    public async Task Saving_battle_record_dispatches_BattleResolved()
    {
        Guid profileId = await SeedProfileAsync();

        var collector = new DispatchedEventsCollector();
        ServiceProvider provider = new ServiceCollection()
            .AddSingleton(collector)
            .AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(RecordingBattleResolvedHandler).Assembly))
            .BuildServiceProvider();

        BattleRecord record = BattleRecord.Create(
            Guid.NewGuid(), profileId, "attempt-evt", Guid.NewGuid(), "stage_demo_01", 7, "VICTORY", 2,
            Array.Empty<BattleReward>(), "{}", Now);

        await using (TestDbContext context = NewContext(provider.GetRequiredService<IPublisher>()))
        {
            context.BattleRecords.Add(record);
            await context.SaveChangesAsync(CancellationToken.None);
        }

        collector.Events.Should().ContainSingle().Which.Should().BeOfType<BattleResolved>()
            .Which.ProfileId.Should().Be(profileId);
        record.DomainEvents.Should().BeEmpty("event phải được clear sau khi dispatch.");
    }
}
