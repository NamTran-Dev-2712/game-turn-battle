using GameTeam.Domain.Heroes;

namespace GameTeam.Application.Abstractions.Persistence;

/// <summary>
/// Repository đặc thù feature cho <see cref="OwnedHero"/> (mở rộng <see cref="IRepository{TEntity,TId}"/>
/// với truy vấn hero-theo-profile). Khai báo ở Application, hiện thực ở Infrastructure (DIP) — hiện thực
/// KHÔNG rò <c>IQueryable</c>/<c>DbContext</c>.
/// </summary>
public interface IOwnedHeroRepository : IRepository<OwnedHero, Guid>
{
    /// <summary>Danh sách hero mà <paramref name="profileId"/> sở hữu (rỗng nếu không có).</summary>
    Task<IReadOnlyList<OwnedHero>> GetByProfileIdAsync(Guid profileId, CancellationToken cancellationToken);
}
