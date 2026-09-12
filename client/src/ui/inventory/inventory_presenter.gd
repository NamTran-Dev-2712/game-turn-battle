# InventoryPresenter — presenter cho InventoryView (Phase 32, Inventory).
# Trách nhiệm: TẢI kho từ server (server-authoritative) → cache (StateCache) → HIỂN THỊ; lọc theo loại phía
# client trên dữ liệu đã tải. Client KHÔNG tự tính/thay số lượng (ADR-007) — chỉ đọc + hiển thị.
#   - GET /api/v1/inventory (parse_inventory) → InventoryDto{items[], owned_heroes[]}.
#   - StateCache.apply_inventory(items, owned_heroes) → cache đĩa (offline-view) + phát state_refreshed.
#   - render(): đọc StateCache.get_inventory() → GHÉP tên hiển thị từ ConfigProvider (item→catalog, fragment→hero,
#     data-driven) → lọc theo tab đang chọn → đẩy vào view.
# Fallback KHÔNG im lặng (Rule E): tải lỗi mà có cache ⇒ hiển thị cache + nhãn lỗi + nút Thử lại; không cache ⇒
# trạng thái rỗng + Thử lại. KHÔNG bịa dữ liệu. Chi tiết: docs/godot/ui-architecture.md.
class_name InventoryPresenter
extends RefCounted

const _EVENT_STATE_REFRESHED: StringName = &"state_refreshed"
const _EVENT_CONFIG_UPDATED: StringName = &"config_updated"
# Id ý định (khớp InventoryView.INTENT_*) — literal cục bộ (view→presenter một chiều).
const _INTENT_FILTER: StringName = &"filter"
const _INTENT_RETRY: StringName = &"retry"
const _INTENT_BACK: StringName = &"back"
const _INVENTORY_PATH: String = "/inventory"
const _ITEM_TYPE: StringName = &"item"
# Bộ lọc tab.
const FILTER_ALL: String = "all"
const FILTER_HEROES: String = "heroes"
const FILTER_FRAGMENT: String = "fragment"
const FILTER_ITEM: String = "item"

var _view: BaseView = null
# Nguồn đọc/điều hướng/mạng (inject cho test; mặc định = autoload). Chỉ đọc-cache, không tự tính chân lý.
var _state_cache: Node = null
var _config_provider: Node = null
var _scene_router: Node = null
var _network: Node = null

var _filter: String = FILTER_ALL
var _status: String = ""
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
	render()   # hiển thị ngay từ cache (nếu có) — mượt + offline-view.
	_fetch()   # rồi làm tươi từ server.


## Dựng dữ liệu HIỂN THỊ từ cache (StateCache) + tên từ config (ConfigProvider), lọc theo tab → view.
func render() -> void:
	var inventory: Dictionary = _state_cache.get_inventory()
	var items: Array = inventory.get("items", [])
	var heroes: Array = inventory.get("owned_heroes", [])
	var rows: Array = _build_rows(items, heroes)
	_view.set_data({
		"status_text": _status,
		"filter": _filter,
		"rows": rows,
		"empty": rows.is_empty(),
		"offline": bool(_state_cache.is_offline()),
		"error_text": _error_text,
	})


## Huỷ đăng ký EventBus (gọi từ view.unbind — tránh Callable treo khi view rời cây).
func dispose() -> void:
	EventBus.unsubscribe(_EVENT_STATE_REFRESHED, _on_refresh_event)
	EventBus.unsubscribe(_EVENT_CONFIG_UPDATED, _on_refresh_event)


# Tải kho từ server → cache. Server-authoritative: client chỉ phản chiếu (ADR-007). Lỗi ⇒ giữ cache + nhãn lỗi
# (fallback KHÔNG im lặng); KHÔNG bịa dữ liệu.
func _fetch() -> void:
	if _network == null:
		return
	_status = "Đang tải kho..."
	_error_text = ""
	render()
	var res = await _network.get_json(_INVENTORY_PATH, NetworkResponseParser.parse_inventory)
	if res == null or not res.ok or res.value == null:
		_status = ""
		_error_text = "Không tải được kho: %s" % _error_code(res)
		render()   # giữ cache cũ nếu có + hiện nhãn lỗi (Rule E).
		return
	var items: Array = []
	for stack in res.value.items:
		items.append({"item_type": stack.item_type, "item_id": stack.item_id, "quantity": stack.quantity})
	var heroes: Array = []
	for hero in res.value.owned_heroes:
		heroes.append({"hero_id": hero.hero_id, "level": hero.level, "stars": hero.stars})
	_status = ""
	_error_text = ""
	# apply_inventory phát state_refreshed → render() (một nguồn hiển thị = cache).
	_state_cache.apply_inventory(items, heroes)


# Ghép hàng hiển thị theo tab: item/fragment là stack (có số lượng), hero là owned hero (không số lượng).
func _build_rows(items: Array, heroes: Array) -> Array:
	var rows: Array = []
	var want_items := _filter == FILTER_ALL or _filter == FILTER_ITEM
	var want_fragments := _filter == FILTER_ALL or _filter == FILTER_FRAGMENT
	var want_heroes := _filter == FILTER_ALL or _filter == FILTER_HEROES

	for stack in items:
		if not (stack is Dictionary):
			continue
		var item_type := str(stack.get("item_type", ""))
		if item_type == FILTER_ITEM and want_items:
			rows.append(_item_row(stack))
		elif item_type == FILTER_FRAGMENT and want_fragments:
			rows.append(_fragment_row(stack))

	if want_heroes:
		for hero in heroes:
			if hero is Dictionary:
				rows.append(_hero_row(hero))
	return rows


# Vật phẩm: tên từ config catalog (data-driven), rơi về id nếu chưa có config.
func _item_row(stack: Dictionary) -> Dictionary:
	var id := str(stack.get("item_id", "?"))
	var name := id
	if _config_provider != null:
		var definition: Dictionary = _config_provider.get_entry(_ITEM_TYPE, id)
		name = str(definition.get("name", id)) if not definition.is_empty() else id
	return {"kind": FILTER_ITEM, "label": name, "quantity": int(stack.get("quantity", 0))}


# Mảnh: tham chiếu hero (data-driven qua config hero) — hiển thị "Mảnh <hero>".
func _fragment_row(stack: Dictionary) -> Dictionary:
	var id := str(stack.get("item_id", "?"))
	return {"kind": FILTER_FRAGMENT, "label": "Mảnh %s" % id, "quantity": int(stack.get("quantity", 0))}


# Hero sở hữu (chiếu từ inventory) — không số lượng (quantity = -1 ⇒ view ẩn "xN").
func _hero_row(hero: Dictionary) -> Dictionary:
	var id := str(hero.get("hero_id", "?"))
	return {"kind": FILTER_HEROES, "label": "%s · Lv.%d ★%d" % [id, int(hero.get("level", 0)), int(hero.get("stars", 0))], "quantity": -1}


func _on_refresh_event(_payload) -> void:
	render()


func _on_intent(intent_name: StringName, payload: Dictionary) -> void:
	if intent_name == _INTENT_FILTER:
		_filter = str(payload.get("filter", FILTER_ALL))
		render()
	elif intent_name == _INTENT_RETRY:
		_fetch()
	elif intent_name == _INTENT_BACK:
		if _scene_router != null:
			_scene_router.back()


func _error_code(res) -> String:
	if res != null and res.error != null:
		return str(res.error.code)
	return "network_error"
