# InventoryView — màn kho đồ (Phase 32). View thuần (ADR-002): data-in (`set_data`→`_render`) → intent-out
# (`emit_intent`). KHÔNG gọi network, KHÔNG đọc StateCache/ConfigProvider trực tiếp, KHÔNG hardcode dữ liệu —
# presenter tải kho (server), ghép tên từ config rồi đẩy vào. Tab lọc [Tất cả][Anh hùng][Mảnh][Vật phẩm] →
# intent filter {filter}. Fallback KHÔNG im lặng (Rule E): nhãn lỗi + nút Thử lại; trạng thái rỗng khi kho trống.
# Client CHỈ hiển thị số lượng server trả (không tự cộng/trừ). Chi tiết: ui-architecture.md.
class_name InventoryView
extends BaseView

## Ý định: đổi bộ lọc tab (payload {filter}). Presenter lọc lại + render.
const INTENT_FILTER: StringName = &"filter"
## Ý định: bấm "Thử lại" (tải lại kho từ server).
const INTENT_RETRY: StringName = &"retry"
## Ý định: quay lại hub.
const INTENT_BACK: StringName = &"back"

# Tab lọc (id khớp InventoryPresenter.FILTER_*).
const _TABS: Array[Dictionary] = [
	{"id": "all", "label": "Tất cả"},
	{"id": "heroes", "label": "Anh hùng"},
	{"id": "fragment", "label": "Mảnh"},
	{"id": "item", "label": "Vật phẩm"},
]

var _title: Label = null
var _status_label: Label = null
var _error_banner: Label = null
var _empty_label: Label = null
var _list: VBoxContainer = null
var _presenter: InventoryPresenter = null


func _ready() -> void:
	_build()
	_presenter = InventoryPresenter.new(self)


func _build() -> void:
	set_anchors_preset(Control.PRESET_FULL_RECT)
	var box := VBoxContainer.new()
	box.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(box)

	_title = Label.new()
	_title.text = "Kho đồ"
	box.add_child(_title)

	_status_label = Label.new()
	box.add_child(_status_label)

	# Banner lỗi — ẩn khi không lỗi (fallback KHÔNG im lặng).
	_error_banner = Label.new()
	_error_banner.visible = false
	box.add_child(_error_banner)

	# Tab lọc theo loại.
	var tabs := HBoxContainer.new()
	box.add_child(tabs)
	for tab in _TABS:
		var button := Button.new()
		button.text = str(tab["label"])
		var filter_id: String = str(tab["id"])
		button.pressed.connect(func() -> void: emit_intent(INTENT_FILTER, {"filter": filter_id}))
		tabs.add_child(button)

	# Thông báo khi kho trống (theo bộ lọc).
	_empty_label = Label.new()
	_empty_label.visible = false
	box.add_child(_empty_label)

	# Vùng danh sách (dựng động từ data — không hardcode).
	_list = VBoxContainer.new()
	box.add_child(_list)

	var buttons := HBoxContainer.new()
	box.add_child(buttons)
	var retry_button := Button.new()
	retry_button.text = "Thử lại"
	retry_button.pressed.connect(func() -> void: emit_intent(INTENT_RETRY))
	buttons.add_child(retry_button)
	var back_button := Button.new()
	back_button.text = "Quay lại"
	back_button.pressed.connect(func() -> void: emit_intent(INTENT_BACK))
	buttons.add_child(back_button)


# Render dữ liệu do presenter đẩy vào. Khoá dữ liệu:
#   status_text:String, filter:String, offline:bool, error_text:String, empty:bool,
#   rows:Array[Dictionary]{kind, label, quantity(int; -1 ⇒ ẩn "xN")}.
func _render(data: Dictionary) -> void:
	if _status_label != null:
		var suffix := " · offline" if bool(data.get("offline", false)) else ""
		_status_label.text = "%s%s" % [str(data.get("status_text", "")), suffix]

	var error_text := str(data.get("error_text", ""))
	if _error_banner != null:
		_error_banner.visible = error_text != ""
		if error_text != "":
			_error_banner.text = "⚠ %s" % error_text

	var rows: Array = data.get("rows", [])
	if _empty_label != null:
		_empty_label.visible = bool(data.get("empty", false))
		if bool(data.get("empty", false)):
			_empty_label.text = "Kho trống."

	_rebuild_list(rows)


# Xoá + dựng lại danh sách từ data (mỗi lần render). Mỗi hàng: "label  xN" (ẩn "xN" khi quantity < 0 — hero).
func _rebuild_list(rows: Array) -> void:
	if _list == null:
		return
	for child in _list.get_children():
		child.queue_free()
	for row in rows:
		if not (row is Dictionary):
			continue
		var quantity := int(row.get("quantity", -1))
		var label := Label.new()
		if quantity >= 0:
			label.text = "%s    x%d" % [str(row.get("label", "?")), quantity]
		else:
			label.text = str(row.get("label", "?"))
		_list.add_child(label)


# Huỷ đăng ký của presenter khi view rời cây (đối xứng vòng đời — tránh Callable treo).
func unbind() -> void:
	if _presenter != null:
		_presenter.dispose()
