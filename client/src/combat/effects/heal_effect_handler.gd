class_name HealEffectHandler
## Effect `heal` (§23) — hồi máu theo `amount_fixed` (fixed-point). Khớp server `Effects/HealEffectHandler.cs`:
## phát `Healed` với lượng hồi **thực** (sau kẹp max_hp) + HP còn lại; mục tiêu (ally/self) do simulator giải
## quyết theo target rule trước khi gọi.
extends EffectHandler

const TYPE_NAME: String = "heal"
const AMOUNT_FIXED_PARAM: String = "amount_fixed"


func effect_type() -> String:
	return TYPE_NAME


func apply(ctx: EffectContext) -> void:
	var heal_amount := FixedPoint.from_fixed(ctx.effect.param(AMOUNT_FIXED_PARAM))
	var before := ctx.target.hp
	var hp_after := ctx.target.heal(heal_amount)
	ctx.emit(CombatEvents.healed(ctx.target.actor_id(), hp_after - before, hp_after))
