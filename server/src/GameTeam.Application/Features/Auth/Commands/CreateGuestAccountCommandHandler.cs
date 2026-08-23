using GameTeam.Application.Abstractions.Persistence;
using GameTeam.Application.Abstractions.Security;
using GameTeam.Contracts.Auth;
using GameTeam.Domain.Accounts;
using GameTeam.Domain.Common;
using GameTeam.Domain.Profiles;
using MediatR;

namespace GameTeam.Application.Features.Auth.Commands;

/// <summary>
/// Handles <see cref="CreateGuestAccountCommand"/>. Thin: creates the guest aggregate and its
/// server-authoritative <see cref="PlayerProfile"/> (ADR-007) in the SAME transaction, stages both for
/// persistence (committed by the transaction behavior), and issues tokens via <see cref="ITokenService"/>.
/// The account id is generated here so the JWT <c>sub</c> is known without a database round-trip; the
/// server clock (<see cref="IClock"/>) stamps creation time — never wall-clock.
/// <para>
/// Eager, atomic profile creation satisfies "guest login → profile created" and guarantees exactly one
/// profile per account (the unique <c>account_id</c> index is the DB-level idempotency backstop).
/// </para>
/// </summary>
public sealed class CreateGuestAccountCommandHandler
    : IRequestHandler<CreateGuestAccountCommand, Result<AuthGuestResponse>>
{
    private readonly IRepository<Account, Guid> _accounts;
    private readonly IPlayerProfileRepository _profiles;
    private readonly ITokenService _tokenService;
    private readonly IClock _clock;

    public CreateGuestAccountCommandHandler(
        IRepository<Account, Guid> accounts,
        IPlayerProfileRepository profiles,
        ITokenService tokenService,
        IClock clock)
    {
        _accounts = accounts;
        _profiles = profiles;
        _tokenService = tokenService;
        _clock = clock;
    }

    public async Task<Result<AuthGuestResponse>> Handle(
        CreateGuestAccountCommand request,
        CancellationToken cancellationToken)
    {
        DateTimeOffset now = _clock.UtcNow;

        Account account = Account.CreateGuest(Guid.NewGuid(), now);
        await _accounts.AddAsync(account, cancellationToken);

        PlayerProfile profile = PlayerProfile.CreateForAccount(Guid.NewGuid(), account.Id, now);
        await _profiles.AddAsync(profile, cancellationToken);

        TokenBundle tokens = _tokenService.CreateTokens(account.Id, account.Type);

        return Result.Success(new AuthGuestResponse(
            tokens.AccessToken,
            tokens.RefreshToken,
            tokens.ExpiresInSeconds));
    }
}
