namespace GameTeam.Domain.Combat.Effects;

/// <summary>
/// Effect buff (§23): cộng modifier chỉ số <b>dương</b> lên mục tiêu (ally/self) trong <c>duration</c> vòng.
/// Data-driven qua <c>params</c> (atk/def/spd + duration); dùng chung <see cref="StatModifierCore"/> với debuff.
/// </summary>
public sealed class ApplyBuffEffectHandler : IEffectHandler
{
    /// <summary>Khoá effect_type.</summary>
    public const string TypeName = "apply_buff";

    /// <inheritdoc/>
    public string EffectType => TypeName;

    /// <inheritdoc/>
    public void Apply(EffectContext context) => StatModifierCore.Apply(context, sign: +1);
}
