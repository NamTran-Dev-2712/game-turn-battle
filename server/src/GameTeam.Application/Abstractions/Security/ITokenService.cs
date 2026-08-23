using GameTeam.Domain.Accounts;

namespace GameTeam.Application.Abstractions.Security;

/// <summary>
/// Port (DIP) for issuing authentication tokens. Declared in Application so handlers depend only on
/// the abstraction; the concrete JWT implementation (signing, claims, key handling) lives in
/// Infrastructure — Application never references a JWT framework (ADR-008).
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Issue an access token (+ a refresh token) for an account. The access token carries the account
    /// identity as <c>sub</c> and the account <paramref name="type"/>, with a bounded lifetime.
    /// </summary>
    /// <param name="accountId">Identity of the authenticated account (becomes the <c>sub</c> claim).</param>
    /// <param name="type">Account type (MVP: <see cref="AccountType.Guest"/>) — surfaced as a claim.</param>
    TokenBundle CreateTokens(Guid accountId, AccountType type);
}

/// <summary>
/// Issued token pair. <paramref name="RefreshToken"/> is an opaque foundation value in the MVP —
/// rotation/validation/persistence of refresh tokens is a later phase, not implemented here.
/// </summary>
/// <param name="AccessToken">Signed JWT bearer token.</param>
/// <param name="RefreshToken">Opaque refresh token (foundation only).</param>
/// <param name="ExpiresInSeconds">Access-token lifetime in seconds from issuance.</param>
public sealed record TokenBundle(string AccessToken, string RefreshToken, int ExpiresInSeconds);
