class_name StatModifierCore
## Lõi dùng chung của buff/debuff (§23) — khớp server `Effects/StatModifierCore.cs`. Đọc `duration` +
## bất kỳ chỉ số nào trong {atk,def,spd} có mặt ở `params` (theo thứ tự tất định), áp modifier có dấu
## (`sign` = +1 buff / −1 debuff) lên `ctx.target` và phát `BuffApplied`. Tất định, không RNG.
extends RefCounted

const DURATION_PARAM: String = "duration"
# Thứ tự cố định atk→def→spd để thứ tự BuffApplied tất định khi một buff tác động nhiều chỉ số.
const STAT_KEYS: Array[String] = ["atk", "def", "spd"]


static func apply(ctx: EffectContext, sign: int) -> void:
	var duration := ctx.effect.param(DURATION_PARAM)
	for key in STAT_KEYS:
		if not ctx.effect.has_param(key):
			continue
		var signed := sign * ctx.effect.param(key)
		ctx.target.apply_stat_modifier(ctx.skill.id, key, signed, duration)
		ctx.emit(CombatEvents.buff_applied(ctx.target.actor_id(), ctx.skill.id, key, signed, duration))
