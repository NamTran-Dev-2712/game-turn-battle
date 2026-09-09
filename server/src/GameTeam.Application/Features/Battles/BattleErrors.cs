using GameTeam.Domain.Common;

namespace GameTeam.Application.Features.Battles;

/// <summary>Lỗi nghiệp vụ của luồng đánh trận (mã ổn định — ánh xạ HTTP bởi <c>ErrorHttpMapping</c>).</summary>
public static class BattleErrors
{
    /// <summary>Chưa xác thực (không có token hợp lệ) → 401.</summary>
    public static Error Unauthenticated => new("UNAUTHENTICATED", "Yêu cầu chưa xác thực.");

    /// <summary>Không tìm thấy profile của người gọi → 404.</summary>
    public static Error ProfileNotFound => new("PROFILE_NOT_FOUND", "Không tìm thấy profile của người gọi.");

    /// <summary>Không tìm thấy đội hình của người gọi hoặc <c>teamId</c> không khớp → 404 (chống IDOR).</summary>
    public static Error TeamNotFound =>
        new("BATTLE_TEAM_NOT_FOUND", "Không tìm thấy đội hình của người chơi hoặc teamId không khớp.");

    /// <summary>Đội hình rỗng (chưa lưu đội) — không thể đánh → 400.</summary>
    public static Error TeamEmpty => new("BATTLE_TEAM_EMPTY", "Đội hình rỗng — hãy lưu đội trước khi đánh.");
}
