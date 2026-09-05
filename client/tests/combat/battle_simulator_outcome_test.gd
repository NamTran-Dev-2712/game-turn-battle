# Test nhánh kết quả & bất biến RNG/thứ tự — Phase 25. Song ánh server `BattleSimulatorOutcomeTests`:
# DEFEAT, DRAW ở max_rounds, miss tiêu thụ đúng 1 roll (không crit roll), turn order theo spd rồi
# actor_id. Tất định.
extends GdUnitTestSuite


func test_defeat_when_ally_wiped() -> void:
	var ally: Array[UnitSnapshot] = [_unit("u_ally", "ally", 0, 100, 10, 10, 50)]
	var enemy: Array[UnitSnapshot] = [_unit("u_enemy", "enemy", 0, 1000, 500, 100, 200)]
	var out := _simulate(_make_input(1, ally, enemy, _rules(10000, 0, 30)))
	assert_str(str(out["result"]["outcome"])).is_equal("DEFEAT")
	assert_str(str(out["result"]["winner_team"])).is_equal("enemy")


func test_draw_at_max_rounds() -> void:
	var ally: Array[UnitSnapshot] = [_unit("u_ally", "ally", 0, 1000, 1, 1000, 100)]
	var enemy: Array[UnitSnapshot] = [_unit("u_enemy", "enemy", 0, 1000, 1, 1000, 90)]
	var out := _simulate(_make_input(1, ally, enemy, _rules(10000, 0, 1)))
	assert_str(str(out["result"]["outcome"])).is_equal("DRAW")
	assert_object(out["result"]["winner_team"]).is_null()
	assert_int(int(out["result"]["rounds"])).is_equal(1)


func test_miss_consumes_one_roll_no_crit_roll() -> void:
	# accuracy_bp=0 ⇒ luôn miss ⇒ mỗi hành động đúng 1 hit-roll, 0 crit-roll, 0 damage.
	var ally: Array[UnitSnapshot] = [_unit("u_ally", "ally", 0, 100, 50, 10, 100)]
	var enemy: Array[UnitSnapshot] = [_unit("u_enemy", "enemy", 0, 100, 50, 10, 90)]
	var out := _simulate(_make_input(1, ally, enemy, _rules(0, 0, 1)))
	var log: Array = out["event_log"]
	var hit_rolls := 0
	var crit_rolls := 0
	var damages := 0
	for e in log:
		if str(e.get("type", "")) == "RandomRoll":
			if str(e.get("purpose", "")) == "hit":
				hit_rolls += 1
			elif str(e.get("purpose", "")) == "crit":
				crit_rolls += 1
		elif str(e.get("type", "")) == "DamageApplied":
			damages += 1
	assert_int(crit_rolls).is_equal(0)
	assert_int(damages).is_equal(0)
	assert_bool(hit_rolls > 0).is_true()


func test_turn_order_spd_then_actor_id() -> void:
	# spd cao đi trước: enemy spd 200 hành động trước ally spd 50.
	var ally: Array[UnitSnapshot] = [_unit("a_ally", "ally", 0, 100, 10, 10, 50)]
	var enemy: Array[UnitSnapshot] = [_unit("z_enemy", "enemy", 0, 100, 10, 10, 200)]
	var out := _simulate(_make_input(1, ally, enemy, _rules(10000, 0, 1)))
	var log: Array = out["event_log"]
	assert_str(str(log[0]["type"])).is_equal("RoundStarted")
	assert_str(str(log[1]["type"])).is_equal("ActionStarted")
	assert_str(str(log[1]["actor"])).is_equal("z_enemy")


func test_turn_order_tiebreak_actor_id_ascending() -> void:
	# spd bằng nhau ⇒ actor_id nhỏ hơn đi trước ("a_ally" < "z_enemy").
	var ally: Array[UnitSnapshot] = [_unit("a_ally", "ally", 0, 100, 10, 10, 100)]
	var enemy: Array[UnitSnapshot] = [_unit("z_enemy", "enemy", 0, 100, 10, 10, 100)]
	var out := _simulate(_make_input(1, ally, enemy, _rules(10000, 0, 1)))
	var log: Array = out["event_log"]
	assert_str(str(log[1]["actor"])).is_equal("a_ally")


# ── Helpers ──────────────────────────────────────────────────────────────────────────────────────

func _simulate(input: BattleInput) -> Dictionary:
	return BattleSimulator.new().simulate(input)


func _rules(accuracy_bp: int, crit_rate_bp: int, max_rounds: int) -> CombatRules:
	var r := CombatRules.new()
	r.def_constant_k = 300
	r.min_damage = 1
	r.crit_multiplier_fixed = 1500
	r.accuracy_bp = accuracy_bp
	r.crit_rate_bp = crit_rate_bp
	r.max_rounds = max_rounds
	r.energy = EnergyRules.new()
	return r


func _unit(id: String, team: String, slot: int, hp: int, atk: int, def: int, spd: int) -> UnitSnapshot:
	var s := UnitStats.new()
	s.hp = hp
	s.atk = atk
	s.def = def
	s.spd = spd
	var u := UnitSnapshot.new()
	u.actor_id = id
	u.hero_id = "hero_test"
	u.team = team
	u.slot = slot
	u.stats = s
	return u


func _make_input(seed_value: int, ally: Array[UnitSnapshot], enemy: Array[UnitSnapshot], rules: CombatRules) -> BattleInput:
	var effects: Array[EffectDef] = [EffectDef.make("damage")]
	var i := BattleInput.new()
	i.config_version = "config@v1"
	i.seed = seed_value
	i.stage = StageInfo.make("stage_test", rules.max_rounds)
	i.ally = ally
	i.enemy = enemy
	i.rules = rules
	i.basic_skill = SkillDef.make("skill_basic", 1000, "default", effects)
	return i
