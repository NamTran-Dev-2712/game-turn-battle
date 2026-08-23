using FluentValidation;

namespace GameTeam.Application.Features.Auth.Commands;

/// <summary>
/// Validates <see cref="CreateGuestAccountCommand"/>. <c>DeviceId</c> is optional (guest login must work
/// with no device id), so the only rule is a length bound when it is supplied — no artificial required
/// rule that would break the guest-first flow.
/// </summary>
public sealed class CreateGuestAccountCommandValidator : AbstractValidator<CreateGuestAccountCommand>
{
    /// <summary>Upper bound on an optional device identifier.</summary>
    public const int MaxDeviceIdLength = 128;

    public CreateGuestAccountCommandValidator()
    {
        RuleFor(x => x.DeviceId)
            .MaximumLength(MaxDeviceIdLength)
            .When(x => x.DeviceId is not null);
    }
}
