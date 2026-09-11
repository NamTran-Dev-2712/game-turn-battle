using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Application.Abstractions.Security;
using GameTeam.Domain.Common;
using MediatR;
using DomainProfile = GameTeam.Domain.Profiles.PlayerProfile;

namespace GameTeam.Application.Features.Economy.Commands;

/// <summary>
/// Handles <see cref="SpendCurrencyCommand"/> — handler <b>mỏng</b>: suy chủ sở hữu từ token
/// (<see cref="ICurrentUser"/>) → profile, rồi uỷ cho cơ chế dùng chung <see cref="CurrencyWalletService"/>
/// (idempotency + khoá dòng + kiểm đủ tiền + spend + ledger). Bao trong transaction bởi <c>TransactionBehavior</c>.
/// </summary>
public sealed class SpendCurrencyCommandHandler
    : IRequestHandler<SpendCurrencyCommand, Result<CurrencyTransactionResult>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IPlayerProfileRepository _profiles;
    private readonly CurrencyWalletService _wallet;

    public SpendCurrencyCommandHandler(
        ICurrentUser currentUser,
        IPlayerProfileRepository profiles,
        CurrencyWalletService wallet)
    {
        _currentUser = currentUser;
        _profiles = profiles;
        _wallet = wallet;
    }

    public async Task<Result<CurrencyTransactionResult>> Handle(
        SpendCurrencyCommand request,
        CancellationToken cancellationToken)
    {
        Guid? accountId = _currentUser.AccountId;
        if (accountId is null)
        {
            return CurrencyErrors.Unauthenticated;
        }

        DomainProfile? profile = await _profiles.GetByAccountIdAsync(accountId.Value, cancellationToken);
        if (profile is null)
        {
            return CurrencyErrors.ProfileNotFound;
        }

        return await _wallet.SpendAsync(
            profile.Id, request.Currency, request.Amount, request.Source, request.IdempotencyKey, cancellationToken);
    }
}
