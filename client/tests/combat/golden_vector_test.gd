# Test golden vector — Phase 25/26. So client sim với ĐÁP ÁN server (shared/combat-vectors/*) theo
# TỪNG sự kiện + result (không chỉ HP cuối). Đây là cross-check §18 tự động hoá, tất định.
# Tự KHÁM PHÁ mọi vector trong shared/combat-vectors/ (thêm vector = KHÔNG sửa code test).
# Baseline sinh từ sim server (nguồn chân lý, ADR-011) — client phải khớp; lệch = FAIL (không sửa vector).
extends GdUnitTestSuite


func test_all_vectors_match_server_baseline() -> void:
	var files := CombatVectorLoader.list_vector_files()
	assert_int(files.size()).override_failure_message(
		"Không tìm thấy golden vector nào trong shared/combat-vectors/").is_greater(0)

	var failures: Array[String] = []
	for file in files:
		var vector := CombatVectorLoader.load_vector(file)
		var expected: Dictionary = vector["expected"]
		var out := BattleSimulator.new().simulate(CombatVectorLoader.build_input(vector["input"]))
		var diff_log := JsonDiff.first_difference(expected["event_log"], out["event_log"], "event_log")
		if diff_log != "":
			failures.append("%s -> %s" % [file, diff_log])
			continue
		var diff_result := JsonDiff.first_difference(expected["result"], out["result"], "result")
		if diff_result != "":
			failures.append("%s -> %s" % [file, diff_result])

	assert_bool(failures.is_empty()).override_failure_message(
		"Golden vector mismatch (client != baseline):\n  %s" % "\n  ".join(failures)).is_true()


func test_vector_01_first_damage_is_158() -> void:
	# Kiểm tra cụ thể đầu tiên: seq 6 = DamageApplied amount 158 (worked example §17).
	var out := BattleSimulator.new().simulate(CombatVectorLoader.load_input("vector_01_basic_hit.json"))
	var log: Array = out["event_log"]
	assert_str(str(log[6]["type"])).is_equal("DamageApplied")
	assert_int(int(log[6]["amount"])).is_equal(158)
