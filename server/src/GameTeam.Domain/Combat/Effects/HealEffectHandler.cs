using GameTeam.Domain.Combat.Events;
using GameTeam.Domain.Combat.Numerics;

namespace GameTeam.Domain.Combat.Effects;

/// <summary>
/// Effect hồi máu (§23). Lượng hồi lấy từ config (<c>amount_fixed</c>, fixed-point) — data-driven, không
/// hardcode. Tất định (không RNG/wall-clock). Phát <see cref="Healed"/> với lượng hồi <b>thực</b> (sau kẹp
/// MaxHp) và HP còn lại; mục tiêu (ally/self) do simulator giải quyết theo target rule trước khi gọi.
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

        int before = context.Target.Hp;
        int hpAfter = context.Target.Heal(heal);
        context.Emit(new Healed(context.Target.ActorId, hpAfter - before, hpAfter));
    }
}
