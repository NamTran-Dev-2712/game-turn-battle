namespace GameTeam.Domain.Accounts;

/// <summary>
/// Loại tài khoản. MVP chỉ có <see cref="Guest"/> (đăng nhập khách, ADR-008); liên kết provider
/// (Google/Apple/email) là Post-MVP (ADR-006) — khi thêm sẽ là giá trị MỚI (additive), không tái dùng số.
/// <para>
/// Đây là khái niệm <b>nội bộ Domain</b> (không nằm trên wire/contract), nên KHÔNG đặt trong
/// <c>GameTeam.Contracts.Enums</c> (tránh sinh model client thừa qua codegen).
/// </para>
/// </summary>
public enum AccountType
{
    /// <summary>Chưa xác định (mặc định an toàn — không dùng cho tài khoản hợp lệ).</summary>
    None = 0,

    /// <summary>Tài khoản khách: tạo tức thời khi client lần đầu vào game, chưa gắn provider.</summary>
    Guest = 1,
}
