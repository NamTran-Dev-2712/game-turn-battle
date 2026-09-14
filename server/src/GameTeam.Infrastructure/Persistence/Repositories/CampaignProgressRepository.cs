using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Domain.Campaign;
using GameTeam.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace GameTeam.Infrastructure.Persistence.Repositories;

/// <summary>
/// Hiện thực EF Core của <see cref="ICampaignProgressRepository"/>. Stage đã clear (<see cref="ClearedStage"/>)
/// là owned collection ⇒ EF tự eager-load cùng aggregate (cần cho kiểm first-clear). Giữ mọi chi tiết query
/// trong Infrastructure — KHÔNG rò <c>IQueryable</c>/<c>DbContext</c>. Persist qua <see cref="IUnitOfWork"/>.
/// </summary>
public sealed class CampaignProgressRepository : ICampaignProgressRepository
{
    private readonly AppDbContext _dbContext;

    public CampaignProgressRepository(AppDbContext dbContext) => _dbContext = Guard.NotNull(dbContext);

    public async Task<CampaignProgress?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await _dbContext.Set<CampaignProgress>().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<CampaignProgress?> GetByProfileIdAsync(Guid profileId, CancellationToken cancellationToken)
        => await _dbContext.Set<CampaignProgress>().FirstOrDefaultAsync(p => p.ProfileId == profileId, cancellationToken);

    public async Task AddAsync(CampaignProgress entity, CancellationToken cancellationToken)
        => await _dbContext.Set<CampaignProgress>().AddAsync(entity, cancellationToken);
}
