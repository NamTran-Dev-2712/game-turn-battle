using FluentAssertions;
using GameTeam.Domain.Accounts;
using GameTeam.Domain.Campaign;
using GameTeam.Domain.Profiles;
using GameTeam.Infrastructure.Persistence;
using GameTeam.Infrastructure.Persistence.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GameTeam.Infrastructure.Tests.Persistence;

/// <summary>
/// Phase 34 — <see cref="CampaignProgress"/> persist trên PostgreSQL thật (Testcontainers): round-trip qua
/// repository (tra theo profile, kèm owned <c>campaign_cleared_stages</c> + current AFK stage); một tiến độ /
/// một profile (unique index); clear stage lưu bền + dispatch <see cref="CampaignStageCleared"/> tại
/// <c>SaveChanges</c>. Yêu cầu Docker.
/// </summary>
public sealed class CampaignProgressPersistenceTests : IClassFixture<PostgresContainerFixture>, IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 9, 13, 9, 0, 0, TimeSpan.Zero);

    private readonly PostgresContainerFixture _fixture;

    public CampaignProgressPersistenceTests(PostgresContainerFixture fixture) => _fixture = fixture;

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
    public async Task Progress_is_persisted_and_read_back_with_cleared_stages_and_afk()
    {
        Guid profileId = await SeedProfileAsync();

        await using (TestDbContext writeContext = NewContext(new NoOpPublisher()))
        {
            var repository = new CampaignProgressRepository(writeContext);
            var unitOfWork = new UnitOfWork(writeContext);

            CampaignProgress progress = CampaignProgress.Create(Guid.NewGuid(), profileId, Now);
            progress.MarkStageCleared("stage_ch01_01", "stage_ch01_01", Now);
            progress.MarkStageCleared("stage_ch01_02", "stage_ch01_02", Now.AddMinutes(1));

            await unitOfWork.BeginTransactionAsync(CancellationToken.None);
            await repository.AddAsync(progress, CancellationToken.None);
            await unitOfWork.CommitAsync(CancellationToken.None);
        }

        await using TestDbContext readContext = NewContext(new NoOpPublisher());
        CampaignProgress? loaded = await new CampaignProgressRepository(readContext)
            .GetByProfileIdAsync(profileId, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.ProfileId.Should().Be(profileId);
        loaded.CurrentAfkStageId.Should().Be("stage_ch01_02");
        loaded.ClearedStages.Select(c => c.StageId).Should().BeEquivalentTo("stage_ch01_01", "stage_ch01_02");
    }

    [Fact]
    public async Task Second_progress_for_same_profile_violates_unique_index()
    {
        Guid profileId = await SeedProfileAsync();

        await using TestDbContext context = NewContext(new NoOpPublisher());
        context.CampaignProgresses.Add(CampaignProgress.Create(Guid.NewGuid(), profileId, Now));
        context.CampaignProgresses.Add(CampaignProgress.Create(Guid.NewGuid(), profileId, Now));

        Func<Task> act = () => context.SaveChangesAsync(CancellationToken.None);

        await act.Should().ThrowAsync<DbUpdateException>("unique profile_id ⇒ một tiến độ / một profile");
    }

    [Fact]
    public async Task Marking_stage_cleared_dispatches_CampaignStageCleared()
    {
        Guid profileId = await SeedProfileAsync();

        var collector = new DispatchedEventsCollector();
        ServiceProvider provider = new ServiceCollection()
            .AddSingleton(collector)
            .AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(RecordingDomainEventHandler).Assembly))
            .BuildServiceProvider();

        CampaignProgress progress = CampaignProgress.Create(Guid.NewGuid(), profileId, Now);
        progress.MarkStageCleared("stage_ch01_01", "stage_ch01_01", Now);

        await using (TestDbContext context = NewContext(provider.GetRequiredService<IPublisher>()))
        {
            context.CampaignProgresses.Add(progress);
            await context.SaveChangesAsync(CancellationToken.None);
        }

        collector.Events.Should().Contain(e => e is CampaignStageCleared);
        progress.DomainEvents.Should().BeEmpty("event phải được clear sau khi dispatch.");
    }
}
