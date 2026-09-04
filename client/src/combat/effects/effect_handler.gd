class_name EffectHandler
## "Interface" handler effect (GDScript = base class override). Một handler = một `effect_type`.
## Mở rộng combat = thêm handler mới + config, KHÔNG `switch` trong lõi (ADR-004). Khớp server
## `Effects/IEffectHandler.cs`.
extends RefCounted


## Khoá registry (`effect_type`) mà handler này xử lý.
func effect_type() -> String:
	push_error("EffectHandler.effect_type() chưa override.")
	return ""


## Áp effect lên ngữ cảnh (biến đổi state + phát sự kiện).
func apply(_ctx: EffectContext) -> void:
	push_error("EffectHandler.apply() chưa override.")
