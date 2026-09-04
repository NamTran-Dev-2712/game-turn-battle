class_name UnitSnapshot
## Ảnh chụp bất biến một unit đầu trận (§9). `actor_id` là khoá tie-break cuối cùng (byte/ordinal).
extends RefCounted

var actor_id: String = ""
var hero_id: String = ""
var team: String = ""
var slot: int = 0
var stats: UnitStats = null


static func from_dict(d: Dictionary) -> UnitSnapshot:
	var u := UnitSnapshot.new()
	u.actor_id = str(d.get("actor_id", ""))
	u.hero_id = str(d.get("hero_id", ""))
	u.team = str(d.get("team", ""))
	u.slot = int(d.get("slot", 0))
	u.stats = UnitStats.from_dict(d.get("stats", {}))
	return u
