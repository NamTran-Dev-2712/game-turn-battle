# 08 — Codegen pipeline (Contracts → client models)

> Mục đích: Sinh **model client (GDScript/Resource)** từ nguồn contract duy nhất (`GameTeam.Contracts`/OpenAPI) để client và server không lệch DTO theo thời gian.

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 1 Hợp đồng & Config | P1 | S1 | nền data-driven |

# Mục tiêu

Pipeline `shared/codegen/` đọc OpenAPI/contract (phase 05) → sinh model client tương ứng (data class/Resource + enum) đặt trong `client/src/data/` (hoặc `client/src/shared/`), chạy được trong CI, idempotent.

# Lý do

ADR-008 yêu cầu contract single-source + client codegen. Sinh tự động tránh việc client gõ tay DTO rồi lệch khi server đổi. Đặt nền sớm để nhóm 3 (client core) dùng model sinh ra thay vì tự viết.

# Phụ thuộc

- **Trước:** 05 (contract/OpenAPI), 03 (client CI).
- **Sau:** 15 (NetworkClient dùng model), mọi feature client dùng DTO.

# Phạm vi

- Script/tool codegen trong `shared/codegen/` (chọn generator phù hợp OpenAPI → GDScript, hoặc template tự viết nếu không có generator sẵn).
- Sinh: DTO nền + enum dùng chung → GDScript.
- Kiểm "generated up-to-date" trong CI (sinh lại → không diff).
- Quy ước: file generated có header cảnh báo "DO NOT EDIT", nằm thư mục riêng.

# Không thuộc phạm vi

- Sinh code server (server là nguồn).
- Logic mạng/parse (phase 15).
- DTO feature (thêm khi feature xuất hiện, tự động qua pipeline).

# Deliverables

- `shared/codegen/` chạy được: contract → model client.
- Model client generated cho DTO nền + enum, có header "generated".
- CI step "codegen drift check" (sinh lại, fail nếu khác).
- README hướng dẫn chạy codegen + quy tắc không sửa tay.

# Công việc cần thực hiện

- [ ] Khảo sát generator OpenAPI→GDScript; nếu không đạt, viết template codegen tối giản (Jinja/Scriban) từ OpenAPI JSON.
- [ ] Cấu hình đầu vào = OpenAPI xuất ở phase 05; đầu ra = `client/src/data/generated/`.
- [ ] Sinh DTO nền + enum (map kiểu C#→GDScript, enum→const/enum GDScript).
- [ ] Thêm header "AUTO-GENERATED — DO NOT EDIT" + đường dẫn nguồn.
- [ ] Thêm CI step: chạy codegen → `git diff --exit-code` (drift check).
- [ ] Loại thư mục generated khỏi format/lint gây nhiễu (nếu cần).
- [ ] README `shared/codegen/`: cách chạy, khi nào chạy (khi contract đổi), quy tắc không sửa tay.

# Tiêu chí hoàn thành

- Chạy codegen sinh ra model client hợp lệ, Godot import không lỗi.
- CI drift check xanh; sửa contract → sinh lại → diff xuất hiện đúng.
- Model generated khớp enum/DTO của `GameTeam.Contracts`.
- File generated có header cảnh báo; nằm thư mục riêng.

# Cách kiểm tra

- Local: chạy codegen → `godot --headless --import` không lỗi trên model mới.
- Đổi 1 DTO trong Contracts → regenerate → diff hợp lý.
- CI: bỏ regenerate → drift check đỏ (thử rồi revert).

# Rủi ro

- **Không có generator OpenAPI→GDScript tốt** → template tự viết tối giản; giới hạn kiểu hỗ trợ, ghi rõ.
- **Generated bị sửa tay rồi mất khi regen** → header cảnh báo + review chặn sửa tay.
- **Kiểu phức tạp (nullable/nested) map sai** → test round-trip parse ở phase 15.

# Ghi chú

Đổi contract ⇒ **bắt buộc** regenerate (doc-sync row "Contract/DTO change"). Model generated là read-model cho client; client không tự định nghĩa DTO trùng.

# Technical Debt Review

- **Maintainability:** single-source contract, client không gõ tay DTO.
- **Scalability:** feature mới tự có model khi contract mở rộng.
- **Testing:** drift check + parse test (phase 15) bảo vệ tương thích.
- **Security:** không sinh code thực thi tuỳ tiện; chỉ data model.
- **Nợ:** hỗ trợ kiểu phức tạp mở rộng dần.

# Phase Review

Đóng khi codegen sinh model client từ contract, CI drift check hoạt động, model khớp enum/DTO nền, Godot import sạch.

---

## Liên kết
- [`../backend/api-and-versioning.md`](../backend/api-and-versioning.md) · [`../godot/resources-and-assets.md`](../godot/resources-and-assets.md) · [`../architecture/dependency-graph.md`](../architecture/dependency-graph.md)
- ADR: [`../adr/ADR-008-networking.md`](../adr/ADR-008-networking.md) · [`../adr/ADR-002-godot-architecture.md`](../adr/ADR-002-godot-architecture.md)
- Roadmap: [`README.md`](README.md) → kế: [`09-backend-domain-foundation.md`](09-backend-domain-foundation.md)
