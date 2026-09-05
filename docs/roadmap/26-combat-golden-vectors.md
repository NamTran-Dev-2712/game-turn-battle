# 26 — Golden test vectors + cross-implementation CI gate

> Mục đích: Tạo bộ **golden test vector** và **cổng CI** chạy cả hai hiện thực (server .NET + client GDScript) trên cùng vector → phải ra cùng output, chặn merge nếu lệch (ADR-011).

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 5 Deterministic Combat Core | P2 | S6 | F04 |

# Mục tiêu

Bộ vector (nhiều kịch bản: đội khác nhau, skill, crit/miss, thắng/thua/hoà) + hai bộ test (server, client) chạy vector trong CI; thêm gate `golden-vector` (đã chừa ở phase 02/03) fail khi client≠server hoặc khác baseline.

# Lý do

ADR-011: golden vector là cơ chế đảm bảo hai hiện thực **không drift** theo thời gian. Đây là "khoá an toàn" cho toàn bộ combat — mọi thay đổi sim sau này phải giữ (hoặc cập nhật có chủ đích) golden.

# Phụ thuộc

- **Trước:** 24 (server sim), 25 (client sim), 23 (format), 02/03 (CI hook).
- **Sau:** 27–30 (gameplay dùng combat), 43 (sweep), 52/54 (hardening).

# Phạm vi

- Bộ vector `shared/` (hoặc `tools/`) dùng chung hai phía: input đầy đủ + output baseline.
- Test server chạy vector → so baseline; test client chạy vector → so baseline.
- CI gate `golden-vector`: chạy cả hai, fail nếu khác nhau hoặc khác baseline.
- Quy trình **cập nhật baseline có chủ đích** (khi đổi công thức, cập nhật vector + ghi lý do — doc-sync).

# Không thuộc phạm vi

- Thêm skill/nội dung mới (phase 28) — vector mở rộng khi có.
- Perf combat (phase 52).

# Deliverables

- Bộ golden vector (đa kịch bản) + baseline output.
- Test server & client chạy vector.
- CI gate `golden-vector` bật bắt buộc (cả server-CI và client-CI hoặc job tổng hợp).
- Quy trình cập nhật baseline tài liệu hoá.

# Công việc cần thực hiện

- [x] Sinh bộ vector đa kịch bản (đội/skill/crit/miss/biên thắng-thua-hoà), lưu chung `shared/combat-vectors/`. — **9 vector** `vector_01..09` (basic/crit/miss/defeat/draw/multi-unit/mixed-crit/boundary-lethal/boundary-survive).
- [x] Sinh baseline output từ sim server (nguồn chân lý) — commit làm "đáp án". — `tools/combat-baseline` (`run.sh generate`, ProjectReference `GameTeam.Domain`, KHÔNG fork sim); baseline = `expected` inline đã commit.
- [x] Test server: chạy mọi vector → so baseline byte-đồng nhất. — `GoldenVectorTests` `[Theory]`+`[MemberData]` tự khám phá; `dotnet test --filter GoldenVector` **9/9** + `run.sh check` byte exit 0.
- [x] Test client (gdUnit4): chạy mọi vector → so baseline. — `golden_vector_test.gd` + `CombatVectorLoader.list_vector_files()`; Godot 4.7.1 **24 test, 0 orphan**, exit 0.
- [ ] CI: bật gate `golden-vector` (server-CI job + client-CI job, hoặc workflow tổng), fail khi lệch. — **Workflow đã viết + BLOCKING + verify local**; kết quả Actions = **CI-verification pending** (§4.5). (`ci-server.yml` job `golden-vector` = `run.sh check` + golden tests; `ci-client.yml` + trigger `shared/combat-vectors/**`.)
- [x] Viết quy trình cập nhật baseline có chủ đích (đổi công thức → regenerate + review + ghi lý do), gắn doc-sync row "Combat sim change". — `tools/combat-baseline/README.md` + `combat-framework.md` §22.4 + doc-sync row **"Combat sim change"** (`.claude/workflows/documentation-sync.md`).
- [x] Thử negative: đổi nhẹ công thức một phía → CI đỏ → revert. — Chạy thật **hai phía** (lệnh gate cục bộ): server `+1` ⇒ `run.sh check` exit 1 + 9/9 đỏ; client `+1` ⇒ golden client đỏ (exit 100); revert ⇒ cả hai xanh.
- [x] Cập nhật [`../gameplay/combat-framework.md`](../gameplay/combat-framework.md) + [`../testing/backend-testing.md`](../testing/backend-testing.md) + [`../testing/godot-testing.md`](../testing/godot-testing.md). — §22 + §4.2 + §3 (+ §21.6 client Phase 25).

# Tiêu chí hoàn thành

- Mọi vector: server output ≡ client output ≡ baseline.
- CI gate `golden-vector` bật; lệch một phía → CI đỏ (đã thử negative).
- Quy trình cập nhật baseline rõ ràng (không sửa baseline âm thầm).
- Vector phủ các kịch bản chính (không chỉ happy-path).

# Cách kiểm tra

- CI chạy gate golden trên PR → xanh khi khớp.
- Negative: sửa rounding một phía → CI đỏ → revert → xanh.
- Local: `dotnet test` (server vectors) + gdUnit4 (client vectors) khớp baseline.

# Rủi ro

- **Baseline sai được "đóng băng"** → baseline sinh từ sim server đã test determinism; review kỹ lần đầu.
- **Vector phủ thiếu** → checklist kịch bản (crit/miss/hoà/nhiều skill); mở rộng khi thêm skill (28).
- **Cập nhật baseline tuỳ tiện che drift** → bắt buộc PR + lý do + review combat-determinism.

# Ghi chú

Đây là **cột mốc P2**: sau phase này combat được khoá an toàn hai phía. Mọi thay đổi sim về sau đi qua golden gate. Bám ADR-011 + doc-sync "Combat sim change".

# Technical Debt Review

- **Maintainability:** golden gate chống drift dài hạn.
- **Scalability:** vector mở rộng theo nội dung.
- **Testing:** đây là hợp đồng kiểm mạnh nhất của dự án.
- **Security:** đảm bảo server-authority nhất quán (chống cheat).
- **Nợ:** phủ vector cho skill mới (28); perf (52).

# Phase Review

Đóng khi golden vector đa kịch bản + gate CI hai phía hoạt động (kèm negative test) + quy trình cập nhật baseline. **Hoàn tất combat core — nền cho gameplay.**

## Kết quả (verify local 2026-09-04)

- **PASS** (còn 1 mục CI-Actions pending — xem dưới). Prerequisite **Phase 25 đóng cùng đợt** (roadmap 25 tick + `.memory/0023`;
  đã sửa nợ test `_input`→`_make_input`).
- **9 vector đa kịch bản** ở `shared/combat-vectors/` phủ: đội khác nhau + multi-unit (turn order `(-spd, actor_id)` + tie-break
  + đổi target khi front chết), skill/damage, crit, **miss**, VICTORY/**DEFEAT**/**DRAW**, biên damage==HP & damage<HP,
  chuỗi nhiều lượt. **Không** happy-path đơn thuần. Ultimate/energy (CB4 `[ĐỀ XUẤT]`) **không** tạo vector (chưa canon).
- **Baseline sinh từ sim server** = `tools/combat-baseline` (.NET console, ProjectReference `GameTeam.Domain` — dùng ĐÚNG một
  `BattleSimulator`, KHÔNG fork sim; parser `input` tự kiểm chéo với test loader). `run.sh generate`/`check` (byte drift guard).
  Tool xUnit **4/4**. Baseline (`expected` inline) đã commit.
- **Test hai phía tự khám phá vector** (thêm vector = không sửa code test): server `GoldenVectorTests` **9/9**; client
  `golden_vector_test.gd` (Godot 4.7.1) **24 test, 0 orphan**. Cả hai so CÙNG baseline ⇒ **server ≡ client ≡ baseline**.
- **Negative đã chạy thật (hai phía):** server `+1` ⇒ `run.sh check` exit 1 + 9/9 golden đỏ; client `+1` ⇒ golden client đỏ
  (exit 100, báo `vector_09`); revert ⇒ cả hai xanh. Chứng minh gate thực sự bắt drift.
- **Quy trình cập nhật baseline có chủ đích** + doc-sync row **"Combat sim change"** đã viết (không sửa baseline âm thầm).
- **Doc-sync đầy đủ:** `combat-framework.md` §21.6/§22 + `backend-testing.md` §4.2 + `godot-testing.md` §3 +
  `ci-cd-pipeline.md` §4 + `shared/combat-vectors/README.md` + `tools/combat-baseline/README.md` + agents
  (`combat-determinism`/`reviewer`) + `.instructions/{combat,backend,client}.md` + doc-sync matrix + CLAUDE.md §4.6 +
  `.memory/0023`+`0024`.
- **CI-verification pending (§4.5):** gate `golden-vector` (`ci-server.yml` + `ci-client.yml`) là **CI-only** — workflow đã
  BLOCKING + verify local, nhưng kết quả xanh/đỏ trên **GitHub Actions** chỉ có sau khi PR chạy. Mục checklist "CI: bật gate"
  giữ `[ ]` cho tới khi có Actions result.

**Đủ điều kiện đóng** (trừ xác nhận Actions ở trên) — **hoàn tất Deterministic Combat Core (Nhóm 5)**.

---

## Liên kết
- [`../gameplay/combat-framework.md`](../gameplay/combat-framework.md) · [`../testing/backend-testing.md`](../testing/backend-testing.md) · [`../testing/godot-testing.md`](../testing/godot-testing.md)
- ADR: [`../adr/ADR-011-combat-authority-and-determinism.md`](../adr/ADR-011-combat-authority-and-determinism.md)
- Roadmap: [`README.md`](README.md) → kế: [`27-hero-system.md`](27-hero-system.md)
