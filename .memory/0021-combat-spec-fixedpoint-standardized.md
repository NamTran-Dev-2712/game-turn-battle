# 0021 — Combat spec + fixed-point + golden vector format standardized (Phase 23)

- **Trạng thái:** Đã chốt & verify cục bộ (2026-09-01). **MỞ Nhóm 5 (Deterministic Combat Core).** Đây là phase **đặc
  tả**, KHÔNG code sim (sim = phase 24/25; bộ vector đầy đủ + CI gate = phase 26).
- **Bối cảnh:** `combat-framework.md` trước Phase 23 mới là doc **kiến trúc/ranh giới**; ADR-011 chốt *tính chất*
  (integer/fixed-point, seeded PRNG, thứ tự ổn định, golden vector) nhưng **không** chốt thuật toán cụ thể (scale, làm
  tròn, PRNG, tie-break, công thức damage, schema event log, định dạng vector). Phase 23 lấp đúng các khoảng trống đó
  thành **hợp đồng combat** để hai hiện thực không lệch.

## Quyết định (mechanism; số liệu để config — user-approved)

- **Thứ tự hành động:** speed-sort **mỗi round**, khoá `(-spd, actor_id)`, **stable sort**; tie-break cuối = `actor_id`
  (so byte/UTF-8, duy nhất toàn trận). Không dựa hash/iteration/insertion/DB order.
- **Damage:** **divisive DEF-ratio** `atk*coeff*K/(K+def)` fixed-point; crit **sau** mitigation; làm tròn cuối
  `from_fixed`; sàn `MIN_DMG`. Thứ tự phép tính + điểm làm tròn **cố định**.
- **Fixed-point:** integer **64-bit** × **`FIXED_SCALE=1000`**; **round-half-up** là **luật làm tròn DUY NHẤT** tại mọi
  `fixed_mul`/`fixed_div`/`from_fixed` (không floor/banker's); chia 0 = guard (không NaN/float).
- **PRNG:** **PCG32** (`pcg_setseq_64_xsh_rr_32`) + nở seed **SplitMix64**; **một stream/trận**; seed `uint64` **server
  sinh**, input tường minh; dịch **logical**, nhân **wrap mod 2^64**; `pcg_bounded` không thiên vị; roll theo basis points.
- **RNG consumption:** thứ tự cố định `hit` → `crit`; **miss = 1 roll, hit = 2 roll** (tiêu thụ roll crit kể cả
  `crit_rate_bp==0`) — chống lệch stream.
- **Event log:** stream có `seq` tăng dần; hai phía phát **cùng chuỗi + cùng trường** (không chỉ HP cuối).
- **Thắng/thua/hoà:** góc nhìn đội `ally`, đánh giá sau mỗi action/round; `max_rounds` ⇒ DRAW.

## CB1–CB6 (`docs/mvp/10-open-questions.md`)

- **CB1/CB2 — CHỐT** (ADR-011): server-authoritative; deterministic-by-seed.
- **CB5 — CHỐT cơ chế** (số liệu config): hit/crit seeded PRNG, ngưỡng `accuracy_bp`/`crit_rate_bp`; tắt ngẫu nhiên =
  `accuracy_bp=10000`/`crit_rate_bp=0`.
- **CB6 — CHỐT cơ chế**, số round/giây mục tiêu **mở**.
- **CB3/CB4 — `[ĐỀ XUẤT]`** (target/aggro tất định + energy-bar), số liệu config, **chờ product** — KHÔNG promote thành
  canon; CB3 chi tiết lưới phụ thuộc GP5 (mở).

## Thay đổi chính

- `docs/gameplay/combat-framework.md`: thêm **§9–§20** (I/O sim, fixed-point, PRNG, vòng đời, thứ tự, target/aggro,
  energy/ultimate, crit/miss, damage, event log, thắng/thua/hoà, golden vector) — pseudo-code ngôn ngữ-trung lập.
- `shared/combat-vectors/`: **MỚI** — `README.md` (định dạng/schema) + `vector_01_basic_hit.json` (59 event → VICTORY) +
  `vector_02_crit_ko.json` (30 event → VICTORY). Sinh bởi **reference calculator** bám đúng pseudo-code, kiểm tay khớp.
- `docs/conventions/code-style.md` §4: thêm bảng "Chốt cụ thể (Phase 23)" (tóm tắt, link canon; không lặp số).
- `docs/mvp/10-open-questions.md`: note CB1–CB6 (2026-09-01) + gạch CB1/CB2 ở §13.
- Doc AI/agent: `.instructions/combat.md`, `.claude/agents/combat-determinism.md`, `.agents/ROLES.md` (completion workflow
  + anti-self-invention), doc-sync matrix (row combat mở rộng), CLAUDE.md §4.6 (block Phase 23).

## Verify (cục bộ)

- Reference calculator (scratchpad, throwaway — KHÔNG commit) sinh 2 vector; **kiểm tay** damage: `fixed_div(300000,380000)=789`,
  `fixed_mul(200000,789)=157800`, `from_fixed=158` (vector_01 ally→enemy) ✓; crit vector_02: `222820 ×1.5 → 334230 → 334` ✓.
- `python -c json.load` → 2 file vector **valid JSON**.
- No-float audit + contradiction audit combat spec **sạch**; không drift `openapi.json`/`client/src/data/generated` (đổi
  doc thuần).

## Ranh giới / Nợ

- Hiện thực sim = **phase 24 (.NET)** / **25 (GDScript)**; bộ vector đầy đủ (miss/draw/multi-unit/ultimate) + **CI gate
  cross-impl** = **phase 26**; skill/effect phức tạp = **phase 28**; UI/animation = **phase 30**.
- Số liệu balance (stat/coeff/rate/K/cost/cooldown) để **config** — không hardcode; CB3/CB4 chờ product.

Liên quan: [[0004-config-schema-standardized]] (`combat_int`) · [[0019-config-service-standardized]] (config version) ·
ADR-011. CLAUDE.md §4.6 (block Phase 23) + doc-sync matrix (row combat).
