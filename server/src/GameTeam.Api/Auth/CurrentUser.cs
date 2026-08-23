using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GameTeam.Application.Abstractions.Security;

namespace GameTeam.Api.Auth;

/// <summary>
/// Presentation-layer adapter for <see cref="ICurrentUser"/>: reads the authenticated identity from the
/// current request's <see cref="ClaimsPrincipal"/> (the JWT <c>sub</c>). Kept in the Api layer so ASP.NET
/// <see cref="HttpContext"/> concerns never leak into Application/Infrastructure.
/// <para>
/// The default JwtBearer inbound-claim mapping rewrites <c>sub</c> to
/// <see cref="ClaimTypes.NameIdentifier"/>, so both claim names are checked (robust to either mapping).
/// </para>
/// </summary>
public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    /// <inheritdoc />
    public Guid? AccountId
    {
        get
        {
            ClaimsPrincipal? user = _httpContextAccessor.HttpContext?.User;
            if (user is null)
            {
                return null;
            }

            string? subject =
                user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);

            return Guid.TryParse(subject, out Guid accountId) ? accountId : null;
        }
    }

    /// <inheritdoc />
    public bool IsAuthenticated
        => _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
}
