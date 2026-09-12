using GameTeam.Contracts.Inventory;
using GameTeam.Domain.Common;
using MediatR;

namespace GameTeam.Application.Features.Inventory.Queries;

/// <summary>
/// Đọc kho đồ của người gọi (chủ sở hữu suy từ token <c>sub</c>). Trả kho <b>rỗng</b> nếu chưa có — không phải
/// lỗi. Hỗ trợ <b>lọc</b> theo loại (<paramref name="ItemType"/>: <c>item</c>/<c>fragment</c>; null = tất cả)
/// và <b>phân trang</b> trên các chồng item; hero sở hữu luôn được chiếu kèm. Read-only: KHÔNG tạo/sửa, không
/// transactional, không cacheable (state người chơi là chân lý server, per-account — ADR-007).
/// </summary>
/// <param name="ItemType">Lọc theo loại stack (<c>item</c>/<c>fragment</c>); null = tất cả loại.</param>
/// <param name="Page">Trang (bắt đầu từ 1) trên danh sách stack.</param>
/// <param name="PageSize">Kích thước trang (&gt; 0).</param>
public sealed record GetInventoryQuery(string? ItemType, int Page, int PageSize) : IRequest<Result<InventoryDto>>;
