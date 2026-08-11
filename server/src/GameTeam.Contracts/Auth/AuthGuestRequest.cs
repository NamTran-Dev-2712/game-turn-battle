namespace GameTeam.Contracts.Auth;

/// <summary>
/// Yêu cầu đăng nhập khách (guest) — cấp JWT tạm, có thể liên kết tài khoản sau (ADR-008).
/// <para>
/// Quyết định tối thiểu (Phase 05): body chỉ mang <see cref="DeviceId"/> (định danh thiết bị, tuỳ chọn)
/// để server gắn/khôi phục guest. Trường bổ sung là additive ở phase hiện thực auth (Phase 13).
/// </para>
/// </summary>
/// <param name="DeviceId">Định danh thiết bị của client (tuỳ chọn ở bản nền).</param>
public sealed record AuthGuestRequest(string? DeviceId);
