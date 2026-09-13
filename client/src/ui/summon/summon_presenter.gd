# SummonPresenter — presenter cho SummonView (Phase 33, Summon/Gacha).
# Trách nhiệm: gửi INTENT triệu hồi qua server (server-authoritative) rồi HIỂN THỊ kết quả server trả:
#   - Banner đọc từ ConfigProvider (đọc-cache, data-driven) — CHỈ để hiển thị (cost/pity); server là chân lý.
#   - POST /api/v1/summon {bannerId, count, requestId} (parse_summon_result) → SummonResult{pulls, pityAfter}.
#   - Sau khi server tiêu tiền + cấp hero/mảnh (atomic + idempotent, Phase 31/32): refresh ví + kho từ server
#     vào StateCache để hub/kho hiển thị số mới. Client KHÔNG tự random / KHÔNG tự quyết kết quả (ADR-011).
# requestId là idempotency key (một lần quay); server chống double khi retry. Fallback KHÔNG im lặng (Rule E):
# lỗi ⇒ nhãn lỗi + nút Thử lại, KHÔNG bịa. Chi tiết: ui-architecture.md, progression-and-economy.md.
class_name SummonPresenter
extends RefCounted

const _INTENT_SELECT: StringName = &"select_banner"
const _INTENT_SUMMON_ONE: StringName = &"summon_one"
const _INTENT_SUMMON_TEN: StringName = &"summon_ten"
const _INTENT_RETRY: StringName = &"retry"
const _INTENT_BACK: StringName = &"back"
const _SUMMON_PATH: String = "/summon"
const _WALLET_PATH: String = "/wallet"
const _INVENTORY_PATH: String = "/inventory"
const _GACHA_TYPE: StringName = &"gacha"
const _SINGLE: int = 1
const _TEN: int = 10

var _view: BaseView = null
# Nguồn đọc/điều hướng/mạng (inject cho test; mặc định = autoload). Chỉ đọc-cache, không tự tính chân lý.
var _state_cache: Node = null
var _config_provider: Node = null
var _scene_router: Node = null
var _network: Node = null

var _banners: Array = []
var _selected: String = ""
var _status: String = ""
var _error_text: String = ""
var _results: Array = []
var _pity_after: int = -1
var _busy: bool = false


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
	_load_banners()
	render()


## Dựng dữ liệu HIỂN THỊ hiện tại → view (banner từ config; kết quả từ server).
func render() -> void:
	_view.set_data({
		"status_text": _status,
		"error_text": _error_text,
		"offline": bool(_state_cache.is_offline()) if _state_cache != null else false,
		"banners": _banners,
		"selected_banner": _selected,
		"can_summon": _selected != "" and not _busy,
		"results": _results,
		"pity_after": _pity_after,
	})


## Không đăng ký EventBus ⇒ dispose không cần huỷ gì (giữ đối xứng vòng đời với các presenter khác).
func dispose() -> void:
	pass


# Đọc danh sách banner từ ConfigProvider (data-driven, đọc-cache). CHỈ để hiển thị (cost/pity) — server là chân lý.
func _load_banners() -> void:
	_banners = []
	if _config_provider == null:
		return
	for banner in _config_provider.get_all(_GACHA_TYPE):
		if not (banner is Dictionary):
			continue
		var id := str(banner.get("id", ""))
		if id == "":
			continue
		_banners.append({"id": id, "label": id, "cost_text": _cost_text(banner)})
	if _selected == "" and not _banners.is_empty():
		_selected = str(_banners[0]["id"])


func _cost_text(banner: Dictionary) -> String:
	var cost: Dictionary = banner.get("cost", {})
	if cost.is_empty():
		return ""
	return "%d %s / lượt" % [int(cost.get("amount", 0)), str(cost.get("currency", ""))]


# Gửi intent triệu hồi (server quyết) → hiển thị kết quả server + refresh ví/kho. Không tự random.
func _summon(count: int) -> void:
	if _network == null or _selected == "" or _busy:
		return
	_busy = true
	_status = "Đang triệu hồi (server)..."
	_error_text = ""
	_results = []
	render()

	var body := {"bannerId": _selected, "count": count, "requestId": _new_request_id()}
	var res = await _network.post_json(_SUMMON_PATH, body, NetworkResponseParser.parse_summon_result)
	_busy = false
	if res == null or not res.ok or res.value == null:
		_status = ""
		_error_text = "Triệu hồi thất bại: %s" % _error_code(res)
		render()
		return

	_present(res.value)
	# Server đã tiêu tiền + cấp hero/mảnh (atomic + idempotent). Refresh ví + kho AUTHORITATIVE từ server vào
	# StateCache (client KHÔNG tự cộng/trừ). Best-effort — lỗi refresh ⇒ bỏ qua (không bịa).
	await _refresh_wallet()
	await _refresh_inventory()


func _present(result) -> void:
	var lines: Array = []
	for pull in result.pulls:
		var star := "★%d" % int(pull.rarity)
		if bool(pull.is_new):
			lines.append("%s %s — MỚI" % [str(pull.hero_id), star])
		else:
			lines.append("%s %s — +%d mảnh" % [str(pull.hero_id), star, int(pull.fragments)])
	_results = lines
	_pity_after = int(result.pity_after)
	_status = "Triệu hồi xong (x%d)." % int(result.count)
	render()


func _refresh_wallet() -> void:
	if _network == null or _state_cache == null:
		return
	var res = await _network.get_json(_WALLET_PATH, NetworkResponseParser.parse_wallet)
	if res == null or not res.ok or res.value == null:
		return
	var balances: Dictionary = {}
	for bal in res.value.balances:
		var code := NetworkResponseParser.currency_code(bal.currency)
		if code != "":
			balances[code] = bal.amount
	_state_cache.apply_wallet(balances)


func _refresh_inventory() -> void:
	if _network == null or _state_cache == null:
		return
	var res = await _network.get_json(_INVENTORY_PATH, NetworkResponseParser.parse_inventory)
	if res == null or not res.ok or res.value == null:
		return
	var items: Array = []
	for stack in res.value.items:
		items.append({"item_type": stack.item_type, "item_id": stack.item_id, "quantity": stack.quantity})
	var heroes: Array = []
	for hero in res.value.owned_heroes:
		heroes.append({"hero_id": hero.hero_id, "level": hero.level, "stars": hero.stars})
	_state_cache.apply_inventory(items, heroes)


func _on_intent(intent_name: StringName, payload: Dictionary) -> void:
	if intent_name == _INTENT_SELECT:
		_selected = str(payload.get("banner_id", _selected))
		_results = []
		render()
	elif intent_name == _INTENT_SUMMON_ONE:
		_summon(_SINGLE)
	elif intent_name == _INTENT_SUMMON_TEN:
		_summon(_TEN)
	elif intent_name == _INTENT_RETRY:
		_load_banners()
		render()
	elif intent_name == _INTENT_BACK:
		if _scene_router != null:
			_scene_router.back()


# requestId (idempotency key) mới cho mỗi lượt quay — server chống double khi trùng. randi() CHỈ để sinh khoá
# duy nhất (KHÔNG dùng để quyết kết quả — kết quả do server random, ADR-011).
func _new_request_id() -> String:
	return "sum-%d-%d" % [Time.get_ticks_usec(), randi()]


func _error_code(res) -> String:
	if res != null and res.error != null:
		return str(res.error.code)
	return "network_error"
