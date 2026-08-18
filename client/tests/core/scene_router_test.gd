# Test SceneRouter — Phase 14.
# Kịch bản goto A → goto B → back → A: scene thực sự đổi, back stack đúng, scene cũ
# được giải phóng (không giữ tham chiếu), và phát `scene_changed` qua EventBus.
# Kiểm qua autoload thật; reset trạng thái mỗi test để cô lập.
# Tất định — chỉ chờ theo FRAME (không wall-clock) (docs/testing/godot-testing.md).
extends GdUnitTestSuite

const _PATH_A: String = "res://tests/core/fixtures/scene_a.tscn"
const _PATH_B: String = "res://tests/core/fixtures/scene_b.tscn"

var _changes: Array = []


func before_test() -> void:
	_changes = []
	_reset_router()
	EventBus.subscribe(&"scene_changed", _on_scene_changed)


func after_test() -> void:
	EventBus.unsubscribe(&"scene_changed", _on_scene_changed)
	_reset_router()


func _on_scene_changed(payload) -> void:
	_changes.append(payload)


func _reset_router() -> void:
	SceneRouter.clear_history()
	if SceneRouter.current_scene != null:
		SceneRouter.current_scene.free()
		SceneRouter.current_scene = null
	SceneRouter.current_path = ""


func test_goto_and_back_navigates_scene_stack() -> void:
	assert_bool(SceneRouter.goto_scene(_PATH_A)).is_true()
	assert_str(SceneRouter.current_path).is_equal(_PATH_A)
	assert_str(String(SceneRouter.current_scene.name)).is_equal("SceneA")
	assert_int(SceneRouter.stack_depth()).is_equal(0)

	var scene_a: Node = SceneRouter.current_scene

	assert_bool(SceneRouter.goto_scene(_PATH_B)).is_true()
	assert_str(SceneRouter.current_path).is_equal(_PATH_B)
	assert_str(String(SceneRouter.current_scene.name)).is_equal("SceneB")
	assert_int(SceneRouter.stack_depth()).is_equal(1)

	# Scene A cũ được queue_free — sau một frame không còn hợp lệ (không rò rỉ).
	await get_tree().process_frame
	assert_bool(is_instance_valid(scene_a)).is_false()

	assert_bool(SceneRouter.back()).is_true()
	assert_str(SceneRouter.current_path).is_equal(_PATH_A)
	assert_str(String(SceneRouter.current_scene.name)).is_equal("SceneA")
	assert_int(SceneRouter.stack_depth()).is_equal(0)


func test_back_on_empty_stack_returns_false() -> void:
	assert_bool(SceneRouter.goto_scene(_PATH_A)).is_true()
	assert_bool(SceneRouter.back()).is_false()
	assert_str(SceneRouter.current_path).is_equal(_PATH_A)


func test_goto_invalid_path_returns_false_and_keeps_scene() -> void:
	assert_bool(SceneRouter.goto_scene(_PATH_A)).is_true()
	assert_bool(SceneRouter.goto_scene("res://tests/core/fixtures/does_not_exist.tscn")).is_false()
	# Scene hiện tại và stack không đổi khi path lỗi.
	assert_str(SceneRouter.current_path).is_equal(_PATH_A)
	assert_int(SceneRouter.stack_depth()).is_equal(0)


func test_navigation_emits_scene_changed() -> void:
	SceneRouter.goto_scene(_PATH_A)
	SceneRouter.goto_scene(_PATH_B)
	assert_int(_changes.size()).is_equal(2)
	assert_dict(_changes[0]).contains_key_value("to", _PATH_A)
	assert_dict(_changes[1]).contains_key_value("to", _PATH_B)
	assert_dict(_changes[1]).contains_key_value("from", _PATH_A)
