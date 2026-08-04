# 07 — Config Validator tool (CI gate)

> Mục đích: Xây `tools/config-validator` kiểm **schema + referential integrity** cho toàn bộ `config/**`, gắn làm **cổng CI bắt buộc** — config sai không bao giờ vào runtime.

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 1 Hợp đồng & Config | P1 | S1 | nền data-driven |

# Mục tiêu

Công cụ CLI nhận `config/**` + `shared/config-schema/**`, validate: (1) mỗi file khớp schema tương ứng, (2) tham chiếu chéo tồn tại (hero→skill, stage→reward, gacha→hero…), (3) `schema_version` hợp lệ. Fail → exit code ≠ 0, báo lỗi rõ vị trí.

# Lý do

Schema (phase 06) chỉ kiểm cấu trúc từng file; cần validator kiểm **liên kết chéo** và bật cổng CI (`validate-config.yml`, đã chừa hook ở phase 03). Đây là "CI fails on invalid config" theo ADR-005.

# Phụ thuộc

- **Trước:** 06 (schema), 03 (workflow hook).
- **Sau:** 21 (Config Service tái dùng validate khi publish bundle), mọi phase thêm config.

# Phạm vi

- CLI validator (ngôn ngữ: theo `tools/` hiện có — Python hoặc .NET tool; chọn khớp repo).
- Kiểm schema per-type + referential integrity + version.
- Tích hợp `validate-config.yml` thành gate bắt buộc.
- Thông báo lỗi rõ (file, path JSON, quy tắc vi phạm).

# Không thuộc phạm vi

- Publish bundle/versioning runtime (phase 21).
- Nạp config vào backend qua provider (phase 21).
- Giá trị balance.

# Deliverables

- `tools/config-validator` chạy được CLI + README dùng.
- Cổng `validate-config.yml` bật bắt buộc (không còn no-op).
- Bộ test validator (fixture hợp lệ + loạt fixture sai).
- Tài liệu mã lỗi & cách đọc báo cáo.

# Công việc cần thực hiện

- [ ] Chọn stack cho validator khớp `tools/` (ưu tiên tái dùng ngôn ngữ đã có); khởi tạo project + README.
- [ ] Nạp schema per-type, map file config → schema theo thư mục/tên.
- [ ] Validate schema từng file; gom lỗi (không dừng ở lỗi đầu).
- [ ] Xây bảng ID toàn cục; kiểm referential integrity (mọi ref trỏ tới ID tồn tại).
- [ ] Kiểm `schema_version` khớp schema hiện hành; cảnh báo version cũ cần migrate.
- [ ] Exit code ≠ 0 khi có lỗi; in báo cáo (file:path:rule).
- [ ] Cập nhật `validate-config.yml`: gọi validator bắt buộc, path filter `config/**`, `shared/config-schema/**`.
- [ ] Viết test: fixture hợp lệ pass; các fixture sai (thiếu ref, sai kiểu, version lạ) fail đúng loại.

# Tiêu chí hoàn thành

- Validator pass trên toàn `config/**` hiện có (kể cả rỗng/fixture).
- Mỗi loại lỗi (schema, ref, version) có test fail tương ứng.
- `validate-config.yml` đỏ khi config sai (đã thử negative rồi revert).
- Báo cáo lỗi đủ để sửa không cần đọc code validator.

# Cách kiểm tra

- Local: chạy validator trên `config/` → exit 0.
- Thả 1 config thiếu ref → validator exit ≠ 0, chỉ đúng file/ref.
- PR đụng `config/**` với lỗi → CI đỏ; sửa → xanh.
- `dotnet test`/`pytest` bộ test validator.

# Rủi ro

- **Hiệu năng khi config lớn** → nạp schema một lần, index ID O(1).
- **Ref vòng hoặc mơ hồ** → định nghĩa rõ loại ref hợp lệ; báo lỗi ref không xác định.
- **Trùng lặp logic với Config Service** → tách thư viện validate dùng chung nếu cùng ngôn ngữ (phase 21 tái dùng).

# Ghi chú

Bám [`../gameplay/configuration-and-data.md`](../gameplay/configuration-and-data.md) + ADR-005. Validator là **cùng logic** mà Config Service (phase 21) sẽ chạy khi publish bundle — cân nhắc chia sẻ code.

# Technical Debt Review

- **Maintainability:** báo lỗi rõ giảm chi phí sửa config.
- **Scalability:** index ID cho phép config lớn dần.
- **Testing:** fixture pass/fail là hợp đồng.
- **Security:** chặn dữ liệu dị dạng/độc hại vào pipeline.
- **Nợ:** chia sẻ code với Config Service (đánh giá ở phase 21).

# Phase Review

Đóng khi validator kiểm schema+ref+version, gate CI bắt buộc hoạt động (kèm negative test), có test đầy đủ.

---

## Liên kết
- [`../gameplay/configuration-and-data.md`](../gameplay/configuration-and-data.md) · [`../deployment/ci-cd-pipeline.md`](../deployment/ci-cd-pipeline.md)
- ADR: [`../adr/ADR-005-configuration-strategy.md`](../adr/ADR-005-configuration-strategy.md) · [`../adr/ADR-004-data-driven-design.md`](../adr/ADR-004-data-driven-design.md)
- Roadmap: [`README.md`](README.md) → kế: [`08-codegen-pipeline.md`](08-codegen-pipeline.md)
