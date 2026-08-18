using GameTeam.Contracts.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GameTeam.Api.Http;

/// <summary>
/// Last-resort handler for UNHANDLED exceptions (Phase 13). Turns any escaped exception into the
/// project's single error contract — a 500 <see cref="ErrorEnvelope"/> carrying only a generic code,
/// a safe message and the traceId. The full exception is logged server-side (with the same traceId
/// for correlation) but NEVER sent to the client: no stack trace, exception message, DB/query, path,
/// connection string or secret leaks (§3, production safety).
/// </summary>
/// <remarks>
/// Registered via <c>services.AddExceptionHandler&lt;GlobalExceptionHandler&gt;()</c> +
/// <c>app.UseExceptionHandler()</c>. It emits <see cref="ErrorEnvelope"/> (not RFC-7807 ProblemDetails)
/// so 500s share the exact same wire shape as validation/business failures — one error contract.
/// </remarks>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    /// <summary>Generic, non-revealing code for any unhandled server fault.</summary>
    public const string InternalErrorCode = "INTERNAL_ERROR";

    private const string SafeMessage = "An unexpected error occurred. Please retry later.";

    private readonly ILogger<GlobalExceptionHandler> _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        string traceId = ApiResults.TraceId(httpContext);

        // Full detail stays in the server log only, correlated by traceId.
        _logger.LogError(
            exception,
            "Unhandled exception on {Method} {Path} (traceId={TraceId}).",
            httpContext.Request.Method,
            httpContext.Request.Path,
            traceId);

        var envelope = new ErrorEnvelope(new ErrorResponse(InternalErrorCode, SafeMessage, traceId));

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(envelope, cancellationToken);

        return true;
    }
}
