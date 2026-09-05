using GameTeam.Domain.Common;

namespace GameTeam.Application.Features.Heroes;

/// <summary>
/// Lỗi nghiệp vụ ổn định của feature hero. Code <c>SCREAMING_SNAKE_CASE</c>, map HTTP qua convention
/// <c>ErrorHttpMapping</c> (phase 13): <c>UNAUTHENTICATED</c> → 401, hậu tố <c>_NOT_FOUND</c> → 404. Không lộ
/// stack/DB.
/// </summary>
public static class HeroErrors
{
    /// <summary>Yêu cầu chưa xác thực (phòng thủ — endpoint hero owned đã protected mặc định).</summary>
    public static readonly Error Unauthenticated =
        new("UNAUTHENTICATED", "Yêu cầu chưa xác thực.");

    /// <summary>Không tìm thấy definition hero trong config hiện hành (id sai / chưa publish).</summary>
    public static readonly Error DefinitionNotFound =
        new("HERO_DEFINITION_NOT_FOUND", "Không tìm thấy định nghĩa hero trong config.");
}
