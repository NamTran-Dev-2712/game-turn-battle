class_name JsonDiff
## Helper TEST: so cấu trúc hai giá trị JSON-like (Dictionary/Array/scalar), trả **đường khác biệt đầu
## tiên** ("" nếu bằng). Chuẩn hoá số (int/float) để 158 == 158.0 (golden parse từ JSON có thể ra
## float). So mảng theo chỉ số (event_log có thứ tự); dict theo tập khoá (thứ tự khoá không quan trọng).
extends RefCounted


## Trả "" nếu bằng, hoặc chuỗi mô tả điểm khác đầu tiên.
static func first_difference(expected: Variant, actual: Variant, path: String = "$") -> String:
	if expected is Dictionary and actual is Dictionary:
		return _diff_dict(expected, actual, path)
	if expected is Array and actual is Array:
		return _diff_array(expected, actual, path)
	if expected is Dictionary or expected is Array or actual is Dictionary or actual is Array:
		return "%s: kiểu khác (exp %s / got %s)" % [path, _kind(expected), _kind(actual)]
	return _diff_scalar(expected, actual, path)


static func _diff_dict(expected: Dictionary, actual: Dictionary, path: String) -> String:
	for key in expected:
		if not actual.has(key):
			return "%s.%s: thiếu khoá ở actual" % [path, key]
		var sub := first_difference(expected[key], actual[key], "%s.%s" % [path, key])
		if sub != "":
			return sub
	for key in actual:
		if not expected.has(key):
			return "%s.%s: khoá thừa ở actual" % [path, key]
	return ""


static func _diff_array(expected: Array, actual: Array, path: String) -> String:
	if expected.size() != actual.size():
		return "%s: kích thước khác (exp %d / got %d)" % [path, expected.size(), actual.size()]
	for i in expected.size():
		var sub := first_difference(expected[i], actual[i], "%s[%d]" % [path, i])
		if sub != "":
			return sub
	return ""


static func _diff_scalar(expected: Variant, actual: Variant, path: String) -> String:
	# bool trước (bool cũng khớp is int trong GDScript).
	if expected is bool or actual is bool:
		if typeof(expected) != typeof(actual) or expected != actual:
			return "%s: exp %s / got %s" % [path, expected, actual]
		return ""
	if (expected is int or expected is float) and (actual is int or actual is float):
		if float(expected) != float(actual):
			return "%s: exp %s / got %s" % [path, expected, actual]
		return ""
	if str(expected) != str(actual):
		return "%s: exp '%s' / got '%s'" % [path, expected, actual]
	return ""


static func _kind(v: Variant) -> String:
	if v is Dictionary:
		return "Dictionary"
	if v is Array:
		return "Array"
	return type_string(typeof(v))
