using GameTeam.Domain.Common;

namespace GameTeam.Application.Features.Profile;

/// <summary>
/// Stable business errors for the profile feature. Codes are <c>SCREAMING_SNAKE_CASE</c> and map to HTTP
/// status via the Phase-13 <c>ErrorHttpMapping</c> convention (<c>UNAUTHENTICATED</c> → 401,
/// <c>_NOT_FOUND</c> suffix → 404). No stack/DB detail leaks through these.
/// </summary>
public static class ProfileErrors
{
    /// <summary>No authenticated identity on the request (defensive — the endpoint is already protected).</summary>
    public static readonly Error Unauthenticated =
        new("UNAUTHENTICATED", "Yêu cầu chưa xác thực.");

    /// <summary>The authenticated account has no profile (pure-read query only; get-or-create never returns this).</summary>
    public static readonly Error NotFound =
        new("PROFILE_NOT_FOUND", "Không tìm thấy hồ sơ người chơi.");
}
