class_name UnitStats
## Chỉ số cơ bản một unit (combat_int, không âm — §9). Bất biến trong trận (HP hiện tại ở [CombatUnitState]).
extends RefCounted

var hp: int = 0
var atk: int = 0
var def: int = 0
var spd: int = 0


static func from_dict(d: Dictionary) -> UnitStats:
	var s := UnitStats.new()
	s.hp = int(d.get("hp", 0))
	s.atk = int(d.get("atk", 0))
	s.def = int(d.get("def", 0))
	s.spd = int(d.get("spd", 0))
	return s
