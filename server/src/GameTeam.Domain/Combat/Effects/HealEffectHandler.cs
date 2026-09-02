using GameTeam.Domain.Combat.Numerics;

namespace GameTeam.Domain.Combat.Effects;

/// <summary>
/// Effect hồi máu — <b>handler mẫu thứ hai</b> chứng minh registry mở rộng được (chưa dùng trong 2 golden
/// vector phase 23). Lượng hồi lấy từ config (<c>amount_fixed</c>, fixed-point) — data-driven, không
/// hardcode. Tất định (không RNG/wall-clock). Sự kiện hồi máu chuyên biệt là phần của phase 28.
/// </summary>
public sealed class HealEffectHandler : IEffectHandler
{
    /// <summary>Khoá effect_type.</summary>
    public const string TypeName = "heal";

    /// <summary>Khoá tham số lượng hồi (fixed-point).</summary>
    public const string AmountFixedParam = "amount_fixed";

    /// <inheritdoc/>
    public string EffectType => TypeName;

    /// <inheritdoc/>
    public void Apply(EffectContext context)
    {
        long amountFixed = context.Effect.Param(AmountFixedParam);
        int heal = (int)FixedPoint.FromFixed(amountFixed);
        context.Target.Heal(heal);
    }
}
