class_name EffectRegistry
## Registry `effect_type` → [EffectHandler] (khớp server `Effects/EffectRegistry.cs`). Lõi sim resolve
## handler theo data, KHÔNG `switch(skill_id)` (ADR-004). Đăng ký trùng khoá = lỗi; resolve khoá lạ = lỗi.
extends RefCounted

var _handlers: Dictionary = {}


## Đăng ký một handler. Trùng `effect_type` là lỗi cấu hình (assert — khớp server ném).
func register(handler: EffectHandler) -> void:
	var key := handler.effect_type()
	assert(not _handlers.has(key), "EffectRegistry: đăng ký trùng effect_type '%s'." % key)
	_handlers[key] = handler


## Lấy handler cho `effect_type`. Khoá lạ là lỗi (assert — khớp server ném `KeyNotFoundException`).
func resolve(effect_type: String) -> EffectHandler:
	assert(_handlers.has(effect_type), "EffectRegistry: không có handler cho effect_type '%s'." % effect_type)
	return _handlers.get(effect_type, null)


func has(effect_type: String) -> bool:
	return _handlers.has(effect_type)


## Registry mặc định: `damage` + `heal` (khớp server `CreateDefault`).
static func create_default() -> EffectRegistry:
	var registry := EffectRegistry.new()
	registry.register(DamageEffectHandler.new())
	registry.register(HealEffectHandler.new())
	return registry
