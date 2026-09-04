# Test golden vector — Phase 25. So client sim với ĐÁP ÁN server (shared/combat-vectors/*) theo
# TỪNG sự kiện + result (không chỉ HP cuối). Đây là cross-check §18 tự động hoá. Tất định.
extends GdUnitTestSuite


func test_vector_01_basic_hit_matches() -> void:
	_assert_vector("vector_01_basic_hit.json")


func test_vector_02_crit_ko_matches() -> void:
	_assert_vector("vector_02_crit_ko.json")


func test_vector_01_first_damage_is_158() -> void:
	# Kiểm tra cụ thể đầu tiên: seq 6 = DamageApplied amount 158 (worked example §17).
	var out := _run("vector_01_basic_hit.json")
	var log: Array = out["event_log"]
	assert_str(str(log[6]["type"])).is_equal("DamageApplied")
	assert_int(int(log[6]["amount"])).is_equal(158)


func _run(file: String) -> Dictionary:
	var input := CombatVectorLoader.load_input(file)
	return BattleSimulator.new().simulate(input)


func _assert_vector(file: String) -> void:
	var vector := CombatVectorLoader.load_vector(file)
	var expected: Dictionary = vector["expected"]
	var out := _run(file)
	var diff_log := JsonDiff.first_difference(expected["event_log"], out["event_log"], "event_log")
	assert_str(diff_log).is_equal("")
	var diff_result := JsonDiff.first_difference(expected["result"], out["result"], "result")
	assert_str(diff_result).is_equal("")
