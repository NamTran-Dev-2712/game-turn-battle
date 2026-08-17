using System.Diagnostics;
using GameTeam.Contracts.Common;
using GameTeam.Domain.Common;
using Microsoft.AspNetCore.Http;

namespace GameTeam.Api.Http;

/// <summary>
/// Thin adapters that turn a Phase-09 <see cref="Result"/>/<see cref="Result{T}"/> (returned by
/// Application/MediatR handlers) into an HTTP response, keeping endpoints one-liners while ALL error
/// shaping stays centralized here (Phase 13). Success → 200; failure → the standard Phase-05
/// <see cref="ErrorEnvelope"/> with the status from <see cref="ErrorHttpMapping"/>.
/// </summary>
public static class ApiResults
{
    /// <summary>Map a non-generic <see cref="Result"/>: success → 200 (no body), failure → error envelope.</summary>
    public static async Task<IResult> ToResponseAsync(Task<Result> resultTask, HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        Result result = await resultTask;
        return result.IsSuccess ? Results.Ok() : Problem(result.Error, context);
    }

    /// <summary>Map a <see cref="Result{T}"/>: success → 200 + value, failure → error envelope.</summary>
    public static async Task<IResult> ToResponseAsync<TValue>(Task<Result<TValue>> resultTask, HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        Result<TValue> result = await resultTask;
        return result.IsSuccess ? Results.Ok(result.Value) : Problem(result.Error, context);
    }

    /// <summary>
    /// Build the standard error response: <see cref="ErrorEnvelope"/> wrapping
    /// <c>{ code, message, traceId }</c>, status from the centralized mapping. Only stable code +
    /// displayable message + traceId are exposed — never internal/infrastructure detail (§3).
    /// </summary>
    public static IResult Problem(Error error, HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(context);

        int status = ErrorHttpMapping.ToStatusCode(error);
        var envelope = new ErrorEnvelope(new ErrorResponse(error.Code, error.Message, TraceId(context)));
        return Results.Json(envelope, statusCode: status);
    }

    /// <summary>
    /// Correlation id for log lookup: prefer the active <see cref="Activity"/> id (distributed trace),
    /// fall back to the ASP.NET request trace identifier.
    /// </summary>
    public static string TraceId(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return Activity.Current?.Id ?? context.TraceIdentifier;
    }
}
