class_name CombatUnitState
## Trạng thái **biến đổi** của một unit trong trận (khớp server `State/UnitState.cs`). HP kẹp ≥ 0; `atk`/
## `def`/`spd` là chỉ số **hiệu dụng** = nền + tổng modifier (buff/debuff, §23), kẹp ≥ 0. Năng lượng +
## hồi chiêu ultimate (§15). Modifier khoá theo (source_skill_id, stat): áp lại ⇒ refresh (không chồng).
extends RefCounted

# Thứ hạng chỉ số để sắp thứ tự BuffExpired tất định (atk<def<spd) — khớp server enum StatKind.
const STAT_RANK: Dictionary = {"atk": 0, "def": 1, "spd": 2}

var snapshot: UnitSnapshot = null
var hp: int = 0
var energy: int = 0
var ultimate_cooldown_remaining: int = 0

# Modifier đang hoạt động: mảng Dictionary {source, stat, amount (có dấu), remaining}.
var _modifiers: Array = []


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


func skills() -> UnitSkillSet:
	return snapshot.skills


func spd() -> int:
	return _effective(snapshot.stats.spd, "spd")


func atk() -> int:
	return _effective(snapshot.stats.atk, "atk")


func def() -> int:
	return _effective(snapshot.stats.def, "def")


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


## Cộng năng lượng (kẹp [0, max]). Trả `true` nếu giá trị đổi.
func add_energy(amount: int, max_energy: int) -> bool:
	var before := energy
	var next := energy + amount
	if next < 0:
		next = 0
	if next > max_energy:
		next = max_energy
	energy = next
	return energy != before


## Tiêu năng lượng (kẹp ≥ 0). Trả `true` nếu giá trị đổi.
func spend_energy(amount: int) -> bool:
	var before := energy
	var next := energy - amount
	energy = 0 if next < 0 else next
	return energy != before


## Đặt hồi chiêu ultimate (số vòng, kẹp ≥ 0).
func set_ultimate_cooldown(rounds: int) -> void:
	ultimate_cooldown_remaining = 0 if rounds < 0 else rounds


## Giảm hồi chiêu ultimate 1 vòng (không xuống dưới 0).
func tick_ultimate_cooldown() -> void:
	if ultimate_cooldown_remaining > 0:
		ultimate_cooldown_remaining -= 1


## Áp/refresh một modifier chỉ số. Khoá = (source_skill_id, stat): đã có ⇒ thay amount + remaining.
func apply_stat_modifier(source_skill_id: String, stat: String, signed_amount: int, duration_rounds: int) -> void:
	for m in _modifiers:
		if m["stat"] == stat and m["source"] == source_skill_id:
			m["amount"] = signed_amount
			m["remaining"] = duration_rounds
			return
	_modifiers.append({"source": source_skill_id, "stat": stat, "amount": signed_amount, "remaining": duration_rounds})


## Giảm 1 vòng mọi modifier; trả danh sách đã gỡ (sắp: stat rank, rồi source ordinal) để phát BuffExpired.
func tick_modifiers() -> Array:
	var expired: Array = []
	var kept: Array = []
	for m in _modifiers:
		m["remaining"] -= 1
		if m["remaining"] <= 0:
			expired.append(m)
		else:
			kept.append(m)
	_modifiers = kept
	expired.sort_custom(_expired_before)
	return expired


func _expired_before(a: Dictionary, b: Dictionary) -> bool:
	var ra: int = STAT_RANK.get(a["stat"], 0)
	var rb: int = STAT_RANK.get(b["stat"], 0)
	if ra != rb:
		return ra < rb
	return a["source"] < b["source"]


func _effective(base_value: int, stat: String) -> int:
	var total := base_value
	for m in _modifiers:
		if m["stat"] == stat:
			total += int(m["amount"])
	return 0 if total < 0 else total
