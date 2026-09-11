using System;
using FluentAssertions;
using GameTeam.Domain.Economy;
using Xunit;

namespace GameTeam.Domain.Tests.Economy;

/// <summary>
/// Phase 31 — <see cref="Wallet"/>: credit/spend trả số dư mới; bất biến <b>số dư không âm</b> (ADR-011);
/// spend vượt số dư là lỗi lập trình (backstop — người gọi phải kiểm <c>BalanceOf</c> trước) ⇒ ném.
/// </summary>
public class WalletTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 10, 9, 0, 0, TimeSpan.Zero);

    private static Wallet NewWallet() => Wallet.CreateFor(Guid.NewGuid(), Guid.NewGuid(), Now);

    [Fact]
    public void Credit_adds_to_balance_and_returns_new_total()
    {
        Wallet wallet = NewWallet();

        long after1 = wallet.Credit("gold", 100, Now);
        long after2 = wallet.Credit("gold", 50, Now);

        after1.Should().Be(100);
        after2.Should().Be(150);
        wallet.BalanceOf("gold").Should().Be(150);
        wallet.BalanceOf("gem").Should().Be(0);
    }

    [Fact]
    public void Spend_subtracts_and_returns_new_total()
    {
        Wallet wallet = NewWallet();
        wallet.Credit("gold", 100, Now);

        long after = wallet.Spend("gold", 80, Now);

        after.Should().Be(20);
        wallet.BalanceOf("gold").Should().Be(20);
    }

    [Fact]
    public void Spend_exact_balance_reaches_zero()
    {
        Wallet wallet = NewWallet();
        wallet.Credit("gem", 30, Now);

        long after = wallet.Spend("gem", 30, Now);

        after.Should().Be(0);
        wallet.BalanceOf("gem").Should().Be(0);
    }

    [Fact]
    public void Spend_more_than_balance_throws_and_never_goes_negative()
    {
        Wallet wallet = NewWallet();
        wallet.Credit("gold", 50, Now);

        FluentActions.Invoking(() => wallet.Spend("gold", 80, Now))
            .Should().Throw<InvalidOperationException>();
        wallet.BalanceOf("gold").Should().Be(50);
    }

    [Fact]
    public void Spend_on_missing_currency_throws()
    {
        Wallet wallet = NewWallet();

        FluentActions.Invoking(() => wallet.Spend("ticket", 1, Now))
            .Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Credit_and_spend_reject_nonpositive_amount()
    {
        Wallet wallet = NewWallet();

        FluentActions.Invoking(() => wallet.Credit("gold", 0, Now)).Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => wallet.Spend("gold", -1, Now)).Should().Throw<ArgumentOutOfRangeException>();
    }
}
