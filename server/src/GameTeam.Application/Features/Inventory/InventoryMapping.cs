using GameTeam.Application.Features.Heroes;
using GameTeam.Contracts.Inventory;
using GameTeam.Domain.Heroes;
using DomainInventory = GameTeam.Domain.Inventory.Inventory;
using DomainItemStack = GameTeam.Domain.Inventory.ItemStack;

namespace GameTeam.Application.Features.Inventory;

/// <summary>
/// Ánh xạ kho (domain) → <see cref="InventoryDto"/> (wire) với <b>lọc theo loại</b> + <b>phân trang</b> +
/// <b>thứ tự tất định</b> ((item_type, item_id)) trên các chồng item; đồng thời <b>chiếu</b> hero sở hữu
/// (aggregate Phase 27) qua <see cref="HeroMapping.ToDto"/> — không nhân bản. EF entity KHÔNG trả thẳng ra API.
/// </summary>
internal static class InventoryMapping
{
    /// <summary>Kho rỗng (không stack, không hero) — dùng khi người chơi chưa có gì.</summary>
    public static InventoryDto Empty => new([], []);

    /// <summary>
    /// Dựng DTO: lọc stack theo <paramref name="itemTypeFilter"/> (null ⇒ tất cả), sắp xếp tất định, phân
    /// trang (<paramref name="page"/> ≥ 1, <paramref name="pageSize"/> &gt; 0), rồi ghép hero sở hữu.
    /// </summary>
    public static InventoryDto ToDto(
        DomainInventory? inventory,
        IReadOnlyList<OwnedHero> ownedHeroes,
        string? itemTypeFilter,
        int page,
        int pageSize)
    {
        IEnumerable<DomainItemStack> stacks = inventory?.Stacks ?? [];

        if (!string.IsNullOrWhiteSpace(itemTypeFilter))
        {
            stacks = stacks.Where(s => string.Equals(s.ItemType, itemTypeFilter, StringComparison.Ordinal));
        }

        List<ItemStackDto> items = stacks
            .OrderBy(s => s.ItemType, StringComparer.Ordinal)
            .ThenBy(s => s.ItemId, StringComparer.Ordinal)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new ItemStackDto(s.ItemType, s.ItemId, s.Quantity))
            .ToList();

        List<Contracts.Hero.OwnedHeroDto> heroes = ownedHeroes.Select(HeroMapping.ToDto).ToList();

        return new InventoryDto(items, heroes);
    }
}
