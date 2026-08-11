namespace GameTeam.Contracts.Profile;

/// <summary>
/// Hồ sơ người chơi (bản nền). Save có version + migration (ADR-007) → mang <see cref="SchemaVersion"/>.
/// <para>
/// Quyết định tối thiểu (Phase 05): chỉ các trường định danh/tiến trình cơ bản; ví/inventory/đội hình
/// là DTO feature, thêm ĐÚNG phase của chúng (không over-design sớm — Rủi ro roadmap 05).
/// </para>
/// </summary>
/// <param name="PlayerId">Định danh người chơi (ổn định).</param>
/// <param name="DisplayName">Tên hiển thị.</param>
/// <param name="Level">Cấp tài khoản người chơi.</param>
/// <param name="SchemaVersion">Phiên bản schema của bản ghi profile (ADR-007).</param>
public sealed record ProfileDto(string PlayerId, string DisplayName, int Level, int SchemaVersion);
