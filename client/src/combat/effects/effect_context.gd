class_name EffectContext
## Ngữ cảnh truyền vào một [EffectHandler] khi áp effect (khớp server `Effects/EffectContext.cs`).
## `emit` nối vào log dùng chung của trận (append theo thứ tự — seq đóng dấu sau).
extends RefCounted

var attacker: CombatUnitState = null
var target: CombatUnitState = null
var skill: SkillDef = null
var effect: EffectDef = null
var rules: CombatRules = null
var is_crit: bool = false

var _log: Array = []


func _init(
	attacker_state: CombatUnitState,
	target_state: CombatUnitState,
	skill_def: SkillDef,
	effect_def: EffectDef,
	combat_rules: CombatRules,
	crit: bool,
	log: Array,
) -> void:
	attacker = attacker_state
	target = target_state
	skill = skill_def
	effect = effect_def
	rules = combat_rules
	is_crit = crit
	_log = log


## Phát một sự kiện vào log dùng chung (append theo thứ tự).
func emit(event: Dictionary) -> void:
	_log.append(event)
