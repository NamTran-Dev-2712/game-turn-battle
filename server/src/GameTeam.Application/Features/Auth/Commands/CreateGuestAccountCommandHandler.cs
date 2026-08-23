using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Application.Abstractions.Security;
using GameTeam.Contracts.Auth;
using GameTeam.Domain.Accounts;
using GameTeam.Domain.Common;
using MediatR;

namespace GameTeam.Application.Features.Auth.Commands;

/// <summary>
/// Handles <see cref="CreateGuestAccountCommand"/>. Thin: creates the guest aggregate, stages it for
/// persistence (committed by the transaction behavior), and issues tokens via <see cref="ITokenService"/>.
/// The account id is generated here so the JWT <c>sub</c> is known without a database round-trip; the
/// server clock (<see cref="IClock"/>) stamps creation time — never wall-clock.
/// </summary>
public sealed class CreateGuestAccountCommandHandler
    : IRequestHandler<CreateGuestAccountCommand, Result<AuthGuestResponse>>
{
    private readonly IRepository<Account, Guid> _accounts;
    private readonly ITokenService _tokenService;
    private readonly IClock _clock;

    public CreateGuestAccountCommandHandler(
        IRepository<Account, Guid> accounts,
        ITokenService tokenService,
        IClock clock)
    {
        _accounts = accounts;
        _tokenService = tokenService;
        _clock = clock;
    }

    public async Task<Result<AuthGuestResponse>> Handle(
        CreateGuestAccountCommand request,
        CancellationToken cancellationToken)
    {
        Account account = Account.CreateGuest(Guid.NewGuid(), _clock.UtcNow);
        await _accounts.AddAsync(account, cancellationToken);

        TokenBundle tokens = _tokenService.CreateTokens(account.Id, account.Type);

        return Result.Success(new AuthGuestResponse(
            tokens.AccessToken,
            tokens.RefreshToken,
            tokens.ExpiresInSeconds));
    }
}
