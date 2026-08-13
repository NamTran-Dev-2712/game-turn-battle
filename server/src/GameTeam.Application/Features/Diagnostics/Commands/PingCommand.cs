using GameTeam.Application.Abstractions.Messaging;
using GameTeam.Domain.Common;
using MediatR;

namespace GameTeam.Application.Features.Diagnostics.Commands;

/// <summary>
/// Sample write command proving the command pipeline end-to-end (validation → transaction → handler
/// → <see cref="Result"/>). Marked <see cref="ITransactionalRequest"/> to exercise
/// <c>TransactionBehavior</c>. Diagnostics only — not a business feature.
/// </summary>
/// <param name="Message">Arbitrary echo payload; validated (non-empty, bounded length).</param>
public sealed record PingCommand(string Message) : IRequest<Result>, ITransactionalRequest;
