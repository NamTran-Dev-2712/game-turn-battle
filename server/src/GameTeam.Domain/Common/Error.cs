namespace GameTeam.Domain.Common;

/// <summary>
/// Giá trị lỗi domain bất biến (so sánh theo giá trị) dùng cho <see cref="Result"/>.
/// <para>
/// AN TOÀN SẢN XUẤT: <see cref="Code"/> là mã ổn định để map API (phase 13) — KHÔNG rò stack trace,
/// chi tiết DB/hạ tầng hay thông tin nhạy cảm. <see cref="Message"/> chỉ để hiển thị/log.
/// </para>
/// Quy ước: <c>Code</c> theo <c>SCREAMING_SNAKE_CASE</c> (vd <c>INSUFFICIENT_CURRENCY</c>).
/// </summary>
/// <param name="Code">Mã lỗi ổn định.</param>
/// <param name="Message">Thông điệp hiển thị được.</param>
public sealed record Error(string Code, string Message)
{
    /// <summary>Trạng thái "không lỗi" — dùng cho <see cref="Result"/> thành công.</summary>
    public static readonly Error None = new(string.Empty, string.Empty);
}
