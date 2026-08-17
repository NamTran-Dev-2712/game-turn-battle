using GameTeam.Application.Common;
using GameTeam.Domain.Common;
using Microsoft.AspNetCore.Http;

namespace GameTeam.Api.Http;

/// <summary>
/// THE single, centralized mapping from a domain <see cref="Error.Code"/> to an HTTP status code
/// (Phase 13 — docs/backend/api-and-versioning.md §3/§4). Every error response goes through here so
/// mapping is never scattered across endpoints (the "one place" the phase Risks section requires).
/// </summary>
/// <remarks>
/// The Phase-09 error model is <c>Error(Code, Message)</c> with stable <c>SCREAMING_SNAKE_CASE</c>
/// codes — there is no closed enum taxonomy yet. So mapping is: (1) explicit well-known codes, then
/// (2) a forward-looking suffix convention that invents NO concrete codes, then (3) a safe default.
/// New endpoints reuse this table; they must not map errors ad-hoc.
/// </remarks>
public static class ErrorHttpMapping
{
    /// <summary>Explicit, well-known codes that already exist in the codebase.</summary>
    private static readonly Dictionary<string, int> KnownCodes = new(StringComparer.Ordinal)
    {
        // Phase 10 pipeline: FluentValidation failure surfaces as this code.
        [ValidationErrors.Code] = StatusCodes.Status400BadRequest,
    };

    /// <summary>
    /// Resolve the HTTP status for <paramref name="error"/>. Success/<see cref="Error.None"/> is a
    /// programming error here (callers only map failures) and defaults to 400.
    /// </summary>
    public static int ToStatusCode(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        string code = error.Code;
        if (string.IsNullOrEmpty(code))
        {
            return StatusCodes.Status400BadRequest;
        }

        if (KnownCodes.TryGetValue(code, out int known))
        {
            return known;
        }

        // Suffix convention — lets later phases add codes (e.g. HERO_NOT_FOUND, TEAM_CONFLICT)
        // and get the right status without touching this file, without inventing codes now.
        if (code.EndsWith("_NOT_FOUND", StringComparison.Ordinal))
        {
            return StatusCodes.Status404NotFound;
        }

        if (code.EndsWith("_CONFLICT", StringComparison.Ordinal))
        {
            return StatusCodes.Status409Conflict;
        }

        if (code is "UNAUTHENTICATED" || code.EndsWith("_UNAUTHORIZED", StringComparison.Ordinal))
        {
            return StatusCodes.Status401Unauthorized;
        }

        if (code.EndsWith("_FORBIDDEN", StringComparison.Ordinal))
        {
            return StatusCodes.Status403Forbidden;
        }

        // Default: treat an unmapped business failure as a bad request (client-correctable).
        return StatusCodes.Status400BadRequest;
    }
}
