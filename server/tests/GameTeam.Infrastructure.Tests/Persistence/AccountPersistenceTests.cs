using FluentAssertions;
using GameTeam.Domain.Accounts;
using GameTeam.Infrastructure.Persistence;
using GameTeam.Infrastructure.Persistence.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GameTeam.Infrastructure.Tests.Persistence;

/// <summary>
/// Phase 18 — the <see cref="Account"/> aggregate persists on real PostgreSQL (Testcontainers): the
/// guest account round-trips through repository + unit of work, and its <see cref="AccountCreated"/>
/// domain event dispatches at <c>SaveChanges</c>. Requires Docker.
/// </summary>
public sealed class AccountPersistenceTests : IClassFixture<PostgresContainerFixture>, IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture;

    public AccountPersistenceTests(PostgresContainerFixture fixture) => _fixture = fixture;

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

    [Fact]
    public async Task Guest_account_is_persisted_and_read_back()
    {
        Guid id = Guid.NewGuid();
        DateTimeOffset createdAt = new(2026, 8, 21, 9, 0, 0, TimeSpan.Zero);

        await using (TestDbContext writeContext = NewContext(new NoOpPublisher()))
        {
            var repository = new EfRepository<Account, Guid>(writeContext);
            var unitOfWork = new UnitOfWork(writeContext);

            await unitOfWork.BeginTransactionAsync(CancellationToken.None);
            await repository.AddAsync(Account.CreateGuest(id, createdAt), CancellationToken.None);
            await unitOfWork.CommitAsync(CancellationToken.None);
        }

        await using TestDbContext readContext = NewContext(new NoOpPublisher());
        var readRepository = new EfRepository<Account, Guid>(readContext);

        Account? loaded = await readRepository.GetByIdAsync(id, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.Type.Should().Be(AccountType.Guest);
        loaded.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public async Task Saving_guest_account_dispatches_AccountCreated()
    {
        var collector = new DispatchedEventsCollector();
        ServiceProvider provider = new ServiceCollection()
            .AddSingleton(collector)
            .AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(RecordingDomainEventHandler).Assembly))
            .BuildServiceProvider();

        Guid id = Guid.NewGuid();
        Account account = Account.CreateGuest(id, new DateTimeOffset(2026, 8, 21, 9, 0, 0, TimeSpan.Zero));

        await using (TestDbContext context = NewContext(provider.GetRequiredService<IPublisher>()))
        {
            context.Add(account);
            await context.SaveChangesAsync(CancellationToken.None);
        }

        collector.Events.Should().ContainSingle()
            .Which.Should().BeOfType<AccountCreated>()
            .Which.AccountId.Should().Be(id);

        account.DomainEvents.Should().BeEmpty("event phải được clear sau khi dispatch.");
    }
}
