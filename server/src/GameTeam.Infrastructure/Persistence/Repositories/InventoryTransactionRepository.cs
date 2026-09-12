using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Domain.Common;
using GameTeam.Domain.Inventory;
using Microsoft.EntityFrameworkCore;

namespace GameTeam.Infrastructure.Persistence.Repositories;

/// <summary>
/// Hiện thực EF Core của <see cref="IInventoryTransactionRepository"/> — sổ cái giao dịch kho đồ (append-only).
/// Tra theo <c>idempotency_key</c> (unique) để nhận diện retry. KHÔNG rò <c>IQueryable</c>/<c>DbContext</c>.
/// </summary>
public sealed class InventoryTransactionRepository : IInventoryTransactionRepository
{
    private readonly AppDbContext _dbContext;

    public InventoryTransactionRepository(AppDbContext dbContext) => _dbContext = Guard.NotNull(dbContext);

    public async Task<InventoryTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await _dbContext.InventoryTransactions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<InventoryTransaction?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken)
        => await _dbContext.InventoryTransactions
            .FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);

    public async Task AddAsync(InventoryTransaction entity, CancellationToken cancellationToken)
        => await _dbContext.InventoryTransactions.AddAsync(entity, cancellationToken);
}
