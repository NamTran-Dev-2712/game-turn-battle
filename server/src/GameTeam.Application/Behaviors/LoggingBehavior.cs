using System.Diagnostics;
using GameTeam.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GameTeam.Application.Behaviors;

/// <summary>
/// Outermost pipeline behavior: structured request/response logging with elapsed time.
/// <para>
/// Logs ONLY the request type name, elapsed milliseconds, and outcome — it never serializes the
/// request/response body, so tokens/PII in a command are not dumped (docs/backend/cross-cutting.md §2).
/// Applies to every request (commands and queries).
/// </para>
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        => _logger = logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        string requestName = typeof(TRequest).Name;
        _logger.LogDebug("Handling {RequestName}", requestName);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            TResponse response = await next();
            stopwatch.Stop();

            _logger.LogInformation(
                "Handled {RequestName} in {ElapsedMilliseconds}ms with outcome {Outcome}",
                requestName,
                stopwatch.ElapsedMilliseconds,
                DescribeOutcome(response));

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "Handling {RequestName} threw after {ElapsedMilliseconds}ms",
                requestName,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    /// <summary>
    /// Outcome label from a <see cref="Result"/> (Success / Failure(CODE) — codes are safe, Phase 09);
    /// falls back to "Completed" for non-<see cref="Result"/> responses. No response payload logged.
    /// </summary>
    private static string DescribeOutcome(TResponse response) => response switch
    {
        Result { IsSuccess: true } => "Success",
        Result { Error: { } error } => $"Failure({error.Code})",
        _ => "Completed",
    };
}
