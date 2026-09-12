namespace GameTeam.Application.Features.Inventory;

/// <summary>
/// Kết quả một giao dịch kho đồ (nội bộ Application — không phải wire DTO). Trả về bởi
/// <c>InventoryService</c> và được trả lại <b>nguyên vẹn</b> khi retry idempotent (dựng lại từ dòng ledger đã
/// lưu).
/// </summary>
/// <param name="Direction">Chiều giao dịch (<c>grant</c>/<c>consume</c>).</param>
/// <param name="IdempotencyKey">Khoá idempotency của giao dịch.</param>
/// <param name="Items">Các khoản thay đổi (có dấu) + số lượng sau thao tác.</param>
public sealed record InventoryTransactionResult(
    string Direction,
    string IdempotencyKey,
    IReadOnlyList<InventoryItemResult> Items);

/// <summary>Một dòng kết quả thay đổi item.</summary>
/// <param name="ItemType">Loại tài sản.</param>
/// <param name="ItemId">Id tham chiếu.</param>
/// <param name="Delta">Biến động có dấu (dương = grant, âm = consume).</param>
/// <param name="QuantityAfter">Số lượng sau thao tác.</param>
public sealed record InventoryItemResult(string ItemType, string ItemId, long Delta, long QuantityAfter);
