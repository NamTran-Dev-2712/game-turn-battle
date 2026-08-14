using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace GameTeam.Infrastructure.Persistence.Repositories;

/// <summary>
/// Hiện thực EF Core generic của port <see cref="IRepository{TEntity, TId}"/> (Phase 10). Giữ chi tiết
/// persistence trong Infrastructure: <b>không</b> rò <see cref="IQueryable"/>/<c>DbSet</c>/<c>DbContext</c> ra
/// ngoài — chỉ trả entity/domain. Tối giản theo contract (chỉ <c>GetByIdAsync</c>+<c>AddAsync</c>); repo đặc thù
/// feature (phase 18+) chuyên biệt hoá sau. Persist thực hiện qua <see cref="IUnitOfWork"/> (SaveChanges ở Commit).
/// </summary>
/// <typeparam name="TEntity">Kiểu entity (aggregate/entity của Domain).</typeparam>
/// <typeparam name="TId">Kiểu định danh.</typeparam>
public sealed class EfRepository<TEntity, TId> : IRepository<TEntity, TId>
    where TEntity : Entity<TId>
    where TId : notnull
{
    private readonly AppDbContext _dbContext;

    public EfRepository(AppDbContext dbContext) => _dbContext = Guard.NotNull(dbContext);

    public async Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken)
        => await _dbContext.Set<TEntity>().FindAsync([id], cancellationToken);

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken)
        => await _dbContext.Set<TEntity>().AddAsync(entity, cancellationToken);
}
