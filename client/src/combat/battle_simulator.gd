class_name BattleSimulator
## Sim combat **thuần, xác định** phía client (combat-framework.md §12–§19, §23, ADR-011). Hiện thực đúng
## spec và song ánh bit-for-bit với server `GameTeam.Domain/Combat/BattleSimulator.cs` (đáp án). Cùng
## `(config_version, team, stage, seed)` ⇒ **cùng** `event_log` + `result`. Skill/effect data-driven: mỗi
## unit chọn basic/ultimate theo năng lượng+hồi chiêu (§15); buff/debuff áp qua modifier chỉ số (§23).
##
## LƯU Ý (ADR-011): client sim CHỈ để **hiển thị/replay/dự đoán**, KHÔNG phải chân lý — không quyết
## kết quả/phần thưởng. Lõi thuần: không Node/scene/UI/network/wall-clock, không `float`, không RNG
## toàn cục (seed truyền tường minh vào [Pcg32]).
extends RefCounted

const TEAM_ALLY: String = "ally"
const TEAM_ENEMY: String = "enemy"
const ROLL_BOUND: int = 10000
const TARGET_SINGLE_ALLY: String = "single_ally"
const TARGET_SELF: String = "self"


## Chạy trận. Trả `{ "event_log": Array[Dictionary], "result": Dictionary }` đúng golden format.
func simulate(input: BattleInput) -> Dictionary:
	var rng := Pcg32.new(input.seed)
	var registry := EffectRegistry.create_default()

	var allies: Array[CombatUnitState] = _build_units(input.ally, input.rules.energy.initial)
	var enemies: Array[CombatUnitState] = _build_units(input.enemy, input.rules.energy.initial)
	var all: Array[CombatUnitState] = []
	all.append_array(allies) # đồng minh trước, địch sau (thứ tự final_hp — §19)
	all.append_array(enemies)

	var log: Array = []
	var rounds_played := 0
	var ended := false

	for round_no in range(1, input.rules.max_rounds + 1):
		rounds_played = round_no
		_start_round(allies, enemies, round_no, log)

		for actor in _build_action_order(all):
			if not actor.is_alive():
				continue # chết ở lượt trước trong vòng ⇒ bỏ lượt
			if not _has_living_enemy(all, actor):
				break # hết địch ⇒ ngừng duyệt
			_execute_action(actor, all, input.basic_skill, input.rules, rng, registry, log)
			if _is_ended(allies, enemies):
				ended = true
				break

		log.append(CombatEvents.round_ended(round_no))
		if ended:
			break

	log.append(CombatEvents.battle_ended())
	var result := _build_result(allies, enemies, rounds_played)
	return _to_output(log, result)


# ── Dựng state ─────────────────────────────────────────────────────────────────────────────────────

func _build_units(snapshots: Array[UnitSnapshot], initial_energy: int) -> Array[CombatUnitState]:
	var states: Array[CombatUnitState] = []
	for snapshot in snapshots:
		states.append(CombatUnitState.new(snapshot, initial_energy))
	return states


# ── Đầu vòng: tick buff (BuffExpired) + hồi chiêu ultimate (§23) ─────────────────────────────────────

func _start_round(allies: Array[CombatUnitState], enemies: Array[CombatUnitState], round_no: int, log: Array) -> void:
	log.append(CombatEvents.round_started(round_no))
	for unit in _tick_order(allies, enemies):
		if not unit.is_alive():
			continue
		for expired in unit.tick_modifiers():
			log.append(CombatEvents.buff_expired(unit.actor_id(), expired["source"], expired["stat"]))
		unit.tick_ultimate_cooldown()


# Thứ tự tick tất định: đồng minh (slot asc, actor_id asc) rồi địch (slot asc, actor_id asc).
func _tick_order(allies: Array[CombatUnitState], enemies: Array[CombatUnitState]) -> Array:
	var a := allies.duplicate()
	a.sort_custom(_target_before)
	var e := enemies.duplicate()
	e.sort_custom(_target_before)
	var order: Array = []
	order.append_array(a)
	order.append_array(e)
	return order


# ── Thứ tự lượt & mục tiêu (§13/§14/§23) ────────────────────────────────────────────────────────────

# Sắp xếp toàn bộ unit theo (-spd, actor_id ordinal asc). Comparator là thứ tự toàn phần (actor_id
# duy nhất) ⇒ kết quả xác định không phụ thuộc tính ổn định của sort. spd() = hiệu dụng (gồm buff).
func _build_action_order(all: Array[CombatUnitState]) -> Array[CombatUnitState]:
	var order: Array[CombatUnitState] = all.duplicate()
	order.sort_custom(_action_order_before)
	return order


func _action_order_before(a: CombatUnitState, b: CombatUnitState) -> bool:
	if a.spd() != b.spd():
		return a.spd() > b.spd() # spd cao đi trước
	return a.actor_id() < b.actor_id() # tie-break: actor_id ordinal tăng dần


func _target_before(a: CombatUnitState, b: CombatUnitState) -> bool:
	if a.slot() != b.slot():
		return a.slot() < b.slot()
	return a.actor_id() < b.actor_id()


func _has_living_enemy(all: Array[CombatUnitState], actor: CombatUnitState) -> bool:
	for u in all:
		if u.team() != actor.team() and u.is_alive():
			return true
	return false


# Giải mục tiêu theo target rule (§23, tập tối thiểu): single_ally = đồng minh sống slot nhỏ nhất (gồm bản
# thân); self = chính actor; còn lại (kể cả default) = kẻ địch sống slot nhỏ nhất — tie-break kết bằng actor_id.
func _resolve_by_rule(actor: CombatUnitState, all: Array[CombatUnitState], rule: String) -> CombatUnitState:
	if rule == TARGET_SELF:
		return actor if actor.is_alive() else null
	var want_ally := rule == TARGET_SINGLE_ALLY
	var candidates: Array[CombatUnitState] = []
	for u in all:
		if u.is_alive() and ((u.team() == actor.team()) == want_ally):
			candidates.append(u)
	if candidates.is_empty():
		return null
	candidates.sort_custom(_target_before)
	return candidates[0]


# ── Thực thi hành động (§15/§16/§17/§23) ─────────────────────────────────────────────────────────────

func _execute_action(
	actor: CombatUnitState,
	all: Array[CombatUnitState],
	fallback_basic: SkillDef,
	rules: CombatRules,
	rng: Pcg32,
	registry: EffectRegistry,
	log: Array,
) -> void:
	var selection := _select_skill(actor, fallback_basic)
	var skill: SkillDef = selection[0]
	var is_ultimate: bool = selection[1]

	log.append(CombatEvents.action_started(actor.actor_id()))

	var primary := _resolve_by_rule(actor, all, skill.target_rule)
	if primary == null:
		log.append(CombatEvents.action_completed(actor.actor_id()))
		return
	log.append(CombatEvents.target_selected(actor.actor_id(), primary.actor_id()))

	var is_crit := false
	if _is_attack_skill(skill):
		# Hit roll — LUÔN tiêu thụ đúng 1 lần (miss ⇒ dừng, KHÔNG roll crit).
		var hit_roll := rng.bounded(ROLL_BOUND)
		log.append(CombatEvents.random_roll("hit", ROLL_BOUND, hit_roll))
		if hit_roll >= rules.accuracy_bp:
			log.append(CombatEvents.miss(actor.actor_id(), primary.actor_id()))
			log.append(CombatEvents.action_completed(actor.actor_id()))
			return
		log.append(CombatEvents.hit(actor.actor_id(), primary.actor_id()))

		# Crit roll — LUÔN tiêu thụ đúng 1 lần khi đã Hit (kể cả crit_rate_bp==0) ⇒ không lệch stream.
		var crit_roll := rng.bounded(ROLL_BOUND)
		log.append(CombatEvents.random_roll("crit", ROLL_BOUND, crit_roll))
		is_crit = crit_roll < rules.crit_rate_bp
		if is_crit:
			log.append(CombatEvents.crit(actor.actor_id(), primary.actor_id()))

	for effect_def in skill.effects:
		var rule: String = effect_def.target if effect_def.target != "" else skill.target_rule
		var target := _resolve_by_rule(actor, all, rule)
		if target == null:
			continue # không còn mục tiêu hợp lệ cho effect này ⇒ bỏ qua
		var ctx := EffectContext.new(actor, target, skill, effect_def, rules, is_crit, log)
		registry.resolve(effect_def.effect_type).apply(ctx)

	_update_attacker_energy(actor, skill, is_ultimate, rules, log)
	log.append(CombatEvents.action_completed(actor.actor_id()))


# Chọn skill lượt này (§15): ultimate nếu đủ năng lượng và hết hồi chiêu, ngược lại basic. Trả [skill, is_ultimate].
func _select_skill(actor: CombatUnitState, fallback_basic: SkillDef) -> Array:
	var skill_set := actor.skills()
	var basic: SkillDef = fallback_basic
	var ultimate: SkillDef = null
	if skill_set != null:
		basic = skill_set.basic
		ultimate = skill_set.ultimate
	if ultimate != null and actor.energy >= ultimate.energy_cost and actor.ultimate_cooldown_remaining == 0:
		return [ultimate, true]
	return [basic, false]


func _is_attack_skill(skill: SkillDef) -> bool:
	for effect_def in skill.effects:
		if effect_def.effect_type == DamageEffectHandler.TYPE_NAME:
			return true
	return false


# §15: ultimate tiêu năng lượng + đặt hồi chiêu; đòn thường nạp on_attack. Phát EnergyChanged khi giá trị đổi.
func _update_attacker_energy(actor: CombatUnitState, skill: SkillDef, is_ultimate: bool, rules: CombatRules, log: Array) -> void:
	if is_ultimate:
		if actor.spend_energy(skill.energy_cost):
			log.append(CombatEvents.energy_changed(actor.actor_id(), actor.energy))
		actor.set_ultimate_cooldown(skill.cooldown_rounds)
	elif actor.add_energy(rules.energy.on_attack, rules.energy.max):
		log.append(CombatEvents.energy_changed(actor.actor_id(), actor.energy))


# ── Điều kiện kết thúc & kết quả (§19) ───────────────────────────────────────────────────────────────

func _is_ended(allies: Array[CombatUnitState], enemies: Array[CombatUnitState]) -> bool:
	return not _any_alive(allies) or not _any_alive(enemies)


func _any_alive(units: Array[CombatUnitState]) -> bool:
	for u in units:
		if u.is_alive():
			return true
	return false


func _build_result(allies: Array[CombatUnitState], enemies: Array[CombatUnitState], rounds_played: int) -> Dictionary:
	var ally_alive := _any_alive(allies)
	var enemy_alive := _any_alive(enemies)

	var outcome := "DRAW"
	var winner_team: Variant = null
	if not enemy_alive and ally_alive:
		outcome = "VICTORY"
		winner_team = TEAM_ALLY
	elif not ally_alive and enemy_alive:
		outcome = "DEFEAT"
		winner_team = TEAM_ENEMY

	var final_hp: Dictionary = {} # allies trước, enemies sau (Dictionary giữ thứ tự chèn — §19)
	for u in allies:
		final_hp[u.actor_id()] = u.hp
	for u in enemies:
		final_hp[u.actor_id()] = u.hp

	return {
		"outcome": outcome,
		"winner_team": winner_team,
		"rounds": rounds_played,
		"final_hp": final_hp,
	}


# Đóng dấu seq theo vị trí (0..n-1) — khớp server (serializer ghi seq = chỉ số list).
func _to_output(log: Array, result: Dictionary) -> Dictionary:
	var out_log: Array = []
	for i in log.size():
		var e: Dictionary = {"seq": i}
		e.merge(log[i])
		out_log.append(e)
	return {"event_log": out_log, "result": result}
