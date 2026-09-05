# HeroDetailPresenter — presenter cho HeroDetailView (Phase 27). Hiển thị CHI TIẾT một hero: ghép owned
# (StateCache: level/sao) + definition (ConfigProvider: faction/class/element/role/rarity/base_stats/skills/
# art). Hero nào: đọc từ SceneRouter.route_context()["hero_id"] (SceneRouter không truyền tham số qua ctor).
# ART TẢI LAZY BẤT ĐỒNG BỘ qua AssetLoader (ADR-009): render text + placeholder NGAY (không chặn), art thật
# đến sau thì đẩy lại; giải phóng art khi rời màn (dispose). KHÔNG network, KHÔNG tự tính chân lý.
# Tự refresh khi config đổi (`config_updated`) / state mới (`state_refreshed`). Chi tiết: ui-architecture.md.
class_name HeroDetailPresenter
extends RefCounted

const _EVENT_CONFIG_UPDATED: StringName = &"config_updated"
const _EVENT_STATE_REFRESHED: StringName = &"state_refreshed"
const _INTENT_BACK: StringName = &"back"

var _view: BaseView = null
var _state_cache: Node = null
var _config_provider: Node = null
var _scene_router: Node = null
var _asset_loader: Node = null

var _hero_id: String = ""
var _art_path: String = ""
var _data: Dictionary = {}


func _init(
		view: BaseView,
		state_cache: Node = null,
		config_provider: Node = null,
		scene_router: Node = null,
		asset_loader: Node = null) -> void:
	_view = view
	_state_cache = state_cache if state_cache != null else StateCache
	_config_provider = config_provider if config_provider != null else ConfigProvider
	_scene_router = scene_router if scene_router != null else SceneRouter
	_asset_loader = asset_loader if asset_loader != null else AssetLoader
	_hero_id = str(_scene_router.route_context().get("hero_id", "")) if _scene_router != null else ""
	_view.intent.connect(_on_intent)
	EventBus.subscribe(_EVENT_CONFIG_UPDATED, _on_refresh_event)
	EventBus.subscribe(_EVENT_STATE_REFRESHED, _on_refresh_event)
	refresh()


## Ghép owned + definition → view (text + placeholder art ngay), rồi tải art thật lazy.
func refresh() -> void:
	var owned: Dictionary = _state_cache.get_hero(_hero_id)
	var definition: Dictionary = _config_provider.get_hero(_hero_id)
	var base_stats: Dictionary = definition.get("base_stats", {}) if definition.has("base_stats") else {}
	_art_path = str(definition.get("art", ""))
	_data = {
		"hero_id": _hero_id,
		"owned": not owned.is_empty(),
		"has_definition": not definition.is_empty(),
		"level": int(owned.get("level", 0)),
		"stars": int(owned.get("stars", 0)),
		"faction": str(definition.get("faction", "?")),
		"class": str(definition.get("class", "?")),
		"element": str(definition.get("element", "?")),
		"role": str(definition.get("role", "?")),
		"rarity": int(definition.get("rarity", 0)),
		"hp": int(base_stats.get("hp", 0)),
		"atk": int(base_stats.get("atk", 0)),
		"def": int(base_stats.get("def", 0)),
		"spd": int(base_stats.get("spd", 0)),
		"skills": definition.get("skills", []),
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
