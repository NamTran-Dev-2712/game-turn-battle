namespace GameTeam.Contracts.Auth;

/// <summary>
/// Kết quả đăng nhập khách: cặp token JWT + thời hạn (ADR-008).
/// </summary>
/// <param name="AccessToken">JWT bearer để gọi API.</param>
/// <param name="RefreshToken">Token làm mới access token.</param>
/// <param name="ExpiresInSeconds">Thời hạn access token (giây) kể từ khi cấp.</param>
public sealed record AuthGuestResponse(string AccessToken, string RefreshToken, int ExpiresInSeconds);
