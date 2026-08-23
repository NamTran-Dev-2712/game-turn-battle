namespace GameTeam.Application.Abstractions.Security;

/// <summary>
/// Port (DIP) exposing the authenticated caller's identity to handlers. Declared in Application so
/// handlers depend only on the abstraction; the concrete adapter (reading the JWT <c>sub</c> claim from
/// the current request) lives in the Api/presentation layer.
/// <para>
/// <b>Ownership boundary (ADR-007/008):</b> the profile owner is resolved ONLY from here (the token
/// <c>sub</c>) — never from a request body/route/query. A handler that trusts a client-supplied account
/// id would open an IDOR hole; always use <see cref="AccountId"/>.
/// </para>
/// </summary>
public interface ICurrentUser
{
    /// <summary>Authenticated account id (the JWT <c>sub</c>), or <c>null</c> when unauthenticated.</summary>
    Guid? AccountId { get; }

    /// <summary><c>true</c> when the request carries a valid authenticated identity.</summary>
    bool IsAuthenticated { get; }
}
