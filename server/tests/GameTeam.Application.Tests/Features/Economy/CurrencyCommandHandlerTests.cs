using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Application.Abstractions.Security;
using GameTeam.Application.Features.Economy;
using GameTeam.Application.Features.Economy.Commands;
using GameTeam.Contracts.Enums;
using GameTeam.Domain.Common;
using GameTeam.Domain.Economy;
using GameTeam.Domain.Profiles;
using NSubstitute;
using Xunit;

namespace GameTeam.Application.Tests.Features.Economy;

/// <summary>
/// Phase 31 — handlers <see cref="GrantCurrencyCommandHandler"/>/<see cref="SpendCurrencyCommandHandler"/>
/// (handler mỏng): suy chủ sở hữu từ token (ICurrentUser) → profile rồi uỷ cho CurrencyWalletService. Auth +
/// profile-not-found + uỷ quyền đúng. Không client-authority (owner từ token, không từ payload).
/// </summary>
public sealed class CurrencyCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 10, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid AccountId = Guid.NewGuid();

    private sealed class Harness
    {
        public ICurrentUser CurrentUser { get; } = Substitute.For<ICurrentUser>();
        public IPlayerProfileRepository Profiles { get; } = Substitute.For<IPlayerProfileRepository>();
        public IWalletRepository Wallets { get; } = Substitute.For<IWalletRepository>();
        public ICurrencyTransactionRepository Ledger { get; } = Substitute.For<ICurrencyTransactionRepository>();
        public IClock Clock { get; } = Substitute.For<IClock>();
        public PlayerProfile Profile { get; } = PlayerProfile.CreateForAccount(Guid.NewGuid(), AccountId, Now);

        public Harness()
        {
            Clock.UtcNow.Returns(Now);
            CurrentUser.AccountId.Returns(AccountId);
            Profiles.GetByAccountIdAsync(AccountId, Arg.Any<CancellationToken>()).Returns(Profile);
        }

        public CurrencyWalletService Service() => new(Wallets, Ledger, Clock);

        public GrantCurrencyCommandHandler Grant() => new(CurrentUser, Profiles, Service());

        public SpendCurrencyCommandHandler Spend() => new(CurrentUser, Profiles, Service());
    }

    [Fact]
    public async Task Grant_delegates_to_service_and_credits_callers_wallet()
    {
        var h = new Harness();
        Wallet wallet = Wallet.CreateFor(Guid.NewGuid(), h.Profile.Id, Now);
        h.Wallets.GetByProfileIdForUpdateAsync(h.Profile.Id, Arg.Any<CancellationToken>()).Returns(wallet);

        Result<CurrencyTransactionResult> result = await h.Grant().Handle(
            new GrantCurrencyCommand(Currency.Gold, 100, "afk_claim", "key-g"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.BalanceAfter.Should().Be(100);
        wallet.BalanceOf("gold").Should().Be(100);
    }

    [Fact]
    public async Task Grant_rejects_unauthenticated()
    {
        var h = new Harness();
        h.CurrentUser.AccountId.Returns((Guid?)null);

        Result<CurrencyTransactionResult> result = await h.Grant().Handle(
            new GrantCurrencyCommand(Currency.Gold, 100, "s", "k"), CancellationToken.None);

        result.Error.Code.Should().Be("UNAUTHENTICATED");
    }

    [Fact]
    public async Task Grant_rejects_when_no_profile()
    {
        var h = new Harness();
        h.Profiles.GetByAccountIdAsync(AccountId, Arg.Any<CancellationToken>()).Returns((PlayerProfile?)null);

        Result<CurrencyTransactionResult> result = await h.Grant().Handle(
            new GrantCurrencyCommand(Currency.Gold, 100, "s", "k"), CancellationToken.None);

        result.Error.Code.Should().Be("PROFILE_NOT_FOUND");
    }

    [Fact]
    public async Task Spend_delegates_to_service_and_debits_callers_wallet()
    {
        var h = new Harness();
        Wallet wallet = Wallet.CreateFor(Guid.NewGuid(), h.Profile.Id, Now);
        wallet.Credit("gem", 50, Now);
        h.Wallets.GetByProfileIdForUpdateAsync(h.Profile.Id, Arg.Any<CancellationToken>()).Returns(wallet);

        Result<CurrencyTransactionResult> result = await h.Spend().Handle(
            new SpendCurrencyCommand(Currency.Gem, 30, "gacha_summon", "key-s"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.BalanceAfter.Should().Be(20);
        wallet.BalanceOf("gem").Should().Be(20);
    }

    [Fact]
    public async Task Spend_insufficient_funds_is_rejected()
    {
        var h = new Harness();
        Wallet wallet = Wallet.CreateFor(Guid.NewGuid(), h.Profile.Id, Now);
        wallet.Credit("gem", 10, Now);
        h.Wallets.GetByProfileIdForUpdateAsync(h.Profile.Id, Arg.Any<CancellationToken>()).Returns(wallet);

        Result<CurrencyTransactionResult> result = await h.Spend().Handle(
            new SpendCurrencyCommand(Currency.Gem, 30, "gacha_summon", "key-s"), CancellationToken.None);

        result.Error.Code.Should().Be("CURRENCY_INSUFFICIENT_FUNDS");
        wallet.BalanceOf("gem").Should().Be(10);
    }
}
