# Test CombatInputResolver — Phase 25. Chứng minh sim đọc chỉ số từ ConfigProvider (data-driven,
# KHÔNG hardcode): đổi atk trong config ⇒ đổi damage, không đổi code. config_version khớp ConfigProvider.
# Tất định — ConfigProvider trỏ cache tạm, không network thật.
extends GdUnitTestSuite

const _CONFIG_PROVIDER := preload("res://src/core/config/config_provider.gd")

var _cache_dir: String


func before_test() -> void:
	_cache_dir = "user://test_combat_config_%d" % Time.get_ticks_usec()


func after_test() -> void:
	_remove_dir_recursive(_cache_dir)


func test_config_version_matches_provider() -> void:
	var provider := _make_provider(_bundle(1, 200))
	var input := CombatInputResolver.new().resolve(_request(), provider)
	assert_str(input.config_version).is_equal("config@v1")


func test_atk_200_yields_damage_158() -> void:
	var provider := _make_provider(_bundle(1, 200))
	var input := CombatInputResolver.new().resolve(_request(), provider)
	var out := BattleSimulator.new().simulate(input)
	var first_damage := _first_damage(out["event_log"])
	assert_int(first_damage).is_equal(158)


func test_atk_400_yields_damage_316_data_driven() -> void:
	# Cùng code, chỉ đổi số trong config ⇒ damage đổi (data-driven, ADR-004).
	var provider := _make_provider(_bundle(2, 400))
	var input := CombatInputResolver.new().resolve(_request(), provider)
	var out := BattleSimulator.new().simulate(input)
	assert_int(_first_damage(out["event_log"])).is_equal(316)


func test_new_skill_via_config_only_runs() -> void:
	# Criterion C (ADR-004): thêm skill mới HOÀN TOÀN bằng config (damage + apply_buff self) gán cho hero
	# qua basic_skill_id ⇒ chạy đúng phía client, KHÔNG sửa code lõi (song ánh server).
	var provider := _make_provider(_bundle_with_config_skill())
	var input := CombatInputResolver.new().resolve(_request(), provider)
	var out := BattleSimulator.new().simulate(input)
	var has_damage := false
	var has_self_buff := false
	for e in out["event_log"]:
		var t := str(e.get("type", ""))
		if t == "DamageApplied":
			has_damage = true
		elif t == "BuffApplied" and str(e.get("unit", "")) == "u_ally_01" \
				and str(e.get("stat", "")) == "atk" and int(e.get("amount", 0)) == 50:
			has_self_buff = true
	assert_bool(has_damage).is_true()
	assert_bool(has_self_buff).is_true()


# ── Helpers ──────────────────────────────────────────────────────────────────────────────────────

func _make_provider(bundle: Dictionary) -> Node:
	var provider: Node = _CONFIG_PROVIDER.new()
	provider.cache_dir = _cache_dir
	add_child(provider)
	auto_free(provider)
	provider.apply_bundle(bundle)
	return provider


func _request() -> Dictionary:
	return {
		"seed": 12345,
		"stage_id": "stage_01",
		"ally": [{"actor_id": "u_ally_01", "hero_id": "hero_ally", "slot": 0}],
	}


# Đầu tiên tìm sự kiện DamageApplied để lấy amount.
func _first_damage(log: Array) -> int:
	for e in log:
		if str(e.get("type", "")) == "DamageApplied":
			return int(e["amount"])
	return -1


# Bundle với hero ally có atk tuỳ biến (chứng minh data-driven) + hero enemy + skill + stage(combat_rules).
func _bundle(version: int, ally_atk: int) -> Dictionary:
	return {
		"config_version": "config@v%d" % version,
		"schema_version": 1,
		"data": {
			"hero": {
				"hero_ally": {"id": "hero_ally", "base_stats": {"hp": 1000, "atk": ally_atk, "def": 100, "spd": 120}, "skills": ["skill_basic"]},
				"hero_enemy": {"id": "hero_enemy", "base_stats": {"hp": 500, "atk": 150, "def": 80, "spd": 90}, "skills": ["skill_basic"]},
			},
			"skill": {
				"skill_basic": {"id": "skill_basic", "target": "single_enemy", "trigger": {"type": "cooldown", "value": 0}, "effects": [{"effect_type": "damage", "params": {"coeff_fixed": 1000}}]},
			},
			"stage": {
				"stage_01": {
					"id": "stage_01",
					"max_rounds": 30,
					"basic_skill_id": "skill_basic",
					"combat_rules": {
						"def_constant_k": 300,
						"min_damage": 1,
						"crit_multiplier_fixed": 1500,
						"accuracy_bp": 10000,
						"crit_rate_bp": 0,
						"max_rounds": 30,
						"energy": {"initial": 0, "on_attack": 0, "on_hit": 0, "ultimate_cost": 100, "max": 100},
					},
					"enemies": [{"hero_id": "hero_enemy", "slot": 0}],
				},
			},
		},
	}


# Bundle: hero_ally gán basic_skill_id = skill_warblade (khai báo CHỈ trong config) = damage + apply_buff self.
func _bundle_with_config_skill() -> Dictionary:
	return {
		"config_version": "config@v1",
		"schema_version": 1,
		"data": {
			"hero": {
				"hero_ally": {"id": "hero_ally", "base_stats": {"hp": 1000, "atk": 200, "def": 100, "spd": 120}, "skills": ["skill_warblade"]},
				"hero_enemy": {"id": "hero_enemy", "base_stats": {"hp": 500, "atk": 150, "def": 80, "spd": 90}, "skills": ["skill_basic"]},
			},
			"skill": {
				"skill_basic": {"id": "skill_basic", "target": "single_enemy", "trigger": {"type": "cooldown", "value": 0}, "effects": [{"effect_type": "damage", "params": {"coeff_fixed": 1000}}]},
				"skill_warblade": {
					"id": "skill_warblade",
					"coeff_fixed": 1000,
					"target_rule": "single_enemy",
					"effects": [
						{"effect_type": "damage", "params": {"coeff_fixed": 1000}},
						{"effect_type": "apply_buff", "target": "self", "params": {"atk": 50, "duration": 3}},
					],
				},
			},
			"stage": {
				"stage_01": {
					"id": "stage_01",
					"max_rounds": 30,
					"basic_skill_id": "skill_basic",
					"combat_rules": {
						"def_constant_k": 300,
						"min_damage": 1,
						"crit_multiplier_fixed": 1500,
						"accuracy_bp": 10000,
						"crit_rate_bp": 0,
						"max_rounds": 30,
						"energy": {"initial": 0, "on_attack": 0, "on_hit": 0, "ultimate_cost": 100, "max": 100},
					},
					"enemies": [{"hero_id": "hero_enemy", "slot": 0}],
				},
			},
		},
	}


func _remove_dir_recursive(path: String) -> void:
	if not DirAccess.dir_exists_absolute(path):
		return
	var dir := DirAccess.open(path)
	if dir == null:
		return
	dir.list_dir_begin()
	var name := dir.get_next()
	while name != "":
		var full := path.path_join(name)
		if dir.current_is_dir():
			_remove_dir_recursive(full)
		else:
			DirAccess.remove_absolute(full)
		name = dir.get_next()
	dir.list_dir_end()
	DirAccess.remove_absolute(path)
