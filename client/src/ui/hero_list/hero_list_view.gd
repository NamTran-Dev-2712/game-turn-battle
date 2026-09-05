# HeroListView — màn danh sách hero NGƯỜI CHƠI SỞ HỮU (Phase 27, Hero System thật). View thuần (ADR-002):
# data-in (`set_data`→`_render`) → intent-out (`emit_intent`). KHÔNG gọi network, KHÔNG đọc ConfigProvider/
# StateCache trực tiếp, KHÔNG hardcode dữ liệu hero — presenter ghép owned (StateCache) + definition
# (ConfigProvider) rồi đẩy vào. DANH SÁCH KHÔNG tải art (art nặng ⇒ chỉ ở màn chi tiết, lazy — ADR-009):
# list không phụ thuộc art đã nạp. Bấm một hero → intent open_hero {id} (presenter mở màn chi tiết).
# Fallback KHÔNG im lặng (Rule E): banner "cache cũ" khi stale; nút Thử lại. Chi tiết: ui-architecture.md.
class_name HeroListView
extends BaseView

## Ý định: mở chi tiết một hero (payload {id}). Presenter → SceneRouter.goto_scene(hero_detail, {hero_id}).
const INTENT_OPEN_HERO: StringName = &"open_hero"
## Ý định: bấm "Thử lại" (tải lại config). Presenter → ConfigProvider.check_for_update.
const INTENT_RETRY: StringName = &"retry"
## Ý định: quay lại hub.
const INTENT_BACK: StringName = &"back"

var _title: Label = null
var _version_label: Label = null
var _stale_banner: Label = null
var _empty_label: Label = null
var _list: VBoxContainer = null
var _presenter: HeroListPresenter = null


func _ready() -> void:
	_build()
	# Presenter ghép dữ liệu hiển thị (owned + config — đọc, không network) + nghe intent của view.
	_presenter = HeroListPresenter.new(self)


func _build() -> void:
	set_anchors_preset(Control.PRESET_FULL_RECT)
	var box := VBoxContainer.new()
	box.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(box)

	_title = Label.new()
	_title.text = "Anh hùng của tôi"
	box.add_child(_title)

	_version_label = Label.new()
	box.add_child(_version_label)

	# Banner cảnh báo cache cũ — ẩn khi không stale (fallback KHÔNG im lặng).
	_stale_banner = Label.new()
	_stale_banner.visible = false
	box.add_child(_stale_banner)

	# Thông báo khi CHƯA sở hữu hero nào — kèm nút Thử lại.
	_empty_label = Label.new()
	_empty_label.visible = false
	box.add_child(_empty_label)

	# Vùng chứa danh sách hero (dựng động từ data — không hardcode trong scene).
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


# Render dữ liệu hiển thị do presenter đẩy vào. Khoá dữ liệu:
#   version_label:String, stale:bool, error_code:String, offline:bool,
#   heroes:Array[Dictionary]{id, level, stars, rarity, class, element, role, has_definition}.
func _render(data: Dictionary) -> void:
	if _version_label != null and data.has("version_label"):
		var suffix := " · offline" if bool(data.get("offline", false)) else ""
		_version_label.text = "%s%s" % [str(data["version_label"]), suffix]

	var stale := bool(data.get("stale", false))
	if _stale_banner != null:
		_stale_banner.visible = stale
		if stale:
			_stale_banner.text = "⚠ Đang dùng cấu hình đã lưu (cache cũ) — %s" % str(data.get("error_code", ""))

	var heroes: Array = data.get("heroes", [])
	if _empty_label != null:
		_empty_label.visible = heroes.is_empty()
		if heroes.is_empty():
			_empty_label.text = "Chưa sở hữu hero nào."

	_rebuild_list(heroes)


# Xoá + dựng lại danh sách hero từ data (mỗi lần render). Mỗi hero là một nút → intent open_hero {id}.
func _rebuild_list(heroes: Array) -> void:
	if _list == null:
		return
	for child in _list.get_children():
		child.queue_free()
	for hero in heroes:
		if not (hero is Dictionary):
			continue
		var id: String = str(hero.get("id", "?"))
		var row := Button.new()
		row.text = "%s · Lv.%d ★%d · rarity %d · %s" % [
			id,
			int(hero.get("level", 0)),
			int(hero.get("stars", 0)),
			int(hero.get("rarity", 0)),
			str(hero.get("class", "?")),
		]
		# Bind id vào callable (mỗi nút mở đúng hero của nó).
		row.pressed.connect(func() -> void: emit_intent(INTENT_OPEN_HERO, {"id": id}))
		_list.add_child(row)


# Huỷ đăng ký của presenter khi view rời cây (đối xứng vòng đời — tránh Callable treo).
func unbind() -> void:
	if _presenter != null:
		_presenter.dispose()
