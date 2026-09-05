# combat-baseline — Golden vector baseline generator (Phase 26)

> Sinh và kiểm tra **baseline** (`expected`) của golden combat vector từ **sim server** — nguồn chân lý
> của kết quả trận (ADR-011). Đây là cơ chế "đáp án" để hai hiện thực (server .NET + client GDScript)
> không drift. **KHÔNG viết baseline bằng tay**; baseline luôn sinh từ `BattleSimulator` (Phase 24).

## Tại sao có tool này

Golden vector = `input → expected`. `expected` (event_log 60+ sự kiện) phải sinh từ **một** hiện thực
authority = sim server, rồi commit làm baseline. Cả server test lẫn client test đều so với baseline này
⇒ transitively **server ≡ client ≡ baseline**. Tool này là chỗ DUY NHẤT tạo/kiểm baseline đó.

Tool **ProjectReference `GameTeam.Domain`** ⇒ gọi đúng một `BattleSimulator` + `CombatEventSerializer`
(KHÔNG fork sim thứ hai). Parser `input` của tool song ánh `GoldenVectorLoader` bên test server; nếu hai
parser lệch nhau, `GoldenVectorTests` tự đỏ (tự kiểm chéo).

## Dùng

```bash
# Sinh/ghi lại baseline cho tất cả vector (hoặc chỉ vài file):
bash tools/combat-baseline/run.sh generate
bash tools/combat-baseline/run.sh generate vector_04_defeat.json

# Kiểm baseline đã commit == sim server hiện tại (KHÔNG ghi file):
bash tools/combat-baseline/run.sh check
```

- `generate` xuất **cả file** về dạng chuẩn tắc: 2-space indent, LF, newline cuối; giữ nguyên
  `format_version/name/description/input`, chỉ tính lại `expected`. **Idempotent** (chạy lại = không đổi).
- `check` regenerate trong bộ nhớ rồi so **byte-for-byte** với file trên đĩa. Exit: `0` khớp / `1` drift /
  `2` lỗi tool. Đây là baseline drift guard trong CI (`.github/workflows/ci-server.yml` job `golden-vector`).

Thư mục vector = `shared/combat-vectors/` (tự định vị repo root bằng cách đi ngược lên tới khi thấy
`shared/combat-vectors` + `server`).

## Quy trình cập nhật baseline CÓ CHỦ ĐÍCH (bắt buộc)

Baseline **không được sửa âm thầm**. Khi thay đổi công thức combat sim một cách cố ý:

```
1. Sửa sim (server và/hoặc client).
2. Chạy golden tests → chúng ĐỎ (đúng: baseline cũ khác sim mới).
3. Xác nhận mismatch là CỐ Ý (không phải bug). Nếu không rõ nguyên nhân → STOP, điều tra;
   KHÔNG regenerate để che.
4. `run.sh generate` → ghi baseline mới từ sim server.
5. REVIEW DIFF từng vector (git diff shared/combat-vectors/*) — hiểu vì sao đổi.
6. Ghi lý do (WHY) trong PR.
7. doc-sync: cập nhật docs liên quan + row "Combat sim change".
8. Review agent `combat-determinism` (+ `reviewer`) trước khi merge.
```

Cấm tuyệt đối: đổi công thức → regenerate → merge mà không review diff + lý do; sửa vector để "làm CI xanh".

## Cấu trúc

- `GameTeam.CombatBaseline/` — core (`RepoPaths`, `VectorInputParser`, `BaselineTool`), ProjectReference Domain.
- `GameTeam.CombatBaseline.Cli/` — CLI mỏng (`combat-baseline generate|check`).
- `GameTeam.CombatBaseline.Tests/` — xUnit (generate→check clean, idempotent, phát hiện drift).
- `run.sh` — build Release (log ra stderr) + chạy CLI; entrypoint gate CI.

Liên quan: `shared/combat-vectors/README.md` (định dạng vector), `docs/gameplay/combat-framework.md` §22,
`docs/testing/backend-testing.md` §4.2, `.claude/agents/combat-determinism.md`.
