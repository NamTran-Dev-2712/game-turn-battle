# HeroListPresenter — presenter cho HeroListView (Phase 27, Hero System THẬT — nâng cấp màn mẫu Phase 22).
# GHÉP hai nguồn ĐỌC (KHÔNG network trực tiếp, KHÔNG tự tính chân lý):
#   - StateCache.get_heroes()  → hero NGƯỜI CHƠI SỞ HỮU (server-authoritative: id, level, sao).
#   - ConfigProvider.get_hero(id) → ĐỊNH NGHĨA từ config (data-driven: rarity/class/element/role/faction).
# → đẩy vào view qua `set_data`; nghe `intent` của view → dịch hành động:
#   - open_hero {id} → SceneRouter.goto_scene(hero_detail, {"hero_id": id}) (mở màn chi tiết).
#   - retry → ConfigProvider.check_for_update() (tải bundle mới nếu có) → refresh. Fallback KHÔNG im lặng.
#   - back  → SceneRouter.back().
# Tự refresh khi: config đổi version (`config_updated` → định nghĩa đổi, data-driven KHÔNG rebuild) HOẶC
# state mới (`state_refreshed` → hero owned đổi). Chi tiết: docs/godot/ui-architecture.md.
class_name HeroListPresenter
extends RefCounted

const _EVENT_CONFIG_UPDATED: StringName = &"config_updated"
const _EVENT_STATE_REFRESHED: StringName = &"state_refreshed"
# Id ý định (khớp HeroListView.INTENT_*) — literal cục bộ, tránh phụ thuộc vòng (view→presenter một chiều).
const _INTENT_RETRY: StringName = &"retry"
const _INTENT_BACK: StringName = &"back"
const _INTENT_OPEN_HERO: StringName = &"open_hero"
## Màn chi tiết hero (Phase 27) — điều hướng kèm ngữ cảnh hero_id qua SceneRouter.
const HERO_DETAIL_PATH: String = "res://src/ui/hero_detail/hero_detail.tscn"

var _view: BaseView = null
# Nguồn đọc/điều hướng (inject cho test; mặc định = autoload). CHỈ đọc-cache, không network.
var _state_cache: Node = null
var _config_provider: Node = null
var _scene_router: Node = null


func _init(view: BaseView, state_cache: Node = null, config_provider: Node = null, scene_router: Node = null) -> void:
	_view = view
	_state_cache = state_cache if state_cache != null else StateCache
	_config_provider = config_provider if config_provider != null else ConfigProvider
	_scene_router = scene_router if scene_router != null else SceneRouter
	_view.intent.connect(_on_intent)
	EventBus.subscribe(_EVENT_CONFIG_UPDATED, _on_refresh_event)
	EventBus.subscribe(_EVENT_STATE_REFRESHED, _on_refresh_event)
	refresh()


## Ghép hero owned (StateCache) + definition (ConfigProvider) → view. Kèm nhãn version + trạng thái stale.
func refresh() -> void:
	var owned: Array = _state_cache.get_heroes()
	var rows: Array = []
	for owned_hero in owned:
		if not (owned_hero is Dictionary):
			continue
		var id: String = str(owned_hero.get("id", "?"))
		var definition: Dictionary = _config_provider.get_hero(id)
		rows.append({
			"id": id,
			"level": int(owned_hero.get("level", 0)),
			"stars": int(owned_hero.get("stars", 0)),
			"rarity": int(definition.get("rarity", 0)),
			"class": str(definition.get("class", "?")),
			"element": str(definition.get("element", "?")),
			"role": str(definition.get("role", "?")),
			"has_definition": not definition.is_empty(),
		})
	var label: String = _config_provider.config_label()
	_view.set_data({
		"version_label": "Config: %s" % (label if label != "" else "chưa có"),
		"stale": bool(_config_provider.is_stale()),
		"error_code": _config_provider.last_error_code(),
		"offline": bool(_state_cache.is_offline()),
		"heroes": rows,
	})


## Huỷ đăng ký EventBus (gọi từ view.unbind — tránh Callable treo khi view rời cây).
func dispose() -> void:
	EventBus.unsubscribe(_EVENT_CONFIG_UPDATED, _on_refresh_event)
	EventBus.unsubscribe(_EVENT_STATE_REFRESHED, _on_refresh_event)


# Config đổi version / state mới → refresh hiển thị (data-driven, KHÔNG rebuild client).
func _on_refresh_event(_payload) -> void:
	refresh()


# Dịch ý định từ view.
func _on_intent(intent_name: StringName, payload: Dictionary) -> void:
	if intent_name == _INTENT_OPEN_HERO:
		var id: String = str(payload.get("id", ""))
		if id != "" and _scene_router != null:
			_scene_router.goto_scene(HERO_DETAIL_PATH, {"hero_id": id})
	elif intent_name == _INTENT_RETRY:
		_retry()
	elif intent_name == _INTENT_BACK:
		if _scene_router != null:
			_scene_router.back()


# Thử lại tải config: check_for_update (best-effort) → refresh. apply_bundle phát config_updated (→ refresh)
# khi có version mới; gọi refresh() ở đây để cập nhật cả nhánh no-op/stale. KHÔNG bịa dữ liệu.
func _retry() -> void:
	if _config_provider == null:
		return
	await _config_provider.check_for_update()
	refresh()
