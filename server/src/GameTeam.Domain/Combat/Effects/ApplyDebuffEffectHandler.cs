namespace GameTeam.Domain.Combat.Effects;

/// <summary>
/// Effect debuff (§23): trừ modifier chỉ số (độ lớn dương ⇒ delta <b>âm</b>) lên mục tiêu (enemy) trong
/// <c>duration</c> vòng. Data-driven qua <c>params</c> (atk/def/spd + duration); dùng chung
/// <see cref="StatModifierCore"/> với buff (chỉ khác dấu).
/// </summary>
public sealed class ApplyDebuffEffectHandler : IEffectHandler
{
    /// <summary>Khoá effect_type.</summary>
    public const string TypeName = "apply_debuff";

    /// <inheritdoc/>
    public string EffectType => TypeName;

    /// <inheritdoc/>
    public void Apply(EffectContext context) => StatModifierCore.Apply(context, sign: -1);
}
