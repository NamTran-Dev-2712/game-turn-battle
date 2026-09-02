namespace GameTeam.Domain.Combat.Model;

/// <summary>
/// Dữ liệu một effect nguyên thủy trong một skill (skill-framework.md, ADR-004): <see cref="EffectType"/>
/// định tuyến tới handler trong registry; <see cref="Params"/> là tham số tuỳ effect (đọc từ config —
/// không hardcode). Mở rộng gameplay = thêm effect_type + handler + config, KHÔNG sửa lõi (OCP).
/// </summary>
public sealed record EffectDef(string EffectType, IReadOnlyDictionary<string, long> Params)
{
    /// <summary>Tạo effect không tham số (ví dụ <c>damage</c> — hệ số lấy từ skill/combat rules).</summary>
    public EffectDef(string effectType)
        : this(effectType, EmptyParams)
    {
    }

    private static readonly IReadOnlyDictionary<string, long> EmptyParams =
        new Dictionary<string, long>(StringComparer.Ordinal);

    /// <summary>Đọc tham số fixed/int theo khoá; ném nếu thiếu (lỗi config — không đoán mặc định).</summary>
    public long Param(string key)
    {
        if (!Params.TryGetValue(key, out long value))
        {
            throw new KeyNotFoundException($"Effect '{EffectType}' thiếu tham số '{key}'.");
        }

        return value;
    }
}
