using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Application.Abstractions.Security;
using GameTeam.Contracts.Inventory;
using GameTeam.Domain.Common;
using GameTeam.Domain.Heroes;
using MediatR;
using DomainInventory = GameTeam.Domain.Inventory.Inventory;
using DomainProfile = GameTeam.Domain.Profiles.PlayerProfile;

namespace GameTeam.Application.Features.Inventory.Queries;

/// <summary>
/// Handles <see cref="GetInventoryQuery"/>: suy chủ sở hữu từ <see cref="ICurrentUser"/> → profile → kho + hero
/// sở hữu. Profile/kho chưa tồn tại ⇒ trả kho rỗng (không lỗi). Lọc/sắp xếp/phân trang stack + chiếu hero ở
/// <see cref="InventoryMapping"/>. Read-only.
/// </summary>
public sealed class GetInventoryQueryHandler : IRequestHandler<GetInventoryQuery, Result<InventoryDto>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IPlayerProfileRepository _profiles;
    private readonly IInventoryRepository _inventories;
    private readonly IOwnedHeroRepository _ownedHeroes;

    public GetInventoryQueryHandler(
        ICurrentUser currentUser,
        IPlayerProfileRepository profiles,
        IInventoryRepository inventories,
        IOwnedHeroRepository ownedHeroes)
    {
        _currentUser = currentUser;
        _profiles = profiles;
        _inventories = inventories;
        _ownedHeroes = ownedHeroes;
    }

    public async Task<Result<InventoryDto>> Handle(GetInventoryQuery request, CancellationToken cancellationToken)
    {
        Guid? accountId = _currentUser.AccountId;
        if (accountId is null)
        {
            return InventoryErrors.Unauthenticated;
        }

        DomainProfile? profile = await _profiles.GetByAccountIdAsync(accountId.Value, cancellationToken);
        if (profile is null)
        {
            return Result.Success(InventoryMapping.Empty);
        }

        DomainInventory? inventory = await _inventories.GetByProfileIdAsync(profile.Id, cancellationToken);
        IReadOnlyList<OwnedHero> heroes = await _ownedHeroes.GetByProfileIdAsync(profile.Id, cancellationToken);

        return Result.Success(
            InventoryMapping.ToDto(inventory, heroes, request.ItemType, request.Page, request.PageSize));
    }
}
