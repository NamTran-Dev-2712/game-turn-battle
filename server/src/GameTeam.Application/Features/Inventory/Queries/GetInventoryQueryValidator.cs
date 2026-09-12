using FluentValidation;

namespace GameTeam.Application.Features.Inventory.Queries;

/// <summary>
/// Validate <b>hình dạng</b> <see cref="GetInventoryQuery"/> (ValidationBehavior → <c>VALIDATION_FAILED</c>,
/// 400): page ≥ 1, pageSize trong [1, <see cref="MaxPageSize"/>], itemType null hoặc hợp lệ
/// (<c>item</c>/<c>fragment</c>). Chặn tải trang quá lớn (rủi ro "kho lớn tải chậm").
/// </summary>
public sealed class GetInventoryQueryValidator : AbstractValidator<GetInventoryQuery>
{
    /// <summary>Trần kích thước trang (chống tải toàn bộ kho lớn một lần).</summary>
    public const int MaxPageSize = 200;

    public GetInventoryQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, MaxPageSize);
        RuleFor(x => x.ItemType)
            .Must(t => t is null || InventoryItemTypes.IsValid(t))
            .WithMessage("ItemType phải null hoặc 'item'/'fragment'.");
    }
}
