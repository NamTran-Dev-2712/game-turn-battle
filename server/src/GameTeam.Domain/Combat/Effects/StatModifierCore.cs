using GameTeam.Domain.Combat.Events;
using GameTeam.Domain.Combat.Model;

namespace GameTeam.Domain.Combat.Effects;

/// <summary>
/// Lõi dùng chung của buff/debuff (§23) — không phải handler, tránh lặp code giữa
/// <see cref="ApplyBuffEffectHandler"/> và <see cref="ApplyDebuffEffectHandler"/>. Đọc <c>duration</c> +
/// bất kỳ chỉ số nào trong {atk,def,spd} có mặt ở <c>params</c> (theo thứ tự tất định), áp modifier có dấu
/// (<paramref name="sign"/> = +1 buff / −1 debuff) lên <see cref="EffectContext.Target"/> và phát
/// <see cref="BuffApplied"/>. Tất định, không RNG.
/// </summary>
internal static class StatModifierCore
{
    /// <summary>Khoá tham số số vòng hiệu lực.</summary>
    public const string DurationParam = "duration";

    // Thứ tự cố định atk→def→spd để thứ tự BuffApplied tất định khi một buff tác động nhiều chỉ số.
    private static readonly (string Key, StatKind Stat)[] StatKeys =
    {
        ("atk", StatKind.Atk),
        ("def", StatKind.Def),
        ("spd", StatKind.Spd),
    };

    /// <summary>Áp buff/debuff theo dấu.</summary>
    public static void Apply(EffectContext context, int sign)
    {
        int duration = (int)context.Effect.Param(DurationParam);

        foreach ((string key, StatKind stat) in StatKeys)
        {
            if (!context.Effect.TryParam(key, out long magnitude))
            {
                continue;
            }

            int signed = sign * (int)magnitude;
            context.Target.ApplyStatModifier(context.Skill.Id, stat, signed, duration);
            context.Emit(new BuffApplied(context.Target.ActorId, context.Skill.Id, key, signed, duration));
        }
    }
}
