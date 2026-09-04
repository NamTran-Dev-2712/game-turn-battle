class_name SkillDef
## Định nghĩa skill (§17): `coeff_fixed` (fixed-point, 1000 = ×1.0), `target_rule`, danh sách effect.
## Skill cơ bản (auto-attack) mặc định một effect `damage` nếu config không liệt kê (khớp server).
extends RefCounted

var id: String = ""
var coeff_fixed: int = 0
var target_rule: String = "default"
var effects: Array[EffectDef] = []


static func make(id_value: String, coeff_fixed_value: int, target_rule_value: String, effects_value: Array[EffectDef]) -> SkillDef:
	var s := SkillDef.new()
	s.id = id_value
	s.coeff_fixed = coeff_fixed_value
	s.target_rule = target_rule_value
	s.effects = effects_value
	return s
