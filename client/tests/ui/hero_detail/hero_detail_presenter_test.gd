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


# StateCache giả: hero owned theo id + ví; apply_heroes/apply_wallet mô phỏng refresh authoritative từ server.
class _StubState extends Node:
	var hero: Dictionary = {}
	var currencies: Dictionary = {}
	var apply_heroes_calls: int = 0
	var apply_wallet_calls: int = 0

	func get_hero(_id: String) -> Dictionary:
		return hero.duplicate(true)

	func get_heroes() -> Array:
		return [hero.duplicate(true)] if not hero.is_empty() else []

	func get_currency(code: String) -> int:
		return int(currencies.get(code, 0))

	func apply_heroes(heroes: Array) -> void:
		apply_heroes_calls += 1
		for h in heroes:
			if h is Dictionary and str((h as Dictionary).get("id", "")) == str(hero.get("id", "")):
				hero = (h as Dictionary).duplicate(true)

	func apply_wallet(balances: Dictionary) -> void:
		apply_wallet_calls += 1
		currencies = balances.duplicate(true)


# ConfigProvider giả: definition theo id (get_hero) + economy (get_entry).
class _StubConfig extends Node:
	var hero: Dictionary = {}
	var economy: Dictionary = {}

	func get_hero(_id: String) -> Dictionary:
		return hero.duplicate(true)

	func get_entry(_type: StringName, _id: String) -> Dictionary:
		return economy.duplicate(true)


# NetworkClient giả: post_json trả kết quả nâng cấp đã xếp + bắt path; get_json trả DTO theo path (refresh).
class _StubNet extends Node:
	var post_result: Variant = null
	var post_calls: int = 0
	var last_path: String = ""
	var heroes_result: Variant = null
	var wallet_result: Variant = null

	func post_json(path: String, _body: Dictionary, _parser := Callable()) -> Variant:
		post_calls += 1
		last_path = path
		return post_result

	func get_json(path: String, _parser := Callable()) -> Variant:
		if path == "/heroes":
			return heroes_result
		if path == "/wallet":
			return wallet_result
		return null


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


# ── Phase 35 — nâng cấp (level/stat/power data-driven, server-authoritative) ───────────────────────

func test_shows_level_scaled_stats_power_and_cost_from_economy() -> void:
	var router := _StubRouter.new(); router.ctx = {"hero_id": "hero_ignis"}; _node(router)
	var state := _StubState.new(); state.hero = {"id": "hero_ignis", "level": 3, "stars": 1}
	state.currencies = {"gold": 500}; _node(state)
	var config := _StubConfig.new(); config.hero = _definition(); config.economy = _economy(); _node(config)
	var asset := _StubAssetLoader.new(); asset.placeholder_tex = _tex(Color.GRAY); _node(asset)
	var net := _StubNet.new(); _node(net)
	var view := _SpyView.new(); _node(view)

	var presenter = _PRESENTER.new(view, state, config, router, asset, net)

	# Chỉ số HIỂN THỊ theo cấp (data-driven, khớp công thức HeroStats) — cao hơn chỉ số nền.
	var expected: Dictionary = HeroStats.scaled_stats(_definition()["base_stats"], 3, 800)
	assert_int(int(view.last.get("atk"))).is_equal(int(expected["atk"]))
	assert_int(int(view.last.get("atk"))).is_greater(220)  # > base
	assert_int(int(view.last.get("power"))).is_equal(HeroStats.power(expected, _economy()["power_weights"]))
	# Chi phí lấy từ đường cong config (level_up[level-1]) + gold hiện có; đủ tiền ⇒ nâng được.
	assert_int(int(view.last.get("upgrade_cost"))).is_equal(220)
	assert_int(int(view.last.get("gold"))).is_equal(500)
	assert_bool(bool(view.last.get("can_upgrade"))).is_true()
	presenter.dispose()


func test_level_up_intent_posts_then_refreshes_authoritative_state() -> void:
	var router := _StubRouter.new(); router.ctx = {"hero_id": "hero_ignis"}; _node(router)
	var state := _StubState.new(); state.hero = {"id": "hero_ignis", "level": 1, "stars": 1}
	state.currencies = {"gold": 100}; _node(state)
	var config := _StubConfig.new(); config.hero = _definition(); config.economy = _economy(); _node(config)
	var asset := _StubAssetLoader.new(); asset.placeholder_tex = _tex(Color.GRAY); _node(asset)
	var net := _StubNet.new()
	net.post_result = _ok(_level_up_response(2))
	net.heroes_result = _ok([_owned("hero_ignis", 2)])
	var wallet := WalletDto.new(); wallet.balances = [] as Array[CurrencyBalanceDto]
	net.wallet_result = _ok(wallet)
	_node(net)
	var view := _SpyView.new(); _node(view)

	var presenter = _PRESENTER.new(view, state, config, router, asset, net)
	view.emit_intent(HeroDetailView.INTENT_LEVEL_UP)

	# Client CHỈ gửi intent tới đúng endpoint; server-authoritative quyết cấp/chỉ số/gold.
	assert_int(net.post_calls).is_equal(1)
	assert_str(net.last_path).is_equal("/heroes/hero_ignis/level-up")
	# Không tự tăng cấp — đọc lại hero + ví AUTHORITATIVE từ server vào StateCache.
	assert_int(state.apply_heroes_calls).is_equal(1)
	assert_int(state.apply_wallet_calls).is_equal(1)
	assert_int(int(view.last.get("level"))).is_equal(2)  # cấp mới từ server
	assert_str(str(view.last.get("error_text"))).is_equal("")
	presenter.dispose()


func test_level_up_failure_shows_error_and_mutates_nothing() -> void:
	var router := _StubRouter.new(); router.ctx = {"hero_id": "hero_ignis"}; _node(router)
	var state := _StubState.new(); state.hero = {"id": "hero_ignis", "level": 1, "stars": 1}
	state.currencies = {"gold": 50}; _node(state)
	var config := _StubConfig.new(); config.hero = _definition(); config.economy = _economy(); _node(config)
	var asset := _StubAssetLoader.new(); asset.placeholder_tex = _tex(Color.GRAY); _node(asset)
	var net := _StubNet.new()
	var err := ErrorResponse.new(); err.code = "CURRENCY_INSUFFICIENT_FUNDS"; err.message = "x"
	net.post_result = NetResult.failure(NetResult.Kind.HTTP_4XX, err, 409)
	_node(net)
	var view := _SpyView.new(); _node(view)

	var presenter = _PRESENTER.new(view, state, config, router, asset, net)
	view.emit_intent(HeroDetailView.INTENT_LEVEL_UP)

	assert_str(str(view.last.get("error_text"))).contains("CURRENCY_INSUFFICIENT_FUNDS")
	assert_int(state.apply_heroes_calls).is_equal(0)  # thất bại ⇒ không refresh/không đổi
	assert_int(state.apply_wallet_calls).is_equal(0)
	assert_int(int(view.last.get("level"))).is_equal(1)  # cấp không đổi
	presenter.dispose()


func _ok(value) -> NetResult:
	return NetResult.success(value, 200)


func _economy() -> Dictionary:
	return {
		"cost_curves": {"level_up": [100, 150, 220]},
		"level_stat_growth_bp": 800,
		"power_weights": {"hp": 1, "atk": 10, "def": 8, "spd": 6},
	}


func _level_up_response(level: int) -> LevelUpHeroResponse:
	var stats := HeroBaseStatsDto.new()
	stats.hp = 0; stats.atk = 0; stats.def = 0; stats.spd = 0
	var dto := LevelUpHeroResponse.new()
	dto.hero_id = "hero_ignis"
	dto.level = level
	dto.stats = stats
	dto.power = 0
	dto.gold_spent = 100
	dto.gold_balance_after = 0
	return dto


func _owned(hero_id: String, level: int) -> OwnedHeroDto:
	var dto := OwnedHeroDto.new()
	dto.hero_id = hero_id
	dto.level = level
	dto.stars = 1
	return dto
