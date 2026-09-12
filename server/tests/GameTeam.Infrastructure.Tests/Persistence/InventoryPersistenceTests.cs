using FluentAssertions;
using GameTeam.Application.Abstractions.Configuration;
using GameTeam.Application.Features.Inventory;
using GameTeam.Contracts.Config;
using GameTeam.Domain.Accounts;
using GameTeam.Domain.Common;
using GameTeam.Domain.Profiles;
using GameTeam.Infrastructure.Persistence;
using GameTeam.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;
using DomainInventory = GameTeam.Domain.Inventory.Inventory;

namespace GameTeam.Infrastructure.Tests.Persistence;

/// <summary>
/// Phase 32 — hệ kho đồ trên PostgreSQL THẬT (Testcontainers): giao dịch <b>atomic nhiều-item</b> (kho + ledger
/// cùng commit/rollback; một item thiếu ⇒ toàn bộ fail, không mutate một phần), <b>idempotent</b> (retry cùng
/// key không double), <b>chặn bớt quá số lượng</b> (không âm), và <b>concurrency-safe</b> (hai consume song song
/// không lost-update / không âm) qua khoá dòng <c>FOR UPDATE</c>. Cần Docker. Hợp đồng hành vi của cơ chế giao
/// dịch dùng chung <see cref="InventoryService"/>.
/// </summary>
public sealed class InventoryPersistenceTests : IClassFixture<PostgresContainerFixture>, IAsyncLifetime
{
    private const string Potion = "item_potion";
    private const string Ore = "item_ore";
    private const string FragmentHero = "hero_a";

    private static readonly DateTimeOffset Now = new(2026, 9, 11, 9, 0, 0, TimeSpan.Zero);

    private readonly PostgresContainerFixture _fixture;

    public InventoryPersistenceTests(PostgresContainerFixture fixture) => _fixture = fixture;

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => Now;
    }

    /// <summary>Config giả: chỉ hai catalog item + một hero (cho fragment) là hợp lệ (data-driven kiểm ở service).</summary>
    private sealed class FakeConfig : IConfigProvider
    {
        public ConfigVersion CurrentVersion => new(1, 1);

        public T? Get<T>(string type, string id)
            where T : class => null;

        public IReadOnlyList<string> GetIds(string type) => type switch
        {
            "item" => [Potion, Ore],
            "hero" => [FragmentHero],
            _ => [],
        };
    }

    private static List<ItemChange> Change(string type, string id, long qty) => [new ItemChange(type, id, qty)];

    private TestDbContext NewContext() =>
        new(
            new DbContextOptionsBuilder<TestDbContext>().UseNpgsql(_fixture.ConnectionString).Options,
            new DomainEventDispatcher(new NoOpPublisher()));

    public async Task InitializeAsync()
    {
        await using TestDbContext context = NewContext();
        await context.Database.EnsureCreatedAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<Guid> SeedProfileAsync()
    {
        Guid accountId = Guid.NewGuid();
        Guid profileId = Guid.NewGuid();
        await using TestDbContext context = NewContext();
        context.Accounts.Add(Account.CreateGuest(accountId, Now));
        context.PlayerProfiles.Add(PlayerProfile.CreateForAccount(profileId, accountId, Now));
        await context.SaveChangesAsync(CancellationToken.None);
        return profileId;
    }

    /// <summary>Chạy một thao tác kho như pipeline: begin → op → commit khi thành công, rollback khi thất bại/exception.</summary>
    private async Task<Result<InventoryTransactionResult>> RunAsync(
        Func<InventoryService, Task<Result<InventoryTransactionResult>>> op)
    {
        await using TestDbContext context = NewContext();
        var uow = new UnitOfWork(context);
        var service = new InventoryService(
            new InventoryRepository(context), new InventoryTransactionRepository(context), new FakeConfig(), new FixedClock());

        await uow.BeginTransactionAsync(CancellationToken.None);
        try
        {
            Result<InventoryTransactionResult> result = await op(service);
            if (result.IsSuccess)
            {
                await uow.CommitAsync(CancellationToken.None);
            }
            else
            {
                await uow.RollbackAsync(CancellationToken.None);
            }

            return result;
        }
        catch
        {
            await uow.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private async Task<long> QuantityAsync(Guid profileId, string itemType, string itemId)
    {
        await using TestDbContext context = NewContext();
        DomainInventory? inventory =
            await new InventoryRepository(context).GetByProfileIdAsync(profileId, CancellationToken.None);
        return inventory?.QuantityOf(itemType, itemId) ?? 0;
    }

    private async Task<int> LedgerCountAsync(Guid profileId)
    {
        await using TestDbContext context = NewContext();
        return await context.InventoryTransactions.CountAsync(x => x.ProfileId == profileId);
    }

    // ── Test 1: grant atomic (kho + ledger cùng commit) ────────────────────────────────────────────
    [Fact]
    public async Task Grant_commits_stacks_and_ledger_atomically()
    {
        Guid profileId = await SeedProfileAsync();

        Result<InventoryTransactionResult> result = await RunAsync(s =>
            s.GrantAsync(profileId, Change(InventoryItemTypes.Item, Potion, 10), "reward", "grant-1", CancellationToken.None));

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Single().QuantityAfter.Should().Be(10);
        (await QuantityAsync(profileId, InventoryItemTypes.Item, Potion)).Should().Be(10);
        (await LedgerCountAsync(profileId)).Should().Be(1);
    }

    // ── Test 2: grant nhiều loại (item + fragment) atomic trong một giao dịch ───────────────────────
    [Fact]
    public async Task Grant_multiple_item_types_atomically()
    {
        Guid profileId = await SeedProfileAsync();

        Result<InventoryTransactionResult> result = await RunAsync(s => s.GrantAsync(
            profileId,
            [new ItemChange(InventoryItemTypes.Item, Potion, 5), new ItemChange(InventoryItemTypes.Fragment, FragmentHero, 3)],
            "reward",
            "grant-2",
            CancellationToken.None));

        result.IsSuccess.Should().BeTrue();
        (await QuantityAsync(profileId, InventoryItemTypes.Item, Potion)).Should().Be(5);
        (await QuantityAsync(profileId, InventoryItemTypes.Fragment, FragmentHero)).Should().Be(3);
        (await LedgerCountAsync(profileId)).Should().Be(1, "một command = một dòng ledger (nhiều-item)");
    }

    // ── Test 3: consume atomic ─────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task Consume_commits_stacks_and_ledger_atomically()
    {
        Guid profileId = await SeedProfileAsync();
        await RunAsync(s => s.GrantAsync(profileId, Change(InventoryItemTypes.Item, Potion, 10), "seed", "grant-3", CancellationToken.None));

        Result<InventoryTransactionResult> result = await RunAsync(s =>
            s.ConsumeAsync(profileId, Change(InventoryItemTypes.Item, Potion, 4), "use", "consume-3", CancellationToken.None));

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Single().Delta.Should().Be(-4);
        (await QuantityAsync(profileId, InventoryItemTypes.Item, Potion)).Should().Be(6);
        (await LedgerCountAsync(profileId)).Should().Be(2);
    }

    // ── Test 4: idempotent grant (retry cùng key ⇒ +10, KHÔNG +20) ─────────────────────────────────
    [Fact]
    public async Task Grant_with_same_key_twice_applies_once()
    {
        Guid profileId = await SeedProfileAsync();

        await RunAsync(s => s.GrantAsync(profileId, Change(InventoryItemTypes.Item, Potion, 10), "reward", "dup-grant", CancellationToken.None));
        await RunAsync(s => s.GrantAsync(profileId, Change(InventoryItemTypes.Item, Potion, 10), "reward", "dup-grant", CancellationToken.None));

        (await QuantityAsync(profileId, InventoryItemTypes.Item, Potion)).Should().Be(10, "cùng idempotency key ⇒ chỉ thêm một lần");
        (await LedgerCountAsync(profileId)).Should().Be(1);
    }

    // ── Test 5: idempotent consume ─────────────────────────────────────────────────────────────────
    [Fact]
    public async Task Consume_with_same_key_twice_applies_once()
    {
        Guid profileId = await SeedProfileAsync();
        await RunAsync(s => s.GrantAsync(profileId, Change(InventoryItemTypes.Item, Potion, 10), "seed", "grant-5", CancellationToken.None));

        await RunAsync(s => s.ConsumeAsync(profileId, Change(InventoryItemTypes.Item, Potion, 4), "use", "dup-consume", CancellationToken.None));
        await RunAsync(s => s.ConsumeAsync(profileId, Change(InventoryItemTypes.Item, Potion, 4), "use", "dup-consume", CancellationToken.None));

        (await QuantityAsync(profileId, InventoryItemTypes.Item, Potion)).Should().Be(6, "cùng idempotency key ⇒ chỉ bớt một lần");
    }

    // ── Test 6: retry trả lại kết quả cũ theo idempotency contract ─────────────────────────────────
    [Fact]
    public async Task Retry_returns_previous_result()
    {
        Guid profileId = await SeedProfileAsync();

        Result<InventoryTransactionResult> first = await RunAsync(s =>
            s.GrantAsync(profileId, Change(InventoryItemTypes.Fragment, FragmentHero, 7), "quest", "retry-key", CancellationToken.None));
        Result<InventoryTransactionResult> second = await RunAsync(s =>
            s.GrantAsync(profileId, Change(InventoryItemTypes.Fragment, FragmentHero, 7), "quest", "retry-key", CancellationToken.None));

        second.IsSuccess.Should().BeTrue();
        second.Value.Should().BeEquivalentTo(first.Value, "retry cùng key trả lại đúng kết quả đã lưu");
    }

    // ── Test 7: bớt quá số lượng bị chặn (không mutate, không ledger) ──────────────────────────────
    [Fact]
    public async Task Consume_over_quantity_is_rejected_without_mutation()
    {
        Guid profileId = await SeedProfileAsync();
        await RunAsync(s => s.GrantAsync(profileId, Change(InventoryItemTypes.Item, Potion, 5), "seed", "grant-7", CancellationToken.None));

        Result<InventoryTransactionResult> result = await RunAsync(s =>
            s.ConsumeAsync(profileId, Change(InventoryItemTypes.Item, Potion, 10), "use", "consume-7", CancellationToken.None));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("INVENTORY_INSUFFICIENT_CONFLICT");
        (await QuantityAsync(profileId, InventoryItemTypes.Item, Potion)).Should().Be(5, "consume thất bại không được đổi số lượng");
        (await LedgerCountAsync(profileId)).Should().Be(1, "chỉ có dòng grant seed — consume bị chặn không ghi ledger");
    }

    // ── Test 8 (§17): nhiều-item, một item thiếu ⇒ TOÀN BỘ fail, KHÔNG mutate một phần ─────────────
    [Fact]
    public async Task Multi_item_partial_shortfall_fails_whole_operation_without_mutation()
    {
        Guid profileId = await SeedProfileAsync();
        // A(Potion)=10, B(Ore)=5
        await RunAsync(s => s.GrantAsync(
            profileId,
            [new ItemChange(InventoryItemTypes.Item, Potion, 10), new ItemChange(InventoryItemTypes.Item, Ore, 5)],
            "seed",
            "grant-8",
            CancellationToken.None));

        // Bớt A5 + B10 ⇒ B thiếu ⇒ toàn bộ fail.
        Result<InventoryTransactionResult> result = await RunAsync(s => s.ConsumeAsync(
            profileId,
            [new ItemChange(InventoryItemTypes.Item, Potion, 5), new ItemChange(InventoryItemTypes.Item, Ore, 10)],
            "use",
            "consume-8",
            CancellationToken.None));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("INVENTORY_INSUFFICIENT_CONFLICT");
        (await QuantityAsync(profileId, InventoryItemTypes.Item, Potion)).Should().Be(10, "A không được bớt một phần");
        (await QuantityAsync(profileId, InventoryItemTypes.Item, Ore)).Should().Be(5, "B giữ nguyên");
        (await LedgerCountAsync(profileId)).Should().Be(1, "chỉ có dòng grant seed — consume atomic bị chặn");
    }

    // ── Test 9: concurrency — hai consume song song, đúng một thành công, số lượng không âm ─────────
    [Fact]
    public async Task Concurrent_consumes_never_overspend_or_go_negative()
    {
        Guid profileId = await SeedProfileAsync();
        await RunAsync(s => s.GrantAsync(profileId, Change(InventoryItemTypes.Item, Potion, 10), "seed", "grant-9", CancellationToken.None));

        Task<Result<InventoryTransactionResult>> a = RunAsync(s =>
            s.ConsumeAsync(profileId, Change(InventoryItemTypes.Item, Potion, 8), "use", "consume-9a", CancellationToken.None));
        Task<Result<InventoryTransactionResult>> b = RunAsync(s =>
            s.ConsumeAsync(profileId, Change(InventoryItemTypes.Item, Potion, 8), "use", "consume-9b", CancellationToken.None));

        Result<InventoryTransactionResult>[] results = await Task.WhenAll(a, b);

        results.Count(r => r.IsSuccess).Should().Be(1, "đúng MỘT consume thành công");
        results.Count(r => r.IsFailure).Should().Be(1, "consume còn lại bị chặn (thiếu số lượng)");
        (await QuantityAsync(profileId, InventoryItemTypes.Item, Potion)).Should().Be(2, "10 - 8 = 2 (không lost-update, không âm)");
    }

    // ── Test 10: rollback — lỗi giữa transaction ⇒ kho + ledger + idempotency đều rollback ─────────
    [Fact]
    public async Task Failure_mid_transaction_rolls_back_stacks_ledger_and_idempotency()
    {
        Guid profileId = await SeedProfileAsync();
        await RunAsync(s => s.GrantAsync(profileId, Change(InventoryItemTypes.Item, Potion, 10), "seed", "grant-10", CancellationToken.None));

        await using (TestDbContext context = NewContext())
        {
            var uow = new UnitOfWork(context);
            var throwingLedger = new ThrowingInventoryTransactionRepository(new InventoryTransactionRepository(context));
            var service = new InventoryService(
                new InventoryRepository(context), throwingLedger, new FakeConfig(), new FixedClock());

            await uow.BeginTransactionAsync(CancellationToken.None);
            Func<Task> act = async () =>
            {
                await service.GrantAsync(profileId, Change(InventoryItemTypes.Item, Potion, 500), "bug", "grant-10-fail", CancellationToken.None);
                await uow.CommitAsync(CancellationToken.None);
            };

            await act.Should().ThrowAsync<InvalidOperationException>();
            await uow.RollbackAsync(CancellationToken.None);
        }

        (await QuantityAsync(profileId, InventoryItemTypes.Item, Potion)).Should().Be(10, "thêm 500 phải rollback cùng ledger");
        (await LedgerCountAsync(profileId)).Should().Be(1, "chỉ còn dòng grant seed — lần lỗi không ghi ledger");

        // Idempotency KHÔNG bị đánh dấu success sai: retry cùng key sau rollback vẫn thực hiện được.
        Result<InventoryTransactionResult> retry = await RunAsync(s =>
            s.GrantAsync(profileId, Change(InventoryItemTypes.Item, Potion, 1), "retry", "grant-10-fail", CancellationToken.None));
        retry.IsSuccess.Should().BeTrue();
        (await QuantityAsync(profileId, InventoryItemTypes.Item, Potion)).Should().Be(11);
    }

    /// <summary>Ledger repo ném ở <c>AddAsync</c> để mô phỏng lỗi giữa transaction (kiểm rollback atomic).</summary>
    private sealed class ThrowingInventoryTransactionRepository
        : Application.Abstractions.Persistence.IInventoryTransactionRepository
    {
        private readonly InventoryTransactionRepository _inner;

        public ThrowingInventoryTransactionRepository(InventoryTransactionRepository inner) => _inner = inner;

        public Task<Domain.Inventory.InventoryTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => _inner.GetByIdAsync(id, cancellationToken);

        public Task<Domain.Inventory.InventoryTransaction?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken)
            => _inner.GetByIdempotencyKeyAsync(idempotencyKey, cancellationToken);

        public Task AddAsync(Domain.Inventory.InventoryTransaction entity, CancellationToken cancellationToken)
            => throw new InvalidOperationException("Lỗi mô phỏng giữa transaction.");
    }
}
