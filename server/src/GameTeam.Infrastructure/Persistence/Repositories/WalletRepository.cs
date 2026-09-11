using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Domain.Common;
using GameTeam.Domain.Economy;
using Microsoft.EntityFrameworkCore;

namespace GameTeam.Infrastructure.Persistence.Repositories;

/// <summary>
/// Hiện thực EF Core của <see cref="IWalletRepository"/>. Số dư (owned JSON) tự eager-load; thực thể trả về
/// được <b>track</b> ⇒ <see cref="Wallet.Credit"/> được lưu ở <c>SaveChanges</c> (trong transaction). KHÔNG rò
/// <c>IQueryable</c>/<c>DbContext</c>.
/// </summary>
public sealed class WalletRepository : IWalletRepository
{
    private readonly AppDbContext _dbContext;

    public WalletRepository(AppDbContext dbContext) => _dbContext = Guard.NotNull(dbContext);

    public async Task<Wallet?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await _dbContext.Wallets.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<Wallet?> GetByProfileIdAsync(Guid profileId, CancellationToken cancellationToken)
        => await _dbContext.Wallets.FirstOrDefaultAsync(x => x.ProfileId == profileId, cancellationToken);

    public async Task<Wallet?> GetByProfileIdForUpdateAsync(Guid profileId, CancellationToken cancellationToken)
        // Khoá dòng ví (SELECT … FOR UPDATE) để tuần tự hoá cấp/tiêu đồng thời (Phase 31). PostgreSQL chỉ khoá
        // trong transaction đang mở (do TransactionBehavior mở). Chọn *: cột JSON `balances` được EF rehydrate.
        // FormattableString ⇒ tham số hoá (chống SQL injection). Tracked ⇒ mutation lưu ở SaveChanges.
        => await _dbContext.Wallets
            .FromSql($"SELECT * FROM wallets WHERE profile_id = {profileId} FOR UPDATE")
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(Wallet entity, CancellationToken cancellationToken)
        => await _dbContext.Wallets.AddAsync(entity, cancellationToken);
}
