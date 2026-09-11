using FluentValidation;

namespace GameTeam.Application.Features.Economy.Commands;

/// <summary>
/// Validate <b>hình dạng</b> <see cref="GrantCurrencyCommand"/> (ValidationBehavior → <c>VALIDATION_FAILED</c>,
/// 400) TRƯỚC handler: amount &gt; 0, source/idempotencyKey không rỗng. Loại tiền hợp lệ (khác <c>None</c>)
/// do handler/service kiểm (trả <c>Result</c> lỗi có mã riêng).
/// </summary>
public sealed class GrantCurrencyCommandValidator : AbstractValidator<GrantCurrencyCommand>
{
    public GrantCurrencyCommandValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Source).NotEmpty();
        RuleFor(x => x.IdempotencyKey).NotEmpty();
    }
}
