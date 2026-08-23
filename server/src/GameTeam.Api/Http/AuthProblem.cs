using GameTeam.Contracts.Common;
using Microsoft.AspNetCore.Http;

namespace GameTeam.Api.Http;

/// <summary>
/// Writes the standard <see cref="ErrorEnvelope"/> for authentication/authorization failures so a
/// missing/invalid token returns the SAME error contract as every other error (Phase 05/13) — never an
/// empty body or a framework-default shape. Reused by the JWT bearer challenge/forbid events.
/// </summary>
public static class AuthProblem
{
    /// <summary>Stable code for a missing/invalid/expired token → 401 (maps in <see cref="ErrorHttpMapping"/>).</summary>
    public const string UnauthenticatedCode = "UNAUTHENTICATED";

    /// <summary>Stable code for an authenticated-but-not-permitted request → 403.</summary>
    public const string ForbiddenCode = "FORBIDDEN";

    /// <summary>Write a 401 <see cref="ErrorEnvelope"/> (used by the JWT challenge event).</summary>
    public static Task WriteUnauthenticatedAsync(HttpContext context)
        => WriteAsync(context, StatusCodes.Status401Unauthorized, UnauthenticatedCode, "Authentication required.");

    /// <summary>Write a 403 <see cref="ErrorEnvelope"/> (used by the JWT forbid event).</summary>
    public static Task WriteForbiddenAsync(HttpContext context)
        => WriteAsync(context, StatusCodes.Status403Forbidden, ForbiddenCode, "You do not have access to this resource.");

    private static Task WriteAsync(HttpContext context, int statusCode, string code, string message)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Response may already be started/handled by the framework challenge — guard against double write.
        if (context.Response.HasStarted)
        {
            return Task.CompletedTask;
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        var envelope = new ErrorEnvelope(new ErrorResponse(code, message, ApiResults.TraceId(context)));
        return context.Response.WriteAsJsonAsync(envelope);
    }
}
