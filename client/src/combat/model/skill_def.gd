class_name SkillDef
## Định nghĩa skill (§17/§23): `coeff_fixed` (fixed-point, 1000 = ×1.0), `target_rule`, danh sách effect;
## `energy_cost`/`cooldown_rounds` (§15) cho ultimate (mặc định 0 = đòn thường). Skill cơ bản (auto-attack)
## mặc định một effect `damage` nếu config không liệt kê (khớp server `Model/SkillDef.cs`).
extends RefCounted

var id: String = ""
var coeff_fixed: int = 0
var target_rule: String = "default"
var effects: Array[EffectDef] = []
var energy_cost: int = 0
var cooldown_rounds: int = 0


static func make(
	id_value: String,
	coeff_fixed_value: int,
	target_rule_value: String,
	effects_value: Array[EffectDef],
	energy_cost_value: int = 0,
	cooldown_rounds_value: int = 0,
) -> SkillDef:
	var s := SkillDef.new()
	s.id = id_value
	s.coeff_fixed = coeff_fixed_value
	s.target_rule = target_rule_value
	s.effects = effects_value
	s.energy_cost = energy_cost_value
	s.cooldown_rounds = cooldown_rounds_value
	return s
