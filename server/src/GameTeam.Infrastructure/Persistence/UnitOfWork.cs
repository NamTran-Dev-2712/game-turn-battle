using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Domain.Common;
using Microsoft.EntityFrameworkCore.Storage;

namespace GameTeam.Infrastructure.Persistence;

/// <summary>
/// Hiện thực EF Core của port <see cref="IUnitOfWork"/> (Phase 10) — quản lý transaction atomic (ADR-007) mà
/// <c>TransactionBehavior</c> điều phối (begin → handler → commit/rollback).
/// <para>
/// Port <b>không</b> có <c>SaveChangesAsync</c>: <see cref="CommitAsync"/> tự gọi
/// <see cref="AppDbContext.SaveChangesAsync(CancellationToken)"/> (persist + dispatch domain event) <b>trong</b>
/// transaction đang mở rồi mới commit ⇒ event dispatch sau persist, cùng transaction. Rollback thực sự rollback;
/// exception làm behavior gọi Rollback (không để transaction lơ lửng).
/// </para>
/// Lifetime: <b>Scoped</b> — cùng scope với <see cref="AppDbContext"/> (một transaction cho mỗi request MediatR).
/// </summary>
public sealed class UnitOfWork : IUnitOfWork, IAsyncDisposable
{
    private readonly AppDbContext _dbContext;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(AppDbContext dbContext) => _dbContext = Guard.NotNull(dbContext);

    public async Task BeginTransactionAsync(CancellationToken cancellationToken)
    {
        if (_transaction is not null)
        {
            throw new InvalidOperationException("Đã có transaction đang mở; không mở lồng transaction thứ hai.");
        }

        _transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        if (_transaction is null)
        {
            throw new InvalidOperationException("Không có transaction đang mở để commit; gọi BeginTransactionAsync trước.");
        }

        try
        {
            // Persist + dispatch domain event (override SaveChanges) BÊN TRONG transaction, trước khi commit.
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _transaction.CommitAsync(cancellationToken);
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken)
    {
        if (_transaction is null)
        {
            return;
        }

        try
        {
            await _transaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    public async ValueTask DisposeAsync() => await DisposeTransactionAsync();

    private async ValueTask DisposeTransactionAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }
}
