namespace GameTeam.Domain.Combat.Effects;

/// <summary>
/// Registry ánh xạ <c>effect_type</c> → <see cref="IEffectHandler"/> (skill-framework.md, ADR-004). Lõi
/// combat định tuyến qua đây — <b>không</b> <c>switch(skillId)</c>. Effect_type lạ ⇒ ném (hợp đồng rõ
/// ràng: lỗi config/lập trình, không "nuốt lặng"). Thứ tự lặp không ảnh hưởng output (định tuyến theo khoá).
/// </summary>
public sealed class EffectRegistry
{
    private readonly Dictionary<string, IEffectHandler> _handlers;

    /// <summary>Tạo registry từ tập handler (trùng khoá ⇒ ném — lỗi cấu hình).</summary>
    public EffectRegistry(IEnumerable<IEffectHandler> handlers)
    {
        _handlers = new Dictionary<string, IEffectHandler>(StringComparer.Ordinal);
        foreach (IEffectHandler handler in handlers)
        {
            if (!_handlers.TryAdd(handler.EffectType, handler))
            {
                throw new InvalidOperationException($"Trùng effect handler cho '{handler.EffectType}'.");
            }
        }
    }

    /// <summary>Registry mặc định của phase 24: <c>damage</c> + <c>heal</c> (mẫu).</summary>
    public static EffectRegistry CreateDefault() =>
        new(new IEffectHandler[] { new DamageEffectHandler(), new HealEffectHandler() });

    /// <summary>Có handler cho <paramref name="effectType"/> không?</summary>
    public bool Has(string effectType) => _handlers.ContainsKey(effectType);

    /// <summary>Lấy handler theo <paramref name="effectType"/>; ném <see cref="KeyNotFoundException"/> nếu không có (unknown effect).</summary>
    public IEffectHandler Resolve(string effectType)
    {
        if (!_handlers.TryGetValue(effectType, out IEffectHandler? handler))
        {
            throw new KeyNotFoundException($"Không có handler cho effect_type '{effectType}' (unknown effect).");
        }

        return handler;
    }
}
