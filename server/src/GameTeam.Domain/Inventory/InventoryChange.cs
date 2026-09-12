namespace GameTeam.Domain.Inventory;

/// <summary>
/// Một dòng thay đổi tài sản trong một <see cref="InventoryTransaction"/> (append-only). Bất biến — chỉ ghi.
/// <see cref="Delta"/> có dấu (dương = grant, âm = consume); <see cref="QuantityAfter"/> là snapshot số lượng
/// sau thao tác (để trả lại kết quả khi retry idempotent). Một transaction có thể chứa NHIỀU dòng
/// (thao tác nhiều-item atomic — Phase 32).
/// </summary>
public sealed class InventoryChange
{
    /// <summary>Ctor không tham số cho hydration/ORM — không dùng trong logic nghiệp vụ.</summary>
    private InventoryChange()
    {
    }

    /// <summary>Dựng một dòng thay đổi. Guard: type/id không rỗng, delta khác 0, quantity-after không âm.</summary>
    public InventoryChange(string itemType, string itemId, long delta, long quantityAfter)
    {
        if (string.IsNullOrWhiteSpace(itemType))
        {
            throw new ArgumentException("ItemType không được rỗng.", nameof(itemType));
        }

        if (string.IsNullOrWhiteSpace(itemId))
        {
            throw new ArgumentException("ItemId không được rỗng.", nameof(itemId));
        }

        if (delta == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(delta), delta, "Delta phải khác 0 (grant dương / consume âm).");
        }

        if (quantityAfter < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantityAfter), quantityAfter, "Số lượng sau thao tác không được âm.");
        }

        ItemType = itemType;
        ItemId = itemId;
        Delta = delta;
        QuantityAfter = quantityAfter;
    }

    /// <summary>Loại tài sản (<c>item</c>/<c>fragment</c>).</summary>
    public string ItemType { get; private set; } = string.Empty;

    /// <summary>Id tham chiếu (catalog item id hoặc hero id với fragment).</summary>
    public string ItemId { get; private set; } = string.Empty;

    /// <summary>Biến động số lượng có dấu: <c>&gt; 0</c> là grant, <c>&lt; 0</c> là consume.</summary>
    public long Delta { get; private set; }

    /// <summary>Số lượng của chồng này sau thao tác (snapshot để trả lại kết quả khi retry idempotent).</summary>
    public long QuantityAfter { get; private set; }
}
