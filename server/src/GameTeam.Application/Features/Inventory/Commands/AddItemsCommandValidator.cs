using FluentValidation;

namespace GameTeam.Application.Features.Inventory.Commands;

/// <summary>
/// Validate <b>hình dạng</b> <see cref="AddItemsCommand"/> (ValidationBehavior → <c>VALIDATION_FAILED</c>, 400)
/// TRƯỚC handler: có ít nhất một item; mỗi item có loại hợp lệ (<c>item</c>/<c>fragment</c>), id không rỗng,
/// số lượng &gt; 0; source/idempotencyKey không rỗng. Item id <b>tồn tại</b> trong config do service kiểm
/// (data-driven — trả <c>Result</c> lỗi <c>INVENTORY_UNKNOWN_ITEM</c>).
/// </summary>
public sealed class AddItemsCommandValidator : AbstractValidator<AddItemsCommand>
{
    public AddItemsCommandValidator()
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
