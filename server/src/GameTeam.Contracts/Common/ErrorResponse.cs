namespace GameTeam.Contracts.Common;

/// <summary>
/// Nội dung lỗi chuẩn hoá (đối tượng bên trong envelope) — docs/backend/api-and-versioning.md §3.
/// <para>
/// AN TOÀN SẢN XUẤT: chỉ chứa <see cref="Code"/> ổn định (enum lỗi), <see cref="Message"/> hiển thị được,
/// và <see cref="TraceId"/> để tra log. TUYỆT ĐỐI không rò stack trace / chi tiết exception / thông tin DB
/// hay hạ tầng nội bộ.
/// </para>
/// </summary>
/// <param name="Code">Mã lỗi ổn định (vd INSUFFICIENT_CURRENCY).</param>
/// <param name="Message">Thông điệp hiển thị cho người dùng.</param>
/// <param name="TraceId">Mã tra cứu log (tuỳ chọn).</param>
public sealed record ErrorResponse(string Code, string Message, string? TraceId);
