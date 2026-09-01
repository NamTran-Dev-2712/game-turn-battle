# HeroListView — màn MẪU chứng minh vòng config data-driven end-to-end (Phase 22). KHÔNG phải Hero
# System (feature thật = phase 27) — chỉ hiển thị danh sách hero placeholder ĐỌC TỪ config bundle để
# xác nhận: server đổi config → publish version mới → client hiển thị dữ liệu mới KHÔNG rebuild.
# View thuần (ADR-002): data-in (`set_data`→`_render`) → intent-out (`emit_intent`). KHÔNG gọi network,
# KHÔNG đọc ConfigProvider trực tiếp, KHÔNG hardcode dữ liệu hero — presenter nạp mọi thứ.
# Fallback KHÔNG im lặng (Rule E): banner "cache cũ" khi stale; nút Thử lại khi lỗi/không có config.
# Chi tiết: docs/godot/ui-architecture.md, docs/gameplay/configuration-and-data.md §4.
class_name HeroListView
extends BaseView

## Ý định: người dùng bấm "Thử lại" (tải lại config). Presenter dịch → ConfigProvider.check_for_update.
const INTENT_RETRY: StringName = &"retry"
## Ý định: quay lại hub.
const INTENT_BACK: StringName = &"back"

var _title: Label = null
var _version_label: Label = null
var _stale_banner: Label = null
var _empty_label: Label = null
var _list: VBoxContainer = null
var _retry_button: Button = null
var _presenter: HeroListPresenter = null


func _ready() -> void:
	_build()
	# Presenter nạp dữ liệu hiển thị (ConfigProvider — đọc, không network) + nghe intent của view.
	_presenter = HeroListPresenter.new(self)


func _build() -> void:
	set_anchors_preset(Control.PRESET_FULL_RECT)
	var box := VBoxContainer.new()
	box.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(box)

	_title = Label.new()
	_title.text = "Anh hùng (mẫu từ config)"
	box.add_child(_title)

	_version_label = Label.new()
	box.add_child(_version_label)

	# Banner cảnh báo cache cũ — ẩn khi không stale (fallback KHÔNG im lặng).
	_stale_banner = Label.new()
	_stale_banner.visible = false
	box.add_child(_stale_banner)

	# Thông báo khi KHÔNG có config nào (no-cache) — kèm nút Thử lại.
	_empty_label = Label.new()
	_empty_label.visible = false
	box.add_child(_empty_label)

	# Vùng chứa danh sách hero (dựng động từ data — không hardcode trong scene).
	_list = VBoxContainer.new()
	box.add_child(_list)

	var buttons := HBoxContainer.new()
	box.add_child(buttons)
	_retry_button = Button.new()
	_retry_button.text = "Thử lại"
	_retry_button.pressed.connect(func() -> void: emit_intent(INTENT_RETRY))
	buttons.add_child(_retry_button)
	var back_button := Button.new()
	back_button.text = "Quay lại"
	back_button.pressed.connect(func() -> void: emit_intent(INTENT_BACK))
	buttons.add_child(back_button)


# Render dữ liệu hiển thị do presenter đẩy vào. Khoá dữ liệu:
#   version_label:String, stale:bool, error_code:String, heroes:Array[Dictionary]{id,rarity,class}.
func _render(data: Dictionary) -> void:
	if _version_label != null and data.has("version_label"):
		_version_label.text = str(data["version_label"])

	var stale := bool(data.get("stale", false))
	if _stale_banner != null:
		_stale_banner.visible = stale
		if stale:
			_stale_banner.text = "⚠ Đang dùng cấu hình đã lưu (cache cũ) — %s" % str(data.get("error_code", ""))

	var heroes: Array = data.get("heroes", [])
	if _empty_label != null:
		_empty_label.visible = heroes.is_empty()
		if heroes.is_empty():
			_empty_label.text = "Chưa có cấu hình hero. Vui lòng thử lại."

	_rebuild_list(heroes)


# Xoá + dựng lại danh sách hero từ data (mỗi lần render). Dữ liệu từ config, KHÔNG hardcode.
func _rebuild_list(heroes: Array) -> void:
	if _list == null:
		return
	for child in _list.get_children():
		child.queue_free()
	for hero in heroes:
		if not (hero is Dictionary):
			continue
		var row := Label.new()
		row.text = "%s · rarity %d · %s" % [
			str(hero.get("id", "?")),
			int(hero.get("rarity", 0)),
			str(hero.get("class", "?")),
		]
		_list.add_child(row)


# Huỷ đăng ký của presenter khi view rời cây (đối xứng vòng đời — tránh Callable treo).
func unbind() -> void:
	if _presenter != null:
		_presenter.dispose()
