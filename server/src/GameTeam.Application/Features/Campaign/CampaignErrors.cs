using GameTeam.Domain.Common;

namespace GameTeam.Application.Features.Campaign;

/// <summary>Lỗi nghiệp vụ của luồng campaign (mã ổn định — ánh xạ HTTP bởi <c>ErrorHttpMapping</c>).</summary>
public static class CampaignErrors
{
    /// <summary>Chưa xác thực (không có token hợp lệ) → 401.</summary>
    public static Error Unauthenticated => new("UNAUTHENTICATED", "Yêu cầu chưa xác thực.");

    /// <summary>Không tìm thấy profile của người gọi → 404.</summary>
    public static Error ProfileNotFound => new("PROFILE_NOT_FOUND", "Không tìm thấy profile của người gọi.");

    /// <summary>Stage không thuộc chuỗi campaign hoặc thiếu config → 404.</summary>
    public static Error StageNotFound(string stageId) =>
        new("CAMPAIGN_STAGE_NOT_FOUND", $"Stage campaign '{stageId}' không tồn tại.");

    /// <summary>Stage chưa mở khoá (phải clear stage trước — chống skip) → 403.</summary>
    public static Error StageLocked(string stageId) =>
        new("CAMPAIGN_STAGE_LOCKED", $"Stage '{stageId}' chưa mở khoá — phải clear stage trước (tuần tự).");
}
