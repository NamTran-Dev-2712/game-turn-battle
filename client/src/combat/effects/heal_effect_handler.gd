class_name HealEffectHandler
## Effect `heal` — hồi máu theo `amount_fixed` (fixed-point). Khớp server `Effects/HealEffectHandler.cs`:
## ở phase 25 **không phát sự kiện** (heal event là phase 28). Không dùng trong 2 golden vector nhưng
## đăng ký sẵn để registry song ánh server.
extends EffectHandler

const TYPE_NAME: String = "heal"
const AMOUNT_FIXED_PARAM: String = "amount_fixed"


func effect_type() -> String:
	return TYPE_NAME


func apply(ctx: EffectContext) -> void:
	var heal_amount := FixedPoint.from_fixed(ctx.effect.param(AMOUNT_FIXED_PARAM))
	ctx.target.heal(heal_amount)
