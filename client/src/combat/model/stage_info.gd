class_name StageInfo
## Thông tin stage cần cho sim (§9): id + số vòng tối đa (chặn trận vô tận → DRAW).
extends RefCounted

var id: String = ""
var max_rounds: int = 0


static func make(id_value: String, max_rounds_value: int) -> StageInfo:
	var s := StageInfo.new()
	s.id = id_value
	s.max_rounds = max_rounds_value
	return s
