using GameTeam.Application.Abstractions.Messaging;
using GameTeam.Contracts.Profile;
using GameTeam.Domain.Common;
using MediatR;

namespace GameTeam.Application.Features.Profile.Commands;

/// <summary>
/// Return the authenticated caller's profile, creating it if none exists yet (get-or-create). The owner is
/// resolved ONLY from the token <c>sub</c> (<c>ICurrentUser</c>) — the command carries NO client input, so
/// ownership cannot be spoofed.
/// <para>
/// Marked <see cref="ITransactionalRequest"/>: it may write (create a missing profile, or persist a
/// read-repair schema migration), so <c>TransactionBehavior</c> makes it atomic. Idempotency is guaranteed
/// at the DB level by a unique index on <c>account_id</c>, not by a check-then-insert.
/// </para>
/// </summary>
public sealed record GetOrCreateProfileCommand
    : IRequest<Result<ProfileDto>>, ITransactionalRequest;
