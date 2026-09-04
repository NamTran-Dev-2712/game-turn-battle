class_name EnergyRules
## Luật năng lượng/ultimate (§15, CB4 [ĐỀ XUẤT]). Được nối dây nhưng **bất hoạt** ở phase 24/25
## (vector đặt on_attack=on_hit=0 ⇒ không phát `EnergyChanged`). KHÔNG kích hoạt nếu chưa có product.
extends RefCounted

var initial: int = 0
var on_attack: int = 0
var on_hit: int = 0
var ultimate_cost: int = 0
var max: int = 0


static func from_dict(d: Dictionary) -> EnergyRules:
	var e := EnergyRules.new()
	e.initial = int(d.get("initial", 0))
	e.on_attack = int(d.get("on_attack", 0))
	e.on_hit = int(d.get("on_hit", 0))
	e.ultimate_cost = int(d.get("ultimate_cost", 0))
	e.max = int(d.get("max", 0))
	return e
