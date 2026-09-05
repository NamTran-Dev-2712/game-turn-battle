# 0023 — Deterministic combat sim client (GDScript) standardized (Phase 25)

- **Trạng thái:** Đã chốt & verify cục bộ (2026-09-04, đóng cùng đợt Phase 26). **Hiện thực** sim client GDScript
  **song ánh bit-for-bit** sim server (Phase 24) — chỉ để **replay/dự đoán** theo seed, **KHÔNG phải chân lý** (ADR-011).
  Code đã ship (#54) nhưng roadmap chưa tick/không có memory; Phase 26 (yêu cầu prerequisite Đóng) đóng nốt phần này.
- **Bối cảnh:** ADR-011 — server là chân lý; client sim phải khớp bit quy tắc để replay đúng kết quả server trả (phase 30).

## Thành phần

- **Lõi ở `client/src/combat/`** + `client/src/shared/fixed_point.gd` (thuần, không phụ thuộc scene/UI):
  `battle_simulator.gd` (`BattleSimulator.simulate(BattleInput) → { event_log, result }`); `FixedPoint`
  (`FIXED_SCALE=1000`, `round_half_up=(num+den/2)/den` — **một luật, không float**); `rng/pcg32.gd` (SplitMix64→PCG32
  `pcg_setseq_64_xsh_rr_32`; xử lý int 64-bit có dấu như bit-pattern unsigned, `_lsr` dịch **logical** vì `>>` GDScript là
  arithmetic; hằng khớp `Pcg32.cs`); `events/combat_events.gd` (13 loại, factory snake_case khớp golden, `seq` theo vị trí);
  `effects/damage_effect_handler.gd` (`compute_damage` divisive DEF-ratio, crit sau mitigation, sàn `min_damage`) + registry;
  `model/*` (mirror server) + `combat_input_resolver.gd`.
- **Thứ tự tất định khớp server:** lượt `sort_custom` theo `(-spd, actor_id<)`; target `(slot<, actor_id<)`; ROLL_BOUND=10000;
  hit luôn 1 roll, crit luôn 1 roll sau hit. `final_hp` insertion order ally→enemy.

## Verify (Godot 4.7.1 local)

- gdUnit4 `client/tests/combat/*`: golden (khớp baseline server), determinism (N lần trùng), outcome
  (DEFEAT/DRAW/miss/turn-order + tie-break), fixed_point, pcg32, resolver — **24–25 test, 0 failure, 0 orphan**.
- Grep sạch: lõi sim không import `core/net`/UI; **không `float`** trong tính combat.

## Sửa nợ (Phase 26 phát hiện)

- `client/tests/combat/battle_simulator_outcome_test.gd`: helper `_input(...)` **trùng virtual** `Node._input(InputEvent)`
  ⇒ Godot 4.7.1 parse-error chặn **cả** suite `tests/combat`. Đổi tên `_input`→`_make_input` (chỉ tên helper test, không đổi
  hành vi sim). Bài học: **không đặt tên hàm test trùng virtual Godot** (`_input`/`_ready`/`_process`…).

## Ranh giới

- Client **không** quyết kết quả/thưởng (server — ADR-011); vào battle flow (phase 30) client replay bằng seed server, kết
  quả phải khớp. Golden cross-impl gate = phase 26 (xem [[0024-combat-golden-vectors-standardized]]).
- Canon: `docs/gameplay/combat-framework.md` §21.6. Server sim = [[0022-combat-sim-server-standardized]];
  spec = [[0021-combat-spec-fixedpoint-standardized]].
