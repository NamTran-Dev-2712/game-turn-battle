# CampaignView — màn chiến dịch PvE (Phase 34). View thuần (ADR-002): data-in (`set_data`→`_render`) →
# intent-out (`emit_intent`). KHÔNG gọi network, KHÔNG đọc ConfigProvider/StateCache trực tiếp, KHÔNG tự suy
# mở-khoá/tiến độ — presenter ghép trạng thái server (cleared/unlocked) + preview config rồi đẩy vào.
# Mỗi stage là một nút; nút CHỈ bấm được khi unlocked (server vẫn validate lại — bảo mật không phụ thuộc client).
# Fallback KHÔNG im lặng (Rule E): hiện lỗi tải + nút Thử lại. Chi tiết: docs/godot/ui-architecture.md.
class_name CampaignView
extends BaseView

## Ý định: đánh một stage (payload {stage_id}). Presenter → SceneRouter.goto_scene(battle, {campaign_stage_id}).
const INTENT_OPEN_STAGE: StringName = &"open_stage"
## Ý định: tải lại tiến độ.
const INTENT_RETRY: StringName = &"retry"
## Ý định: quay lại hub.
const INTENT_BACK: StringName = &"back"

var _title: Label = null
var _afk_label: Label = null
var _error_banner: Label = null
var _list: VBoxContainer = null
var _presenter: CampaignPresenter = null


func _ready() -> void:
	_build()
	# Presenter ghép trạng thái server (đọc-cache) + preview config; fetch authoritative + nghe intent.
	_presenter = CampaignPresenter.new(self)


func _build() -> void:
	set_anchors_preset(Control.PRESET_FULL_RECT)
	var box := VBoxContainer.new()
	box.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(box)

	_title = Label.new()
	_title.text = "Chiến dịch"
	box.add_child(_title)

	# Nhãn "AFK stage hiện hành" (tiến độ) — nguồn cho phase 37.
	_afk_label = Label.new()
	box.add_child(_afk_label)

	# Banner lỗi tải — ẩn khi không lỗi (fallback KHÔNG im lặng).
	_error_banner = Label.new()
	_error_banner.visible = false
	box.add_child(_error_banner)

	# Vùng chứa danh sách stage (dựng động từ data — không hardcode trong scene).
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
#   current_afk_stage_id:String, offline:bool, error_text:String,
#   stages:Array[Dictionary]{stage_id, chapter_id, order, cleared, unlocked, enemies:Array, rewards:Array}.
func _render(data: Dictionary) -> void:
	if _afk_label != null:
		var afk: String = str(data.get("current_afk_stage_id", ""))
		var suffix := " · offline" if bool(data.get("offline", false)) else ""
		_afk_label.text = "AFK stage: %s%s" % [(afk if afk != "" else "—"), suffix]

	var error_text: String = str(data.get("error_text", ""))
	if _error_banner != null:
		_error_banner.visible = error_text != ""
		if error_text != "":
			_error_banner.text = "⚠ %s" % error_text

	_rebuild_list(data.get("stages", []))


# Xoá + dựng lại danh sách stage. Mỗi stage là một nút; disabled khi khoá; bấm → intent open_stage {stage_id}.
func _rebuild_list(stages: Array) -> void:
	if _list == null:
		return
	for child in _list.get_children():
		child.queue_free()
	for stage in stages:
		if not (stage is Dictionary):
			continue
		var stage_id: String = str(stage.get("stage_id", "?"))
		var cleared := bool(stage.get("cleared", false))
		var unlocked := bool(stage.get("unlocked", false))
		var status := "✓ đã qua" if cleared else ("🔓 mở" if unlocked else "🔒 khoá")
		var reward_preview := ", ".join(PackedStringArray(stage.get("rewards", [])))
		var row := Button.new()
		row.text = "%s · %s%s" % [
			stage_id, status,
			(" · thưởng: %s" % reward_preview) if reward_preview != "" else "",
		]
		# Server vẫn validate lại; UX chặn sớm stage khoá (bảo mật không phụ thuộc client).
		row.disabled = not unlocked
		row.pressed.connect(func() -> void: emit_intent(INTENT_OPEN_STAGE, {"stage_id": stage_id}))
		_list.add_child(row)


# Huỷ đăng ký của presenter khi view rời cây (đối xứng vòng đời — tránh Callable treo).
func unbind() -> void:
	if _presenter != null:
		_presenter.dispose()
