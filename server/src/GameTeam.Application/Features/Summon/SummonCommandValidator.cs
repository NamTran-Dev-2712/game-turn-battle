using FluentValidation;

namespace GameTeam.Application.Features.Summon;

/// <summary>
/// Validate <b>hình dạng</b> <see cref="SummonCommand"/> (ValidationBehavior → <c>VALIDATION_FAILED</c>, 400)
/// TRƯỚC handler: bannerId/requestId không rỗng. Số lần quay hợp lệ (1/10) + banner tồn tại/hợp lệ + đủ tiền
/// (phụ thuộc dữ liệu) do handler kiểm (mã lỗi nghiệp vụ riêng).
/// </summary>
public sealed class SummonCommandValidator : AbstractValidator<SummonCommand>
{
    public SummonCommandValidator()
    {
        RuleFor(x => x.BannerId).NotEmpty();
        RuleFor(x => x.RequestId).NotEmpty();
    }
}
