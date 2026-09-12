using System;
using FluentAssertions;
using Xunit;
using DomainInventory = GameTeam.Domain.Inventory.Inventory;

namespace GameTeam.Domain.Tests.Inventory;

/// <summary>
/// Phase 32 — <see cref="DomainInventory"/>: add/remove trả số lượng mới; nhiều chồng độc lập; bất biến
/// <b>số lượng không âm</b> (ADR-011); remove vượt số lượng là lỗi lập trình (backstop — người gọi phải kiểm
/// <c>QuantityOf</c> trước) ⇒ ném.
/// </summary>
public class InventoryTests
{
    private const string Item = "item";
    private const string Fragment = "fragment";

    private static readonly DateTimeOffset Now = new(2026, 9, 11, 9, 0, 0, TimeSpan.Zero);

    private static DomainInventory NewInventory() => DomainInventory.CreateFor(Guid.NewGuid(), Guid.NewGuid(), Now);

    [Fact]
    public void Add_increases_quantity_and_returns_new_total()
    {
        DomainInventory inventory = NewInventory();

        long after1 = inventory.Add(Item, "item_potion", 10, Now);
        long after2 = inventory.Add(Item, "item_potion", 5, Now);

        after1.Should().Be(10);
        after2.Should().Be(15);
        inventory.QuantityOf(Item, "item_potion").Should().Be(15);
        inventory.QuantityOf(Item, "item_ore").Should().Be(0);
    }

    [Fact]
    public void Add_keeps_stacks_of_different_type_or_id_independent()
    {
        DomainInventory inventory = NewInventory();

        inventory.Add(Item, "item_potion", 10, Now);
        inventory.Add(Item, "item_ore", 3, Now);
        inventory.Add(Fragment, "hero_a", 7, Now);

        inventory.QuantityOf(Item, "item_potion").Should().Be(10);
        inventory.QuantityOf(Item, "item_ore").Should().Be(3);
        inventory.QuantityOf(Fragment, "hero_a").Should().Be(7);
        inventory.Stacks.Should().HaveCount(3);
    }

    [Fact]
    public void Remove_subtracts_and_returns_new_total()
    {
        DomainInventory inventory = NewInventory();
        inventory.Add(Item, "item_potion", 10, Now);

        long after = inventory.Remove(Item, "item_potion", 4, Now);

        after.Should().Be(6);
        inventory.QuantityOf(Item, "item_potion").Should().Be(6);
    }

    [Fact]
    public void Remove_exact_quantity_leaves_zero()
    {
        DomainInventory inventory = NewInventory();
        inventory.Add(Item, "item_potion", 5, Now);

        inventory.Remove(Item, "item_potion", 5, Now).Should().Be(0);
        inventory.QuantityOf(Item, "item_potion").Should().Be(0);
    }

    [Fact]
    public void Remove_more_than_owned_throws_and_does_not_mutate()
    {
        DomainInventory inventory = NewInventory();
        inventory.Add(Item, "item_potion", 5, Now);

        Action act = () => inventory.Remove(Item, "item_potion", 10, Now);

        act.Should().Throw<InvalidOperationException>();
        inventory.QuantityOf(Item, "item_potion").Should().Be(5, "remove thất bại không được đổi số lượng");
    }

    [Fact]
    public void Remove_absent_stack_throws()
    {
        DomainInventory inventory = NewInventory();

        Action act = () => inventory.Remove(Item, "item_missing", 1, Now);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Add_non_positive_quantity_throws()
    {
        DomainInventory inventory = NewInventory();

        Action zero = () => inventory.Add(Item, "item_potion", 0, Now);
        Action negative = () => inventory.Add(Item, "item_potion", -1, Now);

        zero.Should().Throw<ArgumentOutOfRangeException>();
        negative.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void CreateFor_rejects_empty_ids()
    {
        Action emptyId = () => DomainInventory.CreateFor(Guid.Empty, Guid.NewGuid(), Now);
        Action emptyProfile = () => DomainInventory.CreateFor(Guid.NewGuid(), Guid.Empty, Now);

        emptyId.Should().Throw<ArgumentException>();
        emptyProfile.Should().Throw<ArgumentException>();
    }
}
