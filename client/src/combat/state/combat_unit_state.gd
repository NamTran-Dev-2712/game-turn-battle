class_name CombatUnitState
## Trạng thái **biến đổi** của một unit trong trận (khớp server `State/UnitState.cs`). HP kẹp ≥ 0;
## `energy` được nối dây nhưng bất hoạt ở phase 25 (§15). Đọc `actor_id`/`slot`/`spd`/`atk`/`def` từ
## snapshot bất biến.
extends RefCounted

var snapshot: UnitSnapshot = null
var hp: int = 0
var energy: int = 0


func _init(unit_snapshot: UnitSnapshot, initial_energy: int) -> void:
	snapshot = unit_snapshot
	hp = unit_snapshot.stats.hp
	energy = 0 if initial_energy < 0 else initial_energy


func actor_id() -> String:
	return snapshot.actor_id


func team() -> String:
	return snapshot.team


func slot() -> int:
	return snapshot.slot


func spd() -> int:
	return snapshot.stats.spd


func atk() -> int:
	return snapshot.stats.atk


func def() -> int:
	return snapshot.stats.def


func max_hp() -> int:
	return snapshot.stats.hp


func is_alive() -> bool:
	return hp > 0


## Trừ máu (kẹp về 0). Trả HP còn lại.
func apply_damage(amount: int) -> int:
	hp = 0 if amount >= hp else hp - amount
	return hp


## Hồi máu (kẹp về max_hp). Trả HP sau hồi.
func heal(amount: int) -> int:
	hp = mini(hp + amount, max_hp())
	return hp
