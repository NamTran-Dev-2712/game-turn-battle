using GameTeam.Domain.Common;

namespace GameTeam.Application.Features.Summon;

/// <summary>Lỗi nghiệp vụ của luồng triệu hồi (mã ổn định — ánh xạ HTTP bởi <c>ErrorHttpMapping</c>).</summary>
public static class SummonErrors
{
    /// <summary>Chưa xác thực (không có token hợp lệ) → 401.</summary>
    public static Error Unauthenticated => new("UNAUTHENTICATED", "Yêu cầu chưa xác thực.");

    /// <summary>Không tìm thấy profile của người gọi → 404.</summary>
    public static Error ProfileNotFound => new("PROFILE_NOT_FOUND", "Không tìm thấy profile của người gọi.");

    /// <summary>Số lần quay không hợp lệ (chỉ 1 hoặc 10) → 400.</summary>
    public static Error InvalidCount =>
        new("GACHA_INVALID_COUNT", "Số lần quay không hợp lệ — chỉ hỗ trợ 1 (đơn) hoặc 10 (mười).");

    /// <summary>Không tìm thấy banner theo id config → 404.</summary>
    public static Error BannerNotFound => new("GACHA_BANNER_NOT_FOUND", "Không tìm thấy banner.");

    /// <summary>Banner cấu hình sai (thiếu cost / rate rarity không có hero / thiếu dupe_fragments…) → 400.</summary>
    public static Error InvalidBanner =>
        new("GACHA_INVALID_BANNER", "Cấu hình banner không hợp lệ (rate/pool/cost/dupe_fragments).");
}
