# Test HeroListPresenter — Phase 22 (màn mẫu config e2e).
# Chứng minh vòng: bundle (mock ở BOUNDARY mạng) → ConfigProvider THẬT index → presenter query →
# view hiển thị. Cover: nhận→query→hiển thị; version bump (v1→v2 KHÔNG rebuild); lỗi→fallback (stale
# + giữ data cũ); no-cache→empty + retry gọi lại check_for_update (đúng abstraction mạng).
# Mock ở tầng transport (FakeHttpTransport) — KHÔNG mock sâu tới mức mất behavior thật.
# (docs/testing/godot-testing.md, docs/gameplay/configuration-and-data.md §4)
extends GdUnitTestSuite

const _PRESENTER := preload("res://src/ui/hero_list/hero_list_presenter.gd")
const _CONFIG_PROVIDER := preload("res://src/core/config/config_provider.gd")
const _NETWORK_CLIENT := preload("res://src/core/net/network_client.gd")

var _cache_dir: String


func before_test() -> void:
	_cache_dir = "user://test_hero_list_%d" % Time.get_ticks_usec()


func after_test() -> void:
	_remove_dir_recursive(_cache_dir)


# View gián điệp: bắt data cuối cùng render.
class _SpyView extends BaseView:
	var last: Dictionary = {}

	func _render(data: Dictionary) -> void:
		last = data.duplicate(true)


# SceneRouter giả — ghi nhận back().
class _StubRouter extends Node:
	var back_calls: int = 0

	func back() -> void:
		back_calls += 1


func _make_view() -> _SpyView:
	var view := _SpyView.new()
	add_child(view)
	auto_free(view)
	return view


func _make_router() -> _StubRouter:
	var router := _StubRouter.new()
	add_child(router)
	auto_free(router)
	return router


func _make_provider() -> Node:
	var provider: Node = _CONFIG_PROVIDER.new()
	provider.cache_dir = _cache_dir
	add_child(provider)
	auto_free(provider)
	return provider


func _make_net_with(bodies: Array) -> Node:
	var net: Node = _NETWORK_CLIENT.new()
	add_child(net)
	auto_free(net)
	var fake := FakeHttpTransport.new()
	for body in bodies:
		fake.queue_ok(200, str(body))
	net.set_transport(fake)
	return net


# Bundle hình MAP như Configuration Service THẬT phát: data = { type: { id: entry } }.
func _bundle_map(version: int, rarity: int) -> Dictionary:
	return {
		"config_version": "config@v%d" % version,
		"schema_version": 1,
		"data": {
			"hero": {
				"hero_sample": {
					"id": "hero_sample", "class": "warrior", "rarity": rarity,
					"base_stats": {"hp": 0, "atk": 0, "def": 0, "spd": 0}, "skills": ["skill_sample_basic"],
				},
			},
		},
	}


func _remove_dir_recursive(path: String) -> void:
	var dir := DirAccess.open(path)
	if dir == null:
		return
	dir.list_dir_begin()
	var name := dir.get_next()
	while name != "":
		if dir.current_is_dir():
			_remove_dir_recursive(path.path_join(name))
		else:
			dir.remove(path.path_join(name))
		name = dir.get_next()
	dir.list_dir_end()
	DirAccess.remove_absolute(path)


# ── Tests ────────────────────────────────────────────────────────────────────────────────────────

func test_receive_query_display_from_config() -> void:
	# Nhận→query→hiển thị: provider có bundle v1 → presenter đọc → view có hàng hero (KHÔNG hardcode).
	var provider := _make_provider()
	provider.apply_bundle(_bundle_map(1, 3))
	var view := _make_view()
	var presenter = _PRESENTER.new(view, provider, _make_router())

	var heroes: Array = view.last.get("heroes", [])
	assert_int(heroes.size()).is_equal(1)
	assert_str(str(heroes[0]["id"])).is_equal("hero_sample")
	assert_int(int(heroes[0]["rarity"])).is_equal(3)
	assert_str(str(view.last.get("version_label"))).contains("config@v1")
	assert_bool(bool(view.last.get("stale"))).is_false()
	presenter.dispose()


func test_version_bump_reflects_new_data_without_rebuild() -> void:
	# v1 hiển thị rarity 3; server publish v2 (rarity 5) → check_for_update → config_updated → refresh.
	var provider := _make_provider()
	provider.apply_bundle(_bundle_map(1, 3))
	var view := _make_view()
	var presenter = _PRESENTER.new(view, provider, _make_router())
	assert_int(int(view.last["heroes"][0]["rarity"])).is_equal(3)

	provider.network_client = _make_net_with([
		JSON.stringify({"version": {"bundle": 2, "schema": 1}}),
		JSON.stringify(_bundle_map(2, 5)),
	])
	await presenter._retry()
	# Cùng mã client, chỉ đổi config → view hiển thị v2 (rarity 5).
	assert_str(str(view.last.get("version_label"))).contains("config@v2")
	assert_int(int(view.last["heroes"][0]["rarity"])).is_equal(5)
	presenter.dispose()


func test_bundle_failure_falls_back_to_old_cache_with_stale_flag() -> void:
	# Case B: có v2 nhưng tải bundle lỗi → giữ v1 + cờ stale hiển thị (fallback KHÔNG im lặng).
	var provider := _make_provider()
	provider.apply_bundle(_bundle_map(1, 3))
	var view := _make_view()
	var presenter = _PRESENTER.new(view, provider, _make_router())

	var net: Node = _NETWORK_CLIENT.new()
	add_child(net)
	auto_free(net)
	var fake := FakeHttpTransport.new()
	fake.queue_ok(200, JSON.stringify({"version": {"bundle": 2, "schema": 1}}))
	fake.queue_ok(500, "")
	net.set_transport(fake)
	provider.network_client = net

	await presenter._retry()
	assert_bool(bool(view.last.get("stale"))).is_true()
	# Data CŨ v1 vẫn hiển thị (KHÔNG bịa, KHÔNG trống).
	assert_int(int(view.last["heroes"][0]["rarity"])).is_equal(3)
	assert_str(str(view.last.get("version_label"))).contains("config@v1")
	presenter.dispose()


func test_no_cache_shows_empty_then_retry_recovers_via_network() -> void:
	# Case C: không có config → view empty (heroes rỗng); retry gọi check_for_update (đúng abstraction) → phục hồi.
	var provider := _make_provider()
	var view := _make_view()
	var presenter = _PRESENTER.new(view, provider, _make_router())
	assert_int((view.last.get("heroes", []) as Array).size()).is_equal(0)

	provider.network_client = _make_net_with([
		JSON.stringify({"version": {"bundle": 1, "schema": 1}}),
		JSON.stringify(_bundle_map(1, 4)),
	])
	await presenter._retry()
	assert_int((view.last.get("heroes", []) as Array).size()).is_equal(1)
	assert_int(int(view.last["heroes"][0]["rarity"])).is_equal(4)
	presenter.dispose()


func test_back_intent_navigates_back() -> void:
	var provider := _make_provider()
	var view := _make_view()
	var router := _make_router()
	var presenter = _PRESENTER.new(view, provider, router)
	view.emit_intent(HeroListView.INTENT_BACK)
	assert_int(router.back_calls).is_equal(1)
	presenter.dispose()
