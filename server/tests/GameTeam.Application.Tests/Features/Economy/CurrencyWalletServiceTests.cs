using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Application.Features.Economy;
using GameTeam.Contracts.Enums;
using GameTeam.Domain.Common;
using GameTeam.Domain.Economy;
using NSubstitute;
using Xunit;

namespace GameTeam.Application.Tests.Features.Economy;

/// <summary>
/// Phase 31 — <see cref="CurrencyWalletService"/> (cơ chế dùng chung, port mock): grant/spend ghi ledger;
/// idempotency (key đã xử lý ⇒ trả kết quả cũ, không áp dụng lại); thiếu tiền ⇒ Result lỗi, KHÔNG mutate;
/// loại tiền None bị từ chối. Atomicity/concurrency thật được phủ ở Infrastructure.Tests (Testcontainers).
/// </summary>
public sealed class CurrencyWalletServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 10, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid ProfileId = Guid.NewGuid();

    private sealed class Harness
    {
        public IWalletRepository Wallets { get; } = Substitute.For<IWalletRepository>();
        public ICurrencyTransactionRepository Ledger { get; } = Substitute.For<ICurrencyTransactionRepository>();
        public IClock Clock { get; } = Substitute.For<IClock>();

        public Harness() => Clock.UtcNow.Returns(Now);

        public CurrencyWalletService Service() => new(Wallets, Ledger, Clock);

        public Wallet SeedWallet(long gold = 0)
        {
            Wallet wallet = Wallet.CreateFor(Guid.NewGuid(), ProfileId, Now);
            if (gold > 0)
            {
                wallet.Credit("gold", gold, Now);
            }

            Wallets.GetByProfileIdForUpdateAsync(ProfileId, Arg.Any<CancellationToken>()).Returns(wallet);
            return wallet;
        }
    }

    [Fact]
    public async Task Grant_credits_wallet_and_writes_ledger()
    {
        var h = new Harness();
        Wallet wallet = h.SeedWallet();
        CurrencyTransaction? ledgerRow = null;
        await h.Ledger.AddAsync(Arg.Do<CurrencyTransaction>(t => ledgerRow = t), Arg.Any<CancellationToken>());

        Result<CurrencyTransactionResult> result =
            await h.Service().GrantAsync(ProfileId, Currency.Gold, 100, "afk_claim", "key-grant", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Delta.Should().Be(100);
        result.Value.BalanceAfter.Should().Be(100);
        wallet.BalanceOf("gold").Should().Be(100);
        ledgerRow!.Delta.Should().Be(100);
        ledgerRow.BalanceAfter.Should().Be(100);
        ledgerRow.IdempotencyKey.Should().Be("key-grant");
        ledgerRow.Source.Should().Be("afk_claim");
    }

    [Fact]
    public async Task Spend_debits_wallet_and_writes_negative_ledger()
    {
        var h = new Harness();
        Wallet wallet = h.SeedWallet(gold: 100);
        CurrencyTransaction? ledgerRow = null;
        await h.Ledger.AddAsync(Arg.Do<CurrencyTransaction>(t => ledgerRow = t), Arg.Any<CancellationToken>());

        Result<CurrencyTransactionResult> result =
            await h.Service().SpendAsync(ProfileId, Currency.Gold, 80, "gacha_summon", "key-spend", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Delta.Should().Be(-80);
        result.Value.BalanceAfter.Should().Be(20);
        wallet.BalanceOf("gold").Should().Be(20);
        ledgerRow!.Delta.Should().Be(-80);
    }

    [Fact]
    public async Task Grant_is_idempotent_returns_stored_result_without_reapplying()
    {
        var h = new Harness();
        CurrencyTransaction stored = CurrencyTransaction.Record(
            Guid.NewGuid(), ProfileId, "gold", +100, 100, "afk_claim", "key-dup", Now);
        h.Ledger.GetByIdempotencyKeyAsync("key-dup", Arg.Any<CancellationToken>()).Returns(stored);

        Result<CurrencyTransactionResult> result =
            await h.Service().GrantAsync(ProfileId, Currency.Gold, 100, "afk_claim", "key-dup", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.BalanceAfter.Should().Be(100);
        result.Value.Currency.Should().Be(Currency.Gold);
        // KHÔNG khoá/tải ví, KHÔNG ghi ledger lần hai.
        await h.Wallets.DidNotReceive().GetByProfileIdForUpdateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await h.Ledger.DidNotReceive().AddAsync(Arg.Any<CurrencyTransaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Spend_insufficient_funds_rejects_without_mutation_or_ledger()
    {
        var h = new Harness();
        Wallet wallet = h.SeedWallet(gold: 50);

        Result<CurrencyTransactionResult> result =
            await h.Service().SpendAsync(ProfileId, Currency.Gold, 100, "shop", "key-x", CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CURRENCY_INSUFFICIENT_FUNDS");
        wallet.BalanceOf("gold").Should().Be(50);
        await h.Ledger.DidNotReceive().AddAsync(Arg.Any<CurrencyTransaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Grant_none_currency_is_rejected()
    {
        var h = new Harness();

        Result<CurrencyTransactionResult> result =
            await h.Service().GrantAsync(ProfileId, Currency.None, 10, "s", "k", CancellationToken.None);

        result.Error.Code.Should().Be("CURRENCY_INVALID");
    }

    [Fact]
    public async Task Grant_creates_wallet_when_none_exists()
    {
        var h = new Harness();
        h.Wallets.GetByProfileIdForUpdateAsync(ProfileId, Arg.Any<CancellationToken>()).Returns((Wallet?)null);
        Wallet? added = null;
        await h.Wallets.AddAsync(Arg.Do<Wallet>(w => added = w), Arg.Any<CancellationToken>());

        Result<CurrencyTransactionResult> result =
            await h.Service().GrantAsync(ProfileId, Currency.Gem, 5, "quest", "key-new", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        added.Should().NotBeNull();
        added!.BalanceOf("gem").Should().Be(5);
    }
}
