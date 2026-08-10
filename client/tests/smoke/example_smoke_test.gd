# Smoke test — Phase 03.
# Mục đích DUY NHẤT: chứng minh hạ tầng test hoạt động end-to-end
# (Godot 4.7 + gdUnit4 headless + CI + JUnit report). KHÔNG chứa gameplay logic.
#
# Ràng buộc (docs/testing/godot-testing.md): tất định — không network, không animation,
# không real-time/wall-clock. Feature test thật thêm ở các phase nhóm 3+.
extends GdUnitTestSuite


func test_framework_boots() -> void:
	# gdUnit4 chạy được và assert số nguyên (tất định).
	assert_int(2 + 2).is_equal(4)


func test_deterministic_identity() -> void:
	# Không phụ thuộc trạng thái ngoài — luôn cho cùng kết quả.
	assert_str("game-team").is_equal("game-team")
	assert_bool(true).is_true()
