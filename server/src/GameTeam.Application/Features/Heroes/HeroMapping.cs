using GameTeam.Contracts.Enums;
using GameTeam.Contracts.Hero;
using DomainOwnedHero = GameTeam.Domain.Heroes.OwnedHero;

namespace GameTeam.Application.Features.Heroes;

/// <summary>
/// Map aggregate/config hero sang DTO wire (phase 05). EF entity KHÔNG bao giờ trả thẳng ra API — chỉ
/// projection read-model. Trục đã chốt (class/element/role/rarity) parse chuỗi config → enum contract
/// (không khớp ⇒ <c>None</c>, an toàn); <c>faction</c> giữ chuỗi (GP2 chưa chốt).
/// </summary>
internal static class HeroMapping
{
    /// <summary>Khoá loại config của hero trong bundle (data-driven — dùng cho <c>IConfigProvider</c>).</summary>
    public const string ConfigType = "hero";

    public static OwnedHeroDto ToDto(DomainOwnedHero hero)
        => new(hero.HeroId, hero.Level, hero.Stars);

    public static HeroDefinitionDto ToDefinitionDto(string heroId, HeroConfig config)
        => new(
            heroId,
            config.Faction,
            ParseEnum<Class>(config.Class),
            ParseEnum<Element>(config.Element),
            ParseEnum<Role>(config.Role),
            ToRarity(config.Rarity),
            new HeroBaseStatsDto(
                config.BaseStats.Hp,
                config.BaseStats.Atk,
                config.BaseStats.Def,
                config.BaseStats.Spd),
            config.Skills,
            config.Art);

    // Parse chuỗi config (lowercase) → enum contract (PascalCase), bỏ qua hoa/thường. Không khớp/không xác
    // định ⇒ None (mặc định an toàn cho contract — không ném).
    private static TEnum ParseEnum<TEnum>(string value)
        where TEnum : struct, Enum
        => Enum.TryParse(value, ignoreCase: true, out TEnum parsed) && Enum.IsDefined(parsed)
            ? parsed
            : default;

    // Rarity là số trong config ({3,4,5}); map sang enum, giá trị không xác định ⇒ None.
    private static Rarity ToRarity(int rarity)
        => Enum.IsDefined(typeof(Rarity), rarity) ? (Rarity)rarity : Rarity.None;
}
