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

- [x] Khảo sát generator OpenAPI→GDScript; nếu không đạt, viết template codegen tối giản từ OpenAPI JSON. → Không có generator OpenAPI→GDScript đạt yêu cầu (openapi-generator/NSwag không có target GDScript) → **tự viết** template .NET 9 tối giản, deterministic, không gói ngoài (`shared/codegen`, `System.Text.Json`). Ghi rõ ở `.memory/0006` §Not this.
- [x] Cấu hình đầu vào = OpenAPI xuất ở phase 05; đầu ra = `client/src/data/generated/`. → CLI `codegen [openapi-path] [output-dir]` mặc định `shared/contracts/openapi.json` → `client/src/data/generated/`.
- [x] Sinh DTO nền + enum (map kiểu C#→GDScript, enum→enum GDScript). → 6 enum + 8 DTO. Enum `class_name <Name>` + `enum {…}` **giữ đúng số C#** (`Rarity` 0,3,4,5) nhờ enrich spec `x-enum-values` (`ContractEnumsDocumentTransformer`). DTO `class_name <Name> extends Resource`, biến typed; bảng kiểu ở `shared/codegen/README.md`. Cấu trúc chưa hỗ trợ ⇒ fail rõ (`schema:property:reason`).
- [x] Thêm header "AUTO-GENERATED — DO NOT EDIT" + đường dẫn nguồn. → Mọi file mở đầu `# AUTO-GENERATED — DO NOT EDIT.` + `# Source: shared/contracts/openapi.json (schema: <Name>)`.
- [x] Thêm CI step: chạy codegen → `git diff --exit-code` (drift check). → `.github/workflows/codegen-check.yml` (regenerate → `git diff --exit-code -- client/src/data/generated`). **CI-verification pending** (chờ Actions xanh trên PR); logic đã chứng minh local (drift test đỏ→revert xanh).
- [x] Loại thư mục generated khỏi format/lint gây nhiễu (nếu cần). → gdformat/gdlint đang **hoãn** (enforcement-map §4) → chưa có formatter chạy để loại; generator phát LF + newline cuối + không trailing whitespace (khớp hook pre-commit hiện có). `.gitattributes`: `client/src/data/generated/** linguist-generated`. `.uid` do Godot import sinh được `.gitignore`.
- [x] README `shared/codegen/`: cách chạy, khi nào chạy (khi contract đổi), quy tắc không sửa tay. → `shared/codegen/README.md` (tiếng Việt, đầy đủ: input/output/chạy/khi nào/CI/bảng kiểu/giới hạn/mở rộng).

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

**Đủ điều kiện đóng** (local PASS 2026-08-12, SDK 9.0.306 + Godot 4.7-stable, Windows):

- **Codegen chạy được:** `shared/codegen` (.NET 9, không gói ngoài) đọc `shared/contracts/openapi.json` → sinh 14 file
  GDScript vào `client/src/data/generated/` (6 enum + 8 DTO). CLI/`run.sh` theo khuôn Phase 07 (core lib + CLI mỏng + xUnit).
- **Model khớp contract:** enum giữ đúng số `GameTeam.Contracts` (`Rarity` = 0,3,4,5 qua `x-enum-values`); DTO map kiểu đúng
  (String/int/float/bool/Array/`$ref`/nullable), field snake_case + `## wire:` cho parse Phase 15.
- **Header + tách thư mục:** mọi file có `AUTO-GENERATED — DO NOT EDIT` + nguồn; nằm riêng `client/src/data/generated/`.
- **Deterministic/idempotent:** chạy 2 lần byte-identical (LF, không CRLF, 1 newline cuối) — test + `git diff` xác nhận.
- **Godot import sạch:** `godot --headless --import --path client` exit 0, **0 lỗi**, 14 class đăng ký (kể cả `class_name Class`).
- **Drift check:** đổi `ProfileDto` → rebuild → regenerate → `profile_dto.gd` có field mới, `git diff --exit-code` **đỏ**;
  revert → **xanh**. Test tự động: **34 codegen** + **66 server** (Api 24 gồm khoá `x-enum-values`).
- **Test/CI xanh; doc-sync xong** (CLAUDE.md §4.6, doc-sync matrix, bootstrap-audit, api-and-versioning, resources-and-assets,
  ci-cd-pipeline, `.instructions/client.md`, `.memory/0006`); không TODO/blocker.

**CI-verification pending:** kết quả Actions của `codegen-check.yml` + `ci-client.yml` trên PR (gate CI-only, §4.5) — logic đã
chứng minh local; đánh dấu xanh khi Actions xanh.

---

## Liên kết
- [`../backend/api-and-versioning.md`](../backend/api-and-versioning.md) · [`../godot/resources-and-assets.md`](../godot/resources-and-assets.md) · [`../architecture/dependency-graph.md`](../architecture/dependency-graph.md)
- ADR: [`../adr/ADR-008-networking.md`](../adr/ADR-008-networking.md) · [`../adr/ADR-002-godot-architecture.md`](../adr/ADR-002-godot-architecture.md)
- Roadmap: [`README.md`](README.md) → kế: [`09-backend-domain-foundation.md`](09-backend-domain-foundation.md)
