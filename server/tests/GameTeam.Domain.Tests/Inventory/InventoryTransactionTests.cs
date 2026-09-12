using System;
using System.Collections.Generic;
using FluentAssertions;
using GameTeam.Domain.Inventory;
using Xunit;

namespace GameTeam.Domain.Tests.Inventory;

/// <summary>
/// Phase 32 — <see cref="InventoryTransaction"/>: dòng ledger append-only, chứa nhiều <see cref="InventoryChange"/>;
/// guard tham số (id/profile/direction/source/key/changes). Nền idempotency (idempotency_key unique) + audit.
/// </summary>
public class InventoryTransactionTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 11, 9, 0, 0, TimeSpan.Zero);

    private static IReadOnlyList<InventoryChange> Changes() =>
        [new InventoryChange("item", "item_potion", +10, 10), new InventoryChange("fragment", "hero_a", +3, 3)];

    [Fact]
    public void Record_grant_captures_changes_and_metadata()
    {
        InventoryTransaction tx = InventoryTransaction.Record(
            Guid.NewGuid(), Guid.NewGuid(), InventoryTransaction.GrantDirection, Changes(), "reward", "key-1", Now);

        tx.IsGrant.Should().BeTrue();
        tx.Direction.Should().Be("grant");
        tx.Changes.Should().HaveCount(2);
        tx.IdempotencyKey.Should().Be("key-1");
        tx.SchemaVersion.Should().Be(InventoryTransaction.CurrentSchemaVersion);
        tx.CreatedAt.Should().Be(Now);
    }

    [Fact]
    public void Record_rejects_empty_changes()
    {
        Action act = () => InventoryTransaction.Record(
            Guid.NewGuid(), Guid.NewGuid(), InventoryTransaction.GrantDirection, [], "reward", "key-2", Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Record_rejects_unknown_direction()
    {
        Action act = () => InventoryTransaction.Record(
            Guid.NewGuid(), Guid.NewGuid(), "sideways", Changes(), "reward", "key-3", Now);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Record_rejects_empty_key_or_source()
    {
        Action emptyKey = () => InventoryTransaction.Record(
            Guid.NewGuid(), Guid.NewGuid(), InventoryTransaction.GrantDirection, Changes(), "reward", "  ", Now);
        Action emptySource = () => InventoryTransaction.Record(
            Guid.NewGuid(), Guid.NewGuid(), InventoryTransaction.GrantDirection, Changes(), "", "key-4", Now);

        emptyKey.Should().Throw<ArgumentException>();
        emptySource.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Change_rejects_zero_delta()
    {
        Action act = () => new InventoryChange("item", "item_potion", 0, 5);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
