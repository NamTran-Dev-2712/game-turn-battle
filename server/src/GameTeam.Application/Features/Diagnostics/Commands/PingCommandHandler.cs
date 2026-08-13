using GameTeam.Domain.Common;
using MediatR;

namespace GameTeam.Application.Features.Diagnostics.Commands;

/// <summary>
/// Handles <see cref="PingCommand"/>. Thin by design — cross-cutting concerns (validation, logging,
/// transaction) live in pipeline behaviors, so the handler only expresses intent and returns a
/// <see cref="Result"/>.
/// </summary>
public sealed class PingCommandHandler : IRequestHandler<PingCommand, Result>
{
    public Task<Result> Handle(PingCommand request, CancellationToken cancellationToken)
        => Task.FromResult(Result.Success());
}
