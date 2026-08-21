# MainHubPresenter — presenter/view-model cho MainHubView (Phase 17). Minh hoạ phía presenter của
# hợp đồng UI: ĐỌC dữ liệu hiển thị từ StateCache/ConfigProvider (đọc-cache, KHÔNG network) → đẩy vào
# view qua `set_data`; nghe `intent` của view → dịch thành hành động. Phase 17: intent = placeholder
# (feature thật ở phase sau). KHÔNG gọi NetworkClient. Chi tiết: docs/godot/ui-architecture.md.
class_name MainHubPresenter
extends RefCounted

var _view: BaseView = null


func _init(view: BaseView) -> void:
	_view = view
	_view.intent.connect(_on_intent)
	refresh()


## Chuẩn bị dữ liệu HIỂN THỊ (config version + nhãn online/offline) từ cache đọc → view.
func refresh() -> void:
	var label: String = ConfigProvider.config_label()
	var status_text := "Config: %s · %s" % [
		label if label != "" else "chưa có",
		"offline" if StateCache.is_offline() else "online",
	]
	_view.set_data({"status_text": status_text})


# Dịch ý định từ view. Phase 17 = placeholder (chưa nghiệp vụ); điều hướng feature ở phase sau
# sẽ đi qua SceneRouter tại đây. KHÔNG tự vẽ nghiệp vụ ngoài phạm vi phase.
func _on_intent(intent_name: StringName, _payload: Dictionary) -> void:
	print_verbose("MainHub: intent '%s' (placeholder — feature ở phase sau)." % intent_name)
