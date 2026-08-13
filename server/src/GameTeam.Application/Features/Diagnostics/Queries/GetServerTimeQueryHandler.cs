using GameTeam.Domain.Common;
using MediatR;

namespace GameTeam.Application.Features.Diagnostics.Queries;

/// <summary>
/// Handles <see cref="GetServerTimeQuery"/> by reading <see cref="IClock.UtcNow"/> — never
/// <c>DateTime.Now/UtcNow</c> directly (server-time boundary, Phase 09). Thin: caching is applied by
/// <c>CachingBehavior</c>, not here.
/// </summary>
public sealed class GetServerTimeQueryHandler
    : IRequestHandler<GetServerTimeQuery, Result<ServerTimeResponse>>
{
    private readonly IClock _clock;

    public GetServerTimeQueryHandler(IClock clock) => _clock = clock;

    public Task<Result<ServerTimeResponse>> Handle(
        GetServerTimeQuery request,
        CancellationToken cancellationToken)
        => Task.FromResult(Result.Success(new ServerTimeResponse(_clock.UtcNow)));
}
