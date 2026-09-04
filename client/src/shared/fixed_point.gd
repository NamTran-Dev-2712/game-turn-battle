class_name FixedPoint
## Fixed-point số học **xác định** cho combat sim (combat-framework.md §10, ADR-011).
##
## Một số fixed-point là [int] 64-bit có dấu biểu diễn (giá trị thực × [constant FIXED_SCALE]).
## **Một luật làm tròn duy nhất — round-half-up** — áp tại MỌI [method mul]/[method div]/
## [method from_fixed]. Mọi đại lượng combat là **không âm**; toán hạng âm là vi phạm hợp đồng
## (assert), KHÔNG bao giờ rơi về float. Chia cho 0 là lỗi logic/config (mẫu số luôn ≥ 1 theo bất
## biến config `K ≥ 1`). **Cấm tuyệt đối float** trong toàn bộ đường sim.
##
## Song ánh bit-for-bit với server `GameTeam.Domain/Combat/Numerics/FixedPoint.cs` (phase 24).
extends RefCounted

## Hệ số tỉ lệ fixed-point (base-10, 3 chữ số thập phân). `1.0 → 1000`, `1.5 → 1500`.
const FIXED_SCALE: int = 1000


## Làm tròn `num/den` theo **round-half-up** (num ≥ 0, den ≥ 1). Chia nguyên cắt về 0:
## `(num + den/2) / den` — đúng vì mọi toán hạng không âm (§10). Khớp `(num + den/2)/den` của server.
static func round_half_up(num: int, den: int) -> int:
	assert(den >= 1, "Mẫu số fixed-point phải ≥ 1 (cấm chia 0 — §10).")
	assert(num >= 0, "Toán hạng fixed-point phải không âm (§10).")
	return (num + (den / 2)) / den


## Chuyển số nguyên → fixed-point (× [constant FIXED_SCALE]). Yêu cầu không âm.
static func to_fixed(value: int) -> int:
	assert(value >= 0, "Giá trị fixed-point phải không âm (§10).")
	return value * FIXED_SCALE


## Chuyển fixed-point → số nguyên (round-half-up về đơn vị 1).
static func from_fixed(fixed_value: int) -> int:
	return round_half_up(fixed_value, FIXED_SCALE)


## Nhân fixed-point (làm tròn về scale tại toán tử — round-half-up).
static func mul(a: int, b: int) -> int:
	return round_half_up(a * b, FIXED_SCALE)


## Chia fixed-point (làm tròn về scale tại toán tử — round-half-up). Yêu cầu `b ≥ 1`.
static func div(a: int, b: int) -> int:
	return round_half_up(a * FIXED_SCALE, b)


## So sánh hai fixed-point: dấu của `a - b` (-1 / 0 / 1).
static func cmp(a: int, b: int) -> int:
	return signi(a - b)


## Kẹp `x` vào [`lo`, `hi`].
static func clamp_int(x: int, lo: int, hi: int) -> int:
	return lo if x < lo else (hi if x > hi else x)
