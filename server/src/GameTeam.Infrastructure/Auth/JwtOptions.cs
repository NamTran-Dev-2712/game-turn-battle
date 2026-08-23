namespace GameTeam.Infrastructure.Auth;

/// <summary>
/// Cấu hình JWT (Phase 18) — bind từ section <c>"Jwt"</c> qua Options pattern. Giá trị KHÔNG bí mật
/// (<see cref="Issuer"/>/<see cref="Audience"/>/<see cref="AccessTokenMinutes"/>) đặt ở appsettings;
/// <see cref="SigningKey"/> LẤY TỪ SECRET/biến môi trường (<c>Jwt__SigningKey</c>) — KHÔNG hardcode,
/// KHÔNG commit, KHÔNG log. Fail-fast khi thiếu (xem <see cref="DependencyInjection.AddInfrastructure"/>).
/// </summary>
public sealed class JwtOptions
{
    /// <summary>Tên section trong configuration.</summary>
    public const string SectionName = "Jwt";

    /// <summary>Độ dài tối thiểu của signing key (byte) cho HS256 — 256-bit.</summary>
    public const int MinSigningKeyLength = 32;

    /// <summary>Bên phát hành token (claim <c>iss</c>).</summary>
    public string Issuer { get; init; } = string.Empty;

    /// <summary>Đối tượng nhận token (claim <c>aud</c>).</summary>
    public string Audience { get; init; } = string.Empty;

    /// <summary>Khoá ký đối xứng (HS256). Lấy từ secret/env — không đặt trong nguồn commit.</summary>
    public string SigningKey { get; init; } = string.Empty;

    /// <summary>Thời hạn access token (phút).</summary>
    public int AccessTokenMinutes { get; init; }
}
