# MainHubPresenter — presenter/view-model cho MainHubView (Phase 17 + profile phase 20). Minh hoạ phía
# presenter của hợp đồng UI: ĐỌC dữ liệu hiển thị từ StateCache/ConfigProvider (đọc-cache, KHÔNG network) →
# đẩy vào view qua `set_data`; nghe `intent` của view → dịch thành hành động.
# Phase 20: hiển thị profile server-authoritative từ StateCache (tên/level) + nhãn offline/cached; tự
# refresh khi có snapshot mới qua EventBus `state_refreshed`. Currency = PLACEHOLDER (profile chưa mang
# currency — feature phase 31). KHÔNG gọi NetworkClient, KHÔNG tự tính chân lý. Chi tiết: ui-architecture.md.
class_name MainHubPresenter
extends RefCounted

const _EVENT_STATE_REFRESHED: StringName = &"state_refreshed"

var _view: BaseView = null
# Nguồn đọc (inject cho test; mặc định = autoload). CHỈ đọc-cache, không network.
var _state_cache: Node = null
var _config_provider: Node = null


func _init(view: BaseView, state_cache: Node = null, config_provider: Node = null) -> void:
	_view = view
	_state_cache = state_cache if state_cache != null else StateCache
	_config_provider = config_provider if config_provider != null else ConfigProvider
	_view.intent.connect(_on_intent)
	# Vào lại hub sau khi profile về (boot/refresh) → hiển thị lại. Huỷ đăng ký ở `dispose()` (view.unbind).
	EventBus.subscribe(_EVENT_STATE_REFRESHED, _on_state_refreshed)
	refresh()


## Chuẩn bị dữ liệu HIỂN THỊ (config version + profile + nhãn online/offline) từ cache đọc → view.
func refresh() -> void:
	var label: String = _config_provider.config_label()
	var offline: bool = _state_cache.is_offline()
	var profile: Dictionary = _state_cache.get_profile()
	var status_text := "Config: %s · %s" % [
		label if label != "" else "chưa có",
		"offline" if offline else "online",
	]
	var profile_text: String
	if profile.is_empty():
		profile_text = "Chưa có profile"
	else:
		profile_text = "%s · Lv.%d" % [str(profile.get("displayName", "?")), int(profile.get("level", 0))]
	_view.set_data({
		"status_text": status_text,
		"profile_text": profile_text,
		"currency_text": _format_currency(_state_cache.get_currencies()),
		"offline": offline,
	})


## Huỷ đăng ký EventBus (gọi từ view.unbind — tránh Callable treo khi view rời cây).
func dispose() -> void:
	EventBus.unsubscribe(_EVENT_STATE_REFRESHED, _on_state_refreshed)


# Snapshot state mới (profile về từ server / boot offline) → refresh hiển thị.
func _on_state_refreshed(_payload) -> void:
	refresh()


# Nhãn currency: profile CHƯA mang currency (feature phase 31) ⇒ placeholder khi rỗng. Chỉ đọc, không tính.
func _format_currency(currencies: Dictionary) -> String:
	if currencies.is_empty():
		return "—"
	var parts: Array[String] = []
	for code in currencies:
		parts.append("%s: %d" % [str(code), int(currencies[code])])
	return ", ".join(parts)


# Dịch ý định từ view. Phase 17/20 = placeholder (chưa nghiệp vụ); điều hướng feature ở phase sau
# sẽ đi qua SceneRouter tại đây. KHÔNG tự vẽ nghiệp vụ ngoài phạm vi phase.
func _on_intent(intent_name: StringName, _payload: Dictionary) -> void:
	print_verbose("MainHub: intent '%s' (placeholder — feature ở phase sau)." % intent_name)
