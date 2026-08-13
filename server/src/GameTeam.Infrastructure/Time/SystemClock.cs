using GameTeam.Domain.Common;

namespace GameTeam.Infrastructure.Time;

/// <summary>
/// Server-time boundary implementation of <see cref="IClock"/> (Domain port). Infrastructure is the
/// one layer allowed to read the real clock — Domain/Application must go through <see cref="IClock"/>
/// (Forbidden Pattern otherwise). Registered in <c>AddInfrastructure</c> so composed handlers (e.g.
/// the sample <c>GetServerTimeQuery</c>) resolve a clock.
/// </summary>
public sealed class SystemClock : IClock
{
    /// <inheritdoc />
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
