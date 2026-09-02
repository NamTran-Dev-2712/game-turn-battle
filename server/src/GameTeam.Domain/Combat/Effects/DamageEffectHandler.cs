using GameTeam.Domain.Combat.Events;
using GameTeam.Domain.Combat.Model;
using GameTeam.Domain.Combat.Numerics;

namespace GameTeam.Domain.Combat.Effects;

/// <summary>
/// Effect sát thương (combat-framework.md §17): mô hình <b>divisive DEF-ratio</b>
/// <c>atk*coeff*K/(K+def)</c> fixed-point, crit áp <b>SAU</b> mitigation, làm tròn cuối
/// <see cref="FixedPoint.FromFixed"/>, sàn <c>MIN_DMG</c>. Thứ tự phép toán + điểm làm tròn cố định.
/// </summary>
public sealed class DamageEffectHandler : IEffectHandler
{
    /// <summary>Khoá effect_type.</summary>
    public const string TypeName = "damage";

    /// <inheritdoc/>
    public string EffectType => TypeName;

    /// <inheritdoc/>
    public void Apply(EffectContext context)
    {
        int amount = ComputeDamage(
            context.Attacker.Atk,
            context.Target.Def,
            context.Skill.CoeffFixed,
            context.IsCrit,
            context.Rules);

        int hpAfter = context.Target.ApplyDamage(amount);
        context.Emit(new DamageApplied(
            context.Attacker.ActorId,
            context.Target.ActorId,
            amount,
            hpAfter,
            context.IsCrit));

        if (hpAfter == 0)
        {
            context.Emit(new Death(context.Target.ActorId));
        }
    }

    /// <summary>
    /// Sát thương cuối (§17), thứ tự cố định 1→6: raw = atk*coeff; ratio = K/(K+def); dmg = raw*ratio;
    /// nếu crit thì ×crit_mult (sau mitigation); from_fixed; sàn <c>MIN_DMG</c>. Không float ở bất kỳ bước nào.
    /// </summary>
    public static int ComputeDamage(int atk, int def, int coeffFixed, bool crit, CombatRules rules)
    {
        long atkFixed = FixedPoint.ToFixed(atk);
        long rawFixed = FixedPoint.Mul(atkFixed, coeffFixed);

        long kFixed = FixedPoint.ToFixed(rules.DefConstantK);
        long ratioFixed = FixedPoint.Div(kFixed, kFixed + FixedPoint.ToFixed(def));

        long damageFixed = FixedPoint.Mul(rawFixed, ratioFixed);
        if (crit)
        {
            damageFixed = FixedPoint.Mul(damageFixed, rules.CritMultiplierFixed);
        }

        long damage = FixedPoint.FromFixed(damageFixed);
        long floored = FixedPoint.Clamp(damage, rules.MinDamage, damage);
        return (int)floored;
    }
}
