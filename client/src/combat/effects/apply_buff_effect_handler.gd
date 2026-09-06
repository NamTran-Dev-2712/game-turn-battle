class_name ApplyBuffEffectHandler
## Effect `apply_buff` (§23): cộng modifier chỉ số **dương** lên mục tiêu (ally/self) trong `duration` vòng.
## Data-driven qua `params` (atk/def/spd + duration); dùng chung [StatModifierCore] với debuff. Khớp server.
extends EffectHandler

const TYPE_NAME: String = "apply_buff"


func effect_type() -> String:
	return TYPE_NAME


func apply(ctx: EffectContext) -> void:
	StatModifierCore.apply(ctx, 1)
