# CampaignPresenter — presenter cho CampaignView (Phase 34, Campaign PvE).
# Trách nhiệm: HIỂN THỊ chuỗi stage campaign + trạng thái mở/khoá/đã-clear (SERVER-AUTHORITATIVE) và cho phép
# đánh một stage đã mở khoá:
#   - GET /campaign/progress (parse_campaign_progress) → StateCache.apply_campaign_progress (đọc-cache).
#   - Ghép trạng thái server (stages: cleared/unlocked/order/chapter) với preview địch/thưởng từ ConfigProvider.
#   - open_stage {stage_id} (chỉ khi unlocked) → SceneRouter.goto_scene(battle, {"campaign_stage_id": id}).
#     BattlePresenter POST /campaign/battles; quay lại (back) ⇒ scene re-instantiate ⇒ tự _fetch lại tiến độ.
# Client KHÔNG tự suy mở-khoá/tiến độ (ADR-007/011): mọi trạng thái LẤY TỪ server. Fallback KHÔNG im lặng
# (Rule E): lỗi tải ⇒ giữ cache + hiện lỗi + nút Thử lại. Chi tiết: docs/godot/ui-architecture.md.
class_name CampaignPresenter
extends RefCounted

const _EVENT_CONFIG_UPDATED: StringName = &"config_updated"
const _EVENT_STATE_REFRESHED: StringName = &"state_refreshed"
# Id ý định (khớp CampaignView.INTENT_*) — literal cục bộ (view→presenter một chiều).
const _INTENT_OPEN_STAGE: StringName = &"open_stage"
const _INTENT_RETRY: StringName = &"retry"
const _INTENT_BACK: StringName = &"back"
const _PROGRESS_PATH: String = "/campaign/progress"
## Màn battle dùng chung (Phase 30) — vào kèm context campaign_stage_id ⇒ POST /campaign/battles.
const BATTLE_PATH: String = "res://src/ui/battle/battle.tscn"
const _STAGE_TYPE: StringName = &"stage"

var _view: BaseView = null
# Nguồn đọc/điều hướng/mạng (inject cho test; mặc định = autoload). Chỉ đọc-cache, không tự tính chân lý.
var _state_cache: Node = null
var _config_provider: Node = null
var _scene_router: Node = null
var _network: Node = null

var _error_text: String = ""


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
	EventBus.subscribe(_EVENT_STATE_REFRESHED, _on_refresh_event)
	EventBus.subscribe(_EVENT_CONFIG_UPDATED, _on_refresh_event)
	render()   # hiển thị ngay từ cache (offline-view)
	_fetch()   # rồi refresh authoritative từ server


## Dựng dữ liệu hiển thị: stages (server-authoritative) ghép preview địch/thưởng (config). Đẩy vào view.
func render() -> void:
	var progress: Dictionary = _state_cache.get_campaign_progress()
	var server_stages: Array = progress.get("stages", [])
	var rows: Array = []
	for stage in server_stages:
		if not (stage is Dictionary):
			continue
		var stage_id: String = str(stage.get("stage_id", "?"))
		var definition: Dictionary = _config_provider.get_entry(_STAGE_TYPE, stage_id)
		rows.append({
			"stage_id": stage_id,
			"chapter_id": str(stage.get("chapter_id", "")),
			"order": int(stage.get("order", 0)),
			"cleared": bool(stage.get("cleared", false)),
			"unlocked": bool(stage.get("unlocked", false)),
			"enemies": _enemy_ids(definition),
			"rewards": _reward_ids(definition),
		})
	rows.sort_custom(func(a, b): return int(a["order"]) < int(b["order"]))
	_view.set_data({
		"current_afk_stage_id": str(progress.get("current_afk_stage_id", "")),
		"stages": rows,
		"offline": bool(_state_cache.is_offline()),
		"error_text": _error_text,
	})


## Huỷ đăng ký EventBus (gọi từ view.unbind — tránh Callable treo khi view rời cây).
func dispose() -> void:
	EventBus.unsubscribe(_EVENT_STATE_REFRESHED, _on_refresh_event)
	EventBus.unsubscribe(_EVENT_CONFIG_UPDATED, _on_refresh_event)


# Tải tiến độ authoritative từ server → StateCache (phát state_refreshed → render). Lỗi ⇒ giữ cache + hiện lỗi.
func _fetch() -> void:
	if _network == null:
		return
	var res = await _network.get_json(_PROGRESS_PATH, NetworkResponseParser.parse_campaign_progress)
	if res == null or not res.ok or res.value == null:
		_error_text = "Không tải được tiến độ: %s" % _error_code(res)
		render()
		return
	_error_text = ""
	_state_cache.apply_campaign_progress(_to_state(res.value))


# CampaignProgressDto → Dictionary phẳng cho StateCache (server-authoritative, chỉ hiển thị).
func _to_state(dto) -> Dictionary:
	var stages: Array = []
	for s in dto.stages:
		stages.append({
			"stage_id": str(s.stage_id),
			"chapter_id": str(s.chapter_id),
			"order": int(s.order),
			"cleared": bool(s.cleared),
			"unlocked": bool(s.unlocked),
		})
	return {"stages": stages, "current_afk_stage_id": str(dto.current_afk_stage_id)}


func _on_refresh_event(_payload) -> void:
	render()


# Dịch ý định từ view.
func _on_intent(intent_name: StringName, payload: Dictionary) -> void:
	if intent_name == _INTENT_OPEN_STAGE:
		_open_stage(str(payload.get("stage_id", "")))
	elif intent_name == _INTENT_RETRY:
		_fetch()
	elif intent_name == _INTENT_BACK:
		if _scene_router != null:
			_scene_router.back()


# Chỉ mở stage ĐÃ MỞ KHOÁ (UX chặn sớm; server vẫn validate lại — không tin client). Vào battle kèm context.
func _open_stage(stage_id: String) -> void:
	if stage_id == "" or _scene_router == null:
		return
	if not _is_unlocked(stage_id):
		return
	_scene_router.goto_scene(BATTLE_PATH, {"campaign_stage_id": stage_id})


func _is_unlocked(stage_id: String) -> bool:
	for stage in _state_cache.get_campaign_progress().get("stages", []):
		if stage is Dictionary and str(stage.get("stage_id", "")) == stage_id:
			return bool(stage.get("unlocked", false))
	return false


func _enemy_ids(definition: Dictionary) -> Array:
	var ids: Array = []
	for enemy in definition.get("enemies", []):
		if enemy is Dictionary and enemy.has("hero_id"):
			ids.append(str(enemy["hero_id"]))
	return ids


func _reward_ids(definition: Dictionary) -> Array:
	var ids: Array = []
	for reward in definition.get("rewards", []):
		ids.append(str(reward))
	return ids


func _error_code(res) -> String:
	if res != null and res.error != null:
		return str(res.error.code)
	return "network_error"
