# Test BattlePresenter — Phase 30 (Battle flow end-to-end). Gửi intent đánh trận (server) → replay bằng seed
# server → hiển thị diễn biến + kết cục + THƯỞNG do server cấp (client KHÔNG tự quyết/cấp — ADR-011/007).
# Stub ở ranh giới hợp lý (net/config/router); giữ behavior thật của presenter (bao gồm replay sim client).
# Kiểm khớp: replay outcome ≡ server outcome (cùng seed) — assert `replay_matches`.
# (docs/testing/godot-testing.md, docs/gameplay/combat-framework.md §24)
extends GdUnitTestSuite

const _PRESENTER := preload("res://src/ui/battle/battle_presenter.gd")


# View gián điệp — bắt dữ liệu render gần nhất.
class _SpyView extends BaseView:
	var last: Dictionary = {}

	func _render(data: Dictionary) -> void:
		last = data.duplicate(true)


# Router giả: cung cấp route context + đếm back().
class _StubRouter extends Node:
	var context: Dictionary = {}
	var back_calls: int = 0

	func route_context() -> Dictionary:
		return context.duplicate(true)

	func back() -> bool:
		back_calls += 1
		return true


# ConfigProvider giả cho replay: {type: {id: entry}} + current_version().
class _StubConfig extends Node:
	var data: Dictionary = {}

	func get_entry(type: StringName, id: String) -> Dictionary:
		var by_id: Dictionary = data.get(String(type), {})
		return (by_id.get(id, {}) as Dictionary).duplicate(true)

	func current_version() -> int:
		return 1


# NetworkClient giả: trả NetResult đã xếp cho get(/team)/post(/battles); ghi lại body + số lần post.
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


func _team(id: String, slots: Array) -> TeamDto:
	var dto := TeamDto.new()
	dto.id = id
	var list: Array[TeamSlotDto] = []
	for pair in slots:
		var s := TeamSlotDto.new()
		s.slot_index = int(pair[0])
		s.hero_id = str(pair[1])
		list.append(s)
	dto.slots = list
	return dto


func _battle_result(seed: int, outcome: String, rewards: Array) -> BattleResultDto:
	var dto := BattleResultDto.new()
	dto.seed = seed
	dto.outcome = outcome
	dto.rounds = 1
	dto.log = "{\"event_log\":[],\"result\":{}}"
	var list: Array[RewardDto] = []
	for pair in rewards:
		var reward := RewardDto.new()
		reward.reward_type = "currency"
		reward.ref_id = str(pair[0])
		reward.amount = int(pair[1])
		list.append(reward)
	dto.rewards = list
	return dto


func _ok(value) -> NetResult:
	return NetResult.success(value, 200)


# Config demo: đội 2 hero mạnh (hero_a/hero_b) vs 1 địch yếu (hero_dummy) ⇒ client sim → VICTORY tất định.
func _demo_config() -> _StubConfig:
	var config := _StubConfig.new()
	config.data = {
		"hero": {
			"hero_a": {"base_stats": {"hp": 1000, "atk": 300, "def": 50, "spd": 120}, "skills": ["skill_basic"]},
			"hero_b": {"base_stats": {"hp": 1000, "atk": 300, "def": 50, "spd": 110}, "skills": ["skill_basic"]},
			"hero_dummy": {"base_stats": {"hp": 100, "atk": 10, "def": 10, "spd": 50}, "skills": ["skill_basic"]},
		},
		"skill": {
			"skill_basic": {"target": "single_enemy", "trigger": {"type": "cooldown", "value": 0},
				"effects": [{"effect_type": "damage", "params": {"coeff_fixed": 1000}}]},
		},
		"stage": {
			"stage_demo_01": {
				"max_rounds": 30,
				"basic_skill_id": "skill_basic",
				"combat_rules": {
					"def_constant_k": 300, "min_damage": 1, "crit_multiplier_fixed": 1500,
					"accuracy_bp": 10000, "crit_rate_bp": 0,
					"energy": {"initial": 0, "on_attack": 0, "on_hit": 0, "ultimate_cost": 100, "max": 100},
				},
				"enemies": [{"hero_id": "hero_dummy", "slot": 0}],
			},
		},
	}
	return config


func _run(net: _StubNet, config: _StubConfig, router: _StubRouter, view: _SpyView) -> Variant:
	return _PRESENTER.new(view, null, config, router, net)


# ── Tests ────────────────────────────────────────────────────────────────────────────────────────

func test_posts_intent_and_displays_server_outcome_and_rewards() -> void:
	var net := _StubNet.new()
	net.get_result = _ok(_team("team-1", [[0, "hero_a"], [1, "hero_b"]]))
	net.post_result = _ok(_battle_result(42, "VICTORY", [["gold", 100]]))
	_node(net)
	var config := _demo_config(); _node(config)
	var router := _StubRouter.new(); _node(router)
	var view := _SpyView.new(); _node(view)

	var presenter = _run(net, config, router, view)

	# Body intent chứa teamId + stageId + attemptId (idempotency key do client sinh).
	assert_int(net.post_calls).is_equal(1)
	assert_str(str(net.posted_body.get("teamId"))).is_equal("team-1")
	assert_str(str(net.posted_body.get("stageId"))).is_equal("stage_demo_01")
	assert_bool(net.posted_body.has("attemptId")).is_true()
	# Outcome + rewards HIỂN THỊ theo server (authority).
	assert_str(str(view.last.get("outcome"))).is_equal("VICTORY")
	assert_array(view.last.get("rewards")).contains(["gold +100"])
	presenter.dispose()


func test_replay_outcome_matches_server_outcome_same_seed() -> void:
	# Kiểm khớp: replay client bằng seed server ⇒ diễn biến (events) + outcome khớp server.
	var net := _StubNet.new()
	net.get_result = _ok(_team("team-1", [[0, "hero_a"], [1, "hero_b"]]))
	net.post_result = _ok(_battle_result(12345, "VICTORY", [["gold", 100]]))
	_node(net)
	var config := _demo_config(); _node(config)
	var router := _StubRouter.new(); _node(router)
	var view := _SpyView.new(); _node(view)

	var presenter = _run(net, config, router, view)

	assert_bool(bool(view.last.get("replay_matches"))).is_true()
	assert_int((view.last.get("events") as Array).size()).is_greater(0)  # replay đã chạy, có diễn biến
	presenter.dispose()


func test_no_team_saved_shows_error_without_posting_battle() -> void:
	var net := _StubNet.new()
	net.get_result = _ok(_team("", []))  # chưa lưu đội
	_node(net)
	var config := _demo_config(); _node(config)
	var router := _StubRouter.new(); _node(router)
	var view := _SpyView.new(); _node(view)

	var presenter = _run(net, config, router, view)

	assert_int(net.post_calls).is_equal(0)
	assert_str(str(view.last.get("error_text"))).contains("Chưa có đội")
	assert_str(str(view.last.get("outcome"))).is_equal("")
	presenter.dispose()


func test_battle_failure_shows_error_and_no_fabricated_result() -> void:
	var net := _StubNet.new()
	net.get_result = _ok(_team("team-1", [[0, "hero_a"], [1, "hero_b"]]))
	var err := ErrorResponse.new(); err.code = "BATTLE_TEAM_NOT_FOUND"; err.message = "x"
	net.post_result = NetResult.failure(NetResult.Kind.HTTP_4XX, err, 404)
	_node(net)
	var config := _demo_config(); _node(config)
	var router := _StubRouter.new(); _node(router)
	var view := _SpyView.new(); _node(view)

	var presenter = _run(net, config, router, view)

	# Không bịa kết quả: outcome rỗng, báo mã lỗi server.
	assert_str(str(view.last.get("outcome"))).is_equal("")
	assert_str(str(view.last.get("error_text"))).contains("BATTLE_TEAM_NOT_FOUND")
	assert_array(view.last.get("rewards")).is_empty()
	presenter.dispose()


func test_retry_intent_reruns_battle() -> void:
	var net := _StubNet.new()
	net.get_result = _ok(_team("team-1", [[0, "hero_a"], [1, "hero_b"]]))
	net.post_result = _ok(_battle_result(42, "VICTORY", [["gold", 100]]))
	_node(net)
	var config := _demo_config(); _node(config)
	var router := _StubRouter.new(); _node(router)
	var view := _SpyView.new(); _node(view)

	var presenter = _run(net, config, router, view)
	assert_int(net.post_calls).is_equal(1)
	view.emit_intent(BattleView.INTENT_RETRY)

	assert_int(net.post_calls).is_equal(2)  # đánh lại = trận mới (attemptId mới)
	presenter.dispose()


func test_back_intent_navigates_back() -> void:
	var net := _StubNet.new()
	net.get_result = _ok(_team("team-1", [[0, "hero_a"], [1, "hero_b"]]))
	net.post_result = _ok(_battle_result(42, "VICTORY", []))
	_node(net)
	var config := _demo_config(); _node(config)
	var router := _StubRouter.new(); _node(router)
	var view := _SpyView.new(); _node(view)

	var presenter = _run(net, config, router, view)
	view.emit_intent(BattleView.INTENT_BACK)

	assert_int(router.back_calls).is_equal(1)
	presenter.dispose()
