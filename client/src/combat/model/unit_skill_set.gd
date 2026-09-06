class_name UnitSkillSet
## Bộ skill của một unit (§23): `basic` (đòn thường, luôn có) + `ultimate` (chiêu cuối theo năng lượng —
## §15, tuỳ chọn). Khớp server `Model/UnitSkillSet.cs`. Gán `null` trên [UnitSnapshot] ⇒ unit dùng basic
## skill dùng chung của trận (tương thích ngược golden vector phase 24/26).
extends RefCounted

var basic: SkillDef = null
var ultimate: SkillDef = null


static func make(basic_skill: SkillDef, ultimate_skill: SkillDef = null) -> UnitSkillSet:
	var s := UnitSkillSet.new()
	s.basic = basic_skill
	s.ultimate = ultimate_skill
	return s
