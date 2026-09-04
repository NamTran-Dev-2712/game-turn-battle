class_name DamageEffectHandler
## Effect `damage` (§17) — mô hình divisive DEF-ratio, fixed-point. Khớp bit-for-bit với server
## `Effects/DamageEffectHandler.cs`. Thứ tự 6 bước + điểm làm tròn CỐ ĐỊNH (round-half-up ở mỗi
## mul/div/from_fixed); crit áp **sau** giảm trừ; sàn `min_damage` cuối cùng.
extends EffectHandler

const TYPE_NAME: String = "damage"


func effect_type() -> String:
	return TYPE_NAME


func apply(ctx: EffectContext) -> void:
	var amount := compute_damage(ctx.attacker.atk(), ctx.target.def(), ctx.skill.coeff_fixed, ctx.is_crit, ctx.rules)
	var hp_after := ctx.target.apply_damage(amount)
	ctx.emit(CombatEvents.damage_applied(ctx.attacker.actor_id(), ctx.target.actor_id(), amount, hp_after, ctx.is_crit))
	if hp_after == 0:
		ctx.emit(CombatEvents.death(ctx.target.actor_id()))


## Tính sát thương (§17). Thuần integer/fixed-point — KHÔNG float.
static func compute_damage(atk: int, def: int, coeff_fixed: int, crit: bool, rules: CombatRules) -> int:
	var atk_fixed := FixedPoint.to_fixed(atk) # atk * 1000
	var raw_fixed := FixedPoint.mul(atk_fixed, coeff_fixed) # raw = atk * coeff
	var k_fixed := FixedPoint.to_fixed(rules.def_constant_k) # K * 1000
	var ratio_fixed := FixedPoint.div(k_fixed, k_fixed + FixedPoint.to_fixed(def)) # K/(K+def)
	var damage_fixed := FixedPoint.mul(raw_fixed, ratio_fixed) # raw * ratio (đã giảm trừ)
	if crit:
		damage_fixed = FixedPoint.mul(damage_fixed, rules.crit_multiplier_fixed) # crit SAU giảm trừ
	var damage := FixedPoint.from_fixed(damage_fixed) # round-half-up về int
	return FixedPoint.clamp_int(damage, rules.min_damage, damage) # sàn MIN_DMG
