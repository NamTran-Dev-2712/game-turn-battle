# BattleView — màn trận (Phase 30). View thuần (ADR-002): data-in (`set_data`→`_render`) → intent-out
# (`emit_intent`). KHÔNG gọi network/StateCache/ConfigProvider trực tiếp; presenter đánh trận (server) + replay
# rồi đẩy dữ liệu vào. Hiển thị: diễn biến trận (replay bằng seed server), kết cục thắng/thua, và THƯỞNG do
# server cấp (client chỉ hiển thị — KHÔNG tự cấp). Chi tiết: ui-architecture.md.
class_name BattleView
extends BaseView

## Ý định: đánh lại (trận mới).
const INTENT_RETRY: StringName = &"retry"
## Ý định: quay lại hub.
const INTENT_BACK: StringName = &"back"

var _title: Label = null
var _status: Label = null
var _outcome: Label = null
var _log: VBoxContainer = null
var _rewards: Label = null
var _view_presenter: BattlePresenter = null


func _ready() -> void:
	_build()
	_view_presenter = BattlePresenter.new(self)


func _build() -> void:
	set_anchors_preset(Control.PRESET_FULL_RECT)
	var box := VBoxContainer.new()
	box.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(box)

	_title = Label.new()
	_title.text = "Trận đấu"
	box.add_child(_title)

	_status = Label.new()
	box.add_child(_status)

	_outcome = Label.new()
	box.add_child(_outcome)

	var log_label := Label.new()
	log_label.text = "Diễn biến (replay):"
	box.add_child(log_label)

	var scroll := ScrollContainer.new()
	scroll.custom_minimum_size = Vector2(0, 240)
	box.add_child(scroll)
	_log = VBoxContainer.new()
	_log.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	scroll.add_child(_log)

	_rewards = Label.new()
	box.add_child(_rewards)

	var buttons := HBoxContainer.new()
	box.add_child(buttons)
	var retry_button := Button.new()
	retry_button.text = "Đánh lại"
	retry_button.pressed.connect(func() -> void: emit_intent(INTENT_RETRY))
	buttons.add_child(retry_button)
	var back_button := Button.new()
	back_button.text = "Quay lại"
	back_button.pressed.connect(func() -> void: emit_intent(INTENT_BACK))
	buttons.add_child(back_button)


# Render dữ liệu do presenter đẩy vào. Khoá:
#   status_text:String, stage_id:String, outcome:String (VICTORY/DEFEAT/DRAW/""), rounds:int,
#   events:Array[String], rewards:Array[String], replay_matches:bool, error_text:String.
func _render(data: Dictionary) -> void:
	if _status != null:
		_status.text = "Màn: %s · %s" % [str(data.get("stage_id", "")), str(data.get("status_text", ""))]

	if _outcome != null:
		var outcome := str(data.get("outcome", ""))
		if outcome == "":
			_outcome.text = ""
		else:
			var warn := "" if bool(data.get("replay_matches", true)) else "  ⚠ replay lệch server (hiện theo server)"
			_outcome.text = "Kết quả: %s (vòng %d)%s" % [outcome, int(data.get("rounds", 0)), warn]

	if _log != null:
		for child in _log.get_children():
			child.queue_free()
		var error_text := str(data.get("error_text", ""))
		if error_text != "":
			var err := Label.new()
			err.text = error_text
			_log.add_child(err)
		for line in data.get("events", []):
			var entry := Label.new()
			entry.text = str(line)
			_log.add_child(entry)

	if _rewards != null:
		var rewards: Array = data.get("rewards", [])
		_rewards.text = "Thưởng: %s" % (", ".join(PackedStringArray(rewards)) if not rewards.is_empty() else "—")


# Huỷ đăng ký của presenter khi view rời cây (đối xứng vòng đời).
func unbind() -> void:
	if _view_presenter != null:
		_view_presenter.dispose()
