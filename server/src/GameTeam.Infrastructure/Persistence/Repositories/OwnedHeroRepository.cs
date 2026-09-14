using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Domain.Common;
using GameTeam.Domain.Heroes;
using Microsoft.EntityFrameworkCore;

namespace GameTeam.Infrastructure.Persistence.Repositories;

/// <summary>
/// Hiện thực EF Core của <see cref="IOwnedHeroRepository"/>. Thêm truy vấn hero-theo-profile trên nền
/// load/add generic, giữ mọi chi tiết query trong Infrastructure — KHÔNG rò <c>IQueryable</c>/<c>DbContext</c>.
/// Persist qua <see cref="IUnitOfWork"/> (SaveChanges lúc Commit).
/// </summary>
public sealed class OwnedHeroRepository : IOwnedHeroRepository
{
    private readonly AppDbContext _dbContext;

    public OwnedHeroRepository(AppDbContext dbContext) => _dbContext = Guard.NotNull(dbContext);

    public async Task<OwnedHero?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await _dbContext.OwnedHeroes.FindAsync([id], cancellationToken);

    public async Task<IReadOnlyList<OwnedHero>> GetByProfileIdAsync(Guid profileId, CancellationToken cancellationToken)
        => await _dbContext.OwnedHeroes
            .Where(h => h.ProfileId == profileId)
            .OrderBy(h => h.CreatedAt)
            .ThenBy(h => h.HeroId)
            .ToListAsync(cancellationToken);

    public async Task<OwnedHero?> GetByProfileAndHeroForUpdateAsync(
        Guid profileId, string heroId, CancellationToken cancellationToken)
        // Khoá dòng hero (SELECT … FOR UPDATE) để tuần tự hoá nâng cấp đồng thời cùng (profile, hero) (Phase 35).
        // PostgreSQL chỉ khoá trong transaction đang mở (do TransactionBehavior mở). FormattableString ⇒ tham số
        // hoá (chống SQL injection). Tracked ⇒ mutation (LevelUp) lưu ở SaveChanges.
        => await _dbContext.OwnedHeroes
            .FromSql($"SELECT * FROM owned_heroes WHERE profile_id = {profileId} AND hero_id = {heroId} FOR UPDATE")
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(OwnedHero entity, CancellationToken cancellationToken)
        => await _dbContext.OwnedHeroes.AddAsync(entity, cancellationToken);
}
