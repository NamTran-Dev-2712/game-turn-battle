using GameTeam.Domain.Teams;

namespace GameTeam.Application.Abstractions.Persistence;

/// <summary>
/// Repository đặc thù feature cho <see cref="Team"/> (mở rộng <see cref="IRepository{TEntity,TId}"/> với
/// truy vấn đội-theo-profile). Đội gắn 1-1 với profile ⇒ tra theo <c>profileId</c> (unique index — bảo đảm
/// idempotency ở DB). Khai báo ở Application, hiện thực ở Infrastructure (DIP) — KHÔNG rò <c>IQueryable</c>/<c>DbContext</c>.
/// </summary>
public interface ITeamRepository : IRepository<Team, Guid>
{
    /// <summary>Đội hình mà <paramref name="profileId"/> sở hữu, hoặc <c>null</c> nếu chưa lưu.</summary>
    Task<Team?> GetByProfileIdAsync(Guid profileId, CancellationToken cancellationToken);
}
