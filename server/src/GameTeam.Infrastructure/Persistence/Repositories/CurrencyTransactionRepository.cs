using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Domain.Common;
using GameTeam.Domain.Economy;
using Microsoft.EntityFrameworkCore;

namespace GameTeam.Infrastructure.Persistence.Repositories;

/// <summary>
/// Hiện thực EF Core của <see cref="ICurrencyTransactionRepository"/> — sổ cái giao dịch tiền tệ (append-only).
/// Tra theo <c>idempotency_key</c> (unique) để nhận diện retry. KHÔNG rò <c>IQueryable</c>/<c>DbContext</c>.
/// </summary>
public sealed class CurrencyTransactionRepository : ICurrencyTransactionRepository
{
    private readonly AppDbContext _dbContext;

    public CurrencyTransactionRepository(AppDbContext dbContext) => _dbContext = Guard.NotNull(dbContext);

    public async Task<CurrencyTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await _dbContext.CurrencyTransactions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<CurrencyTransaction?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken)
        => await _dbContext.CurrencyTransactions
            .FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);

    public async Task AddAsync(CurrencyTransaction entity, CancellationToken cancellationToken)
        => await _dbContext.CurrencyTransactions.AddAsync(entity, cancellationToken);
}
