namespace GameTeam.Application.Abstractions.Persistence;

/// <summary>
/// Unit-of-work / transaction scope port (declared in Application, implemented in Infrastructure — DIP).
/// <para>
/// Used by <c>TransactionBehavior</c> to make a write command atomic: begin before the handler,
/// commit on success, rollback on failure/exception. Exposes NO Infrastructure type (no
/// <c>DbContext</c>, no EF transaction) — Phase 11 (EF Core) implements it.
/// </para>
/// </summary>
public interface IUnitOfWork
{
    /// <summary>Open a transaction/atomic scope for the current write operation.</summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken);

    /// <summary>Commit the current transaction (call once the handler succeeded).</summary>
    Task CommitAsync(CancellationToken cancellationToken);

    /// <summary>Roll back the current transaction (handler returned a failure or threw).</summary>
    Task RollbackAsync(CancellationToken cancellationToken);
}
