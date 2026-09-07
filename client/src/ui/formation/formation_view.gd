# FormationView — màn đội hình (Phase 29). View thuần (ADR-002): data-in (`set_data`→`_render`) →
# intent-out (`emit_intent`). KHÔNG gọi network, KHÔNG đọc StateCache/ConfigProvider trực tiếp, KHÔNG tự lưu:
# presenter đọc lưới (config) + roster (owned) + đội đã lưu (server) rồi đẩy vào; mọi thay đổi là INTENT.
# Luồng: chọn hero (roster) → đặt vào ô (lưới) → đổi vị trí → Lưu (POST qua presenter) → hiển thị lại theo
# server. Lưới dựng từ rows×cols của config (data-driven — KHÔNG hardcode). Chi tiết: ui-architecture.md.
class_name FormationView
extends BaseView

## Ý định: chọn một hero từ roster (payload {hero_id}). Presenter ghi nhớ hero đang chọn.
const INTENT_SELECT_HERO: StringName = &"select_hero"
## Ý định: đặt hero đang chọn vào ô (payload {slot_index}).
const INTENT_PLACE_SLOT: StringName = &"place_slot"
## Ý định: bỏ hero khỏi ô (payload {slot_index}).
const INTENT_CLEAR_SLOT: StringName = &"clear_slot"
## Ý định: lưu đội hình (presenter POST /team).
const INTENT_SAVE: StringName = &"save"
## Ý định: quay lại hub.
const INTENT_BACK: StringName = &"back"

var _title: Label = null
var _status: Label = null
var _grid: GridContainer = null
var _roster: VBoxContainer = null
var _view_presenter: FormationPresenter = null


func _ready() -> void:
	_build()
	_view_presenter = FormationPresenter.new(self)


func _build() -> void:
	set_anchors_preset(Control.PRESET_FULL_RECT)
	var box := VBoxContainer.new()
	box.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(box)

	_title = Label.new()
	_title.text = "Đội hình"
	box.add_child(_title)

	_status = Label.new()
	box.add_child(_status)

	# Lưới ô đội hình (dựng động theo rows×cols của config).
	_grid = GridContainer.new()
	box.add_child(_grid)

	var roster_label := Label.new()
	roster_label.text = "Chọn hero:"
	box.add_child(roster_label)

	# Roster hero sở hữu (dựng động từ data).
	_roster = VBoxContainer.new()
	box.add_child(_roster)

	var buttons := HBoxContainer.new()
	box.add_child(buttons)
	var save_button := Button.new()
	save_button.text = "Lưu"
	save_button.pressed.connect(func() -> void: emit_intent(INTENT_SAVE))
	buttons.add_child(save_button)
	var back_button := Button.new()
	back_button.text = "Quay lại"
	back_button.pressed.connect(func() -> void: emit_intent(INTENT_BACK))
	buttons.add_child(back_button)


# Render dữ liệu do presenter đẩy vào. Khoá dữ liệu:
#   rows:int, cols:int, slots:Array[{slot_index, hero_id}], roster:Array[{id, selected}],
#   selected_hero:String, status_text:String, offline:bool.
func _render(data: Dictionary) -> void:
	if _status != null:
		var offline := " · offline" if bool(data.get("offline", false)) else ""
		_status.text = "%s%s" % [str(data.get("status_text", "")), offline]

	if _grid != null:
		_grid.columns = max(int(data.get("cols", 1)), 1)
		_rebuild_grid(data.get("slots", []))

	if _roster != null:
		_rebuild_roster(data.get("roster", []))


# Dựng lại lưới ô: mỗi ô là nút đặt hero (place_slot) + nút "×" bỏ hero (clear_slot).
func _rebuild_grid(slots: Array) -> void:
	for child in _grid.get_children():
		child.queue_free()
	for slot in slots:
		if not (slot is Dictionary):
			continue
		var slot_index: int = int(slot.get("slot_index", -1))
		var hero_id: String = str(slot.get("hero_id", ""))
		var cell := HBoxContainer.new()
		var place := Button.new()
		place.text = "Ô %d: %s" % [slot_index, hero_id if hero_id != "" else "(trống)"]
		place.pressed.connect(func() -> void: emit_intent(INTENT_PLACE_SLOT, {"slot_index": slot_index}))
		cell.add_child(place)
		if hero_id != "":
			var clear := Button.new()
			clear.text = "×"
			clear.pressed.connect(func() -> void: emit_intent(INTENT_CLEAR_SLOT, {"slot_index": slot_index}))
			cell.add_child(clear)
		_grid.add_child(cell)


# Dựng lại roster: mỗi hero sở hữu là một nút chọn (select_hero). Hero đang chọn được đánh dấu.
func _rebuild_roster(roster: Array) -> void:
	for child in _roster.get_children():
		child.queue_free()
	for entry in roster:
		if not (entry is Dictionary):
			continue
		var id: String = str(entry.get("id", "?"))
		var selected: bool = bool(entry.get("selected", false))
		var button := Button.new()
		button.text = ("▶ " if selected else "") + id
		button.pressed.connect(func() -> void: emit_intent(INTENT_SELECT_HERO, {"hero_id": id}))
		_roster.add_child(button)


# Huỷ đăng ký của presenter khi view rời cây (đối xứng vòng đời — tránh Callable treo).
func unbind() -> void:
	if _view_presenter != null:
		_view_presenter.dispose()
