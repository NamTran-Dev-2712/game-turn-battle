class_name HeroStats
## Công thức chỉ số hero theo cấp + Power Rating (Phase 35) — **tất định, integer** (ADR-011, KHÔNG float)
## và **data-driven** (đường cong/tăng trưởng/trọng số từ economy config, ADR-004).
##
## Song ánh bit-for-bit với server `GameTeam.Application/Features/Heroes/HeroStatCalculator.cs`: dùng để
## HIỂN THỊ chỉ số theo cấp (hero detail) và để client sim tính lại chỉ số ally khi replay. Client KHÔNG
## phải chân lý — server quyết cấp/chỉ số/Power; đây chỉ là bản tính lại tất định để hiển thị/replay.
extends RefCounted

const _GROWTH_DENOMINATOR_BP: int = 10000
const _LEVEL_UP_CURVE: String = "level_up"


## Chỉ số cấp `level` = `base_stat` + round_half_up(base × growth_bp × (level−1) / 10000).
## Cấp ≤ 1 (hoặc growth/base = 0) ⇒ chỉ số nền. Khớp `HeroStatCalculator.ScaleStat` của server.
static func scale_stat(base_stat: int, level: int, growth_bp: int) -> int:
	if level <= 1 or growth_bp == 0 or base_stat == 0:
		return base_stat
	var added := FixedPoint.round_half_up(base_stat * growth_bp * (level - 1), _GROWTH_DENOMINATOR_BP)
	return base_stat + added


## Bộ chỉ số cuối theo cấp: { hp, atk, def, spd } từ `base_stats` (hp/atk/def/spd) + cấp + growth_bp.
static func scaled_stats(base_stats: Dictionary, level: int, growth_bp: int) -> Dictionary:
	return {
		"hp": scale_stat(int(base_stats.get("hp", 0)), level, growth_bp),
		"atk": scale_stat(int(base_stats.get("atk", 0)), level, growth_bp),
		"def": scale_stat(int(base_stats.get("def", 0)), level, growth_bp),
		"spd": scale_stat(int(base_stats.get("spd", 0)), level, growth_bp),
	}


## Power Rating = tổng có trọng số của chỉ số cuối (integer). Trọng số từ config `power_weights`.
static func power(stats: Dictionary, weights: Dictionary) -> int:
	return (int(stats.get("hp", 0)) * int(weights.get("hp", 0))
		+ int(stats.get("atk", 0)) * int(weights.get("atk", 0))
		+ int(stats.get("def", 0)) * int(weights.get("def", 0))
		+ int(stats.get("spd", 0)) * int(weights.get("spd", 0)))


## Tăng trưởng chỉ số/cấp (bp) từ economy config; 0 nếu thiếu.
static func growth_bp(economy: Dictionary) -> int:
	return int(economy.get("level_stat_growth_bp", 0))


## Trọng số Power từ economy config; {} nếu thiếu.
static func power_weights(economy: Dictionary) -> Dictionary:
	var w: Variant = economy.get("power_weights", {})
	return w if w is Dictionary else {}


## Số cấp tối đa = 1 + độ dài đường cong `level_up`. Không có đường cong ⇒ 1.
static func max_level(economy: Dictionary) -> int:
	return 1 + _level_up_curve(economy).size()


## Chi phí (gold) để lên từ `current_level` sang cấp kế = `level_up[current_level-1]`; -1 nếu đã max cấp.
static func level_up_cost(economy: Dictionary, current_level: int) -> int:
	var curve := _level_up_curve(economy)
	var index := current_level - 1
	if index < 0 or index >= curve.size():
		return -1
	return int(curve[index])


static func _level_up_curve(economy: Dictionary) -> Array:
	var curves: Variant = economy.get("cost_curves", {})
	if not (curves is Dictionary):
		return []
	var curve: Variant = (curves as Dictionary).get(_LEVEL_UP_CURVE, [])
	return curve if curve is Array else []
