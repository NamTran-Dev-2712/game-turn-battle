# MainHubView — sảnh chính tối giản (Phase 17). Là "shell" điều hướng/bố cục: tiêu đề + các nút
# placeholder cho feature (chưa nghiệp vụ). View thuần: bấm nút → `emit_intent(<feature>)`; presenter
# (MainHubPresenter) nạp dữ liệu hiển thị + xử lý intent. KHÔNG gọi network. Chi tiết: ui-architecture.md.
class_name MainHubView
extends BaseView

# Danh sách nút placeholder (id ý định + nhãn hiển thị). Feature thật ở các phase sau.
const FEATURES: Array[Dictionary] = [
	{"id": &"heroes", "label": "Anh hùng"},
	{"id": &"battle", "label": "Chiến đấu"},
	{"id": &"summon", "label": "Triệu hồi"},
	{"id": &"shop", "label": "Cửa hàng"},
]

var _profile: Label = null
var _currency: Label = null
var _status: Label = null
var _presenter: MainHubPresenter = null


func _ready() -> void:
	_build()
	# Presenter nạp dữ liệu hiển thị (StateCache/ConfigProvider) + nghe intent của view.
	_presenter = MainHubPresenter.new(self)


func _build() -> void:
	set_anchors_preset(Control.PRESET_FULL_RECT)
	var box := VBoxContainer.new()
	box.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(box)
	var title := Label.new()
	title.text = "game team — Sảnh chính"
	box.add_child(title)
	# Profile server-authoritative (tên · level) + currency placeholder (phase 20).
	_profile = Label.new()
	box.add_child(_profile)
	_currency = Label.new()
	box.add_child(_currency)
	_status = Label.new()
	box.add_child(_status)
	var buttons := HBoxContainer.new()
	box.add_child(buttons)
	for feature in FEATURES:
		var button := Button.new()
		button.text = str(feature["label"])
		var feature_id: StringName = feature["id"]
		# Bấm → phát ý định feature (presenter dịch). View không tự điều hướng/nghiệp vụ.
		button.pressed.connect(func() -> void: emit_intent(feature_id))
		buttons.add_child(button)


func _render(data: Dictionary) -> void:
	if _profile != null and data.has("profile_text"):
		var offline := bool(data.get("offline", false))
		# Nhãn offline/cached rõ ràng — người dùng KHÔNG hiểu nhầm cache là dữ liệu server tươi.
		_profile.text = ("[offline] " if offline else "") + str(data["profile_text"])
	if _currency != null and data.has("currency_text"):
		_currency.text = "Currency: " + str(data["currency_text"])
	if _status != null and data.has("status_text"):
		_status.text = str(data["status_text"])


# Huỷ đăng ký EventBus của presenter khi view rời cây (đối xứng vòng đời — tránh Callable treo).
func unbind() -> void:
	if _presenter != null:
		_presenter.dispose()
