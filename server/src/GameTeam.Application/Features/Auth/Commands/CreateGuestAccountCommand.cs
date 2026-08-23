using GameTeam.Application.Abstractions.Messaging;
using GameTeam.Contracts.Auth;
using GameTeam.Domain.Common;
using MediatR;

namespace GameTeam.Application.Features.Auth.Commands;

/// <summary>
/// Guest login (ADR-008): create a fresh guest <see cref="Domain.Accounts.Account"/> and issue a JWT.
/// Marked <see cref="ITransactionalRequest"/> so <c>TransactionBehavior</c> wraps the account insert in
/// a unit of work (commit on success, rollback on failure). Returns the wire contract
/// <see cref="AuthGuestResponse"/> on success.
/// </summary>
/// <param name="DeviceId">Optional client device identifier (used later to reattach a guest).</param>
public sealed record CreateGuestAccountCommand(string? DeviceId)
    : IRequest<Result<AuthGuestResponse>>, ITransactionalRequest;
