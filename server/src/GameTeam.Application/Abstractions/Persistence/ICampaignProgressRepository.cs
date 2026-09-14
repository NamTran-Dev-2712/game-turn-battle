using GameTeam.Domain.Campaign;

namespace GameTeam.Application.Abstractions.Persistence;

/// <summary>
/// Repository đặc thù feature cho <see cref="CampaignProgress"/> (mở rộng <see cref="IRepository{TEntity,TId}"/>
/// với truy vấn tiến-độ-theo-profile). Tiến độ gắn 1-1 với profile ⇒ tra theo <c>profileId</c> (unique index —
/// bảo đảm idempotency ở DB). Khai báo ở Application, hiện thực ở Infrastructure (DIP) — KHÔNG rò
/// <c>IQueryable</c>/<c>DbContext</c>.
/// </summary>
public interface ICampaignProgressRepository : IRepository<CampaignProgress, Guid>
{
    /// <summary>Tiến độ campaign mà <paramref name="profileId"/> sở hữu, hoặc <c>null</c> nếu chưa có.</summary>
    Task<CampaignProgress?> GetByProfileIdAsync(Guid profileId, CancellationToken cancellationToken);
}
