using GameTeam.Domain.Common;

namespace GameTeam.Application.Abstractions.Persistence;

/// <summary>
/// Generic repository port for aggregates/entities (declared in Application, implemented in
/// Infrastructure — DIP). Deliberately minimal: only the irreducible load + add operations every
/// repository needs. Feature-specific repositories (phase 18+) specialize this with their own
/// query methods — this base does NOT declare speculative Update/Delete/List.
/// </summary>
/// <typeparam name="TEntity">Aggregate/entity type (a Domain <see cref="Entity{TId}"/>).</typeparam>
/// <typeparam name="TId">Identity type of the entity (not hardcoded to Guid/long).</typeparam>
public interface IRepository<TEntity, TId>
    where TEntity : Entity<TId>
    where TId : notnull
{
    /// <summary>Load an entity by identity, or <c>null</c> if it does not exist.</summary>
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken);

    /// <summary>Stage a new entity for persistence (committed by <see cref="IUnitOfWork"/>).</summary>
    Task AddAsync(TEntity entity, CancellationToken cancellationToken);
}
