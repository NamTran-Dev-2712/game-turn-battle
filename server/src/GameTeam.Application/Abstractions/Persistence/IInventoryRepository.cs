using GameTeam.Domain.Inventory;

namespace GameTeam.Application.Abstractions.Persistence;

/// <summary>
/// Repository đặc thù cho <see cref="Inventory"/> (mở rộng <see cref="IRepository{TEntity,TId}"/> với tra
/// kho-theo-profile). Kho gắn 1-1 với profile (unique index). Trả về thực thể <b>được track</b> để thao tác
/// thêm/bớt trong transaction được lưu ở <c>SaveChanges</c>. Khai báo ở Application, hiện thực ở Infrastructure
/// (DIP) — KHÔNG rò <c>IQueryable</c>/<c>DbContext</c>.
/// </summary>
public interface IInventoryRepository : IRepository<Inventory, Guid>
{
    /// <summary>Kho mà <paramref name="profileId"/> sở hữu (được track), hoặc <c>null</c> nếu chưa có.</summary>
    Task<Inventory?> GetByProfileIdAsync(Guid profileId, CancellationToken cancellationToken);

    /// <summary>
    /// Như <see cref="GetByProfileIdAsync"/> nhưng <b>khoá dòng</b> (<c>SELECT … FOR UPDATE</c>) để tuần tự
    /// hoá các thao tác thêm/bớt đồng thời trên cùng kho (chống lost-update / số lượng sai — Phase 32). Phải
    /// gọi <b>bên trong</b> một transaction đang mở (<c>TransactionBehavior</c>). Trả thực thể được track.
    /// </summary>
    Task<Inventory?> GetByProfileIdForUpdateAsync(Guid profileId, CancellationToken cancellationToken);
}
