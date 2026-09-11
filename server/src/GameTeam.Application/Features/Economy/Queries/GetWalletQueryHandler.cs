using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Application.Abstractions.Security;
using GameTeam.Contracts.Economy;
using GameTeam.Domain.Common;
using GameTeam.Domain.Economy;
using MediatR;
using DomainProfile = GameTeam.Domain.Profiles.PlayerProfile;

namespace GameTeam.Application.Features.Economy.Queries;

/// <summary>
/// Handles <see cref="GetWalletQuery"/>: suy chủ sở hữu từ <see cref="ICurrentUser"/> → profile → ví. Ví
/// chưa tồn tại (hoặc profile chưa có) ⇒ trả <see cref="WalletDto"/> rỗng (số dư 0), KHÔNG phải lỗi. Read-only.
/// </summary>
public sealed class GetWalletQueryHandler : IRequestHandler<GetWalletQuery, Result<WalletDto>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IPlayerProfileRepository _profiles;
    private readonly IWalletRepository _wallets;

    public GetWalletQueryHandler(
        ICurrentUser currentUser,
        IPlayerProfileRepository profiles,
        IWalletRepository wallets)
    {
        _currentUser = currentUser;
        _profiles = profiles;
        _wallets = wallets;
    }

    public async Task<Result<WalletDto>> Handle(GetWalletQuery request, CancellationToken cancellationToken)
    {
        Guid? accountId = _currentUser.AccountId;
        if (accountId is null)
        {
            return CurrencyErrors.Unauthenticated;
        }

        DomainProfile? profile = await _profiles.GetByAccountIdAsync(accountId.Value, cancellationToken);
        if (profile is null)
        {
            return Result.Success(WalletMapping.Empty);
        }

        Wallet? wallet = await _wallets.GetByProfileIdAsync(profile.Id, cancellationToken);
        return Result.Success(wallet is null ? WalletMapping.Empty : WalletMapping.ToDto(wallet));
    }
}
