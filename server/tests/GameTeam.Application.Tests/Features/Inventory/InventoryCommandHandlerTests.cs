using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GameTeam.Application.Abstractions.Configuration;
using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Application.Abstractions.Security;
using GameTeam.Application.Features.Inventory;
using GameTeam.Application.Features.Inventory.Commands;
using GameTeam.Contracts.Config;
using GameTeam.Domain.Common;
using GameTeam.Domain.Inventory;
using GameTeam.Domain.Profiles;
using NSubstitute;
using Xunit;
using DomainInventory = GameTeam.Domain.Inventory.Inventory;

namespace GameTeam.Application.Tests.Features.Inventory;

/// <summary>
/// Phase 32 — command handler kho đồ: suy owner từ token sub (chống IDOR) → profile → uỷ cho
/// <see cref="InventoryService"/>. Unauthenticated ⇒ 401; không có profile ⇒ 404; happy path ⇒ delegate tới
/// service (ghi ledger). Validator chặn hình dạng sai.
/// </summary>
public sealed class InventoryCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 11, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid AccountId = Guid.NewGuid();

    private sealed class Harness
    {
        public ICurrentUser CurrentUser { get; } = Substitute.For<ICurrentUser>();
        public IPlayerProfileRepository Profiles { get; } = Substitute.For<IPlayerProfileRepository>();
        public IInventoryRepository Inventories { get; } = Substitute.For<IInventoryRepository>();
        public IInventoryTransactionRepository Ledger { get; } = Substitute.For<IInventoryTransactionRepository>();
        public IConfigProvider Config { get; } = Substitute.For<IConfigProvider>();
        public IClock Clock { get; } = Substitute.For<IClock>();
        public PlayerProfile Profile { get; } = PlayerProfile.CreateForAccount(Guid.NewGuid(), AccountId, Now);

        public Harness()
        {
            CurrentUser.AccountId.Returns(AccountId);
            Profiles.GetByAccountIdAsync(AccountId, Arg.Any<CancellationToken>()).Returns(Profile);
            Clock.UtcNow.Returns(Now);
            Config.CurrentVersion.Returns(new ConfigVersion(1, 1));
            Config.GetIds("item").Returns(new List<string> { "item_potion" });
            Config.GetIds("hero").Returns(new List<string> { "hero_a" });
            Inventories.GetByProfileIdForUpdateAsync(Profile.Id, Arg.Any<CancellationToken>())
                .Returns(DomainInventory.CreateFor(Guid.NewGuid(), Profile.Id, Now));
        }

        public InventoryService Service() => new(Inventories, Ledger, Config, Clock);

        public AddItemsCommandHandler AddHandler() => new(CurrentUser, Profiles, Service());

        public RemoveItemsCommandHandler RemoveHandler() => new(CurrentUser, Profiles, Service());
    }

    private static List<ItemChange> Change(string type, string id, long qty) => [new ItemChange(type, id, qty)];

    [Fact]
    public async Task Add_rejects_unauthenticated()
    {
        var h = new Harness();
        h.CurrentUser.AccountId.Returns((Guid?)null);

        Result<InventoryTransactionResult> result = await h.AddHandler().Handle(
            new AddItemsCommand(Change("item", "item_potion", 1), "reward", "k"), CancellationToken.None);

        result.Error.Code.Should().Be("UNAUTHENTICATED");
    }

    [Fact]
    public async Task Add_rejects_when_no_profile()
    {
        var h = new Harness();
        h.Profiles.GetByAccountIdAsync(AccountId, Arg.Any<CancellationToken>()).Returns((PlayerProfile?)null);

        Result<InventoryTransactionResult> result = await h.AddHandler().Handle(
            new AddItemsCommand(Change("item", "item_potion", 1), "reward", "k"), CancellationToken.None);

        result.Error.Code.Should().Be("PROFILE_NOT_FOUND");
    }

    [Fact]
    public async Task Add_happy_path_delegates_to_service_and_writes_ledger()
    {
        var h = new Harness();

        Result<InventoryTransactionResult> result = await h.AddHandler().Handle(
            new AddItemsCommand(Change("item", "item_potion", 5), "reward", "k-add"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Single().QuantityAfter.Should().Be(5);
        await h.Ledger.Received(1).AddAsync(Arg.Any<InventoryTransaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Remove_rejects_unauthenticated()
    {
        var h = new Harness();
        h.CurrentUser.AccountId.Returns((Guid?)null);

        Result<InventoryTransactionResult> result = await h.RemoveHandler().Handle(
            new RemoveItemsCommand(Change("item", "item_potion", 1), "use", "k"), CancellationToken.None);

        result.Error.Code.Should().Be("UNAUTHENTICATED");
    }

    [Fact]
    public void Add_validator_enforces_shape()
    {
        var validator = new AddItemsCommandValidator();

        validator.Validate(new AddItemsCommand([], "reward", "k")).IsValid.Should().BeFalse();
        validator.Validate(new AddItemsCommand(Change("weapon", "x", 1), "reward", "k")).IsValid.Should().BeFalse();
        validator.Validate(new AddItemsCommand(Change("item", "item_potion", 0), "reward", "k")).IsValid.Should().BeFalse();
        validator.Validate(new AddItemsCommand(Change("item", "item_potion", 1), "", "k")).IsValid.Should().BeFalse();
        validator.Validate(new AddItemsCommand(Change("item", "item_potion", 1), "reward", "")).IsValid.Should().BeFalse();
        validator.Validate(new AddItemsCommand(Change("item", "item_potion", 1), "reward", "k")).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Remove_validator_enforces_shape()
    {
        var validator = new RemoveItemsCommandValidator();

        validator.Validate(new RemoveItemsCommand([], "use", "k")).IsValid.Should().BeFalse();
        validator.Validate(new RemoveItemsCommand(Change("fragment", "hero_a", 2), "use", "k")).IsValid.Should().BeTrue();
    }
}
