# Test HeroListPresenter — Phase 27 (Hero System thật). Presenter GHÉP hero owned (StateCache stub) +
# definition (ConfigProvider THẬT, mock ở BOUNDARY mạng). Cover: ghép owned+config → hiển thị; đổi config
# version → definition đổi KHÔNG rebuild (data-driven); state_refreshed → danh sách owned cập nhật; chưa
# sở hữu → empty; open_hero → điều hướng KÈM ngữ cảnh hero_id; back → back().
# (docs/testing/godot-testing.md, docs/gameplay/hero-system.md)
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


# SceneRouter giả — ghi nhận goto_scene(path, context) + back().
class _StubRouter extends Node:
	var back_calls: int = 0
	var goto_path: String = ""
	var goto_context: Dictionary = {}

	func goto_scene(path: String, context: Dictionary = {}) -> bool:
		goto_path = path
		goto_context = context
		return true

	func back() -> bool:
		back_calls += 1
		return true


# StateCache giả — hero owned (server-authoritative) + nhãn offline.
class _StubStateCache extends Node:
	var heroes: Array = []
	var offline: bool = false

	func get_heroes() -> Array:
		return heroes.duplicate(true)

	func is_offline() -> bool:
		return offline


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


func _make_state(heroes: Array) -> _StubStateCache:
	var state := _StubStateCache.new()
	state.heroes = heroes
	add_child(state)
	auto_free(state)
	return state


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
					"id": "hero_sample", "class": "warrior", "element": "fire", "role": "tank",
					"faction": "none", "rarity": rarity,
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

func test_joins_owned_state_with_config_definition() -> void:
	# Owned (StateCache): hero_sample Lv.5 ★2. Definition (config v1): rarity 3, class warrior.
	var provider := _make_provider()
	provider.apply_bundle(_bundle_map(1, 3))
	var state := _make_state([{"id": "hero_sample", "level": 5, "stars": 2}])
	var view := _make_view()
	var presenter = _PRESENTER.new(view, state, provider, _make_router())

	var heroes: Array = view.last.get("heroes", [])
	assert_int(heroes.size()).is_equal(1)
	assert_str(str(heroes[0]["id"])).is_equal("hero_sample")
	assert_int(int(heroes[0]["level"])).is_equal(5)   # từ StateCache (owned)
	assert_int(int(heroes[0]["stars"])).is_equal(2)   # từ StateCache (owned)
	assert_int(int(heroes[0]["rarity"])).is_equal(3)  # từ config (definition)
	assert_str(str(heroes[0]["class"])).is_equal("warrior")
	presenter.dispose()


func test_config_version_bump_updates_definition_without_rebuild() -> void:
	# Data-driven: owned cố định; đổi config v1(rarity3)→v2(rarity5) → definition đổi KHÔNG sửa code.
	var provider := _make_provider()
	provider.apply_bundle(_bundle_map(1, 3))
	var state := _make_state([{"id": "hero_sample", "level": 1, "stars": 1}])
	var view := _make_view()
	var presenter = _PRESENTER.new(view, state, provider, _make_router())
	assert_int(int(view.last["heroes"][0]["rarity"])).is_equal(3)

	provider.network_client = _make_net_with([
		JSON.stringify({"version": {"bundle": 2, "schema": 1}}),
		JSON.stringify(_bundle_map(2, 5)),
	])
	await presenter._retry()
	assert_str(str(view.last.get("version_label"))).contains("config@v2")
	assert_int(int(view.last["heroes"][0]["rarity"])).is_equal(5)
	presenter.dispose()


func test_state_refreshed_updates_owned_list() -> void:
	# Ban đầu chưa sở hữu → empty; sau khi state có hero + phát state_refreshed → danh sách cập nhật.
	var provider := _make_provider()
	provider.apply_bundle(_bundle_map(1, 3))
	var state := _make_state([])
	var view := _make_view()
	var presenter = _PRESENTER.new(view, state, provider, _make_router())
	assert_int((view.last.get("heroes", []) as Array).size()).is_equal(0)

	state.heroes = [{"id": "hero_sample", "level": 1, "stars": 1}]
	EventBus.emit(&"state_refreshed", {"source": "server"})
	assert_int((view.last.get("heroes", []) as Array).size()).is_equal(1)
	presenter.dispose()


func test_no_owned_heroes_shows_empty() -> void:
	var provider := _make_provider()
	provider.apply_bundle(_bundle_map(1, 3))
	var view := _make_view()
	var presenter = _PRESENTER.new(view, _make_state([]), provider, _make_router())
	assert_int((view.last.get("heroes", []) as Array).size()).is_equal(0)
	presenter.dispose()


func test_open_hero_intent_navigates_with_hero_id_context() -> void:
	var provider := _make_provider()
	provider.apply_bundle(_bundle_map(1, 3))
	var state := _make_state([{"id": "hero_sample", "level": 1, "stars": 1}])
	var view := _make_view()
	var router := _make_router()
	var presenter = _PRESENTER.new(view, state, provider, router)

	view.emit_intent(HeroListView.INTENT_OPEN_HERO, {"id": "hero_sample"})
	assert_str(router.goto_path).is_equal(HeroListPresenter.HERO_DETAIL_PATH)
	assert_str(str(router.goto_context.get("hero_id", ""))).is_equal("hero_sample")
	presenter.dispose()


func test_back_intent_navigates_back() -> void:
	var provider := _make_provider()
	provider.apply_bundle(_bundle_map(1, 3))
	var view := _make_view()
	var router := _make_router()
	var presenter = _PRESENTER.new(view, _make_state([]), provider, router)
	view.emit_intent(HeroListView.INTENT_BACK)
	assert_int(router.back_calls).is_equal(1)
	presenter.dispose()
