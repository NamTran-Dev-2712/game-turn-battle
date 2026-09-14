using GameTeam.Domain.Common;

namespace GameTeam.Application.Features.Heroes;

/// <summary>
/// Lỗi nghiệp vụ ổn định của feature hero. Code <c>SCREAMING_SNAKE_CASE</c>, map HTTP qua convention
/// <c>ErrorHttpMapping</c> (phase 13): <c>UNAUTHENTICATED</c> → 401, hậu tố <c>_NOT_FOUND</c> → 404,
/// <c>_CONFLICT</c> → 409. Không lộ stack/DB.
/// </summary>
public static class HeroErrors
{
    /// <summary>Yêu cầu chưa xác thực (phòng thủ — endpoint hero owned đã protected mặc định).</summary>
    public static readonly Error Unauthenticated =
        new("UNAUTHENTICATED", "Yêu cầu chưa xác thực.");

    /// <summary>Không tìm thấy definition hero trong config hiện hành (id sai / chưa publish).</summary>
    public static readonly Error DefinitionNotFound =
        new("HERO_DEFINITION_NOT_FOUND", "Không tìm thấy định nghĩa hero trong config.");

    /// <summary>Người chơi không sở hữu hero này (hoặc chưa có profile) — chống IDOR, không lộ tồn tại (Phase 35).</summary>
    public static readonly Error OwnedHeroNotFound =
        new("OWNED_HERO_NOT_FOUND", "Không tìm thấy hero thuộc sở hữu của bạn.");

    /// <summary>Hero đã ở cấp tối đa (theo đường cong config) — không thể nâng thêm (Phase 35, → 409).</summary>
    public static readonly Error MaxLevel =
        new("HERO_MAX_LEVEL_CONFLICT", "Hero đã đạt cấp tối đa.");

    /// <summary>Thiếu cấu hình economy (đường cong cấp/tăng trưởng/power) trong config hiện hành (Phase 35).</summary>
    public static readonly Error EconomyConfigNotFound =
        new("ECONOMY_CONFIG_NOT_FOUND", "Không tìm thấy cấu hình economy trong config.");
}
