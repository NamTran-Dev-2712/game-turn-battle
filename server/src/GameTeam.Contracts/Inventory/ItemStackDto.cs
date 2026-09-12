namespace GameTeam.Contracts.Inventory;

/// <summary>
/// Một chồng (stack) tài sản trong kho (bản wire chỉ-đọc, server-authoritative — ADR-007). Client hiển thị số
/// lượng từ đây (qua <c>StateCache</c>), KHÔNG tự cộng/trừ. Tối giản: chỉ loại + id + số lượng; tên/metadata
/// hiển thị client ghép từ config qua <c>ConfigProvider</c> theo <see cref="ItemId"/> (data-driven, ADR-004).
/// </summary>
/// <param name="ItemType">Loại tài sản (<c>item</c>/<c>fragment</c>).</param>
/// <param name="ItemId">Id tham chiếu (catalog item id, hoặc hero id với fragment).</param>
/// <param name="Quantity">Số lượng hiện tại (integer, không âm).</param>
public sealed record ItemStackDto(string ItemType, string ItemId, long Quantity);
