namespace GameTeam.ConfigValidator;

/// <summary>
/// Ánh xạ xác định file config → schema theo quy ước repo:
/// thư mục config SỐ NHIỀU (<c>heroes/</c>) → schema SỐ ÍT (<c>hero.schema.json</c>)
/// (docs/gameplay/configuration-and-data.md §2, config/README.md).
/// </summary>
public static class ConfigFileMapper
{
    /// <summary>Thư mục con (cấp 1 dưới config root) → loại config.</summary>
    public static readonly IReadOnlyDictionary<string, ConfigType> DirectoryToType =
        new Dictionary<string, ConfigType>(StringComparer.Ordinal)
        {
            ["heroes"] = ConfigType.Hero,
            ["skills"] = ConfigType.Skill,
            ["stages"] = ConfigType.Stage,
            ["gacha"] = ConfigType.Gacha,
            ["shop"] = ConfigType.Shop,
            ["rewards"] = ConfigType.Reward,
            ["economy"] = ConfigType.Economy,
            ["quests"] = ConfigType.Quest,
        };

    /// <summary>
    /// Thư mục metadata/không-per-type: bỏ qua (không phải MAP001).
    /// <c>liveops</c> là Post-MVP; <c>_versions</c> là bundle metadata (Phase 21).
    /// </summary>
    public static readonly IReadOnlySet<string> SkippedDirectories =
        new HashSet<string>(StringComparer.Ordinal) { "liveops", "_versions" };

    /// <summary>Tên file schema per-type cho một loại (vd Hero → <c>hero.schema.json</c>).</summary>
    public static string SchemaFileName(ConfigType type) =>
        $"{type.ToString().ToLowerInvariant()}.schema.json";
}
