using System.IdentityModel.Tokens.Jwt;
using System.Text;
using FluentAssertions;
using GameTeam.Application.Abstractions.Security;
using GameTeam.Domain.Accounts;
using GameTeam.Domain.Common;
using GameTeam.Infrastructure.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace GameTeam.Infrastructure.Tests.Auth;

/// <summary>
/// Phase 18 — <see cref="JwtTokenService"/> mints an HS256 JWT with the expected claims
/// (<c>sub</c>/<c>type</c>), a bounded lifetime derived from <see cref="IClock"/>, and a unique opaque
/// refresh token. No Docker/host required.
/// </summary>
public sealed class JwtTokenServiceTests
{
    private sealed class FakeClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow { get; } = now;
    }

    private static readonly JwtOptions TokenOptions = new()
    {
        Issuer = "gameteam-api",
        Audience = "gameteam-client",
        SigningKey = "unit-test-signing-key-not-a-secret-0123456789",
        AccessTokenMinutes = 30,
    };

    private static JwtTokenService CreateService(DateTimeOffset now) =>
        new(Options.Create(TokenOptions), new FakeClock(now));

    [Fact]
    public void CreateTokens_issues_signed_jwt_with_expected_claims_and_lifetime()
    {
        var now = new DateTimeOffset(2026, 8, 21, 9, 0, 0, TimeSpan.Zero);
        Guid accountId = Guid.NewGuid();

        TokenBundle bundle = CreateService(now).CreateTokens(accountId, AccountType.Guest);

        bundle.ExpiresInSeconds.Should().Be(30 * 60);
        bundle.RefreshToken.Should().NotBeNullOrWhiteSpace();

        var handler = new JwtSecurityTokenHandler();
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = TokenOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = TokenOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TokenOptions.SigningKey)),
            ValidateLifetime = false, // lifetime enforcement is proven by the API integration tests
        };

        handler.ValidateToken(bundle.AccessToken, parameters, out SecurityToken validated);

        var jwt = (JwtSecurityToken)validated;
        jwt.Subject.Should().Be(accountId.ToString());
        jwt.Claims.Should().Contain(c => c.Type == "type" && c.Value == "guest");
        (jwt.ValidTo - jwt.ValidFrom).Should().Be(TimeSpan.FromMinutes(30));
    }

    [Fact]
    public void Refresh_tokens_are_unique_per_issue()
    {
        JwtTokenService service = CreateService(DateTimeOffset.UtcNow);

        string first = service.CreateTokens(Guid.NewGuid(), AccountType.Guest).RefreshToken;
        string second = service.CreateTokens(Guid.NewGuid(), AccountType.Guest).RefreshToken;

        first.Should().NotBe(second);
    }
}
