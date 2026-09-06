class_name CombatVectorLoader
## Helper TEST: nạp golden vector từ `shared/combat-vectors/` (nguồn cross-impl DUY NHẤT — KHÔNG
## fork/copy) và dựng [BattleInput] từ `input` + `config_excerpt`. Vector nằm NGOÀI `res://` (repo root
## là cha của `client/`) nên đọc qua đường tuyệt đối `globalize_path`.
extends RefCounted


## Nạp cả object vector ({ input, expected, ... }).
static func load_vector(file: String) -> Dictionary:
	var path := _vector_path(file)
	assert(FileAccess.file_exists(path), "Không thấy vector: %s" % path)
	var f := FileAccess.open(path, FileAccess.READ)
	assert(f != null, "Không mở được vector: %s" % path)
	var text := f.get_as_text()
	f.close()
	var json := JSON.new()
	assert(json.parse(text) == OK, "Vector JSON hỏng: %s" % file)
	return json.data as Dictionary


## Dựng BattleInput từ phần `input` của một vector.
static func build_input(input_dict: Dictionary) -> BattleInput:
	var excerpt: Dictionary = input_dict.get("config_excerpt", {})
	var stage_dict: Dictionary = input_dict.get("stage", {})
	var team: Dictionary = input_dict.get("team_snapshot", {})
	var max_rounds := int(stage_dict.get("max_rounds", 0))

	# §23: bảng skill tuỳ chọn (config_excerpt.skills) cho skill riêng của unit/ultimate — vắng ⇒ rỗng.
	var skills_by_id: Dictionary = {}
	var skills_src: Dictionary = excerpt.get("skills", {})
	for skill_id in skills_src:
		skills_by_id[skill_id] = _build_skill(str(skill_id), skills_src[skill_id])

	var input := BattleInput.new()
	input.config_version = str(input_dict.get("config_version", ""))
	input.seed = int(input_dict.get("seed", 0))
	input.stage = StageInfo.make(str(stage_dict.get("id", "")), max_rounds)
	input.ally = _units(team.get("ally", []), skills_by_id)
	input.enemy = _units(team.get("enemy", []), skills_by_id)
	input.rules = CombatRules.from_dict(excerpt.get("combat_rules", {}), max_rounds)

	var skill_basic: Dictionary = excerpt.get("skill_basic", {})
	var effects: Array[EffectDef] = [EffectDef.make(DamageEffectHandler.TYPE_NAME)]
	input.basic_skill = SkillDef.make("skill_basic", int(skill_basic.get("coeff_fixed", 0)), "default", effects)
	return input


## Tiện ích: nạp vector + dựng input trong một bước.
static func load_input(file: String) -> BattleInput:
	return build_input(load_vector(file).get("input", {}))


## Liệt kê MỌI file vector `*.json` trong `shared/combat-vectors/` (sắp xếp — xác định).
## Dùng để test tự khám phá toàn bộ bộ vector (thêm vector = không sửa code test).
static func list_vector_files() -> PackedStringArray:
	var dir_path := _vector_path("").trim_suffix("/")
	var names := PackedStringArray()
	var d := DirAccess.open(dir_path)
	assert(d != null, "Không mở được thư mục vector: %s" % dir_path)
	d.list_dir_begin()
	var entry := d.get_next()
	while entry != "":
		if not d.current_is_dir() and entry.ends_with(".json"):
			names.append(entry)
		entry = d.get_next()
	d.list_dir_end()
	names.sort()
	return names


static func _units(arr: Array, skills_by_id: Dictionary) -> Array[UnitSnapshot]:
	var out: Array[UnitSnapshot] = []
	for u in arr:
		var snapshot := UnitSnapshot.from_dict(u)
		var d := u as Dictionary
		if d.has("skills"):
			var s := d["skills"] as Dictionary
			var basic: SkillDef = skills_by_id[str(s.get("basic", ""))]
			var ultimate: SkillDef = null
			if s.has("ultimate"):
				ultimate = skills_by_id[str(s["ultimate"])]
			snapshot.skills = UnitSkillSet.make(basic, ultimate)
		out.append(snapshot)
	return out


# Dựng SkillDef từ một entry trong config_excerpt.skills (khớp server GoldenVectorLoader.ParseSkill).
static func _build_skill(skill_id: String, el: Dictionary) -> SkillDef:
	var effects: Array[EffectDef] = []
	for e in el.get("effects", []):
		var ed := e as Dictionary
		effects.append(EffectDef.make(
			str(ed.get("effect_type", "")), ed.get("params", {}), str(ed.get("target", ""))))
	if effects.is_empty():
		effects.append(EffectDef.make(DamageEffectHandler.TYPE_NAME))
	return SkillDef.make(
		skill_id,
		int(el.get("coeff_fixed", 0)),
		str(el.get("target_rule", "default")),
		effects,
		int(el.get("energy_cost", 0)),
		int(el.get("cooldown_rounds", 0)))


# Đường tuyệt đối tới file vector: <repo>/shared/combat-vectors/<file>. `res://` = thư mục client/.
static func _vector_path(file: String) -> String:
	var client_dir := ProjectSettings.globalize_path("res://").trim_suffix("/")
	var repo := client_dir.get_base_dir()
	return "%s/shared/combat-vectors/%s" % [repo, file]
