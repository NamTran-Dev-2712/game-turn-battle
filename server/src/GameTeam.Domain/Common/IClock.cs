namespace GameTeam.Domain.Common;

/// <summary>
/// Cổng (port) thời gian của Domain — <b>ranh giới server-time</b>.
/// <para>
/// MỌI thời gian nghiệp vụ (AFK, energy, cooldown, xác định thời điểm) PHẢI lấy qua <see cref="UtcNow"/>,
/// TUYỆT ĐỐI không dùng <c>DateTime.Now/UtcNow</c> hay <c>DateTimeOffset.Now/UtcNow</c> trực tiếp trong Domain
/// (Forbidden Pattern — docs/ai/coding-rules.md §3; cần cho test tái lập &amp; chống gian lận giờ).
/// </para>
/// Infrastructure hiện thực bằng server time và inject (phase sau). Domain chỉ khai báo port.
/// </summary>
public interface IClock
{
    /// <summary>Thời điểm hiện tại theo giờ server (UTC, không nhập nhằng offset).</summary>
    DateTimeOffset UtcNow { get; }
}
