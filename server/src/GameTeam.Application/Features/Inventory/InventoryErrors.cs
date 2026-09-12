using GameTeam.Domain.Common;

namespace GameTeam.Application.Features.Inventory;

/// <summary>Lỗi nghiệp vụ của hệ kho đồ (mã ổn định — ánh xạ HTTP bởi <c>ErrorHttpMapping</c>).</summary>
public static class InventoryErrors
{
    /// <summary>Chưa xác thực (không có token hợp lệ) → 401.</summary>
    public static Error Unauthenticated => new("UNAUTHENTICATED", "Yêu cầu chưa xác thực.");

    /// <summary>Không tìm thấy profile của người gọi → 404.</summary>
    public static Error ProfileNotFound => new("PROFILE_NOT_FOUND", "Không tìm thấy profile của người gọi.");

    /// <summary>Item không hợp lệ: loại sai hoặc id không tồn tại trong config → 400.</summary>
    public static Error UnknownItem =>
        new("INVENTORY_UNKNOWN_ITEM", "Vật phẩm không hợp lệ (loại sai hoặc id không tồn tại trong config).");

    /// <summary>
    /// Số lượng không đủ để bớt → 409 (lỗi nghiệp vụ mong đợi; KHÔNG làm thay đổi kho — thao tác nhiều-item
    /// atomic: một item thiếu ⇒ toàn bộ command fail).
    /// </summary>
    public static Error Insufficient =>
        new("INVENTORY_INSUFFICIENT_CONFLICT", "Số lượng vật phẩm không đủ để thực hiện thao tác.");
}
