# 23 — Combat spec & fixed-point math + golden vector format

> Mục đích: Chốt **đặc tả combat dùng chung** (một ruleset, hai hiện thực) + thư viện **fixed-point/integer math** + **định dạng golden test vector** — nền cho combat deterministic (ADR-011).

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 5 Deterministic Combat Core | P2 | S6 | F04 (nền) |

# Mục tiêu

Viết đặc tả combat chi tiết đủ để hiện thực **giống hệt** ở .NET và GDScript (thứ tự lượt, target/aggro, energy/ultimate, damage formula bằng integer/fixed-point, seeded RNG), + thư viện fixed-point dùng chung về mặt quy tắc, + định dạng golden vector (input: config version, team snapshot, stage, seed → output: log + kết quả).

# Lý do

ADR-011: combat là **rủi ro kỹ thuật cao nhất**, phải deterministic-by-seed và giống nhau hai phía. Chốt spec + math + vector format **trước** khi code sim để hai hiện thực không lệch, và có "hợp đồng kiểm" (golden vector) ngay từ đầu.

# Phụ thuộc

- **Trước:** 22 (đọc hero/skill config), 06 (schema hero/skill), ADR-011 (Accepted).
- **Sau:** 24 (sim server), 25 (sim client), 26 (golden vectors).

# Phạm vi

- Đặc tả combat: vòng lượt, thứ tự hành động (ổn định, không phụ thuộc iteration-order), target/aggro theo vị trí, energy/cooldown/ultimate, công thức damage/crit/miss **bằng integer/fixed-point**, điều kiện thắng/thua/hoà.
- Thư viện fixed-point/integer math: phép toán xác định, làm tròn cố định, không `float`.
- Seeded PRNG: thuật toán cụ thể (cùng công thức hai phía), seed truyền vào (không global RNG).
- Định dạng golden vector (JSON): input đầy đủ + output kỳ vọng (log sự kiện + kết quả).

# Không thuộc phạm vi

- Hiện thực sim (phase 24–25).
- Bộ vector đầy đủ (phase 26 — ở đây chỉ định dạng + 1–2 vector mẫu).
- Hiển thị/animation client (phase 30).

# Deliverables

- Tài liệu spec combat (mở rộng [`../gameplay/combat-framework.md`](../gameplay/combat-framework.md)) đủ để hai người hiện thực ra kết quả trùng.
- Đặc tả thuật toán PRNG + fixed-point (pseudo-code ngôn ngữ-trung lập).
- Schema/định dạng golden vector + 1–2 vector mẫu.
- Danh sách "quy tắc xác định" (no float, thứ tự ổn định, RNG seeded) đưa vào `../conventions/code-style.md`.

# Công việc cần thực hiện

- [x] Viết spec vòng lượt & thứ tự hành động **xác định** (tiêu chí sắp xếp rõ, không dựa thứ tự dictionary).
  *(`combat-framework.md` §12 vòng đời + §13 thứ tự: speed-sort mỗi round, khoá `(-spd, actor_id)`, **stable sort**,
  tie-break cuối = `actor_id` byte/UTF-8; cấm hash/iteration/insertion/DB order. Pseudo-code `build_action_order`.)*
- [x] Đặc tả target/aggro theo vị trí (CB3), energy/ultimate (CB4), crit/miss (CB5) — số liệu để config, spec chỉ mô tả cơ chế; link [`../gameplay/combat-framework.md`](../gameplay/combat-framework.md).
  *(§14 target/aggro [ĐỀ XUẤT] — ứng viên = địch sống, sắp `(slot, actor_id)`, policy config, re-resolve khi target chết;
  §15 energy/ultimate [ĐỀ XUẤT] — energy-bar, số config; §16 crit/miss [CHỐT cơ chế] — hit→crit, ngưỡng bp. CB3/CB4 để mở.)*
- [x] Đặc tả công thức damage bằng integer/fixed-point (điểm làm tròn cố định).
  *(§17 divisive DEF-ratio `atk*coeff*K/(K+def)` fixed-point, crit sau mitigation, làm tròn cuối `from_fixed`, sàn
  `MIN_DMG`; thứ tự phép tính 1→6 + điểm làm tròn cố định. §10 `FIXED_SCALE=1000` + **round-half-up** luật duy nhất.)*
- [x] Chọn & đặc tả thuật toán PRNG seeded (vd PCG/xorshift) — pseudo-code, cùng hai phía.
  *(§11 **PCG32** `pcg_setseq_64_xsh_rr_32` + nở seed **SplitMix64**; pseudo-code `pcg_seed`/`pcg_next_u32`/`pcg_bounded`;
  logical shift + wrap 64-bit; một stream/trận, seed server. User-approved: PCG32/SplitMix64.)*
- [x] Định nghĩa định dạng golden vector JSON (config_version, team snapshot, stage, seed → event log + result).
  *(`../../shared/combat-vectors/README.md` — schema top-level/input/Unit/config_excerpt/event_log/result; §18/§20 combat-framework.)*
- [x] Tạo 1–2 vector mẫu tay/tham chiếu để 24–25 kiểm sơ bộ.
  *(`vector_01_basic_hit.json` 59 event → VICTORY; `vector_02_crit_ko.json` 30 event → VICTORY. Sinh bởi reference
  calculator bám pseudo-code, **kiểm tay** damage khớp (789/157800/158); valid JSON.)*
- [x] Ghi "quy tắc xác định" vào `../conventions/code-style.md` + [`../gameplay/combat-framework.md`](../gameplay/combat-framework.md).
  *(`code-style.md` §4 thêm bảng "Chốt cụ thể (Phase 23)" — link canon, không lặp số; combat-framework §9–§20 là canon.)*
- [x] Cập nhật các open-question combat (CB1–CB6) đã chốt/để lại trong `../mvp/10`.
  *(`10-open-questions.md` §2 note 2026-09-01: CB1/CB2 chốt (ADR-011), CB5/CB6 cơ chế chốt, CB3/CB4 [ĐỀ XUẤT]; §13 gạch CB1/CB2.)*

# Tiêu chí hoàn thành

- Spec đủ chi tiết: hai lập trình viên độc lập hiện thực → cùng output cho vector mẫu (kiểm ý niệm).
- Không `float` trong bất kỳ phép toán combat nào (spec cấm rõ).
- PRNG seeded xác định, công thức mô tả đủ để tái lập.
- Định dạng golden vector rõ + vector mẫu hợp lệ.

# Cách kiểm tra

- Review chéo spec bởi vai trò combat-determinism (charter `.agents/`).
- Tính tay/tham chiếu 1 vector mẫu → khớp mô tả spec.
- Rà spec: mọi giá trị combat là integer/fixed-point; RNG nhận seed.

# Rủi ro

- **Spec mơ hồ → hai hiện thực lệch** → viết pseudo-code + vector mẫu làm "chân lý"; review chặt.
- **Iteration-order không ổn định** → quy định tiêu chí sắp xếp tường minh (id ổn định).
- **Fixed-point làm tròn khác nhau** → đặc tả điểm & luật làm tròn duy nhất.

# Ghi chú

Đây là phase **đặc tả**, không code sim. Là "hợp đồng combat" mà phase 24–25 phải tuân. Số liệu balance để config (data-driven); spec chỉ mô tả cơ chế. Bám ADR-011 + [`../gameplay/combat-framework.md`](../gameplay/combat-framework.md).

# Technical Debt Review

- **Maintainability:** một spec cho hai hiện thực giảm drift.
- **Scalability:** thêm skill/effect qua registry (ADR-004), không sửa lõi.
- **Testing:** golden vector format là xương sống kiểm.
- **Security:** determinism là nền chống cheat (server re-sim).
- **Nợ:** bộ vector đầy đủ & skill phức tạp (phase 26/28).

# Phase Review

Đóng khi spec + fixed-point + PRNG + golden format chốt, vector mẫu khớp, quy tắc xác định tài liệu hoá, review combat-determinism đạt. **Hoàn tất Phase 23 — mở Nhóm 5 (Deterministic Combat Core); hợp đồng combat sẵn sàng cho sim 24/25.**

**Kết quả (2026-09-01 — local PASS, phase đặc tả không code sim):**

- **Quyết định hiến pháp (mechanism; số liệu để config — user-approved):** thứ tự hành động = speed-sort mỗi round khoá
  `(-spd, actor_id)` stable (tie-break `actor_id` byte/UTF-8); fixed-point 64-bit × **`FIXED_SCALE=1000`** + **round-half-up**
  là luật làm tròn **DUY NHẤT** (mọi `fixed_mul`/`fixed_div`/`from_fixed`; chia 0 = guard); PRNG **PCG32 + SplitMix64**
  (một stream/trận, seed `uint64` server, dịch logical + nhân wrap); RNG order `hit`→`crit` (miss=1/hit=2 roll); damage
  **divisive DEF-ratio** crit-sau-mitigation sàn `MIN_DMG`; event log `seq`-ordered (cùng chuỗi + trường); thắng/thua/hoà
  góc `ally`, `max_rounds`⇒DRAW.
- **CB status:** CB1/CB2 chốt (ADR-011); CB5 cơ chế chốt (số liệu config, tắt ngẫu nhiên = `accuracy_bp=10000`/`crit_rate_bp=0`);
  CB6 cơ chế chốt (số round/giây mở); **CB3/CB4 `[ĐỀ XUẤT]`** — không promote thành canon, chờ product (CB3 phụ thuộc GP5).
- **Deliverables:** `combat-framework.md` §9–§20 (spec chi tiết pseudo-code) · `shared/combat-vectors/` (README schema +
  `vector_01_basic_hit.json` + `vector_02_crit_ko.json`) · `code-style.md` §4 (tóm tắt) · `10-open-questions.md` CB1–CB6.
- **Verify:** reference calculator (scratchpad, throwaway) sinh 2 vector bám đúng pseudo-code; **kiểm tay** damage khớp
  (`fixed_div(300000,380000)=789` → `fixed_mul(200000,789)=157800` → `from_fixed=158`; crit `222820×1.5=334230→334`);
  2 file **valid JSON**; **no-float audit** + **contradiction audit** combat spec sạch; không drift `openapi.json`/generated
  (đổi doc thuần). Review combat-determinism (self) đạt: no float, seed tường minh, PRNG + call order tất định, no global
  RNG, thứ tự ổn định, event order, fixed-point/rounding/division/overflow/clamp rõ, cross-language reproducible.
- **Scope discipline:** KHÔNG code sim (24/25), KHÔNG bộ vector đầy đủ / CI gate cross-impl (26), KHÔNG skill/effect phức
  tạp (28), KHÔNG UI (30). Số liệu balance để config (`combat_int`).
- **Doc-sync:** `docs/gameplay/combat-framework.md` §9–§20 · `shared/combat-vectors/{README,vector_01,vector_02}` ·
  `docs/conventions/code-style.md` §4 · `docs/mvp/10-open-questions.md` · `.instructions/combat.md` ·
  `.claude/agents/combat-determinism.md` · `.agents/ROLES.md` · `.claude/workflows/documentation-sync.md` (row combat) ·
  `CLAUDE.md` §4.6 (block Phase 23) · `.memory/0021-combat-spec-fixedpoint-standardized.md` (+ `.memory/README.md`).
- **Đủ điều kiện đóng:** 8/8 `# Công việc cần thực hiện` `[x]` có bằng chứng; `# Tiêu chí hoàn thành` đạt (spec đủ để hai
  hiện thực trùng vector mẫu; không `float`; PRNG tái lập; định dạng vector rõ + mẫu hợp lệ); không TODO/blocker.

---

## Liên kết
- [`../gameplay/combat-framework.md`](../gameplay/combat-framework.md) · [`../gameplay/skill-framework.md`](../gameplay/skill-framework.md) · [`../conventions/code-style.md`](../conventions/code-style.md) · [`../mvp/10-open-questions.md`](../mvp/10-open-questions.md)
- ADR: [`../adr/ADR-011-combat-authority-and-determinism.md`](../adr/ADR-011-combat-authority-and-determinism.md)
- Roadmap: [`README.md`](README.md) → kế: [`24-combat-sim-server.md`](24-combat-sim-server.md)
