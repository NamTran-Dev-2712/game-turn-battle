class_name EffectDef
## Định nghĩa một effect data-driven (§17): `effect_type` (khoá registry) + `params` (map int).
## Mở rộng effect = thêm handler + config, KHÔNG `switch(skill_id)` (ADR-004).
extends RefCounted

var effect_type: String = ""
var params: Dictionary = {}


static func make(effect_type_value: String, params_value: Dictionary = {}) -> EffectDef:
	var e := EffectDef.new()
	e.effect_type = effect_type_value
	e.params = params_value
	return e


## Đọc một tham số int bắt buộc. Assert nếu thiếu (khớp server ném `KeyNotFoundException`).
func param(key: String) -> int:
	assert(params.has(key), "EffectDef thiếu param '%s'." % key)
	return int(params.get(key, 0))
