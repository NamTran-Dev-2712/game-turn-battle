# HeroDetailView — màn CHI TIẾT một hero (Phase 27). View thuần (ADR-002): data-in (`set_data`→`_render`)
# → intent-out (`emit_intent`). KHÔNG network, KHÔNG đọc ConfigProvider/StateCache/AssetLoader trực tiếp —
# presenter ghép owned + definition và nạp art (lazy) rồi đẩy texture vào. Art hiển thị bằng TextureRect:
# bắt đầu là placeholder (do presenter đưa), thay bằng art thật khi tải xong (KHÔNG chặn UI — ADR-009).
class_name HeroDetailView
extends BaseView

## Ý định: quay lại danh sách hero.
const INTENT_BACK: StringName = &"back"

var _art: TextureRect = null
var _name_label: Label = null
var _traits_label: Label = null
var _stats_label: Label = null
var _skills_label: Label = null


func _ready() -> void:
	_build()
	_presenter = HeroDetailPresenter.new(self)


var _presenter: HeroDetailPresenter = null


func _build() -> void:
	set_anchors_preset(Control.PRESET_FULL_RECT)
	var box := VBoxContainer.new()
	box.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(box)

	# Art (lazy): giữ chỗ bằng TextureRect kích thước cố định — không giãn theo art nặng.
	_art = TextureRect.new()
	_art.custom_minimum_size = Vector2(96, 96)
	_art.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	_art.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT
	box.add_child(_art)

	_name_label = Label.new()
	box.add_child(_name_label)

	_traits_label = Label.new()
	box.add_child(_traits_label)

	_stats_label = Label.new()
	box.add_child(_stats_label)

	_skills_label = Label.new()
	box.add_child(_skills_label)

	var back_button := Button.new()
	back_button.text = "Quay lại"
	back_button.pressed.connect(func() -> void: emit_intent(INTENT_BACK))
	box.add_child(back_button)


# Render chi tiết hero do presenter đẩy vào. Khoá dữ liệu:
#   hero_id, owned:bool, has_definition:bool, level, stars, faction, class, element, role, rarity,
#   hp, atk, def, spd, skills:Array[String], art_texture:Texture2D.
func _render(data: Dictionary) -> void:
	if _art != null and data.has("art_texture"):
		_art.texture = data["art_texture"]

	if _name_label != null:
		_name_label.text = "%s · Lv.%d ★%d" % [
			str(data.get("hero_id", "?")),
			int(data.get("level", 0)),
			int(data.get("stars", 0)),
		]

	if _traits_label != null:
		if bool(data.get("has_definition", false)):
			_traits_label.text = "%s · %s · %s · %s · rarity %d" % [
				str(data.get("faction", "?")),
				str(data.get("class", "?")),
				str(data.get("element", "?")),
				str(data.get("role", "?")),
				int(data.get("rarity", 0)),
			]
		else:
			_traits_label.text = "(chưa có định nghĩa trong config)"

	if _stats_label != null:
		_stats_label.text = "HP %d · ATK %d · DEF %d · SPD %d" % [
			int(data.get("hp", 0)),
			int(data.get("atk", 0)),
			int(data.get("def", 0)),
			int(data.get("spd", 0)),
		]

	if _skills_label != null:
		var skills: Array = data.get("skills", [])
		_skills_label.text = "Skills: %s" % (", ".join(skills.map(func(s): return str(s))) if not skills.is_empty() else "—")


func unbind() -> void:
	if _presenter != null:
		_presenter.dispose()
