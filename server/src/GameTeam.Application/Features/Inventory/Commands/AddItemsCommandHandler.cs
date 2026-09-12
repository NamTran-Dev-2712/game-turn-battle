using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Application.Abstractions.Security;
using GameTeam.Domain.Common;
using MediatR;
using DomainProfile = GameTeam.Domain.Profiles.PlayerProfile;

namespace GameTeam.Application.Features.Inventory.Commands;

/// <summary>
/// Handles <see cref="AddItemsCommand"/> — handler <b>mỏng</b>: suy chủ sở hữu từ token
/// (<see cref="ICurrentUser"/>) → profile, rồi uỷ cho cơ chế dùng chung <see cref="InventoryService"/>
/// (idempotency + kiểm config + khoá dòng + thêm atomic + ledger). Bao trong transaction bởi <c>TransactionBehavior</c>.
/// </summary>
public sealed class AddItemsCommandHandler
    : IRequestHandler<AddItemsCommand, Result<InventoryTransactionResult>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IPlayerProfileRepository _profiles;
    private readonly InventoryService _inventory;

    public AddItemsCommandHandler(
        ICurrentUser currentUser,
        IPlayerProfileRepository profiles,
        InventoryService inventory)
    {
        _currentUser = currentUser;
        _profiles = profiles;
        _inventory = inventory;
    }

    public async Task<Result<InventoryTransactionResult>> Handle(
        AddItemsCommand request,
        CancellationToken cancellationToken)
    {
        Guid? accountId = _currentUser.AccountId;
        if (accountId is null)
        {
            return InventoryErrors.Unauthenticated;
        }

        DomainProfile? profile = await _profiles.GetByAccountIdAsync(accountId.Value, cancellationToken);
        if (profile is null)
        {
            return InventoryErrors.ProfileNotFound;
        }

        return await _inventory.GrantAsync(
            profile.Id, request.Items, request.Source, request.IdempotencyKey, cancellationToken);
    }
}
