using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Domain.Common;
using Microsoft.EntityFrameworkCore;
using DomainInventory = GameTeam.Domain.Inventory.Inventory;

namespace GameTeam.Infrastructure.Persistence.Repositories;

/// <summary>
/// Hiện thực EF Core của <see cref="IInventoryRepository"/>. Các chồng tài sản (owned JSON) tự eager-load; thực
/// thể trả về được <b>track</b> ⇒ thêm/bớt được lưu ở <c>SaveChanges</c> (trong transaction). KHÔNG rò
/// <c>IQueryable</c>/<c>DbContext</c>.
/// </summary>
public sealed class InventoryRepository : IInventoryRepository
{
    private readonly AppDbContext _dbContext;

    public InventoryRepository(AppDbContext dbContext) => _dbContext = Guard.NotNull(dbContext);

    public async Task<DomainInventory?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await _dbContext.Inventories.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<DomainInventory?> GetByProfileIdAsync(Guid profileId, CancellationToken cancellationToken)
        => await _dbContext.Inventories.FirstOrDefaultAsync(x => x.ProfileId == profileId, cancellationToken);

    public async Task<DomainInventory?> GetByProfileIdForUpdateAsync(Guid profileId, CancellationToken cancellationToken)
        // Khoá dòng kho (SELECT … FOR UPDATE) để tuần tự hoá thêm/bớt đồng thời (Phase 32). PostgreSQL chỉ khoá
        // trong transaction đang mở (do TransactionBehavior mở). Chọn *: cột JSON `stacks` được EF rehydrate.
        // FormattableString ⇒ tham số hoá (chống SQL injection). Tracked ⇒ mutation lưu ở SaveChanges.
        => await _dbContext.Inventories
            .FromSql($"SELECT * FROM inventories WHERE profile_id = {profileId} FOR UPDATE")
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(DomainInventory entity, CancellationToken cancellationToken)
        => await _dbContext.Inventories.AddAsync(entity, cancellationToken);
}
