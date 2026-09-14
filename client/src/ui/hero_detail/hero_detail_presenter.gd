# HeroDetailPresenter — presenter cho HeroDetailView (Phase 27, + nâng cấp Phase 35). Hiển thị CHI TIẾT một
# hero: ghép owned (StateCache: level/sao) + definition (ConfigProvider: faction/class/…/base_stats/skills/art).
# Phase 35: hiển thị chỉ số THEO CẤP (base × cấp, công thức HeroStats — data-driven từ economy config) + Power
# Rating + chi phí nâng cấp + gold; nút "Nâng cấp" gửi INTENT → POST /heroes/{id}/level-up (server-authoritative)
# → refresh hero + ví AUTHORITATIVE từ server vào StateCache. Client KHÔNG tự tăng cấp/chỉ số/Power/trừ tiền.
# Hero nào: đọc từ SceneRouter.route_context()["hero_id"]. ART TẢI LAZY qua AssetLoader (ADR-009): render text +
# placeholder NGAY (không chặn), art thật đến sau thì đẩy lại; giải phóng art khi rời màn (dispose). Presenter là
# touchpoint mạng DUY NHẤT (view network-free). Tự refresh khi config đổi / state mới. Chi tiết: ui-architecture.md.
class_name HeroDetailPresenter
extends RefCounted

const _EVENT_CONFIG_UPDATED: StringName = &"config_updated"
const _EVENT_STATE_REFRESHED: StringName = &"state_refreshed"
const _INTENT_BACK: StringName = &"back"
const _INTENT_LEVEL_UP: StringName = &"level_up"
const _ECONOMY_TYPE: StringName = &"economy"
const _ECONOMY_ID: String = "economy_default"
const _GOLD_CODE: String = "gold"
const _HEROES_PATH: String = "/heroes"
const _WALLET_PATH: String = "/wallet"

var _view: BaseView = null
var _state_cache: Node = null
var _config_provider: Node = null
var _scene_router: Node = null
var _asset_loader: Node = null
var _network: Node = null

var _hero_id: String = ""
var _art_path: String = ""
var _status_text: String = ""
var _error_text: String = ""
var _data: Dictionary = {}


func _init(
		view: BaseView,
		state_cache: Node = null,
		config_provider: Node = null,
		scene_router: Node = null,
		asset_loader: Node = null,
		network_client: Node = null) -> void:
	_view = view
	_state_cache = state_cache if state_cache != null else StateCache
	_config_provider = config_provider if config_provider != null else ConfigProvider
	_scene_router = scene_router if scene_router != null else SceneRouter
	_asset_loader = asset_loader if asset_loader != null else AssetLoader
	_network = network_client if network_client != null else NetworkClient
	_hero_id = str(_scene_router.route_context().get("hero_id", "")) if _scene_router != null else ""
	_view.intent.connect(_on_intent)
	EventBus.subscribe(_EVENT_CONFIG_UPDATED, _on_refresh_event)
	EventBus.subscribe(_EVENT_STATE_REFRESHED, _on_refresh_event)
	refresh()


## Ghép owned + definition → view (chỉ số THEO CẤP + Power + chi phí nâng cấp; text + placeholder art ngay),
## rồi tải art thật lazy.
func refresh() -> void:
	var owned: Dictionary = _state_cache.get_hero(_hero_id)
	var definition: Dictionary = _config_provider.get_hero(_hero_id)
	var base_stats: Dictionary = definition.get("base_stats", {}) if definition.has("base_stats") else {}
	var economy: Dictionary = _economy_config()
	_art_path = str(definition.get("art", ""))

	var owned_flag := not owned.is_empty()
	var level := int(owned.get("level", 0))
	var display_level: int = level if level >= 1 else 1
	var growth := HeroStats.growth_bp(economy)
	var scaled: Dictionary = HeroStats.scaled_stats(base_stats, display_level, growth)
	var power: int = HeroStats.power(scaled, HeroStats.power_weights(economy)) if not base_stats.is_empty() else 0

	var has_economy := not economy.is_empty()
	var cost := HeroStats.level_up_cost(economy, level) if (has_economy and owned_flag) else -1
	var is_max := has_economy and owned_flag and cost < 0
	var gold := int(_state_cache.get_currency(_GOLD_CODE))
	var can_upgrade := owned_flag and has_economy and cost >= 0 and gold >= cost and _status_text == ""

	_data = {
		"hero_id": _hero_id,
		"owned": owned_flag,
		"has_definition": not definition.is_empty(),
		"level": level,
		"stars": int(owned.get("stars", 0)),
		"faction": str(definition.get("faction", "?")),
		"class": str(definition.get("class", "?")),
		"element": str(definition.get("element", "?")),
		"role": str(definition.get("role", "?")),
		"rarity": int(definition.get("rarity", 0)),
		# Chỉ số HIỂN THỊ theo cấp (data-driven; công thức khớp server HeroStatCalculator).
		"hp": int(scaled.get("hp", 0)),
		"atk": int(scaled.get("atk", 0)),
		"def": int(scaled.get("def", 0)),
		"spd": int(scaled.get("spd", 0)),
		"power": power,
		"skills": definition.get("skills", []),
		"has_economy": has_economy,
		"upgrade_cost": cost,
		"gold": gold,
		"is_max_level": is_max,
		"can_upgrade": can_upgrade,
		"status_text": _status_text,
		"error_text": _error_text,
		# Placeholder ngay (list/detail không chặn chờ art) — art thật đẩy lại sau.
		"art_texture": _asset_loader.placeholder() if _asset_loader != null else null,
	}
	_view.set_data(_data)
	_load_art()


## Huỷ đăng ký EventBus + giải phóng art (gọi từ view.unbind).
func dispose() -> void:
	EventBus.unsubscribe(_EVENT_CONFIG_UPDATED, _on_refresh_event)
	EventBus.unsubscribe(_EVENT_STATE_REFRESHED, _on_refresh_event)
	if _asset_loader != null and _art_path != "":
		_asset_loader.release(_art_path)


# Tải art LAZY (coroutine): nạp texture theo path config → đẩy lại vào view. Thiếu path/lỗi ⇒ placeholder
# (AssetLoader tự lo). KHÔNG chặn: refresh() đã render text + placeholder trước khi gọi hàm này.
func _load_art() -> void:
	if _asset_loader == null:
		return
	var texture: Texture2D = await _asset_loader.load_texture(_art_path)
	_data["art_texture"] = texture
	_view.set_data(_data)


func _on_refresh_event(_payload) -> void:
	refresh()


func _on_intent(intent_name: StringName, _payload: Dictionary) -> void:
	if intent_name == _INTENT_BACK and _scene_router != null:
		_scene_router.back()
	elif intent_name == _INTENT_LEVEL_UP:
		_level_up()


# Nâng cấp hero (server-authoritative, Phase 35): gửi INTENT rỗng → server tính chi phí/cấp/chỉ số. Thành công ⇒
# refresh hero + ví AUTHORITATIVE từ server vào StateCache (fires state_refreshed → refresh). Client KHÔNG tự
# tăng cấp/chỉ số/trừ tiền; thất bại (thiếu gold/max cấp) ⇒ hiển thị mã lỗi, không mutate.
func _level_up() -> void:
	if _network == null or _hero_id == "":
		return
	if _status_text != "":
		return  # tránh double-submit khi đang nâng
	_status_text = "Đang nâng cấp..."
	_error_text = ""
	refresh()

	var path := "%s/%s/level-up" % [_HEROES_PATH, _hero_id]
	var res = await _network.post_json(path, {}, NetworkResponseParser.parse_level_up_hero_response)
	_status_text = ""
	if res == null or not res.ok or res.value == null:
		_error_text = "Nâng cấp thất bại: %s" % _error_code(res)
		refresh()
		return

	# Server đã tăng cấp + trừ gold + tính chỉ số/Power (atomic). Đọc lại hero + ví AUTHORITATIVE vào StateCache
	# ⇒ state_refreshed → refresh() vẽ cấp/chỉ số/gold mới. (apply_* tự phát state_refreshed.)
	await _refresh_heroes()
	await _refresh_wallet()
	_status_text = ""
	_error_text = ""
	refresh()


# Đọc lại danh sách hero sở hữu từ server → StateCache.apply_heroes (server-authoritative). Lỗi ⇒ bỏ qua (không bịa).
func _refresh_heroes() -> void:
	if _network == null or _state_cache == null:
		return
	var res = await _network.get_json(_HEROES_PATH, NetworkResponseParser.parse_my_heroes)
	if res == null or not res.ok or res.value == null:
		return
	var heroes: Array = []
	for h in res.value:
		heroes.append({"id": h.hero_id, "level": h.level, "stars": h.stars})
	_state_cache.apply_heroes(heroes)


# Đọc lại số dư ví từ server → StateCache.apply_wallet (server-authoritative). Lỗi ⇒ bỏ qua.
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


func _error_code(res) -> String:
	if res != null and res.error != null:
		return str(res.error.code)
	return "network_error"


# Đọc economy config (đường cong cấp/tăng trưởng/power) qua ConfigProvider; {} nếu thiếu (⇒ chỉ số nền, không
# chi phí). has_method để tương thích provider giả trong test không hiện thực get_entry.
func _economy_config() -> Dictionary:
	if _config_provider != null and _config_provider.has_method("get_entry"):
		var e: Variant = _config_provider.get_entry(_ECONOMY_TYPE, _ECONOMY_ID)
		if e is Dictionary:
			return e
	return {}
