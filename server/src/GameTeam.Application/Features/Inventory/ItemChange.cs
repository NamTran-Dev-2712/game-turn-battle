namespace GameTeam.Application.Features.Inventory;

/// <summary>
/// Một khoản thay đổi item yêu cầu (đầu vào command — nội bộ Application, không phải wire DTO). Số lượng
/// <b>luôn dương</b> (chiều grant/consume do command quyết định). Nhiều <see cref="ItemChange"/> trong một
/// command ⇒ thao tác nhiều-item atomic (Phase 32).
/// </summary>
/// <param name="ItemType">Loại tài sản (<c>item</c>/<c>fragment</c> — xem <see cref="InventoryItemTypes"/>).</param>
/// <param name="ItemId">Id tham chiếu (catalog item id hoặc hero id với fragment).</param>
/// <param name="Quantity">Số lượng (&gt; 0).</param>
public sealed record ItemChange(string ItemType, string ItemId, long Quantity);
