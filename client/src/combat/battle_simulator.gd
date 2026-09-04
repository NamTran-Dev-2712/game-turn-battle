class_name BattleSimulator
## Sim combat **thuần, xác định** phía client (combat-framework.md §12–§19, ADR-011). Hiện thực đúng
## spec phase 23 và song ánh bit-for-bit với server `GameTeam.Domain/Combat/BattleSimulator.cs`
## (phase 24 = đáp án). Cùng `(config_version, team, stage, seed)` ⇒ **cùng** `event_log` + `result`.
##
## LƯU Ý (ADR-011): client sim CHỈ để **hiển thị/replay/dự đoán**, KHÔNG phải chân lý — không quyết
## kết quả/phần thưởng. Lõi thuần: không Node/scene/UI/network/wall-clock, không `float`, không RNG
## toàn cục (seed truyền tường minh vào [Pcg32]).
extends RefCounted

const TEAM_ALLY: String = "ally"
const TEAM_ENEMY: String = "enemy"
const ROLL_BOUND: int = 10000


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
		log.append(CombatEvents.round_started(round_no))

		for actor in _build_action_order(all):
			if not actor.is_alive():
				continue # chết ở lượt trước trong vòng ⇒ bỏ lượt
			if not _has_living_enemy(all, actor):
				break # hết địch ⇒ ngừng duyệt
			_execute_attack(actor, all, input.basic_skill, input.rules, rng, registry, log)
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


# ── Thứ tự lượt & mục tiêu (§13/§14) ────────────────────────────────────────────────────────────────

# Sắp xếp toàn bộ unit theo (-spd, actor_id ordinal asc). Comparator là thứ tự toàn phần (actor_id
# duy nhất) ⇒ kết quả xác định không phụ thuộc tính ổn định của sort.
func _build_action_order(all: Array[CombatUnitState]) -> Array[CombatUnitState]:
	var order: Array[CombatUnitState] = all.duplicate()
	order.sort_custom(_action_order_before)
	return order


func _action_order_before(a: CombatUnitState, b: CombatUnitState) -> bool:
	if a.spd() != b.spd():
		return a.spd() > b.spd() # spd cao đi trước
	return a.actor_id() < b.actor_id() # tie-break: actor_id ordinal tăng dần


# Mục tiêu: unit sống ở đội đối phương, sắp (slot asc, actor_id asc), lấy đầu tiên. null nếu không có.
func _resolve_target(actor: CombatUnitState, all: Array[CombatUnitState]) -> CombatUnitState:
	var candidates: Array[CombatUnitState] = []
	for u in all:
		if u.team() != actor.team() and u.is_alive():
			candidates.append(u)
	if candidates.is_empty():
		return null
	candidates.sort_custom(_target_before)
	return candidates[0]


func _target_before(a: CombatUnitState, b: CombatUnitState) -> bool:
	if a.slot() != b.slot():
		return a.slot() < b.slot()
	return a.actor_id() < b.actor_id()


func _has_living_enemy(all: Array[CombatUnitState], actor: CombatUnitState) -> bool:
	for u in all:
		if u.team() != actor.team() and u.is_alive():
			return true
	return false


# ── Thực thi hành động (§16/§17) ────────────────────────────────────────────────────────────────────

func _execute_attack(
	actor: CombatUnitState,
	all: Array[CombatUnitState],
	skill: SkillDef,
	rules: CombatRules,
	rng: Pcg32,
	registry: EffectRegistry,
	log: Array,
) -> void:
	log.append(CombatEvents.action_started(actor.actor_id()))

	var target := _resolve_target(actor, all)
	if target == null:
		log.append(CombatEvents.action_completed(actor.actor_id()))
		return
	log.append(CombatEvents.target_selected(actor.actor_id(), target.actor_id()))

	# Hit roll — LUÔN tiêu thụ đúng 1 lần (miss ⇒ dừng, KHÔNG roll crit).
	var hit_roll := rng.bounded(ROLL_BOUND)
	log.append(CombatEvents.random_roll("hit", ROLL_BOUND, hit_roll))
	if hit_roll >= rules.accuracy_bp:
		log.append(CombatEvents.miss(actor.actor_id(), target.actor_id()))
		log.append(CombatEvents.action_completed(actor.actor_id()))
		return
	log.append(CombatEvents.hit(actor.actor_id(), target.actor_id()))

	# Crit roll — LUÔN tiêu thụ đúng 1 lần khi đã Hit (kể cả crit_rate_bp==0) ⇒ không lệch stream.
	var crit_roll := rng.bounded(ROLL_BOUND)
	log.append(CombatEvents.random_roll("crit", ROLL_BOUND, crit_roll))
	var is_crit := crit_roll < rules.crit_rate_bp
	if is_crit:
		log.append(CombatEvents.crit(actor.actor_id(), target.actor_id()))

	for effect_def in skill.effects:
		var handler := registry.resolve(effect_def.effect_type)
		var ctx := EffectContext.new(actor, target, skill, effect_def, rules, is_crit, log)
		handler.apply(ctx)

	log.append(CombatEvents.action_completed(actor.actor_id()))


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
