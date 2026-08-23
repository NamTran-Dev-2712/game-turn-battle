using GameTeam.Domain.Profiles;

namespace GameTeam.Application.Abstractions.Persistence;

/// <summary>
/// Feature-specific repository for <see cref="PlayerProfile"/> (specializes <see cref="IRepository{TEntity,TId}"/>
/// with the query the profile feature needs). Declared in Application, implemented in Infrastructure (DIP) —
/// the implementation must NOT leak <c>IQueryable</c>/<c>DbContext</c>.
/// <para>
/// The profile is 1-1 with an account, so lookup is by <c>accountId</c> (backed by a unique index — the
/// DB-level idempotency guarantee, not a check-then-insert).
/// </para>
/// </summary>
public interface IPlayerProfileRepository : IRepository<PlayerProfile, Guid>
{
    /// <summary>Load the profile owned by <paramref name="accountId"/>, or <c>null</c> if none exists.</summary>
    Task<PlayerProfile?> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken);
}
