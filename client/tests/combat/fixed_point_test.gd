# Test FixedPoint + công thức damage — Phase 25. Song ánh worked example server (§17) & bảng
# round-half-up. KHÔNG float. Tất định.
extends GdUnitTestSuite


func test_worked_example_vector_01() -> void:
	# atk 200, coeff 1000, K 300, def 80 → dmg 158 (combat-framework.md §17).
	assert_int(FixedPoint.div(300000, 380000)).is_equal(789)
	assert_int(FixedPoint.mul(200000, 789)).is_equal(157800)
	assert_int(FixedPoint.from_fixed(157800)).is_equal(158)


func test_worked_example_vector_02_crit() -> void:
	# atk 260, def 50, K 300, crit ×1.5 → 334.
	assert_int(FixedPoint.div(300000, 350000)).is_equal(857)
	assert_int(FixedPoint.mul(260000, 857)).is_equal(222820)
	assert_int(FixedPoint.mul(222820, 1500)).is_equal(334230)
	assert_int(FixedPoint.from_fixed(334230)).is_equal(334)


func test_round_half_up_table() -> void:
	assert_int(FixedPoint.from_fixed(157800)).is_equal(158)
	assert_int(FixedPoint.from_fixed(157500)).is_equal(158) # đúng .5 → lên
	assert_int(FixedPoint.from_fixed(157499)).is_equal(157)
	assert_int(FixedPoint.from_fixed(157400)).is_equal(157)


func test_to_from_fixed_roundtrip() -> void:
	assert_int(FixedPoint.to_fixed(200)).is_equal(200000)
	assert_int(FixedPoint.from_fixed(FixedPoint.to_fixed(200))).is_equal(200)


func test_clamp() -> void:
	assert_int(FixedPoint.clamp_int(0, 1, 999)).is_equal(1)
	assert_int(FixedPoint.clamp_int(500, 1, 999)).is_equal(500)
	assert_int(FixedPoint.clamp_int(1500, 1, 999)).is_equal(999)


func test_compute_damage_end_to_end() -> void:
	var rules := CombatRules.new()
	rules.def_constant_k = 300
	rules.min_damage = 1
	rules.crit_multiplier_fixed = 1500
	# vector_01 ally→enemy (no crit): 158. enemy→ally: 113.
	assert_int(DamageEffectHandler.compute_damage(200, 80, 1000, false, rules)).is_equal(158)
	assert_int(DamageEffectHandler.compute_damage(150, 100, 1000, false, rules)).is_equal(113)
	# vector_02 ally→enemy (crit): 334. enemy→ally (crit): 169.
	assert_int(DamageEffectHandler.compute_damage(260, 50, 1000, true, rules)).is_equal(334)
	assert_int(DamageEffectHandler.compute_damage(150, 100, 1000, true, rules)).is_equal(169)


func test_min_damage_floor() -> void:
	var rules := CombatRules.new()
	rules.def_constant_k = 300
	rules.min_damage = 1
	rules.crit_multiplier_fixed = 1500
	# atk 1 vs def khổng lồ → sàn 1.
	assert_int(DamageEffectHandler.compute_damage(1, 1000000, 1000, false, rules)).is_equal(1)
