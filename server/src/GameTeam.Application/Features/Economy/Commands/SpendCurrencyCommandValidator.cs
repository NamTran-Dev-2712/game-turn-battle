using FluentValidation;

namespace GameTeam.Application.Features.Economy.Commands;

/// <summary>
/// Validate <b>hình dạng</b> <see cref="SpendCurrencyCommand"/> (ValidationBehavior → <c>VALIDATION_FAILED</c>,
/// 400) TRƯỚC handler: amount &gt; 0, source/idempotencyKey không rỗng. Đủ số dư (phụ thuộc dữ liệu) do
/// service kiểm (trả <c>Result</c> <c>CURRENCY_INSUFFICIENT_FUNDS</c>).
/// </summary>
public sealed class SpendCurrencyCommandValidator : AbstractValidator<SpendCurrencyCommand>
{
    public SpendCurrencyCommandValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Source).NotEmpty();
        RuleFor(x => x.IdempotencyKey).NotEmpty();
    }
}
