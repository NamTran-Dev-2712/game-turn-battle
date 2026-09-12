using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GameTeam.Application.Abstractions.Configuration;
using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Application.Features.Inventory;
using GameTeam.Contracts.Config;
using GameTeam.Domain.Common;
using GameTeam.Domain.Inventory;
using NSubstitute;
using Xunit;
using DomainInventory = GameTeam.Domain.Inventory.Inventory;

namespace GameTeam.Application.Tests.Features.Inventory;

/// <summary>
/// Phase 32 — <see cref="InventoryService"/> (cơ chế dùng chung, port mock): grant/consume ghi ledger;
/// kiểm data-driven theo config (id lạ ⇒ lỗi, không mutate); thiếu số lượng ⇒ lỗi (không mutate); nhiều-item
/// atomic (một item thiếu ⇒ toàn bộ fail); idempotency (key đã xử lý ⇒ trả kết quả cũ, không áp dụng lại).
/// Atomicity/concurrency thật được phủ ở Infrastructure.Tests (Testcontainers).
/// </summary>
public sealed class InventoryServiceTests
{
    private const string Item = InventoryItemTypes.Item;
    private const string Fragment = InventoryItemTypes.Fragment;

    private static readonly DateTimeOffset Now = new(2026, 9, 11, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid ProfileId = Guid.NewGuid();

    private sealed class Harness
    {
        public IInventoryRepository Inventories { get; } = Substitute.For<IInventoryRepository>();
        public IInventoryTransactionRepository Ledger { get; } = Substitute.For<IInventoryTransactionRepository>();
        public IConfigProvider Config { get; } = Substitute.For<IConfigProvider>();
        public IClock Clock { get; } = Substitute.For<IClock>();

        public Harness()
        {
            Clock.UtcNow.Returns(Now);
            Config.CurrentVersion.Returns(new ConfigVersion(1, 1));
            Config.GetIds("item").Returns(new List<string> { "item_potion", "item_ore" });
            Config.GetIds("hero").Returns(new List<string> { "hero_a" });
        }

        public InventoryService Service() => new(Inventories, Ledger, Config, Clock);

        public DomainInventory SeedInventory(params (string type, string id, long qty)[] stacks)
        {
            DomainInventory inventory = DomainInventory.CreateFor(Guid.NewGuid(), ProfileId, Now);
            foreach ((string type, string id, long qty) in stacks)
            {
                inventory.Add(type, id, qty, Now);
            }

            Inventories.GetByProfileIdForUpdateAsync(ProfileId, Arg.Any<CancellationToken>()).Returns(inventory);
            return inventory;
        }
    }

    private static List<ItemChange> Change(string type, string id, long qty) => [new ItemChange(type, id, qty)];

    [Fact]
    public async Task Grant_adds_stack_and_writes_ledger()
    {
        var h = new Harness();
        DomainInventory inventory = h.SeedInventory();
        InventoryTransaction? row = null;
        await h.Ledger.AddAsync(Arg.Do<InventoryTransaction>(t => row = t), Arg.Any<CancellationToken>());

        Result<InventoryTransactionResult> result =
            await h.Service().GrantAsync(ProfileId, Change(Item, "item_potion", 10), "reward", "k1", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Single().QuantityAfter.Should().Be(10);
        inventory.QuantityOf(Item, "item_potion").Should().Be(10);
        row!.IsGrant.Should().BeTrue();
        row.IdempotencyKey.Should().Be("k1");
    }

    [Fact]
    public async Task Grant_fragment_validates_against_hero_config()
    {
        var h = new Harness();
        h.SeedInventory();

        Result<InventoryTransactionResult> ok =
            await h.Service().GrantAsync(ProfileId, Change(Fragment, "hero_a", 3), "reward", "k-frag", CancellationToken.None);
        Result<InventoryTransactionResult> bad =
            await h.Service().GrantAsync(ProfileId, Change(Fragment, "hero_missing", 3), "reward", "k-frag2", CancellationToken.None);

        ok.IsSuccess.Should().BeTrue();
        bad.IsFailure.Should().BeTrue();
        bad.Error.Code.Should().Be("INVENTORY_UNKNOWN_ITEM");
    }

    [Fact]
    public async Task Grant_unknown_item_id_is_rejected_without_ledger()
    {
        var h = new Harness();
        h.SeedInventory();

        Result<InventoryTransactionResult> result =
            await h.Service().GrantAsync(ProfileId, Change(Item, "item_nope", 5), "reward", "k2", CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("INVENTORY_UNKNOWN_ITEM");
        await h.Ledger.DidNotReceive().AddAsync(Arg.Any<InventoryTransaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Grant_unknown_item_type_is_rejected()
    {
        var h = new Harness();
        h.SeedInventory();

        Result<InventoryTransactionResult> result =
            await h.Service().GrantAsync(ProfileId, Change("weapon", "item_potion", 5), "reward", "k3", CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("INVENTORY_UNKNOWN_ITEM");
    }

    [Fact]
    public async Task Consume_insufficient_is_rejected_without_mutation()
    {
        var h = new Harness();
        DomainInventory inventory = h.SeedInventory((Item, "item_potion", 5));

        Result<InventoryTransactionResult> result =
            await h.Service().ConsumeAsync(ProfileId, Change(Item, "item_potion", 10), "use", "k4", CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("INVENTORY_INSUFFICIENT_CONFLICT");
        inventory.QuantityOf(Item, "item_potion").Should().Be(5);
        await h.Ledger.DidNotReceive().AddAsync(Arg.Any<InventoryTransaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_multi_item_partial_shortfall_fails_whole_without_mutation()
    {
        var h = new Harness();
        DomainInventory inventory = h.SeedInventory((Item, "item_potion", 10), (Item, "item_ore", 5));

        Result<InventoryTransactionResult> result = await h.Service().ConsumeAsync(
            ProfileId,
            [new ItemChange(Item, "item_potion", 5), new ItemChange(Item, "item_ore", 10)],
            "use",
            "k5",
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        inventory.QuantityOf(Item, "item_potion").Should().Be(10, "không bớt một phần khi một item thiếu");
        inventory.QuantityOf(Item, "item_ore").Should().Be(5);
    }

    [Fact]
    public async Task Retry_with_seen_key_returns_prior_result_without_applying()
    {
        var h = new Harness();
        DomainInventory inventory = h.SeedInventory((Item, "item_potion", 10));
        InventoryTransaction seen = InventoryTransaction.Record(
            Guid.NewGuid(), ProfileId, InventoryTransaction.GrantDirection,
            [new InventoryChange(Item, "item_potion", +10, 10)], "reward", "dup", Now);
        h.Ledger.GetByIdempotencyKeyAsync("dup", Arg.Any<CancellationToken>()).Returns(seen);

        Result<InventoryTransactionResult> result =
            await h.Service().GrantAsync(ProfileId, Change(Item, "item_potion", 10), "reward", "dup", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Single().QuantityAfter.Should().Be(10);
        inventory.QuantityOf(Item, "item_potion").Should().Be(10, "key đã xử lý ⇒ KHÔNG áp dụng lại");
        await h.Ledger.DidNotReceive().AddAsync(Arg.Any<InventoryTransaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Grant_merges_duplicate_entries_of_same_item()
    {
        var h = new Harness();
        DomainInventory inventory = h.SeedInventory();

        Result<InventoryTransactionResult> result = await h.Service().GrantAsync(
            ProfileId,
            [new ItemChange(Item, "item_potion", 3), new ItemChange(Item, "item_potion", 4)],
            "reward",
            "k6",
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle();
        inventory.QuantityOf(Item, "item_potion").Should().Be(7, "trùng (type,id) ⇒ gộp tổng");
    }
}
