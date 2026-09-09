using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Domain.Battles;
using GameTeam.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace GameTeam.Infrastructure.Persistence.Repositories;

/// <summary>
/// Hiện thực EF Core của <see cref="IBattleRecordRepository"/>. Thưởng (owned JSON) tự eager-load cùng aggregate.
/// Giữ chi tiết query trong Infrastructure — KHÔNG rò <c>IQueryable</c>/<c>DbContext</c>. Persist qua
/// <see cref="IUnitOfWork"/> (SaveChanges lúc Commit).
/// </summary>
public sealed class BattleRecordRepository : IBattleRecordRepository
{
    private readonly AppDbContext _dbContext;

    public BattleRecordRepository(AppDbContext dbContext) => _dbContext = Guard.NotNull(dbContext);

    public async Task<BattleRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await _dbContext.BattleRecords.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<BattleRecord?> GetByProfileAndAttemptAsync(
        Guid profileId, string attemptId, CancellationToken cancellationToken)
        => await _dbContext.BattleRecords
            .FirstOrDefaultAsync(x => x.ProfileId == profileId && x.AttemptId == attemptId, cancellationToken);

    public async Task AddAsync(BattleRecord entity, CancellationToken cancellationToken)
        => await _dbContext.BattleRecords.AddAsync(entity, cancellationToken);
}
