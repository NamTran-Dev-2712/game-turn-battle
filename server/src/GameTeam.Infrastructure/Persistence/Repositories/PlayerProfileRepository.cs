using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Domain.Common;
using GameTeam.Domain.Profiles;
using Microsoft.EntityFrameworkCore;

namespace GameTeam.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IPlayerProfileRepository"/>. Adds the by-account lookup on top of
/// the generic load/add, keeping all query detail inside Infrastructure — no <c>IQueryable</c>/<c>DbContext</c>
/// leaks out. Persist happens via <see cref="IUnitOfWork"/> (SaveChanges at Commit).
/// </summary>
public sealed class PlayerProfileRepository : IPlayerProfileRepository
{
    private readonly AppDbContext _dbContext;

    public PlayerProfileRepository(AppDbContext dbContext) => _dbContext = Guard.NotNull(dbContext);

    public async Task<PlayerProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await _dbContext.PlayerProfiles.FindAsync([id], cancellationToken);

    public async Task<PlayerProfile?> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken)
        => await _dbContext.PlayerProfiles
            .FirstOrDefaultAsync(p => p.AccountId == accountId, cancellationToken);

    public async Task AddAsync(PlayerProfile entity, CancellationToken cancellationToken)
        => await _dbContext.PlayerProfiles.AddAsync(entity, cancellationToken);
}
