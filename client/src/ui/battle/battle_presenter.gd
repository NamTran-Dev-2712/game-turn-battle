# BattlePresenter — presenter cho BattleView (Phase 30, Battle flow end-to-end).
# Trách nhiệm: gửi INTENT đánh trận qua server (server-authoritative) rồi REPLAY bằng seed server để hiển thị:
#   - GET /team (parse_team) → teamId + đội (server-authoritative).
#   - POST /battles {teamId, stageId, attemptId} (parse_battle_result) → BattleResult{seed, outcome, rewards, log}.
#   - Replay: CombatInputResolver + ConfigProvider (đọc-cache) → BattleSimulator.simulate(seed) → diễn biến để vẽ.
# Client KHÔNG tự quyết outcome / tự cấp thưởng: outcome + rewards LẤY TỪ server (ADR-011/007); replay chỉ để
# HIỂN THỊ. Nếu replay lệch server ⇒ hiển thị theo server (authority) + cảnh báo, KHÔNG bịa. attemptId là
# idempotency key (một lần đánh); server chống double-grant. Chi tiết: ui-architecture.md, combat-framework.md.
class_name BattlePresenter
extends RefCounted

const _INTENT_BACK: StringName = &"back"
const _INTENT_RETRY: StringName = &"retry"
const _TEAM_PATH: String = "/team"
const _BATTLES_PATH: String = "/battles"
## Stage demo mặc định (khớp config/stages/stage_demo_01.json) khi không có context.
const _DEFAULT_STAGE: String = "stage_demo_01"
const _STAGE_TYPE: StringName = &"stage"

var _view: BaseView = null
# Nguồn đọc/điều hướng/mạng (inject cho test; mặc định = autoload). Chỉ đọc-cache, không tự tính chân lý.
var _state_cache: Node = null
var _config_provider: Node = null
var _scene_router: Node = null
var _network: Node = null

var _stage_id: String = _DEFAULT_STAGE
var _status: String = ""


func _init(
		view: BaseView,
		state_cache: Node = null,
		config_provider: Node = null,
		scene_router: Node = null,
		network_client: Node = null) -> void:
	_view = view
	_state_cache = state_cache if state_cache != null else StateCache
	_config_provider = config_provider if config_provider != null else ConfigProvider
	_scene_router = scene_router if scene_router != null else SceneRouter
	_network = network_client if network_client != null else NetworkClient
	_view.intent.connect(_on_intent)
	if _scene_router != null:
		_stage_id = str(_scene_router.route_context().get("stage_id", _DEFAULT_STAGE))
	render()
	_start()


## Dựng dữ liệu HIỂN THỊ hiện tại → view (server là authority về outcome/rewards; events từ replay client).
func render(data: Dictionary = {}) -> void:
	var base := {
		"status_text": _status,
		"stage_id": _stage_id,
		"outcome": "",
		"rounds": 0,
		"events": [],
		"rewards": [],
		"replay_matches": true,
		"error_text": "",
	}
	base.merge(data, true)
	_view.set_data(base)


## Không đăng ký EventBus ⇒ dispose không cần huỷ gì (giữ đối xứng vòng đời với các presenter khác).
func dispose() -> void:
	pass


# Luồng: tải đội (server) → POST /battles (server re-sim quyết) → replay bằng seed để vẽ.
func _start() -> void:
	if _network == null:
		return

	_status = "Đang tải đội..."
	render()
	var team_res = await _network.get_json(_TEAM_PATH, NetworkResponseParser.parse_team)
	if team_res == null or not team_res.ok or team_res.value == null:
		_fail("Không tải được đội: %s" % _error_code(team_res))
		return
	var team = team_res.value
	if team.slots.is_empty():
		_fail("Chưa có đội hình — hãy lưu đội trước khi đánh.")
		return

	_status = "Đang đánh (server re-sim)..."
	render()
	var body := {"teamId": str(team.id), "stageId": _stage_id, "attemptId": _new_attempt_id()}
	var battle_res = await _network.post_json(_BATTLES_PATH, body, NetworkResponseParser.parse_battle_result)
	if battle_res == null or not battle_res.ok or battle_res.value == null:
		_fail("Đánh thất bại: %s" % _error_code(battle_res))
		return

	_present(team, battle_res.value)


# Hiển thị kết quả: outcome + rewards THEO SERVER (authority); diễn biến từ REPLAY client bằng seed server.
func _present(team, result) -> void:
	var replay := _replay(team, int(result.seed))
	var events: Array = []
	var replay_matches := true
	if not replay.is_empty():
		events = _format_events(replay.get("event_log", []))
		var replay_outcome := str(replay.get("result", {}).get("outcome", ""))
		replay_matches = replay_outcome == str(result.outcome)
		if not replay_matches:
			# Server là authority — KHÔNG bịa/không copy: hiển thị outcome server, cảnh báo lệch để điều tra.
			push_warning("BattlePresenter: client replay outcome '%s' != server '%s'." % [replay_outcome, result.outcome])

	_status = "Trận xong."
	render({
		"outcome": str(result.outcome),
		"rounds": int(result.rounds),
		"events": events,
		"rewards": _format_rewards(result.rewards),
		"replay_matches": replay_matches,
	})


# Replay client bằng seed server (display-only): dựng đội ally khớp server (actor_id "ally_{slot}" — TeamSnapshotFactory),
# đọc stage/hero/skill từ ConfigProvider (đọc-cache), chạy sim client. Thiếu config stage ⇒ bỏ replay (vẫn hiện kết quả server).
func _replay(team, seed: int) -> Dictionary:
	if _config_provider == null or _config_provider.get_entry(_STAGE_TYPE, _stage_id).is_empty():
		return {}
	var ally: Array = []
	for slot in team.slots:
		ally.append({
			"actor_id": "ally_%d" % int(slot.slot_index),
			"hero_id": str(slot.hero_id),
			"slot": int(slot.slot_index),
		})
	var request := {"seed": seed, "stage_id": _stage_id, "ally": ally}
	var input: BattleInput = CombatInputResolver.new().resolve(request, _config_provider)
	return BattleSimulator.new().simulate(input)


func _on_intent(intent_name: StringName, _payload: Dictionary) -> void:
	if intent_name == _INTENT_BACK:
		if _scene_router != null:
			_scene_router.back()
	elif intent_name == _INTENT_RETRY:
		_start()


func _fail(message: String) -> void:
	_status = message
	render({"error_text": message})


# attemptId (idempotency key) mới cho mỗi lần đánh — re-fight = trận mới; server chống double-grant nếu trùng.
func _new_attempt_id() -> String:
	return "att-%d-%d" % [Time.get_ticks_usec(), randi()]


func _format_events(log: Array) -> Array:
	var lines: Array = []
	for event in log:
		if event is Dictionary:
			var line := _describe_event(event)
			if line != "":
				lines.append(line)
	return lines


func _describe_event(event: Dictionary) -> String:
	var type := str(event.get("type", ""))
	match type:
		"RoundStarted":
			return "— Vòng %d —" % int(event.get("round", 0))
		"DamageApplied":
			var crit := " (CHÍ MẠNG)" if bool(event.get("crit", false)) else ""
			return "%s ⚔ %s: -%d HP (còn %d)%s" % [
				str(event.get("actor", "")), str(event.get("target", "")),
				int(event.get("amount", 0)), int(event.get("target_hp_after", 0)), crit]
		"Healed":
			return "%s ✚ %s: +%d HP" % [str(event.get("actor", "")), str(event.get("target", "")), int(event.get("amount", 0))]
		"Miss":
			return "%s trượt %s" % [str(event.get("actor", "")), str(event.get("target", ""))]
		"Death":
			return "%s gục" % str(event.get("target", ""))
		"BattleEnded":
			return "— Kết thúc —"
		_:
			return ""


func _format_rewards(rewards) -> Array:
	var out: Array = []
	if rewards == null:
		return out
	for reward in rewards:
		out.append("%s +%d" % [str(reward.ref_id), int(reward.amount)])
	return out


func _error_code(res) -> String:
	if res != null and res.error != null:
		return str(res.error.code)
	return "network_error"
