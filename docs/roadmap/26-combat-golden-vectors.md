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

- [ ] Sinh bộ vector đa kịch bản (đội/skill/crit/miss/biên thắng-thua-hoà), lưu chung `shared/combat-vectors/` (hoặc `tools/`).
- [ ] Sinh baseline output từ sim server (nguồn chân lý) — commit làm "đáp án".
- [ ] Test server: chạy mọi vector → so baseline byte-đồng nhất.
- [ ] Test client (gdUnit4): chạy mọi vector → so baseline.
- [ ] CI: bật gate `golden-vector` (server-CI job + client-CI job, hoặc workflow tổng), fail khi lệch.
- [ ] Viết quy trình cập nhật baseline có chủ đích (đổi công thức → regenerate + review + ghi lý do), gắn doc-sync row "Combat sim change".
- [ ] Thử negative: đổi nhẹ công thức một phía → CI đỏ → revert.
- [ ] Cập nhật [`../gameplay/combat-framework.md`](../gameplay/combat-framework.md) + [`../testing/backend-testing.md`](../testing/backend-testing.md) + [`../testing/godot-testing.md`](../testing/godot-testing.md).

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

---

## Liên kết
- [`../gameplay/combat-framework.md`](../gameplay/combat-framework.md) · [`../testing/backend-testing.md`](../testing/backend-testing.md) · [`../testing/godot-testing.md`](../testing/godot-testing.md)
- ADR: [`../adr/ADR-011-combat-authority-and-determinism.md`](../adr/ADR-011-combat-authority-and-determinism.md)
- Roadmap: [`README.md`](README.md) → kế: [`27-hero-system.md`](27-hero-system.md)
