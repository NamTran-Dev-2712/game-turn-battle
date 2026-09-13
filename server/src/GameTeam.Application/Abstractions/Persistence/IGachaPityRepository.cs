using GameTeam.Domain.Gacha;

namespace GameTeam.Application.Abstractions.Persistence;

/// <summary>
/// Repository đặc thù cho <see cref="GachaPity"/> (mở rộng <see cref="IRepository{TEntity,TId}"/>). Bộ đếm pity
/// theo (<c>profileId</c>, <c>bannerId</c>) unique — server-authoritative (ADR-007/011). Khai báo ở Application,
/// hiện thực ở Infrastructure (DIP) — KHÔNG rò <c>IQueryable</c>/<c>DbContext</c>.
/// </summary>
public interface IGachaPityRepository : IRepository<GachaPity, Guid>
{
    /// <summary>Bộ đếm pity theo (profile, banner), hoặc <c>null</c> nếu chưa có.</summary>
    Task<GachaPity?> GetByProfileAndBannerAsync(Guid profileId, string bannerId, CancellationToken cancellationToken);

    /// <summary>
    /// Bộ đếm pity theo (profile, banner) với <b>khoá dòng</b> (<c>SELECT … FOR UPDATE</c>) — tuần tự hoá các
    /// lượt triệu hồi đồng thời của cùng (profile, banner). Chỉ có tác dụng trong transaction đang mở
    /// (TransactionBehavior). <c>null</c> nếu chưa có.
    /// </summary>
    Task<GachaPity?> GetByProfileAndBannerForUpdateAsync(Guid profileId, string bannerId, CancellationToken cancellationToken);
}
