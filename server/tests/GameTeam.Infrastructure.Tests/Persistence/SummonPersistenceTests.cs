using FluentAssertions;
using GameTeam.Domain.Accounts;
using GameTeam.Domain.Gacha;
using GameTeam.Domain.Profiles;
using GameTeam.Infrastructure.Persistence;
using GameTeam.Infrastructure.Persistence.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameTeam.Infrastructure.Tests.Persistence;

/// <summary>
/// Phase 33 — <see cref="SummonRecord"/> + <see cref="GachaPity"/> persist trên PostgreSQL thật (Testcontainers):
/// round-trip cột JSON (pulls), ràng buộc <b>unique (profile_id, request_id)</b> và <b>(profile_id, banner_id)</b>
/// (idempotency + một pity/banner tầng DB), và khoá dòng pity (<c>FOR UPDATE</c>) trong transaction. Cần Docker.
/// </summary>
public sealed class SummonPersistenceTests : IClassFixture<PostgresContainerFixture>, IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 9, 12, 9, 0, 0, TimeSpan.Zero);
    private const string Banner = "gacha_standard";

    private readonly PostgresContainerFixture _fixture;

    public SummonPersistenceTests(PostgresContainerFixture fixture) => _fixture = fixture;

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
    public async Task Summon_record_round_trips_with_pulls_json()
    {
        Guid profileId = await SeedProfileAsync();
        var pulls = new[]
        {
            new SummonPullLine("hero_ignis", 5, isNew: true, fragments: 0),
            new SummonPullLine("hero_ignis", 5, isNew: false, fragments: 50),
        };

        await using (TestDbContext write = NewContext(new NoOpPublisher()))
        {
            var repo = new SummonRecordRepository(write);
            var uow = new UnitOfWork(write);
            await uow.BeginTransactionAsync(CancellationToken.None);
            await repo.AddAsync(
                SummonRecord.Create(Guid.NewGuid(), profileId, "req-1", Banner, 2, 987654, pityAfter: 3, pulls, Now),
                CancellationToken.None);
            await uow.CommitAsync(CancellationToken.None);
        }

        await using TestDbContext read = NewContext(new NoOpPublisher());
        SummonRecord? loaded = await new SummonRecordRepository(read)
            .GetByProfileAndRequestAsync(profileId, "req-1", CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.BannerId.Should().Be(Banner);
        loaded.Seed.Should().Be(987654);
        loaded.PityAfter.Should().Be(3);
        loaded.Pulls.Should().HaveCount(2);
        loaded.Pulls[0].IsNew.Should().BeTrue();
        loaded.Pulls[1].Fragments.Should().Be(50);
    }

    [Fact]
    public async Task Duplicate_profile_and_request_violates_unique_index()
    {
        Guid profileId = await SeedProfileAsync();

        await using TestDbContext context = NewContext(new NoOpPublisher());
        context.SummonRecords.Add(SummonRecord.Create(
            Guid.NewGuid(), profileId, "req-dup", Banner, 1, 1, 0, Array.Empty<SummonPullLine>(), Now));
        context.SummonRecords.Add(SummonRecord.Create(
            Guid.NewGuid(), profileId, "req-dup", Banner, 1, 2, 0, Array.Empty<SummonPullLine>(), Now));

        Func<Task> act = () => context.SaveChangesAsync(CancellationToken.None);

        await act.Should().ThrowAsync<DbUpdateException>(
            "unique (profile_id, request_id) chống double khi race (idempotency tầng DB)");
    }

    [Fact]
    public async Task Gacha_pity_round_trips_and_for_update_returns_locked_row()
    {
        Guid profileId = await SeedProfileAsync();

        await using (TestDbContext write = NewContext(new NoOpPublisher()))
        {
            var repo = new GachaPityRepository(write);
            var uow = new UnitOfWork(write);
            await uow.BeginTransactionAsync(CancellationToken.None);
            GachaPity pity = GachaPity.CreateFor(Guid.NewGuid(), profileId, Banner, Now);
            pity.SetCount(7, Now);
            await repo.AddAsync(pity, CancellationToken.None);
            await uow.CommitAsync(CancellationToken.None);
        }

        await using TestDbContext read = NewContext(new NoOpPublisher());
        var readRepo = new GachaPityRepository(read);
        var readUow = new UnitOfWork(read);
        await readUow.BeginTransactionAsync(CancellationToken.None);
        GachaPity? locked = await readRepo.GetByProfileAndBannerForUpdateAsync(profileId, Banner, CancellationToken.None);
        await readUow.CommitAsync(CancellationToken.None);

        locked.Should().NotBeNull("FOR UPDATE phải trả dòng pity hiện có trong transaction");
        locked!.Count.Should().Be(7);
    }

    [Fact]
    public async Task Duplicate_profile_and_banner_pity_violates_unique_index()
    {
        Guid profileId = await SeedProfileAsync();

        await using TestDbContext context = NewContext(new NoOpPublisher());
        context.GachaPities.Add(GachaPity.CreateFor(Guid.NewGuid(), profileId, Banner, Now));
        context.GachaPities.Add(GachaPity.CreateFor(Guid.NewGuid(), profileId, Banner, Now));

        Func<Task> act = () => context.SaveChangesAsync(CancellationToken.None);

        await act.Should().ThrowAsync<DbUpdateException>(
            "unique (profile_id, banner_id) — một bộ đếm pity cho mỗi (profile, banner)");
    }
}
