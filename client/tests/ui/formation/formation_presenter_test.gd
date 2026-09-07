# Test FormationPresenter — Phase 29. Dựng lưới (config) + roster (owned StateCache) + đội đã lưu (server);
# chọn hero → đặt ô → đổi vị trí → Lưu (POST intent) → hiển thị lại theo SERVER. Stub ở ranh giới hợp lý
# (config/state/net/router) — giữ behavior thật của presenter. Client KHÔNG tự lưu cục bộ (ADR-007/011).
# (docs/testing/godot-testing.md, docs/gameplay/hero-system.md — Formation & Team)
extends GdUnitTestSuite

const _PRESENTER := preload("res://src/ui/formation/formation_presenter.gd")


# View gián điệp — bắt dữ liệu render gần nhất.
class _SpyView extends BaseView:
	var last: Dictionary = {}

	func _render(data: Dictionary) -> void:
		last = data.duplicate(true)


# Router giả: ghi nhận back().
class _StubRouter extends Node:
	var back_calls: int = 0

	func back() -> bool:
		back_calls += 1
		return true


# StateCache giả: roster hero owned + nhãn offline.
class _StubState extends Node:
	var heroes: Array = []

	func get_heroes() -> Array:
		return heroes.duplicate(true)

	func is_offline() -> bool:
		return false


# ConfigProvider giả: lưới formation theo id.
class _StubConfig extends Node:
	var grid: Dictionary = {}

	func get_entry(_type: StringName, _id: String) -> Dictionary:
		return grid.duplicate(true)


# NetworkClient giả: trả NetResult đã xếp cho get/post; ghi lại body đã POST.
class _StubNet extends Node:
	var get_result: Variant = null
	var post_result: Variant = null
	var posted_body: Dictionary = {}
	var post_calls: int = 0

	func get_json(_path: String, _parser := Callable()) -> Variant:
		return get_result

	func post_json(_path: String, body: Dictionary, _parser := Callable()) -> Variant:
		post_calls += 1
		posted_body = body.duplicate(true)
		return post_result


func _node(n: Node) -> Node:
	add_child(n)
	auto_free(n)
	return n


func _team(slots: Array) -> TeamDto:
	# slots: Array[[slot_index:int, hero_id:String]]
	var dto := TeamDto.new()
	var list: Array[TeamSlotDto] = []
	for pair in slots:
		var s := TeamSlotDto.new()
		s.slot_index = int(pair[0])
		s.hero_id = str(pair[1])
		list.append(s)
	dto.slots = list
	return dto


func _ok(value) -> NetResult:
	return NetResult.success(value, 200)


func _make(net: _StubNet, state: _StubState, config: _StubConfig, router: _StubRouter, view: _SpyView) -> Variant:
	return _PRESENTER.new(view, state, config, router, net)


func _grid_1x2() -> _StubConfig:
	var config := _StubConfig.new(); config.grid = {"rows": 1, "cols": 2}; return config


# ── Tests ────────────────────────────────────────────────────────────────────────────────────────

func test_renders_grid_from_config_and_roster_from_owned() -> void:
	var net := _StubNet.new(); net.get_result = _ok(_team([])); _node(net)
	var state := _StubState.new(); state.heroes = [{"id": "hero_a"}, {"id": "hero_b"}, {"id": "hero_c"}]; _node(state)
	var config := _grid_1x2(); _node(config)
	var router := _StubRouter.new(); _node(router)
	var view := _SpyView.new(); _node(view)

	var presenter = _make(net, state, config, router, view)

	assert_int(int(view.last.get("rows"))).is_equal(1)
	assert_int(int(view.last.get("cols"))).is_equal(2)
	assert_int((view.last.get("slots") as Array).size()).is_equal(2)  # 1×2 = 2 ô
	assert_int((view.last.get("roster") as Array).size()).is_equal(3)
	presenter.dispose()


func test_select_and_place_fills_slot() -> void:
	var net := _StubNet.new(); net.get_result = _ok(_team([])); _node(net)
	var state := _StubState.new(); state.heroes = [{"id": "hero_a"}, {"id": "hero_b"}]; _node(state)
	var config := _grid_1x2(); _node(config)
	var router := _StubRouter.new(); _node(router)
	var view := _SpyView.new(); _node(view)

	var presenter = _make(net, state, config, router, view)
	view.emit_intent(FormationView.INTENT_SELECT_HERO, {"hero_id": "hero_a"})
	view.emit_intent(FormationView.INTENT_PLACE_SLOT, {"slot_index": 0})

	assert_str(_hero_at(view, 0)).is_equal("hero_a")
	assert_str(_hero_at(view, 1)).is_equal("")
	presenter.dispose()


func test_changing_position_moves_hero_and_keeps_one_slot() -> void:
	var net := _StubNet.new(); net.get_result = _ok(_team([])); _node(net)
	var state := _StubState.new(); state.heroes = [{"id": "hero_a"}, {"id": "hero_b"}]; _node(state)
	var config := _grid_1x2(); _node(config)
	var router := _StubRouter.new(); _node(router)
	var view := _SpyView.new(); _node(view)

	var presenter = _make(net, state, config, router, view)
	# Đặt hero_a vào ô 0, rồi chuyển hero_a sang ô 1 → ô 0 trống, ô 1 = hero_a (một-hero-một-ô).
	view.emit_intent(FormationView.INTENT_SELECT_HERO, {"hero_id": "hero_a"})
	view.emit_intent(FormationView.INTENT_PLACE_SLOT, {"slot_index": 0})
	view.emit_intent(FormationView.INTENT_SELECT_HERO, {"hero_id": "hero_a"})
	view.emit_intent(FormationView.INTENT_PLACE_SLOT, {"slot_index": 1})

	assert_str(_hero_at(view, 0)).is_equal("")
	assert_str(_hero_at(view, 1)).is_equal("hero_a")
	presenter.dispose()


func test_save_posts_intent_and_reflects_server_team() -> void:
	var net := _StubNet.new(); net.get_result = _ok(_team([])); _node(net)
	var state := _StubState.new(); state.heroes = [{"id": "hero_a"}, {"id": "hero_b"}]; _node(state)
	var config := _grid_1x2(); _node(config)
	var router := _StubRouter.new(); _node(router)
	var view := _SpyView.new(); _node(view)

	var presenter = _make(net, state, config, router, view)
	view.emit_intent(FormationView.INTENT_SELECT_HERO, {"hero_id": "hero_a"})
	view.emit_intent(FormationView.INTENT_PLACE_SLOT, {"slot_index": 0})
	view.emit_intent(FormationView.INTENT_SELECT_HERO, {"hero_id": "hero_b"})
	view.emit_intent(FormationView.INTENT_PLACE_SLOT, {"slot_index": 1})

	# Server chấp nhận và trả đội chuẩn (echo) — client hiển thị lại theo SERVER.
	net.post_result = _ok(_team([[0, "hero_a"], [1, "hero_b"]]))
	await presenter._save()

	assert_int(net.post_calls).is_equal(1)
	# Body gửi lên = intent 2 ô (server validate số lượng theo config).
	assert_int((net.posted_body.get("slots") as Array).size()).is_equal(2)
	assert_str(_hero_at(view, 0)).is_equal("hero_a")
	assert_str(_hero_at(view, 1)).is_equal("hero_b")
	assert_str(str(view.last.get("status_text"))).contains("Đã lưu")
	presenter.dispose()


func test_open_loads_saved_team_from_server() -> void:
	# Mở màn: GET /team trả đội đã lưu → hiển thị lại đúng vị trí (server-authoritative).
	var net := _StubNet.new(); net.get_result = _ok(_team([[0, "hero_x"], [1, "hero_y"]])); _node(net)
	var state := _StubState.new(); state.heroes = [{"id": "hero_x"}, {"id": "hero_y"}]; _node(state)
	var config := _grid_1x2(); _node(config)
	var router := _StubRouter.new(); _node(router)
	var view := _SpyView.new(); _node(view)

	var presenter = _make(net, state, config, router, view)

	assert_str(_hero_at(view, 0)).is_equal("hero_x")
	assert_str(_hero_at(view, 1)).is_equal("hero_y")
	presenter.dispose()


func test_save_failure_keeps_draft_and_reports_error() -> void:
	var net := _StubNet.new(); net.get_result = _ok(_team([])); _node(net)
	var state := _StubState.new(); state.heroes = [{"id": "hero_a"}, {"id": "hero_b"}]; _node(state)
	var config := _grid_1x2(); _node(config)
	var router := _StubRouter.new(); _node(router)
	var view := _SpyView.new(); _node(view)

	var presenter = _make(net, state, config, router, view)
	view.emit_intent(FormationView.INTENT_SELECT_HERO, {"hero_id": "hero_a"})
	view.emit_intent(FormationView.INTENT_PLACE_SLOT, {"slot_index": 0})

	var err := ErrorResponse.new(); err.code = "TEAM_INVALID_SIZE"; err.message = "x"
	net.post_result = NetResult.failure(NetResult.Kind.HTTP_4XX, err, 400)
	await presenter._save()

	# Bản nháp GIỮ nguyên (không bịa là đã lưu); báo lỗi có mã.
	assert_str(_hero_at(view, 0)).is_equal("hero_a")
	assert_str(str(view.last.get("status_text"))).contains("TEAM_INVALID_SIZE")
	presenter.dispose()


func test_back_intent_navigates_back() -> void:
	var net := _StubNet.new(); net.get_result = _ok(_team([])); _node(net)
	var state := _StubState.new(); _node(state)
	var config := _grid_1x2(); _node(config)
	var router := _StubRouter.new(); _node(router)
	var view := _SpyView.new(); _node(view)

	var presenter = _make(net, state, config, router, view)
	view.emit_intent(FormationView.INTENT_BACK)

	assert_int(router.back_calls).is_equal(1)
	presenter.dispose()


func _hero_at(view: _SpyView, slot_index: int) -> String:
	for slot in view.last.get("slots", []):
		if int(slot.get("slot_index", -1)) == slot_index:
			return str(slot.get("hero_id", ""))
	return "<missing>"
