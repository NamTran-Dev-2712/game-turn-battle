class_name CombatRules
## Tham số cân bằng combat (combat_int, từ config — KHÔNG hardcode; ADR-004). Nguồn = stage config
## (`combat_rules`) đúng như server `CombatInputResolver`. `crit_multiplier_fixed` ở scale fixed-point
## (1500 = ×1.5); `accuracy_bp`/`crit_rate_bp` là basis points [0..10000].
extends RefCounted

var def_constant_k: int = 0
var min_damage: int = 1
var crit_multiplier_fixed: int = 0
var accuracy_bp: int = 0
var crit_rate_bp: int = 0
var max_rounds: int = 0
var energy: EnergyRules = null


static func from_dict(d: Dictionary, max_rounds_value: int) -> CombatRules:
	var r := CombatRules.new()
	r.def_constant_k = int(d.get("def_constant_k", 0))
	r.min_damage = int(d.get("min_damage", 1))
	r.crit_multiplier_fixed = int(d.get("crit_multiplier_fixed", 0))
	r.accuracy_bp = int(d.get("accuracy_bp", 0))
	r.crit_rate_bp = int(d.get("crit_rate_bp", 0))
	r.max_rounds = max_rounds_value
	r.energy = EnergyRules.from_dict(d.get("energy", {}))
	return r
