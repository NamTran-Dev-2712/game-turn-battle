using System.Reflection;
using FluentValidation;
using FluentValidation.Results;
using GameTeam.Application.Common;
using GameTeam.Domain.Common;
using MediatR;

namespace GameTeam.Application.Behaviors;

/// <summary>
/// Runs FluentValidation validators BEFORE the handler. On failure it short-circuits the pipeline
/// and returns a failed <see cref="Result"/> (code <see cref="ValidationErrors.Code"/>) — the handler
/// is NOT invoked and NO validation exception leaks to the API (docs/backend/cross-cutting.md).
/// <para>
/// Constrained to <c>TResponse : Result</c> so a typed failure can always be produced. Requests with
/// no registered validator pass straight through.
/// </para>
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    private static readonly MethodInfo FailureOfTDefinition = typeof(Result)
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Single(m => m is { Name: nameof(Result.Failure), IsGenericMethodDefinition: true });

    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        => _validators = validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);
        ValidationFailure[] failures = (await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToArray();

        if (failures.Length == 0)
        {
            return await next();
        }

        Error error = ValidationErrors.ToError(failures);
        return CreateFailure(error);
    }

    /// <summary>
    /// Build a failed <typeparamref name="TResponse"/> — <see cref="Result.Failure(Error)"/> for a
    /// plain <see cref="Result"/>, or the closed <c>Result.Failure&lt;T&gt;</c> for a <see cref="Result{T}"/>.
    /// </summary>
    private static TResponse CreateFailure(Error error)
    {
        if (typeof(TResponse) == typeof(Result))
        {
            return (TResponse)Result.Failure(error);
        }

        Type valueType = typeof(TResponse).GetGenericArguments()[0];
        object failure = FailureOfTDefinition
            .MakeGenericMethod(valueType)
            .Invoke(null, [error])!;

        return (TResponse)failure;
    }
}
