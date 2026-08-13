using FluentValidation.Results;
using GameTeam.Domain.Common;

namespace GameTeam.Application.Common;

/// <summary>
/// Converts FluentValidation failures into the project's standard <see cref="Error"/> (Phase 09) —
/// so a validation failure surfaces as a normal failed <see cref="Result"/>, never a thrown
/// exception leaking to the API. No second error/validation paradigm is introduced.
/// </summary>
public static class ValidationErrors
{
    /// <summary>Stable code for any validation failure (mapped to HTTP 400 in Phase 13).</summary>
    public const string Code = "VALIDATION_FAILED";

    /// <summary>
    /// Aggregate <paramref name="failures"/> into a single <see cref="Error"/>. The message lists
    /// each <c>{PropertyName}: {ErrorMessage}</c>; property names + validator messages only —
    /// never the offending input value (no sensitive-data leak).
    /// </summary>
    public static Error ToError(IEnumerable<ValidationFailure> failures)
    {
        string message = string.Join(
            "; ",
            failures
                .Where(f => f is not null)
                .Select(f => $"{f.PropertyName}: {f.ErrorMessage}"));

        return new Error(Code, message);
    }
}
