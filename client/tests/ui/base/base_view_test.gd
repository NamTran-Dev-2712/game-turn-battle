# Test BaseView — Phase 17.
# Chứng minh hợp đồng UI: DỮ LIỆU VÀO (`set_data`→`_render`) và Ý ĐỊNH RA (`emit_intent`→signal
# `intent`). View thuần hiển thị/phát ý định — không network. Tất định (docs/testing/godot-testing.md).
extends GdUnitTestSuite


func test_set_data_triggers_render() -> void:
	var view := _SpyView.new()
	add_child(view)
	auto_free(view)
	view.set_data({"a": 1})
	assert_int(view.rendered.size()).is_equal(1)
	assert_dict(view.rendered[0]).contains_key_value("a", 1)


func test_emit_intent_fires_intent_signal_with_name_and_payload() -> void:
	var view := _SpyView.new()
	add_child(view)
	auto_free(view)
	var received: Array = []
	view.intent.connect(func(n, p) -> void: received.append([n, p]))
	view.emit_intent(&"retry", {"x": 2})
	assert_int(received.size()).is_equal(1)
	assert_str(String(received[0][0])).is_equal("retry")
	assert_dict(received[0][1]).contains_key_value("x", 2)


func test_emit_intent_default_payload_is_empty_dict() -> void:
	var view := _SpyView.new()
	add_child(view)
	auto_free(view)
	var received: Array = []
	view.intent.connect(func(_n, p) -> void: received.append(p))
	view.emit_intent(&"open")
	assert_int(received.size()).is_equal(1)
	assert_dict(received[0]).is_empty()


# View giả ghi lại mỗi lần `_render` được gọi (chứng minh set_data → _render).
class _SpyView extends BaseView:
	var rendered: Array = []

	func _render(data: Dictionary) -> void:
		rendered.append(data)
