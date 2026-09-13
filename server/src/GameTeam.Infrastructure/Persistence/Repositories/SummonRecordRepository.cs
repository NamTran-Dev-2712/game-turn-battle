using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Domain.Common;
using GameTeam.Domain.Gacha;
using Microsoft.EntityFrameworkCore;

namespace GameTeam.Infrastructure.Persistence.Repositories;

/// <summary>
/// Hiện thực EF Core của <see cref="ISummonRecordRepository"/>. Pulls (owned JSON) tự eager-load cùng aggregate.
/// Giữ chi tiết query trong Infrastructure — KHÔNG rò <c>IQueryable</c>/<c>DbContext</c>. Persist qua
/// <see cref="IUnitOfWork"/> (SaveChanges lúc Commit).
/// </summary>
public sealed class SummonRecordRepository : ISummonRecordRepository
{
    private readonly AppDbContext _dbContext;

    public SummonRecordRepository(AppDbContext dbContext) => _dbContext = Guard.NotNull(dbContext);

    public async Task<SummonRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await _dbContext.SummonRecords.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<SummonRecord?> GetByProfileAndRequestAsync(
        Guid profileId, string requestId, CancellationToken cancellationToken)
        => await _dbContext.SummonRecords
            .FirstOrDefaultAsync(x => x.ProfileId == profileId && x.RequestId == requestId, cancellationToken);

    public async Task AddAsync(SummonRecord entity, CancellationToken cancellationToken)
        => await _dbContext.SummonRecords.AddAsync(entity, cancellationToken);
}
