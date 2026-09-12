using FluentValidation;

namespace GameTeam.Application.Features.Inventory.Commands;

/// <summary>
/// Validate <b>hình dạng</b> <see cref="RemoveItemsCommand"/> (ValidationBehavior → <c>VALIDATION_FAILED</c>,
/// 400) TRƯỚC handler: có ít nhất một item; mỗi item có loại hợp lệ (<c>item</c>/<c>fragment</c>), id không
/// rỗng, số lượng &gt; 0; source/idempotencyKey không rỗng. Đủ số lượng do service kiểm (trả <c>Result</c> lỗi
/// <c>INVENTORY_INSUFFICIENT_CONFLICT</c>, atomic — không bớt một phần).
/// </summary>
public sealed class RemoveItemsCommandValidator : AbstractValidator<RemoveItemsCommand>
{
    public RemoveItemsCommandValidator()
    {
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ItemType).Must(InventoryItemTypes.IsValid)
                .WithMessage("ItemType phải là 'item' hoặc 'fragment'.");
            item.RuleFor(i => i.ItemId).NotEmpty();
            item.RuleFor(i => i.Quantity).GreaterThan(0);
        });
        RuleFor(x => x.Source).NotEmpty();
        RuleFor(x => x.IdempotencyKey).NotEmpty();
    }
}
