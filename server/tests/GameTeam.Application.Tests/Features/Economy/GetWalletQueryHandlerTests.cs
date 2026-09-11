using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Application.Abstractions.Security;
using GameTeam.Application.Features.Economy.Queries;
using GameTeam.Contracts.Economy;
using GameTeam.Contracts.Enums;
using GameTeam.Domain.Common;
using GameTeam.Domain.Economy;
using GameTeam.Domain.Profiles;
using NSubstitute;
using Xunit;

namespace GameTeam.Application.Tests.Features.Economy;

/// <summary>
/// Phase 31 — <see cref="GetWalletQueryHandler"/>: đọc số dư của CHÍNH người gọi (owner từ token sub); ví
/// chưa có ⇒ ví rỗng (không lỗi); map mã chuỗi → enum Currency; auth bắt buộc.
/// </summary>
public sealed class GetWalletQueryHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 10, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid AccountId = Guid.NewGuid();

    private sealed class Harness
    {
        public ICurrentUser CurrentUser { get; } = Substitute.For<ICurrentUser>();
        public IPlayerProfileRepository Profiles { get; } = Substitute.For<IPlayerProfileRepository>();
        public IWalletRepository Wallets { get; } = Substitute.For<IWalletRepository>();
        public PlayerProfile Profile { get; } = PlayerProfile.CreateForAccount(Guid.NewGuid(), AccountId, Now);

        public Harness()
        {
            CurrentUser.AccountId.Returns(AccountId);
            Profiles.GetByAccountIdAsync(AccountId, Arg.Any<CancellationToken>()).Returns(Profile);
        }

        public GetWalletQueryHandler Handler() => new(CurrentUser, Profiles, Wallets);
    }

    [Fact]
    public async Task Returns_balances_mapped_to_currency_enum()
    {
        var h = new Harness();
        Wallet wallet = Wallet.CreateFor(Guid.NewGuid(), h.Profile.Id, Now);
        wallet.Credit("gold", 1500, Now);
        wallet.Credit("gem", 42, Now);
        h.Wallets.GetByProfileIdAsync(h.Profile.Id, Arg.Any<CancellationToken>()).Returns(wallet);

        Result<WalletDto> result = await h.Handler().Handle(new GetWalletQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Balances.Should().Contain(b => b.Currency == Currency.Gold && b.Amount == 1500);
        result.Value.Balances.Should().Contain(b => b.Currency == Currency.Gem && b.Amount == 42);
    }

    [Fact]
    public async Task Returns_empty_wallet_when_none_exists()
    {
        var h = new Harness();
        h.Wallets.GetByProfileIdAsync(h.Profile.Id, Arg.Any<CancellationToken>()).Returns((Wallet?)null);

        Result<WalletDto> result = await h.Handler().Handle(new GetWalletQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Balances.Should().BeEmpty();
    }

    [Fact]
    public async Task Returns_empty_wallet_when_no_profile()
    {
        var h = new Harness();
        h.Profiles.GetByAccountIdAsync(AccountId, Arg.Any<CancellationToken>()).Returns((PlayerProfile?)null);

        Result<WalletDto> result = await h.Handler().Handle(new GetWalletQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Balances.Should().BeEmpty();
    }

    [Fact]
    public async Task Rejects_unauthenticated()
    {
        var h = new Harness();
        h.CurrentUser.AccountId.Returns((Guid?)null);

        Result<WalletDto> result = await h.Handler().Handle(new GetWalletQuery(), CancellationToken.None);

        result.Error.Code.Should().Be("UNAUTHENTICATED");
    }
}
