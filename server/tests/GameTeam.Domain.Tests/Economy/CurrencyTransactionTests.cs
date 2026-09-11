using System;
using FluentAssertions;
using GameTeam.Domain.Economy;
using Xunit;

namespace GameTeam.Domain.Tests.Economy;

/// <summary>
/// Phase 31 — <see cref="CurrencyTransaction"/> ledger: factory gán trường, phân biệt grant/spend theo dấu
/// delta, guard tham số (delta khác 0, balance không âm, key/source/currency không rỗng), restore không event.
/// </summary>
public class CurrencyTransactionTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 10, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Record_grant_sets_fields_and_is_grant()
    {
        Guid id = Guid.NewGuid();
        Guid profileId = Guid.NewGuid();

        CurrencyTransaction tx = CurrencyTransaction.Record(id, profileId, "gold", +100, 100, "battle_reward", "key-1", Now);

        tx.Id.Should().Be(id);
        tx.ProfileId.Should().Be(profileId);
        tx.Currency.Should().Be("gold");
        tx.Delta.Should().Be(100);
        tx.BalanceAfter.Should().Be(100);
        tx.Source.Should().Be("battle_reward");
        tx.IdempotencyKey.Should().Be("key-1");
        tx.IsGrant.Should().BeTrue();
        tx.CreatedAt.Should().Be(Now);
    }

    [Fact]
    public void Record_spend_has_negative_delta_and_is_not_grant()
    {
        CurrencyTransaction tx = CurrencyTransaction.Record(
            Guid.NewGuid(), Guid.NewGuid(), "gem", -40, 10, "gacha_summon", "key-2", Now);

        tx.Delta.Should().Be(-40);
        tx.IsGrant.Should().BeFalse();
    }

    [Fact]
    public void Record_rejects_invalid_arguments()
    {
        Guid profileId = Guid.NewGuid();

        FluentActions.Invoking(() => CurrencyTransaction.Record(Guid.Empty, profileId, "gold", 1, 1, "s", "k", Now))
            .Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => CurrencyTransaction.Record(Guid.NewGuid(), Guid.Empty, "gold", 1, 1, "s", "k", Now))
            .Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => CurrencyTransaction.Record(Guid.NewGuid(), profileId, " ", 1, 1, "s", "k", Now))
            .Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => CurrencyTransaction.Record(Guid.NewGuid(), profileId, "gold", 0, 1, "s", "k", Now))
            .Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => CurrencyTransaction.Record(Guid.NewGuid(), profileId, "gold", 1, -1, "s", "k", Now))
            .Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => CurrencyTransaction.Record(Guid.NewGuid(), profileId, "gold", 1, 1, " ", "k", Now))
            .Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => CurrencyTransaction.Record(Guid.NewGuid(), profileId, "gold", 1, 1, "s", " ", Now))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Restore_rebuilds_without_event()
    {
        Guid id = Guid.NewGuid();

        CurrencyTransaction tx = CurrencyTransaction.Restore(
            id, Guid.NewGuid(), "ticket", -1, 4, "shop_purchase", "key-3", 1, Now);

        tx.Id.Should().Be(id);
        tx.Currency.Should().Be("ticket");
        tx.BalanceAfter.Should().Be(4);
        tx.DomainEvents.Should().BeEmpty();
    }
}
