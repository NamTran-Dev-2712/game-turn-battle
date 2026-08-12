using System.Text.Json.Nodes;

namespace GameTeam.ConfigValidator;

/// <summary>
/// Referential integrity giữa các file config. Đồ thị tham chiếu lấy NGUYÊN VĂN từ 8 schema +
/// docs/gameplay/configuration-and-data.md §2b/§3 — KHÔNG phát minh quan hệ mới.
/// Chỉ trích tham chiếu khi cấu trúc đúng như schema kỳ vọng; cấu trúc sai để SchemaValidator báo (SCH001),
/// tránh lỗi trùng/nhiễu. Thiếu id → REF001; sai định dạng/sai loại đích → REF002.
/// </summary>
public static class ReferenceValidator
{
    /// <summary>Currency hợp lệ (khớp common.schema.json#/$defs/currency).</summary>
    private static readonly HashSet<string> Currencies =
        new(StringComparer.Ordinal) { "gold", "gem", "ticket" };

    public static IEnumerable<ValidationError> Validate(ConfigEntity entity, IdIndex index)
    {
        if (entity.Root is not JsonObject obj)
        {
            return [];
        }

        return entity.Type switch
        {
            ConfigType.Hero => Hero(entity, obj, index),
            ConfigType.Stage => Stage(entity, obj, index),
            ConfigType.Gacha => Gacha(entity, obj, index),
            ConfigType.Shop => Shop(entity, obj, index),
            ConfigType.Quest => Quest(entity, obj, index),
            ConfigType.Reward => Reward(entity, obj, index),
            _ => [], // skill, economy: không có tham chiếu id chéo.
        };
    }

    // hero.skills[] → skill
    private static IEnumerable<ValidationError> Hero(ConfigEntity e, JsonObject obj, IdIndex index)
    {
        foreach ((string? id, string path) in StringItems(obj, "skills"))
        {
            if (id is not null && !index.Contains(ConfigType.Skill, id))
            {
                yield return Missing(e, path, ConfigType.Skill, id);
            }
        }
    }

    // stage.enemies[].hero_id → hero ; stage.rewards[] → reward ; requirements.prerequisite_stage_id → stage
    private static IEnumerable<ValidationError> Stage(ConfigEntity e, JsonObject obj, IdIndex index)
    {
        if (obj["enemies"] is JsonArray enemies)
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] is JsonObject enemy && TryString(enemy, "hero_id", out string? heroId) &&
                    heroId is not null && !index.Contains(ConfigType.Hero, heroId))
                {
                    yield return Missing(e, $"/enemies/{i}/hero_id", ConfigType.Hero, heroId);
                }
            }
        }

        foreach ((string? id, string path) in StringItems(obj, "rewards"))
        {
            if (id is not null && !index.Contains(ConfigType.Reward, id))
            {
                yield return Missing(e, path, ConfigType.Reward, id);
            }
        }

        if (obj["requirements"] is JsonObject req &&
            TryString(req, "prerequisite_stage_id", out string? prereq) &&
            prereq is not null && !index.Contains(ConfigType.Stage, prereq))
        {
            yield return Missing(e, "/requirements/prerequisite_stage_id", ConfigType.Stage, prereq);
        }
    }

    // gacha.pool[] → hero
    private static IEnumerable<ValidationError> Gacha(ConfigEntity e, JsonObject obj, IdIndex index)
    {
        foreach ((string? id, string path) in StringItems(obj, "pool"))
        {
            if (id is not null && !index.Contains(ConfigType.Hero, id))
            {
                yield return Missing(e, path, ConfigType.Hero, id);
            }
        }
    }

    // shop.items[].reward_ref → reward
    private static IEnumerable<ValidationError> Shop(ConfigEntity e, JsonObject obj, IdIndex index)
    {
        if (obj["items"] is not JsonArray items)
        {
            yield break;
        }

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] is JsonObject item && TryString(item, "reward_ref", out string? rewardRef) &&
                rewardRef is not null && !index.Contains(ConfigType.Reward, rewardRef))
            {
                yield return Missing(e, $"/items/{i}/reward_ref", ConfigType.Reward, rewardRef);
            }
        }
    }

    // quest.reward_refs[] → reward
    private static IEnumerable<ValidationError> Quest(ConfigEntity e, JsonObject obj, IdIndex index)
    {
        foreach ((string? id, string path) in StringItems(obj, "reward_refs"))
        {
            if (id is not null && !index.Contains(ConfigType.Reward, id))
            {
                yield return Missing(e, path, ConfigType.Reward, id);
            }
        }
    }

    // reward.entries[].ref_id → đa hình theo reward_type.
    //   currency → ref_id ∈ {gold,gem,ticket} (else REF002)
    //   hero     → tồn tại trong hero index (else REF001)
    //   fragment/item → KHÔNG có loại config tương ứng → chỉ kiểm định dạng, không kiểm tồn tại
    //                   (giới hạn có chủ đích — tránh phát minh quan hệ; xem README §Known limitations).
    private static IEnumerable<ValidationError> Reward(ConfigEntity e, JsonObject obj, IdIndex index)
    {
        if (obj["entries"] is not JsonArray entries)
        {
            yield break;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] is not JsonObject entry ||
                !TryString(entry, "reward_type", out string? type) || type is null ||
                !TryString(entry, "ref_id", out string? refId) || refId is null)
            {
                continue; // cấu trúc thiếu → SchemaValidator báo SCH001.
            }

            string path = $"/entries/{i}/ref_id";
            switch (type)
            {
                case "currency" when !Currencies.Contains(refId):
                    yield return new ValidationError(
                        e.FilePath, path, ErrorCode.Ref002Invalid,
                        $"ref_id '{refId}' không phải currency hợp lệ (gold|gem|ticket) cho reward_type=currency.");
                    break;

                case "hero" when !index.Contains(ConfigType.Hero, refId):
                    yield return Missing(e, path, ConfigType.Hero, refId);
                    break;

                // fragment/item: không kiểm tồn tại (không có config type). currency/hero hợp lệ: bỏ qua.
                default:
                    break;
            }
        }
    }

    /// <summary>Duyệt mảng chuỗi ở thuộc tính <paramref name="property"/> → (giá trị, json-pointer).</summary>
    private static IEnumerable<(string? Id, string Path)> StringItems(JsonObject obj, string property)
    {
        if (obj[property] is not JsonArray array)
        {
            yield break;
        }

        for (int i = 0; i < array.Count; i++)
        {
            string? value = array[i] is JsonValue v && v.TryGetValue(out string? s) ? s : null;
            yield return (value, $"/{property}/{i}");
        }
    }

    private static bool TryString(JsonObject obj, string property, out string? value)
    {
        if (obj.TryGetPropertyValue(property, out JsonNode? node) &&
            node is JsonValue v && v.TryGetValue(out string? s))
        {
            value = s;
            return true;
        }

        value = null;
        return false;
    }

    private static ValidationError Missing(ConfigEntity e, string path, ConfigType target, string id) =>
        new(e.FilePath, path, ErrorCode.Ref001Missing,
            $"{target.ToString().ToLowerInvariant()} id '{id}' được tham chiếu nhưng không tồn tại.");
}
