namespace GameTeam.Contracts.Config;

/// <summary>
/// Gói cấu hình client tải về (bản nền). Client phụ thuộc schema, không phụ thuộc giá trị cụ thể (ADR-005, R6).
/// <para>
/// Quyết định tối thiểu (Phase 05): chỉ mang <see cref="Version"/>. Nội dung (bảng số liệu balance)
/// được mô hình hoá ĐÚNG phase Config Service (Phase 07) — không nhét dữ liệu balance vào contract sớm.
/// </para>
/// </summary>
/// <param name="Version">Phiên bản của gói cấu hình.</param>
public sealed record ConfigBundleDto(ConfigVersion Version);
