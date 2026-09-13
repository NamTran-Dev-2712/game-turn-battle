# SummonView — màn triệu hồi (gacha, Phase 33). View thuần (ADR-002): data-in (`set_data`→`_render`) →
# intent-out (`emit_intent`). KHÔNG gọi network, KHÔNG đọc StateCache/ConfigProvider trực tiếp, KHÔNG tự
# random — presenter gửi intent tới server và đẩy KẾT QUẢ SERVER vào đây. Nút [Quay 1][Quay 10] → intent;
# danh sách banner từ config (chỉ hiển thị). Fallback KHÔNG im lặng (Rule E): nhãn lỗi + nút Thử lại.
class_name SummonView
extends BaseView

## Ý định: chọn banner (payload {banner_id}).
const INTENT_SELECT: StringName = &"select_banner"
## Ý định: quay đơn (1 lần).
const INTENT_SUMMON_ONE: StringName = &"summon_one"
## Ý định: quay mười (10 lần).
const INTENT_SUMMON_TEN: StringName = &"summon_ten"
## Ý định: thử lại (nạp lại banner từ config).
const INTENT_RETRY: StringName = &"retry"
## Ý định: quay lại hub.
const INTENT_BACK: StringName = &"back"

var _title: Label = null
var _status_label: Label = null
var _error_banner: Label = null
var _banner_box: VBoxContainer = null
var _selected_label: Label = null
var _pity_label: Label = null
var _results_box: VBoxContainer = null
var _one_button: Button = null
var _ten_button: Button = null
var _presenter: SummonPresenter = null


func _ready() -> void:
	_build()
	_presenter = SummonPresenter.new(self)


func _build() -> void:
	set_anchors_preset(Control.PRESET_FULL_RECT)
	var box := VBoxContainer.new()
	box.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(box)

	_title = Label.new()
	_title.text = "Triệu hồi"
	box.add_child(_title)

	_status_label = Label.new()
	box.add_child(_status_label)

	_error_banner = Label.new()
	_error_banner.visible = false
	box.add_child(_error_banner)

	# Danh sách banner (dựng động từ config — không hardcode).
	_banner_box = VBoxContainer.new()
	box.add_child(_banner_box)

	_selected_label = Label.new()
	box.add_child(_selected_label)

	# Nút quay.
	var actions := HBoxContainer.new()
	box.add_child(actions)
	_one_button = Button.new()
	_one_button.text = "Quay 1"
	_one_button.pressed.connect(func() -> void: emit_intent(INTENT_SUMMON_ONE))
	actions.add_child(_one_button)
	_ten_button = Button.new()
	_ten_button.text = "Quay 10"
	_ten_button.pressed.connect(func() -> void: emit_intent(INTENT_SUMMON_TEN))
	actions.add_child(_ten_button)

	_pity_label = Label.new()
	box.add_child(_pity_label)

	# Kết quả (dựng động từ data server trả).
	_results_box = VBoxContainer.new()
	box.add_child(_results_box)

	var nav := HBoxContainer.new()
	box.add_child(nav)
	var retry_button := Button.new()
	retry_button.text = "Thử lại"
	retry_button.pressed.connect(func() -> void: emit_intent(INTENT_RETRY))
	nav.add_child(retry_button)
	var back_button := Button.new()
	back_button.text = "Quay lại"
	back_button.pressed.connect(func() -> void: emit_intent(INTENT_BACK))
	nav.add_child(back_button)


# Render dữ liệu do presenter đẩy vào. Khoá dữ liệu:
#   status_text:String, error_text:String, offline:bool, banners:Array[{id,label,cost_text}],
#   selected_banner:String, can_summon:bool, results:Array[String], pity_after:int(-1 ⇒ ẩn).
func _render(data: Dictionary) -> void:
	if _status_label != null:
		var suffix := " · offline" if bool(data.get("offline", false)) else ""
		_status_label.text = "%s%s" % [str(data.get("status_text", "")), suffix]

	var error_text := str(data.get("error_text", ""))
	if _error_banner != null:
		_error_banner.visible = error_text != ""
		if error_text != "":
			_error_banner.text = "⚠ %s" % error_text

	var selected := str(data.get("selected_banner", ""))
	_rebuild_banners(data.get("banners", []), selected)

	if _selected_label != null:
		_selected_label.text = "Banner: %s" % (selected if selected != "" else "(chưa có)")

	var can_summon := bool(data.get("can_summon", false))
	if _one_button != null:
		_one_button.disabled = not can_summon
	if _ten_button != null:
		_ten_button.disabled = not can_summon

	if _pity_label != null:
		var pity := int(data.get("pity_after", -1))
		_pity_label.visible = pity >= 0
		if pity >= 0:
			_pity_label.text = "Pity: %d" % pity

	_rebuild_results(data.get("results", []))


func _rebuild_banners(banners: Array, selected: String) -> void:
	if _banner_box == null:
		return
	for child in _banner_box.get_children():
		child.queue_free()
	for banner in banners:
		if not (banner is Dictionary):
			continue
		var id := str(banner.get("id", ""))
		var label := str(banner.get("label", id))
		var cost := str(banner.get("cost_text", ""))
		var button := Button.new()
		var mark := "▶ " if id == selected else ""
		button.text = "%s%s   %s" % [mark, label, cost]
		button.pressed.connect(func() -> void: emit_intent(INTENT_SELECT, {"banner_id": id}))
		_banner_box.add_child(button)


func _rebuild_results(results: Array) -> void:
	if _results_box == null:
		return
	for child in _results_box.get_children():
		child.queue_free()
	for line in results:
		var label := Label.new()
		label.text = str(line)
		_results_box.add_child(label)


# Huỷ đăng ký của presenter khi view rời cây (đối xứng vòng đời — tránh Callable treo).
func unbind() -> void:
	if _presenter != null:
		_presenter.dispose()
