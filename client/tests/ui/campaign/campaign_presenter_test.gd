# Test CampaignPresenter — Phase 34 (Campaign PvE). Tải tiến độ từ server (server-authoritative) → cache →
# hiển thị trạng thái mở/khoá/đã-clear; chỉ mở stage đã unlock (chống skip UX; server vẫn validate); fallback
# KHÔNG im lặng khi lỗi (giữ cache + nhãn lỗi). Client KHÔNG tự suy tiến độ/mở-khoá (ADR-007/011).
# Stub ở ranh giới hợp lý (net/state/config/router); giữ behavior thật của presenter. (docs/testing/godot-testing.md)
extends GdUnitTestSuite

const _PRESENTER := preload("res://src/ui/campaign/campaign_presenter.gd")


# View gián điệp — bắt dữ liệu render gần nhất.
class _SpyView extends BaseView:
	var last: Dictionary = {}

	func _render(data: Dictionary) -> void:
		last = data.duplicate(true)


# Router giả: ghi goto (path + context) và đếm back().
class _StubRouter extends Node:
	var last_path: String = ""
	var last_context: Dictionary = {}
	var goto_calls: int = 0
	var back_calls: int = 0

	func goto_scene(path: String, context: Dictionary = {}) -> bool:
		goto_calls += 1
		last_path = path
		last_context = context.duplicate(true)
		return true

	func back() -> bool:
		back_calls += 1
		return true


# ConfigProvider giả: {type: {id: entry}} cho preview địch/thưởng.
class _StubConfig extends Node:
	var data: Dictionary = {}

	func get_entry(type: StringName, id: String) -> Dictionary:
		var by_id: Dictionary = data.get(String(type), {})
		return (by_id.get(id, {}) as Dictionary).duplicate(true)


# NetworkClient giả: trả NetResult đã xếp cho get(/campaign/progress); đếm số lần gọi.
class _StubNet extends Node:
	var result: Variant = null
	var get_calls: int = 0

	func get_json(_path: String, _parser := Callable()) -> Variant:
		get_calls += 1
		return result


# StateCache giả — apply_campaign_progress lưu + phát state_refreshed. Đọc trả bản đã lưu.
class _StubStateCache extends Node:
	var campaign: Dictionary = {}
	var apply_calls: int = 0
	var offline: bool = false

	func apply_campaign_progress(progress: Dictionary) -> void:
		apply_calls += 1
		campaign = progress.duplicate(true)
		EventBus.emit(&"state_refreshed", {"source": "server"})

	func get_campaign_progress() -> Dictionary:
		return campaign.duplicate(true)

	func is_offline() -> bool:
		return offline


func _node(n: Node) -> Node:
	add_child(n)
	auto_free(n)
	return n


# stages: Array of [stage_id, chapter_id, order, cleared, unlocked].
func _progress_dto(stages: Array, afk: String) -> CampaignProgressDto:
	var dto := CampaignProgressDto.new()
	var list: Array[CampaignStageDto] = []
	for row in stages:
		var s := CampaignStageDto.new()
		s.stage_id = str(row[0])
		s.chapter_id = str(row[1])
		s.order = int(row[2])
		s.cleared = bool(row[3])
		s.unlocked = bool(row[4])
		list.append(s)
	dto.stages = list
	dto.current_afk_stage_id = afk
	return dto


func _ok(value) -> NetResult:
	return NetResult.success(value, 200)


func _config() -> _StubConfig:
	var config := _StubConfig.new()
	config.data = {"stage": {
		"stage_ch01_01": {"enemies": [{"hero_id": "hero_terra"}], "rewards": ["reward_ch01_01"]},
		"stage_ch01_02": {"enemies": [{"hero_id": "hero_ignis"}], "rewards": ["reward_ch01_02"]},
	}}
	return config


# ── Tests ────────────────────────────────────────────────────────────────────────────────────────

func test_fetch_populates_progress_and_renders_locked_unlocked_cleared() -> void:
	var net := _StubNet.new()
	net.result = _ok(_progress_dto([
		["stage_ch01_01", "chapter_01", 0, true, true],
		["stage_ch01_02", "chapter_01", 1, false, true],
		["stage_ch01_03", "chapter_01", 2, false, false],
	], "stage_ch01_01"))
	_node(net)
	var config := _config(); _node(config)
	var state := _StubStateCache.new(); _node(state)
	var router := _StubRouter.new(); _node(router)
	var view := _SpyView.new(); _node(view)

	var presenter = _PRESENTER.new(view, state, config, router, net)

	assert_int(state.apply_calls).is_equal(1)  # tiến độ tải từ server → cache (server-authoritative)
	assert_str(str(view.last.get("current_afk_stage_id"))).is_equal("stage_ch01_01")
	var rows: Array = view.last.get("stages", [])
	assert_int(rows.size()).is_equal(3)
	assert_bool(bool(rows[0].get("cleared"))).is_true()
	assert_bool(bool(rows[1].get("unlocked"))).is_true()
	assert_bool(bool(rows[2].get("unlocked"))).is_false()  # stage 3 khoá (chống skip)
	presenter.dispose()


func test_open_unlocked_stage_navigates_to_battle_with_campaign_context() -> void:
	var net := _StubNet.new()
	net.result = _ok(_progress_dto([["stage_ch01_01", "chapter_01", 0, false, true]], ""))
	_node(net)
	var config := _config(); _node(config)
	var state := _StubStateCache.new(); _node(state)
	var router := _StubRouter.new(); _node(router)
	var view := _SpyView.new(); _node(view)

	var presenter = _PRESENTER.new(view, state, config, router, net)
	view.emit_intent(CampaignView.INTENT_OPEN_STAGE, {"stage_id": "stage_ch01_01"})

	assert_int(router.goto_calls).is_equal(1)
	assert_str(router.last_path).contains("battle")
	assert_str(str(router.last_context.get("campaign_stage_id"))).is_equal("stage_ch01_01")
	presenter.dispose()


func test_open_locked_stage_does_not_navigate() -> void:
	var net := _StubNet.new()
	net.result = _ok(_progress_dto([
		["stage_ch01_01", "chapter_01", 0, false, true],
		["stage_ch01_02", "chapter_01", 1, false, false],
	], ""))
	_node(net)
	var config := _config(); _node(config)
	var state := _StubStateCache.new(); _node(state)
	var router := _StubRouter.new(); _node(router)
	var view := _SpyView.new(); _node(view)

	var presenter = _PRESENTER.new(view, state, config, router, net)
	view.emit_intent(CampaignView.INTENT_OPEN_STAGE, {"stage_id": "stage_ch01_02"})

	assert_int(router.goto_calls).is_equal(0)  # stage khoá ⇒ UX không cho đi (server vẫn chặn)
	presenter.dispose()


func test_fetch_failure_shows_error_and_keeps_cache() -> void:
	var net := _StubNet.new()
	var err := ErrorResponse.new(); err.code = "NETWORK_ERROR"; err.message = "x"
	net.result = NetResult.failure(NetResult.Kind.NETWORK_ERROR, err, 0)
	_node(net)
	var config := _config(); _node(config)
	var state := _StubStateCache.new()
	state.campaign = {"stages": [{"stage_id": "stage_ch01_01", "chapter_id": "chapter_01", "order": 0, "cleared": true, "unlocked": true}], "current_afk_stage_id": "stage_ch01_01"}
	_node(state)
	var router := _StubRouter.new(); _node(router)
	var view := _SpyView.new(); _node(view)

	var presenter = _PRESENTER.new(view, state, config, router, net)

	assert_int(state.apply_calls).is_equal(0)  # lỗi ⇒ KHÔNG áp dụng gì (không bịa)
	assert_str(str(view.last.get("error_text"))).contains("NETWORK_ERROR")
	assert_int((view.last.get("stages") as Array).size()).is_equal(1)  # vẫn hiện cache cũ
	presenter.dispose()


func test_newly_unlocked_stage_appears_after_progress_update() -> void:
	# Lần 1: chỉ stage 1 unlocked (chưa clear). Lần 2 (retry sau khi thắng): stage 1 cleared ⇒ stage 2 unlocked.
	var net := _StubNet.new()
	net.result = _ok(_progress_dto([
		["stage_ch01_01", "chapter_01", 0, false, true],
		["stage_ch01_02", "chapter_01", 1, false, false],
	], ""))
	_node(net)
	var config := _config(); _node(config)
	var state := _StubStateCache.new(); _node(state)
	var router := _StubRouter.new(); _node(router)
	var view := _SpyView.new(); _node(view)

	var presenter = _PRESENTER.new(view, state, config, router, net)
	assert_bool(bool((view.last.get("stages") as Array)[1].get("unlocked"))).is_false()

	net.result = _ok(_progress_dto([
		["stage_ch01_01", "chapter_01", 0, true, true],
		["stage_ch01_02", "chapter_01", 1, false, true],
	], "stage_ch01_01"))
	view.emit_intent(CampaignView.INTENT_RETRY)

	assert_bool(bool((view.last.get("stages") as Array)[1].get("unlocked"))).is_true()
	assert_str(str(view.last.get("current_afk_stage_id"))).is_equal("stage_ch01_01")
	presenter.dispose()


func test_back_intent_navigates_back() -> void:
	var net := _StubNet.new()
	net.result = _ok(_progress_dto([], ""))
	_node(net)
	var config := _config(); _node(config)
	var state := _StubStateCache.new(); _node(state)
	var router := _StubRouter.new(); _node(router)
	var view := _SpyView.new(); _node(view)

	var presenter = _PRESENTER.new(view, state, config, router, net)
	view.emit_intent(CampaignView.INTENT_BACK)

	assert_int(router.back_calls).is_equal(1)
	presenter.dispose()
