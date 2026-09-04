class_name Pcg32
## PRNG **PCG32** (variant `pcg_setseq_64_xsh_rr_32`) + seed-expander **SplitMix64**
## (combat-framework.md §11, ADR-011). Một [Pcg32] = **một stream/trận**, seed là một uint64
## server-generated truyền tường minh — **KHÔNG** RNG global/ambient, KHÔNG `randi`/`randf`/
## `RandomNumberGenerator`, KHÔNG timestamp/OS randomness.
##
## GDScript `int` là 64-bit **có dấu**: state được xử lý như bit-pattern **unsigned 64-bit**. Nhân/cộng
## wrap mod 2^64 tự nhiên (two's-complement); dịch phải `>>` của GDScript là **arithmetic** nên mọi
## dịch phải logic đi qua [method _lsr]; output 32-bit mask `& 0xFFFFFFFF`. Mọi hằng/độ rộng khớp
## bit-for-bit với server `GameTeam.Domain/Combat/Rng/Pcg32.cs` (phase 24).
extends RefCounted

const _PCG_MULT: int = 6364136223846793005 # 0x5851F42D4C957F2D
const _SM_INC: int = -7046029254386353131 # 0x9E3779B97F4A7C15 (unsigned; two's-complement dạng có dấu)
const _SM_MIX1: int = -4658895280553007687 # 0xBF58476D1CE4E5B9
const _SM_MIX2: int = -7723592293110705685 # 0x94D049BB133111EB
const _U32_MASK: int = 0xFFFFFFFF
const _POW_2_32: int = 0x100000000 # 2^64/2^32 boundary → 4294967296

var _state: int = 0
var _inc: int = 0


## Khởi tạo stream từ `seed_value` (uint64) qua SplitMix64 → (initstate, initseq) → PCG seed.
func _init(seed_value: int) -> void:
	var sm: int = seed_value
	var init_state: int
	var init_seq: int
	var out1: Array = _splitmix64_next(sm)
	sm = out1[0]
	init_state = out1[1]
	var out2: Array = _splitmix64_next(sm)
	init_seq = out2[1]

	_inc = (init_seq << 1) | 1 # phải lẻ
	_state = 0
	_state = (_state * _PCG_MULT) + _inc # step
	_state += init_state
	_state = (_state * _PCG_MULT) + _inc # step


## Sinh 32-bit kế tiếp (advance state, xuất qua xorshift + rotate — logical shift). Trả [0, 2^32).
func next_u32() -> int:
	var old: int = _state
	_state = (old * _PCG_MULT) + _inc
	var xorshifted: int = _lsr(_lsr(old, 18) ^ old, 27) & _U32_MASK
	var rot: int = _lsr(old, 59) & 31
	return ((xorshifted >> rot) | ((xorshifted << ((-rot) & 31)) & _U32_MASK)) & _U32_MASK


## Sinh số không thiên vị trong [0, `bound`) bằng rejection-sampling (§11). Vòng lặp KHÔNG có trần
## thử lại (theo spec — không tồn tại hằng cap). `bound` ≥ 1.
func bounded(bound: int) -> int:
	assert(bound >= 1, "bound phải ≥ 1.")
	var threshold: int = (_POW_2_32 - bound) % bound
	while true:
		var r: int = next_u32()
		if r >= threshold:
			return r % bound
	return 0 # không đạt tới (thoả mãn kiểm tra kiểu)


# Một bước SplitMix64: trả [state mới, output 64-bit]. Nhân/cộng wrap 64-bit; dịch phải logic qua _lsr.
func _splitmix64_next(state: int) -> Array:
	state = state + _SM_INC
	var z: int = state
	z = (z ^ _lsr(z, 30)) * _SM_MIX1
	z = (z ^ _lsr(z, 27)) * _SM_MIX2
	z = z ^ _lsr(z, 31)
	return [state, z]


# Logical shift right trên bit-pattern 64-bit (GDScript `>>` là arithmetic cho số âm). n trong [1,63].
func _lsr(value: int, n: int) -> int:
	if n <= 0:
		return value
	return (value >> n) & ((1 << (64 - n)) - 1)
