class_name ApplyDebuffEffectHandler
## Effect `apply_debuff` (§23): trừ modifier chỉ số (độ lớn dương ⇒ delta **âm**) lên mục tiêu (enemy) trong
## `duration` vòng. Data-driven qua `params` (atk/def/spd + duration); dùng chung [StatModifierCore] (khác dấu).
extends EffectHandler

const TYPE_NAME: String = "apply_debuff"


func effect_type() -> String:
	return TYPE_NAME


func apply(ctx: EffectContext) -> void:
	StatModifierCore.apply(ctx, -1)
