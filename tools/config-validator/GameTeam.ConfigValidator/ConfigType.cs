namespace GameTeam.ConfigValidator;

/// <summary>
/// 9 loại config data-driven (ADR-004). Khớp 9 schema per-type ở shared/config-schema/.
/// <c>Formation</c> thêm ở Phase 29 (lưới đội hình) — không phát minh loại mới ngoài scope.
/// </summary>
public enum ConfigType
{
    Hero,
    Skill,
    Stage,
    Gacha,
    Shop,
    Reward,
    Economy,
    Quest,
    Formation,
}
