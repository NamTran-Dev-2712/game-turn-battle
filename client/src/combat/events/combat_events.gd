class_name CombatEvents
## Nhà máy sự kiện combat — sinh Dictionary đúng **golden format** (`shared/combat-vectors/`, §18).
## Một nơi DUY NHẤT giữ tên trường (snake_case) để khớp bit-for-bit với server
## `GameTeam.Domain/Combat/Events/*` + `CombatEventSerializer`. `seq` được đóng dấu theo vị trí khi
## build output (KHÔNG mang trong event) — khớp server (serializer ghi seq = chỉ số list).
##
## 13 loại sự kiện. Output là Dictionary (không phải class có kiểu) vì golden log là JSON: dùng kiểu
## ở nơi state/logic, dùng Dictionary ở nơi đã serialize.
extends RefCounted


static func round_started(round_no: int) -> Dictionary:
	return {"type": "RoundStarted", "round": round_no}


static func round_ended(round_no: int) -> Dictionary:
	return {"type": "RoundEnded", "round": round_no}


static func action_started(actor: String) -> Dictionary:
	return {"type": "ActionStarted", "actor": actor}


static func action_completed(actor: String) -> Dictionary:
	return {"type": "ActionCompleted", "actor": actor}


static func target_selected(actor: String, target: String) -> Dictionary:
	return {"type": "TargetSelected", "actor": actor, "target": target}


static func random_roll(purpose: String, bound: int, value: int) -> Dictionary:
	return {"type": "RandomRoll", "purpose": purpose, "bound": bound, "value": value}


static func hit(actor: String, target: String) -> Dictionary:
	return {"type": "Hit", "actor": actor, "target": target}


static func miss(actor: String, target: String) -> Dictionary:
	return {"type": "Miss", "actor": actor, "target": target}


static func crit(actor: String, target: String) -> Dictionary:
	return {"type": "Crit", "actor": actor, "target": target}


static func damage_applied(actor: String, target: String, amount: int, target_hp_after: int, is_crit: bool) -> Dictionary:
	return {
		"type": "DamageApplied",
		"actor": actor,
		"target": target,
		"amount": amount,
		"target_hp_after": target_hp_after,
		"crit": is_crit,
	}


static func death(unit: String) -> Dictionary:
	return {"type": "Death", "unit": unit}


static func energy_changed(unit: String, energy_after: int) -> Dictionary:
	return {"type": "EnergyChanged", "unit": unit, "energy_after": energy_after}


static func battle_ended() -> Dictionary:
	return {"type": "BattleEnded"}
