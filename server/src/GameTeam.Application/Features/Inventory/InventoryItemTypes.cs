namespace GameTeam.Application.Features.Inventory;

/// <summary>
/// Vocabulary loại tài sản dạng stack trong inventory (Phase 32) — khớp <c>item_type</c>
/// (common.schema.json) và <c>reward_type</c> (reward.schema.json) trừ currency/hero. Nguồn sự thật một chỗ
/// cho mã chuỗi + kiểm hợp lệ, tránh rải literal.
/// <list type="bullet">
/// <item><c>item</c> — vật phẩm catalog chung (<c>item_id</c> ở config type <c>item</c>).</item>
/// <item><c>fragment</c> — mảnh của một hero (<c>item_id</c> là <c>hero_id</c> ở config type <c>hero</c>).</item>
/// </list>
/// </summary>
public static class InventoryItemTypes
{
    /// <summary>Vật phẩm catalog chung.</summary>
    public const string Item = "item";

    /// <summary>Mảnh của một hero (tham chiếu hero_id).</summary>
    public const string Fragment = "fragment";

    /// <summary>Khoá config type của catalog item (khớp <c>ConfigBundleBuilder.TypeKey(Item)</c>).</summary>
    public const string ItemConfigType = "item";

    /// <summary><c>true</c> nếu là loại stack hợp lệ.</summary>
    public static bool IsValid(string? itemType) => itemType is Item or Fragment;
}
