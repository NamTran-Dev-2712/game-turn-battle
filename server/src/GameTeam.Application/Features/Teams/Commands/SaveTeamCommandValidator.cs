using FluentValidation;

namespace GameTeam.Application.Features.Teams.Commands;

/// <summary>
/// Validate <b>hình dạng</b> request <see cref="SaveTeamCommand"/> (ValidationBehavior → <c>VALIDATION_FAILED</c>,
/// 400) TRƯỚC handler: slots không null/rỗng, mỗi ô có heroId không rỗng và slotIndex không âm. Các ràng buộc
/// <b>phụ thuộc dữ liệu</b> (đúng số ô theo config, slot trong lưới, hero thuộc sở hữu, không trùng) do handler
/// kiểm (cần <c>IConfigProvider</c>/DB) — trả <c>Result</c> lỗi có mã riêng.
/// </summary>
public sealed class SaveTeamCommandValidator : AbstractValidator<SaveTeamCommand>
{
    public SaveTeamCommandValidator()
    {
        RuleFor(x => x.Slots)
            .NotNull()
            .NotEmpty();

        RuleForEach(x => x.Slots).ChildRules(slot =>
        {
            slot.RuleFor(s => s.HeroId).NotEmpty();
            slot.RuleFor(s => s.SlotIndex).GreaterThanOrEqualTo(0);
        });
    }
}
