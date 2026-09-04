# Test determinism — Phase 25. Cùng input chạy N lần ⇒ output trùng khít (byte qua JSON.stringify);
# đổi seed ⇒ output khác. Tiêu chí hoàn thành cốt lõi (cùng seed → cùng output).
extends GdUnitTestSuite

const _RUNS: int = 100


func test_n_runs_identical() -> void:
	var input := CombatVectorLoader.load_input("vector_01_basic_hit.json")
	var baseline := JSON.stringify(BattleSimulator.new().simulate(input))
	for i in _RUNS:
		var again := JSON.stringify(BattleSimulator.new().simulate(input))
		assert_str(again).is_equal(baseline)


func test_different_seed_differs() -> void:
	var base_input := CombatVectorLoader.load_input("vector_01_basic_hit.json")
	var base := JSON.stringify(BattleSimulator.new().simulate(base_input))
	var other_input := CombatVectorLoader.load_input("vector_01_basic_hit.json")
	other_input.seed = base_input.seed + 1
	var other := JSON.stringify(BattleSimulator.new().simulate(other_input))
	assert_str(other).is_not_equal(base)
