class_name EffectDef
## Định nghĩa một effect data-driven (§17/§23): `effect_type` (khoá registry) + `params` (map int) +
## `target` (tuỳ chọn, ghi đè target rule của skill cho riêng effect). Mở rộng effect = thêm handler +
## config, KHÔNG `switch(skill_id)` (ADR-004). Khớp server `Model/EffectDef.cs`.
extends RefCounted

var effect_type: String = ""
var params: Dictionary = {}
var target: String = ""


static func make(effect_type_value: String, params_value: Dictionary = {}, target_value: String = "") -> EffectDef:
	var e := EffectDef.new()
	e.effect_type = effect_type_value
	e.params = params_value
	e.target = target_value
	return e


## Đọc một tham số int bắt buộc. Assert nếu thiếu (khớp server ném `KeyNotFoundException`).
func param(key: String) -> int:
	assert(params.has(key), "EffectDef thiếu param '%s'." % key)
	return int(params.get(key, 0))


## Có tham số tuỳ chọn theo khoá? (dùng cho buff/debuff nhiều chỉ số — khớp server `TryParam`).
func has_param(key: String) -> bool:
	return params.has(key)
