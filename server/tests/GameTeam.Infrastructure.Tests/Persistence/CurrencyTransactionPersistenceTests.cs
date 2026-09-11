using FluentAssertions;
using GameTeam.Application.Features.Economy;
using GameTeam.Contracts.Enums;
using GameTeam.Domain.Accounts;
using GameTeam.Domain.Common;
using GameTeam.Domain.Economy;
using GameTeam.Domain.Profiles;
using GameTeam.Infrastructure.Persistence;
using GameTeam.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameTeam.Infrastructure.Tests.Persistence;

/// <summary>
/// Phase 31 — hệ tiền tệ trên PostgreSQL THẬT (Testcontainers): giao dịch <b>atomic</b> (balance + ledger cùng
/// commit/rollback), <b>idempotent</b> (retry cùng key không double), <b>chặn thiếu tiền</b> (số dư không âm),
/// và <b>concurrency-safe</b> (hai spend song song không lost-update / không âm) qua khoá dòng <c>FOR UPDATE</c>.
/// Cần Docker. Đây là hợp đồng hành vi của cơ chế giao dịch dùng chung <see cref="CurrencyWalletService"/>.
/// </summary>
public sealed class CurrencyTransactionPersistenceTests : IClassFixture<PostgresContainerFixture>, IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 9, 10, 9, 0, 0, TimeSpan.Zero);

    private readonly PostgresContainerFixture _fixture;

    public CurrencyTransactionPersistenceTests(PostgresContainerFixture fixture) => _fixture = fixture;

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => Now;
    }

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

    /// <summary>Chạy một thao tác qua ví như pipeline: begin → op → commit khi Result thành công, rollback khi thất bại/exception.</summary>
    private async Task<Result<CurrencyTransactionResult>> RunAsync(
        Func<CurrencyWalletService, Task<Result<CurrencyTransactionResult>>> op)
    {
        await using TestDbContext context = NewContext();
        var uow = new UnitOfWork(context);
        var service = new CurrencyWalletService(
            new WalletRepository(context), new CurrencyTransactionRepository(context), new FixedClock());

        await uow.BeginTransactionAsync(CancellationToken.None);
        try
        {
            Result<CurrencyTransactionResult> result = await op(service);
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

    private async Task<long> BalanceAsync(Guid profileId, string code)
    {
        await using TestDbContext context = NewContext();
        Wallet? wallet = await new WalletRepository(context).GetByProfileIdAsync(profileId, CancellationToken.None);
        return wallet?.BalanceOf(code) ?? 0;
    }

    private async Task<int> LedgerCountAsync(Guid profileId)
    {
        await using TestDbContext context = NewContext();
        return await context.CurrencyTransactions.CountAsync(x => x.ProfileId == profileId);
    }

    // ── Test 1: grant atomic (balance + ledger cùng commit) ────────────────────────────────────────
    [Fact]
    public async Task Grant_commits_balance_and_ledger_atomically()
    {
        Guid profileId = await SeedProfileAsync();

        Result<CurrencyTransactionResult> result = await RunAsync(s =>
            s.GrantAsync(profileId, Currency.Gold, 100, "afk_claim", "grant-1", CancellationToken.None));

        result.IsSuccess.Should().BeTrue();
        result.Value.BalanceAfter.Should().Be(100);
        (await BalanceAsync(profileId, "gold")).Should().Be(100);
        (await LedgerCountAsync(profileId)).Should().Be(1);
    }

    // ── Test 2: spend atomic ───────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task Spend_commits_balance_and_ledger_atomically()
    {
        Guid profileId = await SeedProfileAsync();
        await RunAsync(s => s.GrantAsync(profileId, Currency.Gold, 100, "seed", "grant-2", CancellationToken.None));

        Result<CurrencyTransactionResult> result = await RunAsync(s =>
            s.SpendAsync(profileId, Currency.Gold, 30, "shop", "spend-2", CancellationToken.None));

        result.IsSuccess.Should().BeTrue();
        result.Value.Delta.Should().Be(-30);
        (await BalanceAsync(profileId, "gold")).Should().Be(70);
        (await LedgerCountAsync(profileId)).Should().Be(2);
    }

    // ── Test 3: idempotent grant (retry cùng key ⇒ +100, KHÔNG +200) ───────────────────────────────
    [Fact]
    public async Task Grant_with_same_key_twice_applies_once()
    {
        Guid profileId = await SeedProfileAsync();

        await RunAsync(s => s.GrantAsync(profileId, Currency.Gold, 100, "afk_claim", "dup-grant", CancellationToken.None));
        await RunAsync(s => s.GrantAsync(profileId, Currency.Gold, 100, "afk_claim", "dup-grant", CancellationToken.None));

        (await BalanceAsync(profileId, "gold")).Should().Be(100, "cùng idempotency key ⇒ chỉ cấp một lần");
        (await LedgerCountAsync(profileId)).Should().Be(1);
    }

    // ── Test 4: idempotent spend ───────────────────────────────────────────────────────────────────
    [Fact]
    public async Task Spend_with_same_key_twice_applies_once()
    {
        Guid profileId = await SeedProfileAsync();
        await RunAsync(s => s.GrantAsync(profileId, Currency.Gold, 100, "seed", "grant-4", CancellationToken.None));

        await RunAsync(s => s.SpendAsync(profileId, Currency.Gold, 40, "shop", "dup-spend", CancellationToken.None));
        await RunAsync(s => s.SpendAsync(profileId, Currency.Gold, 40, "shop", "dup-spend", CancellationToken.None));

        (await BalanceAsync(profileId, "gold")).Should().Be(60, "cùng idempotency key ⇒ chỉ tiêu một lần");
    }

    // ── Test 5: retry trả lại kết quả cũ theo idempotency contract ─────────────────────────────────
    [Fact]
    public async Task Retry_returns_previous_result()
    {
        Guid profileId = await SeedProfileAsync();

        Result<CurrencyTransactionResult> first = await RunAsync(s =>
            s.GrantAsync(profileId, Currency.Gem, 25, "quest", "retry-key", CancellationToken.None));
        Result<CurrencyTransactionResult> second = await RunAsync(s =>
            s.GrantAsync(profileId, Currency.Gem, 25, "quest", "retry-key", CancellationToken.None));

        second.IsSuccess.Should().BeTrue();
        second.Value.Should().BeEquivalentTo(first.Value, "retry cùng key trả lại đúng kết quả đã lưu");
    }

    // ── Test 6: spend vượt số dư bị chặn (không mutate, không ledger) ──────────────────────────────
    [Fact]
    public async Task Spend_over_balance_is_rejected_without_mutation()
    {
        Guid profileId = await SeedProfileAsync();
        await RunAsync(s => s.GrantAsync(profileId, Currency.Gold, 50, "seed", "grant-6", CancellationToken.None));

        Result<CurrencyTransactionResult> result = await RunAsync(s =>
            s.SpendAsync(profileId, Currency.Gold, 100, "shop", "spend-6", CancellationToken.None));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CURRENCY_INSUFFICIENT_FUNDS");
        (await BalanceAsync(profileId, "gold")).Should().Be(50, "spend thất bại không được đổi số dư");
        (await LedgerCountAsync(profileId)).Should().Be(1, "chỉ có dòng grant seed — spend bị chặn không ghi ledger");
    }

    // ── Test 7: concurrency — hai spend song song, đúng một thành công, số dư không âm ─────────────
    [Fact]
    public async Task Concurrent_spends_never_overspend_or_go_negative()
    {
        Guid profileId = await SeedProfileAsync();
        await RunAsync(s => s.GrantAsync(profileId, Currency.Gold, 100, "seed", "grant-7", CancellationToken.None));

        Task<Result<CurrencyTransactionResult>> a = RunAsync(s =>
            s.SpendAsync(profileId, Currency.Gold, 80, "shop", "spend-7a", CancellationToken.None));
        Task<Result<CurrencyTransactionResult>> b = RunAsync(s =>
            s.SpendAsync(profileId, Currency.Gold, 80, "shop", "spend-7b", CancellationToken.None));

        Result<CurrencyTransactionResult>[] results = await Task.WhenAll(a, b);

        results.Count(r => r.IsSuccess).Should().Be(1, "đúng MỘT spend thành công");
        results.Count(r => r.IsFailure).Should().Be(1, "spend còn lại bị chặn (thiếu tiền)");
        (await BalanceAsync(profileId, "gold")).Should().Be(20, "100 - 80 = 20 (không lost-update, không âm)");
    }

    // ── Test 8: rollback — lỗi giữa transaction ⇒ balance + ledger + idempotency đều rollback ──────
    [Fact]
    public async Task Failure_mid_transaction_rolls_back_balance_ledger_and_idempotency()
    {
        Guid profileId = await SeedProfileAsync();
        await RunAsync(s => s.GrantAsync(profileId, Currency.Gold, 100, "seed", "grant-8", CancellationToken.None));

        // Ledger repo ném khi AddAsync (sau khi ví đã credit trong bộ nhớ) ⇒ transaction rollback.
        await using (TestDbContext context = NewContext())
        {
            var uow = new UnitOfWork(context);
            var throwingLedger = new ThrowingCurrencyTransactionRepository(new CurrencyTransactionRepository(context));
            var service = new CurrencyWalletService(new WalletRepository(context), throwingLedger, new FixedClock());

            await uow.BeginTransactionAsync(CancellationToken.None);
            Func<Task> act = async () =>
            {
                await service.GrantAsync(profileId, Currency.Gold, 500, "bug", "grant-8-fail", CancellationToken.None);
                await uow.CommitAsync(CancellationToken.None);
            };

            await act.Should().ThrowAsync<InvalidOperationException>();
            await uow.RollbackAsync(CancellationToken.None);
        }

        (await BalanceAsync(profileId, "gold")).Should().Be(100, "credit 500 phải rollback cùng ledger");
        (await LedgerCountAsync(profileId)).Should().Be(1, "chỉ còn dòng grant seed — lần lỗi không ghi ledger");

        // Idempotency KHÔNG bị đánh dấu success sai: retry cùng key sau rollback vẫn thực hiện được.
        Result<CurrencyTransactionResult> retry = await RunAsync(s =>
            s.GrantAsync(profileId, Currency.Gold, 10, "retry", "grant-8-fail", CancellationToken.None));
        retry.IsSuccess.Should().BeTrue();
        (await BalanceAsync(profileId, "gold")).Should().Be(110);
    }

    /// <summary>Ledger repo ném ở <c>AddAsync</c> để mô phỏng lỗi giữa transaction (kiểm rollback atomic).</summary>
    private sealed class ThrowingCurrencyTransactionRepository
        : Application.Abstractions.Persistence.ICurrencyTransactionRepository
    {
        private readonly CurrencyTransactionRepository _inner;

        public ThrowingCurrencyTransactionRepository(CurrencyTransactionRepository inner) => _inner = inner;

        public Task<CurrencyTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => _inner.GetByIdAsync(id, cancellationToken);

        public Task<CurrencyTransaction?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken)
            => _inner.GetByIdempotencyKeyAsync(idempotencyKey, cancellationToken);

        public Task AddAsync(CurrencyTransaction entity, CancellationToken cancellationToken)
            => throw new InvalidOperationException("Lỗi mô phỏng giữa transaction.");
    }
}
