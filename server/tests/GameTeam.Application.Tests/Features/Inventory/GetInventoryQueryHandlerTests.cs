using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Application.Abstractions.Security;
using GameTeam.Application.Features.Inventory;
using GameTeam.Application.Features.Inventory.Queries;
using GameTeam.Contracts.Inventory;
using GameTeam.Domain.Common;
using GameTeam.Domain.Heroes;
using GameTeam.Domain.Profiles;
using NSubstitute;
using Xunit;
using DomainInventory = GameTeam.Domain.Inventory.Inventory;

namespace GameTeam.Application.Tests.Features.Inventory;

/// <summary>
/// Phase 32 — <see cref="GetInventoryQueryHandler"/>: đọc kho của CHÍNH người gọi (owner từ token sub); chiếu
/// hero sở hữu (Phase 27); lọc theo loại + phân trang + thứ tự tất định; profile/kho chưa có ⇒ rỗng (không lỗi);
/// auth bắt buộc.
/// </summary>
public sealed class GetInventoryQueryHandlerTests
{
    private const string Item = InventoryItemTypes.Item;
    private const string Fragment = InventoryItemTypes.Fragment;

    private static readonly DateTimeOffset Now = new(2026, 9, 11, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid AccountId = Guid.NewGuid();

    private sealed class Harness
    {
        public ICurrentUser CurrentUser { get; } = Substitute.For<ICurrentUser>();
        public IPlayerProfileRepository Profiles { get; } = Substitute.For<IPlayerProfileRepository>();
        public IInventoryRepository Inventories { get; } = Substitute.For<IInventoryRepository>();
        public IOwnedHeroRepository OwnedHeroes { get; } = Substitute.For<IOwnedHeroRepository>();
        public PlayerProfile Profile { get; } = PlayerProfile.CreateForAccount(Guid.NewGuid(), AccountId, Now);

        public Harness()
        {
            CurrentUser.AccountId.Returns(AccountId);
            Profiles.GetByAccountIdAsync(AccountId, Arg.Any<CancellationToken>()).Returns(Profile);
            OwnedHeroes.GetByProfileIdAsync(Profile.Id, Arg.Any<CancellationToken>())
                .Returns(new List<OwnedHero>());
        }

        public GetInventoryQueryHandler Handler() => new(CurrentUser, Profiles, Inventories, OwnedHeroes);

        public void SeedInventory(params (string type, string id, long qty)[] stacks)
        {
            DomainInventory inventory = DomainInventory.CreateFor(Guid.NewGuid(), Profile.Id, Now);
            foreach ((string type, string id, long qty) in stacks)
            {
                inventory.Add(type, id, qty, Now);
            }

            Inventories.GetByProfileIdAsync(Profile.Id, Arg.Any<CancellationToken>()).Returns(inventory);
        }

        public void SeedHeroes(params string[] heroIds)
        {
            List<OwnedHero> heroes = heroIds
                .Select(id => OwnedHero.Grant(Guid.NewGuid(), Profile.Id, id, OwnedHero.InitialLevel, OwnedHero.InitialStars, Now))
                .ToList();
            OwnedHeroes.GetByProfileIdAsync(Profile.Id, Arg.Any<CancellationToken>()).Returns(heroes);
        }
    }

    [Fact]
    public async Task Returns_stacks_and_projects_owned_heroes()
    {
        var h = new Harness();
        h.SeedInventory((Item, "item_potion", 10), (Fragment, "hero_a", 5));
        h.SeedHeroes("hero_a", "hero_b");

        Result<InventoryDto> result = await h.Handler().Handle(new GetInventoryQuery(null, 1, 50), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.OwnedHeroes.Select(x => x.HeroId).Should().BeEquivalentTo("hero_a", "hero_b");
    }

    [Fact]
    public async Task Filters_stacks_by_item_type()
    {
        var h = new Harness();
        h.SeedInventory((Item, "item_potion", 10), (Item, "item_ore", 3), (Fragment, "hero_a", 5));

        Result<InventoryDto> onlyFragments =
            await h.Handler().Handle(new GetInventoryQuery(Fragment, 1, 50), CancellationToken.None);

        onlyFragments.Value.Items.Should().ContainSingle();
        onlyFragments.Value.Items.Single().ItemType.Should().Be(Fragment);
    }

    [Fact]
    public async Task Paginates_stacks_with_deterministic_order()
    {
        var h = new Harness();
        h.SeedInventory(
            (Item, "item_c", 1), (Item, "item_a", 1), (Item, "item_b", 1), (Item, "item_d", 1));

        Result<InventoryDto> page1 = await h.Handler().Handle(new GetInventoryQuery(Item, 1, 2), CancellationToken.None);
        Result<InventoryDto> page2 = await h.Handler().Handle(new GetInventoryQuery(Item, 2, 2), CancellationToken.None);

        page1.Value.Items.Select(x => x.ItemId).Should().Equal("item_a", "item_b");
        page2.Value.Items.Select(x => x.ItemId).Should().Equal("item_c", "item_d");
    }

    [Fact]
    public async Task Returns_empty_when_no_profile()
    {
        var h = new Harness();
        h.Profiles.GetByAccountIdAsync(AccountId, Arg.Any<CancellationToken>()).Returns((PlayerProfile?)null);

        Result<InventoryDto> result = await h.Handler().Handle(new GetInventoryQuery(null, 1, 50), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.OwnedHeroes.Should().BeEmpty();
    }

    [Fact]
    public async Task Returns_empty_when_no_inventory()
    {
        var h = new Harness();
        h.Inventories.GetByProfileIdAsync(h.Profile.Id, Arg.Any<CancellationToken>()).Returns((DomainInventory?)null);

        Result<InventoryDto> result = await h.Handler().Handle(new GetInventoryQuery(null, 1, 50), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Rejects_unauthenticated()
    {
        var h = new Harness();
        h.CurrentUser.AccountId.Returns((Guid?)null);

        Result<InventoryDto> result = await h.Handler().Handle(new GetInventoryQuery(null, 1, 50), CancellationToken.None);

        result.Error.Code.Should().Be("UNAUTHENTICATED");
    }

    [Fact]
    public void Validator_rejects_bad_paging_and_type()
    {
        var validator = new GetInventoryQueryValidator();

        validator.Validate(new GetInventoryQuery(null, 0, 50)).IsValid.Should().BeFalse();
        validator.Validate(new GetInventoryQuery(null, 1, 0)).IsValid.Should().BeFalse();
        validator.Validate(new GetInventoryQuery(null, 1, GetInventoryQueryValidator.MaxPageSize + 1)).IsValid.Should().BeFalse();
        validator.Validate(new GetInventoryQuery("weapon", 1, 50)).IsValid.Should().BeFalse();
        validator.Validate(new GetInventoryQuery("item", 1, 50)).IsValid.Should().BeTrue();
        validator.Validate(new GetInventoryQuery(null, 1, 50)).IsValid.Should().BeTrue();
    }
}
