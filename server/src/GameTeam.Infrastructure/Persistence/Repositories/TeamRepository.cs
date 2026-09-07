using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Domain.Common;
using GameTeam.Domain.Teams;
using Microsoft.EntityFrameworkCore;

namespace GameTeam.Infrastructure.Persistence.Repositories;

/// <summary>
/// Hiện thực EF Core của <see cref="ITeamRepository"/>. Ô đội hình (<see cref="TeamSlot"/>) là owned collection
/// ⇒ EF tự eager-load cùng aggregate. Giữ mọi chi tiết query trong Infrastructure — KHÔNG rò
/// <c>IQueryable</c>/<c>DbContext</c>. Persist qua <see cref="IUnitOfWork"/> (SaveChanges lúc Commit).
/// </summary>
public sealed class TeamRepository : ITeamRepository
{
    private readonly AppDbContext _dbContext;

    public TeamRepository(AppDbContext dbContext) => _dbContext = Guard.NotNull(dbContext);

    public async Task<Team?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await _dbContext.Teams.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<Team?> GetByProfileIdAsync(Guid profileId, CancellationToken cancellationToken)
        => await _dbContext.Teams.FirstOrDefaultAsync(t => t.ProfileId == profileId, cancellationToken);

    public async Task AddAsync(Team entity, CancellationToken cancellationToken)
        => await _dbContext.Teams.AddAsync(entity, cancellationToken);
}
