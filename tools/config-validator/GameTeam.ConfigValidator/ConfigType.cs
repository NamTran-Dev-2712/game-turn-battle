namespace GameTeam.ConfigValidator;

/// <summary>
/// 8 loại config data-driven (ADR-004). Khớp 8 schema per-type ở shared/config-schema/.
/// Không phát minh loại mới ở Phase 07.
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
}
