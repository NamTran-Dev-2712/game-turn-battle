# 0024 — Golden vector suite + cross-impl CI gate standardized (Phase 26)

- **Trạng thái:** Đã chốt & verify cục bộ (2026-09-04). **Đóng Nhóm 5 (Deterministic Combat Core).** Bộ golden vector đa
  kịch bản + baseline sinh từ sim server + gate CI hai phía + quy trình cập nhật baseline có chủ đích. Từ đây **mọi thay
  đổi combat sim phải đi qua golden gate** (ADR-011). Không đổi spec (§9–§20 giữ nguyên).
- **Bối cảnh:** Phase 23 chốt định dạng + 2 vector mẫu; Phase 24/25 hiện thực sim server/client. Trước Phase 26 chỉ có 2
  vector happy-path (VICTORY) và job CI `golden-vector` là **placeholder**. Cả hai phía đã tự so với `expected` inline nhưng
  chưa có bộ đa kịch bản, chưa có tool sinh baseline, chưa có gate thật.

## Quyết định (user-approved)

- **Đóng Phase 25 trước** (prerequisite chưa Đóng dù code đã ship): verify + tick roadmap 25 + `.memory/0023` — xem
  [[0023-combat-sim-client-standardized]].
- **Baseline sinh từ tool `tools/combat-baseline`** (.NET console, KHÔNG viết tay), không dùng generator trong test-project.
- **Baseline = `expected` inline trong mỗi vector** (giữ convention Phase 23, không tách file baseline riêng).
- **Cross-impl = transitive qua baseline chung:** server so baseline + client so baseline ⇒ **server ≡ client ≡ baseline**
  (không cần so trực tiếp output server-vs-client trong 1 tiến trình).

## Thành phần

- **`tools/combat-baseline/`** (.NET 9, khuôn `tools/config-validator`: core lib + CLI mỏng + xUnit + `run.sh`):
  **ProjectReference `GameTeam.Domain`** ⇒ dùng ĐÚNG một `BattleSimulator`+`CombatEventSerializer` (**KHÔNG fork sim**).
  `run.sh generate` ghi khối `expected` từ sim server (chuẩn tắc 2-space/LF/newline cuối, idempotent); `run.sh check`
  regenerate trong bộ nhớ rồi so **byte** với vector đã commit (exit 0/1/2 — drift guard). Parser `input` của tool song ánh
  `GoldenVectorLoader` bên test ⇒ nếu lệch, `GoldenVectorTests` tự đỏ (tự kiểm chéo). 4 xUnit test (generate→check clean,
  idempotent, phát hiện drift).
- **9 vector** ở `shared/combat-vectors/` (baseline server-generated): `vector_01_basic_hit`, `vector_02_crit_ko`,
  `vector_03_miss` (Miss), `vector_04_defeat` (DEFEAT), `vector_05_draw` (DRAW ở max_rounds), `vector_06_multi_unit`
  (2v2: turn order `(-spd, actor_id)` + tie-break + đổi target khi front chết), `vector_07_mixed_crit` (crit lẫn thường),
  `vector_08_boundary_lethal` (damage == HP ⇒ Death), `vector_09_boundary_survive` (damage < HP ⇒ còn 1 HP, không Death).
- **Test tự khám phá:** server `GameTeam.Domain.Tests/Combat/GoldenVectorTests` = `[Theory]`+`[MemberData]` liệt kê vector
  dir; client `golden_vector_test.gd` dùng `CombatVectorLoader.list_vector_files()`. Thêm vector = **KHÔNG sửa code test**.
- **Gate CI `golden-vector` (BLOCKING):** `.github/workflows/ci-server.yml` job thật = `run.sh check` + `dotnet test
  --filter GoldenVector`; nửa client ở `ci-client.yml` (gdUnit4) + thêm trigger `shared/combat-vectors/**`. Không
  `continue-on-error`/`|| true`.

## Verify (local 2026-09-04)

- Server: `dotnet test --filter GoldenVector` **9/9 pass**; `run.sh check` exit 0 (9 vector khớp); tool xUnit **4/4**.
- Client (Godot 4.7.1): gdUnit4 `tests/combat` — golden auto-discover xanh, **24 test, 0 orphan**, exit 0.
- **Negative (đã chạy thật, hai phía):** `+1` vào `DamageEffectHandler.ComputeDamage` (server) ⇒ `run.sh check` exit 1 +
  9/9 golden đỏ; `+1` ở `damage_effect_handler.gd` (client) ⇒ golden client đỏ (exit 100, báo tên `vector_09`). Revert ⇒
  cả hai xanh.

## Quy tắc (binding về sau)

- **Cập nhật baseline CÓ CHỦ ĐÍCH:** đổi công thức → golden đỏ → xác nhận cố ý → `run.sh generate` → **review diff** → ghi
  WHY → doc-sync ("Combat sim change") → review `combat-determinism`. **Cấm** regenerate âm thầm / sửa vector để CI xanh /
  nới lỏng comparison / làm gate non-blocking. Baseline là **server-generated** — client replay, không định nghĩa kết quả.
- **Ngoài phạm vi:** vector ultimate/energy (CB4 `[ĐỀ XUẤT]`, chưa canon) + skill mới = phase 28; signed/nén/delta = Post-MVP.
- Canon: `docs/gameplay/combat-framework.md` §22 + `shared/combat-vectors/README.md` + `tools/combat-baseline/README.md`;
  doc-sync row **"Combat sim change"**. CI-pending: gate trên Actions.
