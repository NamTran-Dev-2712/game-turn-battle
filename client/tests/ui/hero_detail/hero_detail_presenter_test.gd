# Test HeroDetailPresenter — Phase 27. Ghép owned (StateCache) + definition (ConfigProvider) cho hero chọn
# (id đọc từ SceneRouter.route_context); ART tải LAZY qua AssetLoader (placeholder trước, art thật sau —
# ADR-009); back → back(); config đổi → refresh (data-driven). Stub ở ranh giới hợp lý (route/state/config/
# asset) — giữ behavior thật của presenter. (docs/testing/godot-testing.md, docs/gameplay/hero-system.md)
extends GdUnitTestSuite

const _PRESENTER := preload("res://src/ui/hero_detail/hero_detail_presenter.gd")


# View gián điệp.
class _SpyView extends BaseView:
	var last: Dictionary = {}

	func _render(data: Dictionary) -> void:
		last = data.duplicate(true)


# Router giả: cung cấp route_context (hero_id) + ghi nhận back().
class _StubRouter extends Node:
	var ctx: Dictionary = {}
	var back_calls: int = 0

	func route_context() -> Dictionary:
		return ctx.duplicate(true)

	func back() -> bool:
		back_calls += 1
		return true


# StateCache giả: hero owned theo id.
class _StubState extends Node:
	var hero: Dictionary = {}

	func get_hero(_id: String) -> Dictionary:
		return hero.duplicate(true)


# ConfigProvider giả: definition theo id.
class _StubConfig extends Node:
	var hero: Dictionary = {}

	func get_hero(_id: String) -> Dictionary:
		return hero.duplicate(true)


# AssetLoader giả: placeholder + load_texture (coroutine) ghi nhận path yêu cầu/giải phóng.
class _StubAssetLoader extends Node:
	var placeholder_tex: Texture2D = null
	var loaded_tex: Texture2D = null
	var requested_path: String = ""
	var released_path: String = ""

	func placeholder() -> Texture2D:
		return placeholder_tex

	func load_texture(path: String) -> Texture2D:
		requested_path = path
		await get_tree().process_frame  # mô phỏng bất đồng bộ (coroutine thật).
		return loaded_tex

	func release(path: String) -> void:
		released_path = path


func _tex(color: Color) -> Texture2D:
	var image := Image.create(4, 4, false, Image.FORMAT_RGBA8)
	image.fill(color)
	return ImageTexture.create_from_image(image)


func _node(n: Node) -> Node:
	add_child(n)
	auto_free(n)
	return n


func _definition() -> Dictionary:
	return {
		"id": "hero_ignis", "faction": "none", "class": "mage", "element": "fire", "role": "dps",
		"rarity": 5, "base_stats": {"hp": 900, "atk": 220, "def": 60, "spd": 110},
		"skills": ["skill_ignis_strike"], "art": "res://assets/heroes/hero_ignis.png",
	}


# ── Tests ────────────────────────────────────────────────────────────────────────────────────────

func test_renders_owned_state_and_config_definition() -> void:
	var router := _StubRouter.new(); router.ctx = {"hero_id": "hero_ignis"}; _node(router)
	var state := _StubState.new(); state.hero = {"id": "hero_ignis", "level": 7, "stars": 3}; _node(state)
	var config := _StubConfig.new(); config.hero = _definition(); _node(config)
	var asset := _StubAssetLoader.new(); asset.placeholder_tex = _tex(Color.GRAY); _node(asset)
	var view := _SpyView.new(); _node(view)

	var presenter = _PRESENTER.new(view, state, config, router, asset)

	assert_str(str(view.last.get("hero_id"))).is_equal("hero_ignis")
	assert_int(int(view.last.get("level"))).is_equal(7)      # owned
	assert_int(int(view.last.get("stars"))).is_equal(3)      # owned
	assert_str(str(view.last.get("class"))).is_equal("mage") # config
	assert_int(int(view.last.get("rarity"))).is_equal(5)     # config
	assert_int(int(view.last.get("atk"))).is_equal(220)      # config base_stats
	assert_bool(bool(view.last.get("has_definition"))).is_true()
	presenter.dispose()


func test_art_is_lazy_loaded_from_config_path_after_placeholder() -> void:
	var router := _StubRouter.new(); router.ctx = {"hero_id": "hero_ignis"}; _node(router)
	var state := _StubState.new(); state.hero = {"id": "hero_ignis", "level": 1, "stars": 1}; _node(state)
	var config := _StubConfig.new(); config.hero = _definition(); _node(config)
	var placeholder := _tex(Color.GRAY)
	var real_art := _tex(Color.RED)
	var asset := _StubAssetLoader.new(); asset.placeholder_tex = placeholder; asset.loaded_tex = real_art; _node(asset)
	var view := _SpyView.new(); _node(view)

	var presenter = _PRESENTER.new(view, state, config, router, asset)
	# Ngay sau refresh: art là PLACEHOLDER (không chặn chờ art).
	assert_object(view.last.get("art_texture")).is_same(placeholder)

	# Ép hoàn tất tải lazy → art thật đẩy vào view; đúng đường dẫn art từ config.
	await presenter._load_art()
	assert_str(asset.requested_path).is_equal("res://assets/heroes/hero_ignis.png")
	assert_object(view.last.get("art_texture")).is_same(real_art)
	presenter.dispose()


func test_dispose_releases_art() -> void:
	var router := _StubRouter.new(); router.ctx = {"hero_id": "hero_ignis"}; _node(router)
	var state := _StubState.new(); state.hero = {"id": "hero_ignis", "level": 1, "stars": 1}; _node(state)
	var config := _StubConfig.new(); config.hero = _definition(); _node(config)
	var asset := _StubAssetLoader.new(); asset.placeholder_tex = _tex(Color.GRAY); _node(asset)
	var view := _SpyView.new(); _node(view)

	var presenter = _PRESENTER.new(view, state, config, router, asset)
	presenter.dispose()
	assert_str(asset.released_path).is_equal("res://assets/heroes/hero_ignis.png")


func test_back_intent_navigates_back() -> void:
	var router := _StubRouter.new(); router.ctx = {"hero_id": "hero_ignis"}; _node(router)
	var state := _StubState.new(); state.hero = {}; _node(state)
	var config := _StubConfig.new(); config.hero = _definition(); _node(config)
	var asset := _StubAssetLoader.new(); asset.placeholder_tex = _tex(Color.GRAY); _node(asset)
	var view := _SpyView.new(); _node(view)

	var presenter = _PRESENTER.new(view, state, config, router, asset)
	view.emit_intent(&"back")
	assert_int(router.back_calls).is_equal(1)
	presenter.dispose()
