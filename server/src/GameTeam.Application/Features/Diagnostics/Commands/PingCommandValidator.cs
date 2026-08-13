using FluentValidation;

namespace GameTeam.Application.Features.Diagnostics.Commands;

/// <summary>
/// Validates <see cref="PingCommand"/> — proves <c>ValidationBehavior</c> stops the handler on bad
/// input. Rules are meaningful (message required, bounded) — no artificial rule for coverage.
/// </summary>
public sealed class PingCommandValidator : AbstractValidator<PingCommand>
{
    /// <summary>Upper bound on the echo payload length.</summary>
    public const int MaxMessageLength = 256;

    public PingCommandValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty()
            .MaximumLength(MaxMessageLength);
    }
}
