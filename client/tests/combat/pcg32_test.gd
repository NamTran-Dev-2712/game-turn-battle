# Test Pcg32 — Phase 25. Parity với server `Pcg32.cs` là kiểm tra QUAN TRỌNG NHẤT: mọi thứ hạ nguồn
# (damage/crit/turn) phụ thuộc chuỗi số trùng khít. Anchor = combat-framework.md §21.2.
# Tất định — không RNG toàn cục/wall-clock (docs/testing/godot-testing.md).
extends GdUnitTestSuite

const _BOUND: int = 10000


func test_seed_12345_anchor_rolls() -> void:
	var rng := Pcg32.new(12345)
	assert_int(rng.bounded(_BOUND)).is_equal(7329)
	assert_int(rng.bounded(_BOUND)).is_equal(4605)
	assert_int(rng.bounded(_BOUND)).is_equal(1261)
	assert_int(rng.bounded(_BOUND)).is_equal(2745)


func test_seed_999_anchor_rolls() -> void:
	var rng := Pcg32.new(999)
	assert_int(rng.bounded(_BOUND)).is_equal(8003)
	assert_int(rng.bounded(_BOUND)).is_equal(8884)
	assert_int(rng.bounded(_BOUND)).is_equal(2400)
	assert_int(rng.bounded(_BOUND)).is_equal(33)


func test_same_seed_same_stream() -> void:
	var a := Pcg32.new(42)
	var b := Pcg32.new(42)
	for i in 1000:
		assert_int(a.next_u32()).is_equal(b.next_u32())


func test_different_seed_diverges() -> void:
	var a := Pcg32.new(1)
	var b := Pcg32.new(2)
	var any_diff := false
	for i in 8:
		if a.next_u32() != b.next_u32():
			any_diff = true
	assert_bool(any_diff).is_true()


func test_bounded_in_range() -> void:
	var rng := Pcg32.new(7)
	for i in 500:
		var r := rng.bounded(_BOUND)
		assert_bool(r >= 0 and r < _BOUND).is_true()
