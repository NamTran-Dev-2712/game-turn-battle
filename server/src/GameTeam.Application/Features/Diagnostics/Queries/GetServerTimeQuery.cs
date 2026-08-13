using GameTeam.Application.Abstractions.Messaging;
using GameTeam.Domain.Common;
using MediatR;

namespace GameTeam.Application.Features.Diagnostics.Queries;

/// <summary>
/// Sample read query proving the query pipeline (logging → caching → handler → <see cref="Result{T}"/>)
/// and that Application reads time through the <c>IClock</c> port, not the system clock. Marked
/// <see cref="ICacheableQuery"/> to exercise <c>CachingBehavior</c>. Diagnostics only.
/// </summary>
public sealed record GetServerTimeQuery : IRequest<Result<ServerTimeResponse>>, ICacheableQuery
{
    /// <inheritdoc />
    public string CacheKey => "server-time";

    /// <inheritdoc />
    public TimeSpan CacheTtl => TimeSpan.FromSeconds(1);
}

/// <summary>Server time projection returned by <see cref="GetServerTimeQuery"/>.</summary>
/// <param name="UtcNow">Current server time (UTC) sourced from <c>IClock</c>.</param>
public sealed record ServerTimeResponse(DateTimeOffset UtcNow);
