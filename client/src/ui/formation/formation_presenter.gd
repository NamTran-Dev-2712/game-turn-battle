# FormationPresenter — presenter cho FormationView (Phase 29, Đội hình & Team-of-6).
# Trách nhiệm: dựng UI đội hình từ hai nguồn ĐỌC + gửi INTENT lưu qua server (KHÔNG tự quyết chân lý):
#   - ConfigProvider.get_entry("formation","formation_default") → lưới rows×cols (data-driven, KHÔNG hardcode).
#   - StateCache.get_heroes() → hero NGƯỜI CHƠI SỞ HỮU (server-authoritative) làm roster để chọn.
#   - NetworkClient.get_json("/team", parse_team) → đội đã lưu (server-authoritative) làm bản nháp khởi tạo.
# Người chơi chỉnh BẢN NHÁP cục bộ (chọn hero → đặt ô → đổi vị trí); khi Lưu → POST /team (intent). Server
# validate (đúng số ô, không trùng, thuộc sở hữu) rồi trả đội chuẩn → hiển thị lại theo SERVER (bản nháp bị
# thay bằng đội server). KHÔNG lưu cục bộ rồi coi như server đã nhận (ADR-007/011). Chi tiết: ui-architecture.md.
class_name FormationPresenter
extends RefCounted

const _EVENT_STATE_REFRESHED: StringName = &"state_refreshed"
# Id ý định (khớp FormationView.INTENT_*) — literal cục bộ, giữ view→presenter một chiều.
const _INTENT_SELECT_HERO: StringName = &"select_hero"
const _INTENT_PLACE_SLOT: StringName = &"place_slot"
const _INTENT_CLEAR_SLOT: StringName = &"clear_slot"
const _INTENT_SAVE: StringName = &"save"
const _INTENT_BACK: StringName = &"back"

const _TEAM_PATH: String = "/team"
const _FORMATION_TYPE: StringName = &"formation"
const _FORMATION_ID: String = "formation_default"

var _view: BaseView = null
# Nguồn đọc/điều hướng/mạng (inject cho test; mặc định = autoload).
var _state_cache: Node = null
var _config_provider: Node = null
var _scene_router: Node = null
var _network: Node = null

# Lưới (từ config) + bản nháp cục bộ (slot_index → hero_id, "" = trống) + hero đang chọn.
var _rows: int = 0
var _cols: int = 0
var _draft: PackedStringArray = PackedStringArray()
var _selected_hero: String = ""
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
	# Owned hero đổi (snapshot mới) → cập nhật roster.
	EventBus.subscribe(_EVENT_STATE_REFRESHED, _on_state_refreshed)
	_read_grid()
	render()
	# Tải đội đã lưu (server-authoritative) làm bản nháp khởi tạo — best-effort, không chặn hiển thị lưới.
	_load_team()


## Đọc kích thước lưới từ config (data-driven). Thiếu config ⇒ lưới rỗng (0 ô) — không hardcode 2×3.
func _read_grid() -> void:
	var grid: Dictionary = _config_provider.get_entry(_FORMATION_TYPE, _FORMATION_ID)
	_rows = int(grid.get("rows", 0))
	_cols = int(grid.get("cols", 0))
	var slot_count: int = max(_rows * _cols, 0)
	_draft = PackedStringArray()
	_draft.resize(slot_count)


## Dựng dữ liệu HIỂN THỊ (lưới + ô + roster owned + trạng thái) → view. Chỉ đọc, không tính chân lý.
func render() -> void:
	var slots: Array = []
	for i in _draft.size():
		slots.append({"slot_index": i, "hero_id": _draft[i]})
	var roster: Array = []
	for owned in _state_cache.get_heroes():
		if owned is Dictionary:
			roster.append({"id": str(owned.get("id", "?")), "selected": str(owned.get("id", "")) == _selected_hero})
	_view.set_data({
		"rows": _rows,
		"cols": _cols,
		"slots": slots,
		"roster": roster,
		"selected_hero": _selected_hero,
		"status_text": _status,
		"offline": bool(_state_cache.is_offline()),
	})


## Huỷ đăng ký EventBus (gọi từ view.unbind — tránh Callable treo khi view rời cây).
func dispose() -> void:
	EventBus.unsubscribe(_EVENT_STATE_REFRESHED, _on_state_refreshed)


func _on_state_refreshed(_payload) -> void:
	render()


# Dịch ý định từ view.
func _on_intent(intent_name: StringName, payload: Dictionary) -> void:
	if intent_name == _INTENT_SELECT_HERO:
		_selected_hero = str(payload.get("hero_id", ""))
		render()
	elif intent_name == _INTENT_PLACE_SLOT:
		_place(int(payload.get("slot_index", -1)))
	elif intent_name == _INTENT_CLEAR_SLOT:
		_clear(int(payload.get("slot_index", -1)))
	elif intent_name == _INTENT_SAVE:
		_save()
	elif intent_name == _INTENT_BACK:
		if _scene_router != null:
			_scene_router.back()


# Đặt hero đang chọn vào ô (bỏ khỏi ô cũ nếu đã ở đâu đó — một-hero-một-ô). Không có hero chọn ⇒ bỏ qua.
func _place(slot_index: int) -> void:
	if slot_index < 0 or slot_index >= _draft.size() or _selected_hero == "":
		return
	for i in _draft.size():
		if _draft[i] == _selected_hero:
			_draft[i] = ""
	_draft[slot_index] = _selected_hero
	render()


func _clear(slot_index: int) -> void:
	if slot_index < 0 or slot_index >= _draft.size():
		return
	_draft[slot_index] = ""
	render()


# Gửi INTENT lưu: POST /team các ô đã có hero. Thành công ⇒ hiển thị lại theo đội SERVER trả về (chân lý);
# thất bại ⇒ báo lỗi, GIỮ bản nháp (KHÔNG bịa là đã lưu). Client không tự lưu cục bộ.
func _save() -> void:
	if _network == null:
		return
	var slots: Array = []
	for i in _draft.size():
		if _draft[i] != "":
			slots.append({"slotIndex": i, "heroId": _draft[i]})
	var res = await _network.post_json(_TEAM_PATH, {"slots": slots}, NetworkResponseParser.parse_team)
	if res != null and res.ok and res.value != null:
		_apply_server_team(res.value)
		_status = "Đã lưu đội hình."
	else:
		_status = "Lưu thất bại: %s" % _error_code(res)
	render()


# Tải đội đã lưu từ server làm bản nháp khởi tạo. Thất bại ⇒ giữ lưới trống (không bịa).
func _load_team() -> void:
	if _network == null:
		return
	var res = await _network.get_json(_TEAM_PATH, NetworkResponseParser.parse_team)
	if res != null and res.ok and res.value != null:
		_apply_server_team(res.value)
		render()


# Áp đội SERVER-authoritative vào bản nháp (thay hoàn toàn). value = TeamDto (slots: Array[TeamSlotDto]).
func _apply_server_team(team) -> void:
	for i in _draft.size():
		_draft[i] = ""
	if team == null:
		return
	for slot in team.slots:
		var idx: int = int(slot.slot_index)
		if idx >= 0 and idx < _draft.size():
			_draft[idx] = str(slot.hero_id)


func _error_code(res) -> String:
	if res != null and res.error != null:
		return str(res.error.code)
	return "network_error"
