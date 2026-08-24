# Test MainHubPresenter — Phase 20.
# Chứng minh: presenter ĐỌC profile từ StateCache → view hiển thị tên/level; nhãn offline khi cache;
# tự refresh khi có `state_refreshed`. Chỉ đọc-cache (KHÔNG network). Deps inject (stub) ⇒ tất định.
# (docs/testing/godot-testing.md, docs/godot/ui-architecture.md)
extends GdUnitTestSuite

const _PRESENTER := preload("res://src/ui/main_hub/main_hub_presenter.gd")


# View gián điệp: bắt data cuối cùng được render.
class _SpyView extends BaseView:
	var last: Dictionary = {}

	func _render(data: Dictionary) -> void:
		last = data.duplicate(true)


# StateCache giả (đọc-cache) — cấu hình profile/currency/offline.
class _StubStateCache extends Node:
	var profile: Dictionary = {}
	var currencies: Dictionary = {}
	var offline: bool = false

	func get_profile() -> Dictionary:
		return profile

	func get_currencies() -> Dictionary:
		return currencies

	func is_offline() -> bool:
		return offline


# ConfigProvider giả — nhãn config.
class _StubConfig extends Node:
	var label: String = "config@v1"

	func config_label() -> String:
		return label


func _make_view() -> _SpyView:
	var view := _SpyView.new()
	add_child(view)
	auto_free(view)
	return view


func _make_dep(node: Node) -> Node:
	add_child(node)
	auto_free(node)
	return node


func test_renders_profile_name_and_level_from_state_cache() -> void:
	var view := _make_view()
	var sc := _StubStateCache.new()
	sc.profile = {"displayName": "Guest", "level": 1}
	_make_dep(sc)
	var cfg := _make_dep(_StubConfig.new())
	var presenter = _PRESENTER.new(view, sc, cfg)  # refresh() chạy trong _init

	assert_str(str(view.last.get("profile_text"))).contains("Guest")
	assert_str(str(view.last.get("profile_text"))).contains("Lv.1")
	assert_bool(bool(view.last.get("offline"))).is_false()
	presenter.dispose()


func test_offline_label_when_cache_source() -> void:
	var view := _make_view()
	var sc := _StubStateCache.new()
	sc.profile = {"displayName": "Cached", "level": 5}
	sc.offline = true
	_make_dep(sc)
	var cfg := _make_dep(_StubConfig.new())
	var presenter = _PRESENTER.new(view, sc, cfg)

	assert_bool(bool(view.last.get("offline"))).is_true()
	assert_str(str(view.last.get("status_text"))).contains("offline")
	presenter.dispose()


func test_state_refreshed_event_rerenders_profile() -> void:
	var view := _make_view()
	var sc := _StubStateCache.new()  # profile rỗng ban đầu
	_make_dep(sc)
	var cfg := _make_dep(_StubConfig.new())
	var presenter = _PRESENTER.new(view, sc, cfg)
	assert_str(str(view.last.get("profile_text"))).is_equal("Chưa có profile")

	# Profile về (snapshot mới) → phát state_refreshed → presenter refresh lại.
	sc.profile = {"displayName": "NewGuy", "level": 2}
	EventBus.emit(&"state_refreshed", {"source": "server"})
	assert_str(str(view.last.get("profile_text"))).contains("NewGuy")
	assert_str(str(view.last.get("profile_text"))).contains("Lv.2")
	presenter.dispose()
