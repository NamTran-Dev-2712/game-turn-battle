# HeroListPresenter — presenter cho HeroListView (Phase 22, màn MẪU config e2e). ĐỌC dữ liệu hero từ
# ConfigProvider (cửa đọc config DUY NHẤT — data-driven, KHÔNG hardcode, KHÔNG network trực tiếp) →
# đẩy vào view qua `set_data`; nghe `intent` của view → dịch thành hành động.
#   - Retry → await ConfigProvider.check_for_update() (tải bundle mới nếu có) → refresh hiển thị.
#             Xử lý no-cache (Case C) + stale/fallback (Case B) — báo rõ, KHÔNG im lặng (Rule E).
#   - Back  → SceneRouter.back() về hub.
# Tự refresh khi config đổi version qua EventBus `config_updated` (server publish → client thấy đổi
# KHÔNG rebuild). KHÔNG gọi NetworkClient trực tiếp, KHÔNG tự tính chân lý. Chi tiết: ui-architecture.md.
class_name HeroListPresenter
extends RefCounted

const _EVENT_CONFIG_UPDATED: StringName = &"config_updated"
# Id ý định (khớp HeroListView.INTENT_*). Dùng literal cục bộ — KHÔNG tham chiếu lớp HeroListView để
# tránh phụ thuộc vòng (view→presenter là chiều DUY NHẤT, như cặp MainHub).
const _INTENT_RETRY: StringName = &"retry"
const _INTENT_BACK: StringName = &"back"

var _view: BaseView = null
# Nguồn đọc/điều hướng (inject cho test; mặc định = autoload). ConfigProvider CHỈ đọc-cache config.
var _config_provider: Node = null
var _scene_router: Node = null


func _init(view: BaseView, config_provider: Node = null, scene_router: Node = null) -> void:
	_view = view
	_config_provider = config_provider if config_provider != null else ConfigProvider
	_scene_router = scene_router if scene_router != null else SceneRouter
	_view.intent.connect(_on_intent)
	# Config đổi version (server publish → client tải) → hiển thị lại. Huỷ đăng ký ở dispose() (view.unbind).
	EventBus.subscribe(_EVENT_CONFIG_UPDATED, _on_config_updated)
	refresh()


## Đọc hero từ ConfigProvider (data-driven) → view. Kèm nhãn version + trạng thái stale (fallback rõ ràng).
func refresh() -> void:
	var heroes: Array = _config_provider.get_all(&"hero")
	var rows: Array = []
	for hero in heroes:
		if hero is Dictionary:
			rows.append({
				"id": str(hero.get("id", "?")),
				"rarity": int(hero.get("rarity", 0)),
				"class": str(hero.get("class", "?")),
			})
	var label: String = _config_provider.config_label()
	_view.set_data({
		"version_label": "Config: %s" % (label if label != "" else "chưa có"),
		"stale": bool(_config_provider.is_stale()),
		"error_code": _config_provider.last_error_code(),
		"heroes": rows,
	})


## Huỷ đăng ký EventBus (gọi từ view.unbind — tránh Callable treo khi view rời cây).
func dispose() -> void:
	EventBus.unsubscribe(_EVENT_CONFIG_UPDATED, _on_config_updated)


# Config đổi version → refresh hiển thị (không rebuild client).
func _on_config_updated(_payload) -> void:
	refresh()


# Dịch ý định từ view.
func _on_intent(intent_name: StringName, _payload: Dictionary) -> void:
	if intent_name == _INTENT_RETRY:
		_retry()
	elif intent_name == _INTENT_BACK:
		if _scene_router != null:
			_scene_router.back()


# Thử lại tải config: check_for_update (best-effort) → refresh. apply_bundle sẽ phát config_updated
# (→ _on_config_updated → refresh) khi có version mới; gọi refresh() ở đây để cập nhật cả nhánh
# no-op/stale (không phát event). KHÔNG bịa dữ liệu — chỉ hiển thị lại trạng thái thật.
func _retry() -> void:
	if _config_provider == null:
		return
	await _config_provider.check_for_update()
	refresh()
