using GameTeam.Domain.Common;

namespace GameTeam.Application.Features.Teams;

/// <summary>
/// Lỗi nghiệp vụ ổn định của feature team/formation. Code <c>SCREAMING_SNAKE_CASE</c>, map HTTP qua
/// <c>ErrorHttpMapping</c> (phase 13): <c>UNAUTHENTICATED</c> → 401; các mã dưới (không hậu tố đặc biệt)
/// → 400 (bad request — intent client sai). Không lộ stack/DB.
/// </summary>
public static class TeamErrors
{
    /// <summary>Yêu cầu chưa xác thực (phòng thủ — endpoint team đã protected mặc định).</summary>
    public static readonly Error Unauthenticated =
        new("UNAUTHENTICATED", "Yêu cầu chưa xác thực.");

    /// <summary>Chưa có profile cho account (chưa khởi tạo save) ⇒ chưa thể lưu đội.</summary>
    public static readonly Error ProfileNotFound =
        new("PROFILE_NOT_FOUND", "Chưa có profile cho tài khoản này.");

    /// <summary>Không đọc được cấu hình lưới formation (config chưa publish / thiếu).</summary>
    public static readonly Error FormationConfigMissing =
        new("FORMATION_CONFIG_MISSING", "Không tìm thấy cấu hình lưới đội hình trong config.");

    /// <summary>Số hero không đúng số ô yêu cầu (đội phải đủ đúng <c>rows*cols</c> hero).</summary>
    public static readonly Error InvalidSize =
        new("TEAM_INVALID_SIZE", "Đội hình phải có đúng số hero theo lưới cấu hình.");

    /// <summary>Ô không hợp lệ: ngoài lưới, âm, hoặc trùng vị trí.</summary>
    public static readonly Error InvalidSlot =
        new("TEAM_INVALID_SLOT", "Vị trí ô không hợp lệ (ngoài lưới hoặc trùng).");

    /// <summary>Có hero bị lặp trong đội (một hero chỉ được xếp một ô).</summary>
    public static readonly Error DuplicateHero =
        new("TEAM_DUPLICATE_HERO", "Không được xếp trùng hero trong đội.");

    /// <summary>Có hero không thuộc quyền sở hữu của người chơi (không tin client).</summary>
    public static readonly Error HeroNotOwned =
        new("TEAM_HERO_NOT_OWNED", "Có hero không thuộc quyền sở hữu của người chơi.");
}
