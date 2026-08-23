using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using GameTeam.Application.Abstractions.Security;
using GameTeam.Domain.Accounts;
using GameTeam.Domain.Common;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GameTeam.Infrastructure.Auth;

/// <summary>
/// Hiện thực <see cref="ITokenService"/> bằng JWT ký HS256 (khoá đối xứng từ <see cref="JwtOptions.SigningKey"/>).
/// Claims: <c>sub</c> = account id, <c>type</c> = loại tài khoản, cùng <c>jti</c>/<c>iat</c>/<c>exp</c>/<c>iss</c>/<c>aud</c>.
/// Thời gian lấy từ <see cref="IClock"/> (server-time boundary). Refresh token là chuỗi ngẫu nhiên đục
/// (nền tảng — vòng đời/rotation refresh là phase sau). KHÔNG log khoá/token.
/// </summary>
public sealed class JwtTokenService : ITokenService
{
    /// <summary>Giá trị claim <c>type</c> cho tài khoản khách.</summary>
    public const string GuestTypeClaimValue = "guest";

    /// <summary>Tên claim tuỳ biến mang loại tài khoản.</summary>
    public const string TypeClaimName = "type";

    private static readonly JwtSecurityTokenHandler TokenHandler = new();

    private readonly JwtOptions _options;
    private readonly IClock _clock;

    public JwtTokenService(IOptions<JwtOptions> options, IClock clock)
    {
        _options = Guard.NotNull(options).Value;
        _clock = Guard.NotNull(clock);
    }

    public TokenBundle CreateTokens(Guid accountId, AccountType type)
    {
        DateTimeOffset issuedAt = _clock.UtcNow;
        DateTimeOffset expiresAt = issuedAt.AddMinutes(_options.AccessTokenMinutes);

        SymmetricSecurityKey signingKey = new(Encoding.UTF8.GetBytes(_options.SigningKey));
        SigningCredentials credentials = new(signingKey, SecurityAlgorithms.HmacSha256);

        Claim[] claims =
        [
            new Claim(JwtRegisteredClaimNames.Sub, accountId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(
                JwtRegisteredClaimNames.Iat,
                issuedAt.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture),
                ClaimValueTypes.Integer64),
            new Claim(TypeClaimName, ToClaimValue(type)),
        ];

        JwtSecurityToken token = new(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: issuedAt.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        string accessToken = TokenHandler.WriteToken(token);
        string refreshToken = GenerateOpaqueRefreshToken();
        int expiresInSeconds = (int)(expiresAt - issuedAt).TotalSeconds;

        return new TokenBundle(accessToken, refreshToken, expiresInSeconds);
    }

    /// <summary>Chuỗi 256-bit ngẫu nhiên (base64url) làm refresh token nền tảng — không đoán được.</summary>
    private static string GenerateOpaqueRefreshToken()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(32);
        return Base64UrlEncoder.Encode(bytes);
    }

    private static string ToClaimValue(AccountType type) => type switch
    {
        AccountType.Guest => GuestTypeClaimValue,
        _ => type.ToString().ToLowerInvariant(),
    };
}
