namespace GameTeam.ConfigValidator;

/// <summary>
/// 10 loại config data-driven (ADR-004). Khớp các schema per-type ở shared/config-schema/.
/// <c>Formation</c> thêm ở Phase 29 (lưới đội hình); <c>Item</c> thêm ở Phase 32 (catalog vật phẩm cho
/// inventory) — không phát minh loại mới ngoài scope.
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
    Item,
}
