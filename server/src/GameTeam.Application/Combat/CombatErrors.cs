using GameTeam.Domain.Common;

namespace GameTeam.Application.Combat;

/// <summary>Lỗi nghiệp vụ khi dựng đầu vào combat (mã ổn định — ánh xạ HTTP ở phase 13/30).</summary>
public static class CombatErrors
{
    /// <summary>Không tìm thấy hero config theo id.</summary>
    public static Error HeroNotFound(string heroId) =>
        new("COMBAT_HERO_CONFIG_NOT_FOUND", $"Không có hero config '{heroId}'.");

    /// <summary>Không tìm thấy skill config theo id.</summary>
    public static Error SkillNotFound(string skillId) =>
        new("COMBAT_SKILL_CONFIG_NOT_FOUND", $"Không có skill config '{skillId}'.");

    /// <summary>Không tìm thấy stage config theo id.</summary>
    public static Error StageNotFound(string stageId) =>
        new("COMBAT_STAGE_CONFIG_NOT_FOUND", $"Không có stage config '{stageId}'.");
}
