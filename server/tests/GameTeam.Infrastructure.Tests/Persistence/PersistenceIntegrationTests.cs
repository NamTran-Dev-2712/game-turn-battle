using FluentAssertions;
using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Infrastructure.Persistence;
using GameTeam.Infrastructure.Persistence.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GameTeam.Infrastructure.Tests.Persistence;

/// <summary>
/// Hợp đồng persistence Phase 11 trên PostgreSQL thật (Testcontainers): CRUD qua repository/UoW,
/// transaction rollback thực sự rollback, và domain event dispatch tại SaveChanges. Yêu cầu Docker.
/// </summary>
public sealed class PersistenceIntegrationTests : IClassFixture<PostgresContainerFixture>, IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture;

    public PersistenceIntegrationTests(PostgresContainerFixture fixture) => _fixture = fixture;

    private TestDbContext NewContext(IPublisher publisher) =>
        new(
            new DbContextOptionsBuilder<TestDbContext>().UseNpgsql(_fixture.ConnectionString).Options,
            new DomainEventDispatcher(publisher));

    /// <summary>Tạo schema (schema_metadata + sample_aggregate) một lần cho container.</summary>
    public async Task InitializeAsync()
    {
        await using TestDbContext context = NewContext(new NoOpPublisher());
        await context.Database.EnsureCreatedAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Commit_persists_entity_and_can_be_read_back()
    {
        Guid id = Guid.NewGuid();

        await using (TestDbContext writeContext = NewContext(new NoOpPublisher()))
        {
            var repository = new EfRepository<SampleAggregate, Guid>(writeContext);
            var unitOfWork = new UnitOfWork(writeContext);

            await unitOfWork.BeginTransactionAsync(CancellationToken.None);
            await repository.AddAsync(new SampleAggregate(id, "crud"), CancellationToken.None);
            await unitOfWork.CommitAsync(CancellationToken.None);
        }

        await using TestDbContext readContext = NewContext(new NoOpPublisher());
        var readRepository = new EfRepository<SampleAggregate, Guid>(readContext);

        SampleAggregate? loaded = await readRepository.GetByIdAsync(id, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.Name.Should().Be("crud");
    }

    [Fact]
    public async Task Rollback_discards_persisted_but_uncommitted_changes()
    {
        Guid id = Guid.NewGuid();

        await using (TestDbContext writeContext = NewContext(new NoOpPublisher()))
        {
            var unitOfWork = new UnitOfWork(writeContext);

            await unitOfWork.BeginTransactionAsync(CancellationToken.None);
            writeContext.Add(new SampleAggregate(id, "rollback"));
            // Persist BÊN TRONG transaction (ghi thật xuống DB), rồi rollback ⇒ phải bị huỷ.
            await writeContext.SaveChangesAsync(CancellationToken.None);
            await unitOfWork.RollbackAsync(CancellationToken.None);
        }

        await using TestDbContext readContext = NewContext(new NoOpPublisher());
        var readRepository = new EfRepository<SampleAggregate, Guid>(readContext);

        SampleAggregate? loaded = await readRepository.GetByIdAsync(id, CancellationToken.None);

        loaded.Should().BeNull("rollback phải huỷ hàng đã ghi trong transaction chưa commit.");
    }

    [Fact]
    public async Task SaveChanges_dispatches_domain_events_after_persist_then_clears()
    {
        var collector = new DispatchedEventsCollector();
        ServiceProvider provider = new ServiceCollection()
            .AddSingleton(collector)
            .AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(RecordingDomainEventHandler).Assembly))
            .BuildServiceProvider();

        var id = Guid.NewGuid();
        SampleAggregate aggregate = new(id, "with-event");

        await using (TestDbContext context = NewContext(provider.GetRequiredService<IPublisher>()))
        {
            context.Add(aggregate);
            await context.SaveChangesAsync(CancellationToken.None);
        }

        collector.Events.Should().ContainSingle()
            .Which.Should().BeOfType<SampleCreated>()
            .Which.Id.Should().Be(id);

        aggregate.DomainEvents.Should().BeEmpty("event phải được clear sau khi dispatch.");
    }
}
