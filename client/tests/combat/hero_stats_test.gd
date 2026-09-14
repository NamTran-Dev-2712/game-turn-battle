# Test HeroStats (Phase 35) — công thức chỉ số theo cấp + Power Rating: tất định, integer (ADR-011),
# data-driven. Các giá trị kỳ vọng KHỚP server `HeroStatCalculatorTests.cs` (bit-parity giữa hai bên).
extends GdUnitTestSuite

const _HERO_STATS := preload("res://src/shared/hero_stats.gd")


func test_scale_stat_at_level_1_returns_base() -> void:
	assert_int(_HERO_STATS.scale_stat(100, 1, 800)).is_equal(100)
	assert_int(_HERO_STATS.scale_stat(1000, 1, 0)).is_equal(1000)


func test_scale_stat_applies_config_growth_matching_server() -> void:
	# base + round_half_up(base * bp * (level-1) / 10000) — trùng InlineData server.
	assert_int(_HERO_STATS.scale_stat(100, 2, 800)).is_equal(108)
	assert_int(_HERO_STATS.scale_stat(100, 3, 800)).is_equal(116)
	assert_int(_HERO_STATS.scale_stat(7, 2, 800)).is_equal(8)     # round-half-up (0.56 → 1)
	assert_int(_HERO_STATS.scale_stat(200, 5, 1000)).is_equal(280)


func test_power_is_weighted_integer_sum() -> void:
	var weights := {"hp": 1, "atk": 10, "def": 8, "spd": 6}
	var stats := {"hp": 1000, "atk": 200, "def": 100, "spd": 120}
	# 1000 + 2000 + 800 + 720 = 4520 (khớp server).
	assert_int(_HERO_STATS.power(stats, weights)).is_equal(4520)


func test_max_level_and_level_up_cost_from_curve() -> void:
	var economy := {"cost_curves": {"level_up": [100, 150, 220]}}
	assert_int(_HERO_STATS.max_level(economy)).is_equal(4)
	assert_int(_HERO_STATS.level_up_cost(economy, 1)).is_equal(100)
	assert_int(_HERO_STATS.level_up_cost(economy, 3)).is_equal(220)
	assert_int(_HERO_STATS.level_up_cost(economy, 4)).is_equal(-1)  # đã max cấp


func test_max_level_is_1_without_curve() -> void:
	assert_int(_HERO_STATS.max_level({})).is_equal(1)
	assert_int(_HERO_STATS.level_up_cost({}, 1)).is_equal(-1)
