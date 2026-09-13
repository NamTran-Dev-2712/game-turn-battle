using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Domain.Common;
using GameTeam.Domain.Gacha;
using Microsoft.EntityFrameworkCore;

namespace GameTeam.Infrastructure.Persistence.Repositories;

/// <summary>
/// Hiện thực EF Core của <see cref="IGachaPityRepository"/>. Bộ đếm pity theo (profile, banner). Biến thể
/// <c>ForUpdate</c> khoá dòng (<c>SELECT … FOR UPDATE</c>) để tuần tự hoá lượt quay đồng thời. KHÔNG rò
/// <c>IQueryable</c>/<c>DbContext</c>. Persist qua <see cref="IUnitOfWork"/> (SaveChanges lúc Commit).
/// </summary>
public sealed class GachaPityRepository : IGachaPityRepository
{
    private readonly AppDbContext _dbContext;

    public GachaPityRepository(AppDbContext dbContext) => _dbContext = Guard.NotNull(dbContext);

    public async Task<GachaPity?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await _dbContext.GachaPities.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<GachaPity?> GetByProfileAndBannerAsync(
        Guid profileId, string bannerId, CancellationToken cancellationToken)
        => await _dbContext.GachaPities
            .FirstOrDefaultAsync(x => x.ProfileId == profileId && x.BannerId == bannerId, cancellationToken);

    public async Task<GachaPity?> GetByProfileAndBannerForUpdateAsync(
        Guid profileId, string bannerId, CancellationToken cancellationToken)
        // Khoá dòng pity (SELECT … FOR UPDATE) để tuần tự hoá lượt quay đồng thời cùng (profile, banner).
        // PostgreSQL chỉ khoá trong transaction đang mở (TransactionBehavior). FormattableString ⇒ tham số hoá.
        => await _dbContext.GachaPities
            .FromSql($"SELECT * FROM gacha_pity WHERE profile_id = {profileId} AND banner_id = {bannerId} FOR UPDATE")
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(GachaPity entity, CancellationToken cancellationToken)
        => await _dbContext.GachaPities.AddAsync(entity, cancellationToken);
}
