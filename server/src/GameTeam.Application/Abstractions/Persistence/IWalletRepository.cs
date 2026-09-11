using GameTeam.Domain.Economy;

namespace GameTeam.Application.Abstractions.Persistence;

/// <summary>
/// Repository đặc thù cho <see cref="Wallet"/> (mở rộng <see cref="IRepository{TEntity,TId}"/> với tra
/// ví-theo-profile). Ví gắn 1-1 với profile (unique index). Trả về thực thể <b>được track</b> để credit trong
/// transaction được lưu ở <c>SaveChanges</c>. Khai báo ở Application, hiện thực ở Infrastructure (DIP).
/// </summary>
public interface IWalletRepository : IRepository<Wallet, Guid>
{
    /// <summary>Ví mà <paramref name="profileId"/> sở hữu (được track), hoặc <c>null</c> nếu chưa có.</summary>
    Task<Wallet?> GetByProfileIdAsync(Guid profileId, CancellationToken cancellationToken);

    /// <summary>
    /// Như <see cref="GetByProfileIdAsync"/> nhưng <b>khoá dòng</b> (<c>SELECT … FOR UPDATE</c>) để tuần tự
    /// hoá các thao tác cấp/tiêu đồng thời trên cùng ví (chống lost-update / số dư sai — Phase 31). Phải gọi
    /// <b>bên trong</b> một transaction đang mở (<c>TransactionBehavior</c>). Trả thực thể được track.
    /// </summary>
    Task<Wallet?> GetByProfileIdForUpdateAsync(Guid profileId, CancellationToken cancellationToken);
}
