namespace GameTeam.ConfigValidator;

/// <summary>
/// Bảng ID toàn cục theo loại, dựng MỘT LẦN từ toàn bộ entity → tra cứu O(1) cho referential integrity.
/// (Rủi ro Phase 07: config lớn → index ID O(1), không quét lại từng lần.)
/// </summary>
public sealed class IdIndex
{
    private readonly IReadOnlyDictionary<ConfigType, HashSet<string>> _byType;

    private IdIndex(IReadOnlyDictionary<ConfigType, HashSet<string>> byType) => _byType = byType;

    public static IdIndex Build(IEnumerable<ConfigEntity> entities)
    {
        Dictionary<ConfigType, HashSet<string>> byType = [];
        foreach (ConfigType type in Enum.GetValues<ConfigType>())
        {
            byType[type] = new HashSet<string>(StringComparer.Ordinal);
        }

        foreach (ConfigEntity entity in entities)
        {
            if (entity.Id is { Length: > 0 } id)
            {
                byType[entity.Type].Add(id);
            }
        }

        return new IdIndex(byType);
    }

    /// <summary>True nếu <paramref name="id"/> tồn tại cho <paramref name="type"/> (tra cứu O(1)).</summary>
    public bool Contains(ConfigType type, string id) => _byType[type].Contains(id);
}
