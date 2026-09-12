namespace GameTeam.Domain.Inventory;

/// <summary>
/// Một chồng (stack) tài sản trong <see cref="Inventory"/> — một dòng <c>(item_type, item_id) → quantity</c>.
/// Hỗ trợ cộng (grant) và trừ (consume); bất biến <b>số nguyên không âm</b> (ADR-011) được bảo vệ ở
/// <see cref="Subtract"/>. <c>item_type=item</c> ⇒ <c>item_id</c> trỏ catalog item; <c>item_type=fragment</c>
/// ⇒ <c>item_id</c> trỏ hero (mảnh của hero) — kiểm data-driven ở tầng Application (Phase 32).
/// </summary>
public sealed class ItemStack
{
    /// <summary>Ctor không tham số cho hydration/ORM — không dùng trong logic nghiệp vụ.</summary>
    private ItemStack()
    {
    }

    /// <summary>Dựng một chồng tài sản. Guard: type/id không rỗng, quantity không âm.</summary>
    public ItemStack(string itemType, string itemId, long quantity)
    {
        if (string.IsNullOrWhiteSpace(itemType))
        {
            throw new ArgumentException("ItemType không được rỗng.", nameof(itemType));
        }

        if (string.IsNullOrWhiteSpace(itemId))
        {
            throw new ArgumentException("ItemId không được rỗng.", nameof(itemId));
        }

        if (quantity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Quantity không được âm.");
        }

        ItemType = itemType;
        ItemId = itemId;
        Quantity = quantity;
    }

    /// <summary>Loại tài sản (<c>item</c>/<c>fragment</c>).</summary>
    public string ItemType { get; private set; } = string.Empty;

    /// <summary>Id tham chiếu (catalog item id hoặc hero id với fragment).</summary>
    public string ItemId { get; private set; } = string.Empty;

    /// <summary>Số lượng hiện tại (không âm).</summary>
    public long Quantity { get; private set; }

    /// <summary>Cộng thêm vào số lượng (chỉ dùng nội bộ <see cref="Inventory.Add"/>).</summary>
    internal void Add(long delta)
    {
        if (delta < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(delta), delta, "Delta cộng không được âm.");
        }

        Quantity += delta;
    }

    /// <summary>
    /// Trừ khỏi số lượng (chỉ dùng nội bộ <see cref="Inventory.Remove"/>). Bất biến không âm: ném nếu kết quả &lt; 0.
    /// </summary>
    internal void Subtract(long delta)
    {
        if (delta < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(delta), delta, "Delta trừ không được âm.");
        }

        if (Quantity - delta < 0)
        {
            throw new InvalidOperationException("Số lượng không được âm (bất biến ADR-011).");
        }

        Quantity -= delta;
    }
}
