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

- [x] Chọn stack cho validator khớp `tools/` (ưu tiên tái dùng ngôn ngữ đã có); khởi tạo project + README.
      → **.NET 9 console** (`JsonSchema.Net`), khớp codebase server + CPM (ADR-010); `tools/config-validator/ConfigValidator.sln` (core lib + CLI + test) + README dùng đầy đủ.
- [x] Nạp schema per-type, map file config → schema theo thư mục/tên.
      → `SchemaSet` nạp + đăng ký MỌI schema MỘT LẦN (memo theo thư mục); `ConfigFileMapper` map thư mục SỐ NHIỀU (`heroes/`) → schema SỐ ÍT (`hero.schema.json`).
- [x] Validate schema từng file; gom lỗi (không dừng ở lỗi đầu).
      → `SchemaValidator` (draft 2020-12, `OutputFormat.List`) gom mọi vi phạm → `SCH001`; test `Multiple_distinct_errors_are_all_collected_*` chứng minh không dừng ở lỗi đầu.
- [x] Xây bảng ID toàn cục; kiểm referential integrity (mọi ref trỏ tới ID tồn tại).
      → `IdIndex` (Dictionary theo loại, tra cứu O(1)); `ReferenceValidator` theo đồ thị cố định (hero→skill, stage→hero/reward/stage, gacha→hero, shop→reward, quest→reward, reward.ref_id đa hình) → `REF001`/`REF002`.
- [x] Kiểm `schema_version` khớp schema hiện hành; cảnh báo version cũ cần migrate.
      → `VersionValidator`: `VER001` (thiếu/không phải số nguyên), `VER002` (không được hỗ trợ). Chưa có migration nào (mọi schema ở v1) → phiên bản lạ là lỗi, KHÔNG phát minh migrate (ghi rõ giới hạn ở README + `_versions/`).
- [x] Exit code ≠ 0 khi có lỗi; in báo cáo (file:path:rule).
      → CLI exit `0`/`1`/`2`; report `file:jsonpath:CODE message` (gom + sắp xếp xác định).
- [x] Cập nhật `validate-config.yml`: gọi validator bắt buộc, path filter `config/**`, `shared/config-schema/**`.
      → GATE bắt buộc (setup-dotnet theo `global.json` + cache NuGet + `run.sh`); path filter thêm `tools/config-validator/**`, `global.json`; run.sh có exec bit (mode `100755`). *(CI runner đỏ thật = CI-verification pending trên PR.)*
- [x] Viết test: fixture hợp lệ pass; các fixture sai (thiếu ref, sai kiểu, version lạ) fail đúng loại.
      → 45 test xanh: valid tree pass; missing-ref→REF001; invalid-ref-type→REF002; bad/missing version→VER002/VER001; schema-invalid→SCH001; MAP001/JSON001; + tái dùng fixture Phase 06 (`fixtures/*.valid|invalid.json`).

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

**PASS (local) — đủ điều kiện đóng, chờ CI xanh/đỏ thật trên PR.**

Bằng chứng xác minh (chạy thật, .NET SDK `9.0.306`, Windows):

- **Build:** `dotnet build tools/config-validator/ConfigValidator.sln -c Release` → **0 Warning, 0 Error**.
- **Test:** `dotnet test tools/config-validator/ConfigValidator.sln -c Release` → **45 pass / 0 fail** (bao gồm:
  valid tree pass; `REF001`/`REF002`; `VER001`/`VER002`; `SCH001` ở đúng leaf path; `MAP001`; `JSON001`;
  gom-nhiều-lỗi không dừng ở lỗi đầu; report actionable; mapping; IdIndex; và tái dùng 16 fixture Phase 06).
- **Config thật:** `bash tools/config-validator/run.sh config shared/config-schema` → **exit 0**
  (`config/**` hiện chỉ có README stub → 0 file, pass đúng theo tiêu chí "kể cả rỗng").
- **Negative test (đã revert):** thả `config/heroes/_negtest_broken.json` (hero tham chiếu skill không tồn tại)
  → validator **exit 1** với `config/heroes/_negtest_broken.json:/skills/0:REF001 …`; **xoá file** → **exit 0**;
  `git status` sạch (không còn artifact). Đây là bản mô phỏng local đúng bước GATE của `validate-config.yml`.

**Còn lại (CI-verification pending):** kết quả **đỏ thật trên GitHub Actions** khi PR chứa config sai chỉ xác nhận
được sau khi Actions chạy trên PR (runner `ubuntu-latest`, setup-dotnet theo `global.json`). Logic gate đã
mô phỏng local như trên; entrypoint `run.sh` có exec bit (`100755`) nên bước `[ -x ]` sẽ chạy GATE thật.

**Phạm vi (KHÔNG làm ở phase này):** Config Service, publish bundle, runtime config loading, migration
execution, giá trị balance — thuộc Phase 21 / phase gameplay. Core validate được tách sạch khỏi CLI để Phase 21
**project-reference** và gọi `ConfigValidationRunner.Run(...)` (xem README §Phase 21 reuse + `.memory/0005`).

---

## Liên kết
- [`../gameplay/configuration-and-data.md`](../gameplay/configuration-and-data.md) · [`../deployment/ci-cd-pipeline.md`](../deployment/ci-cd-pipeline.md)
- ADR: [`../adr/ADR-005-configuration-strategy.md`](../adr/ADR-005-configuration-strategy.md) · [`../adr/ADR-004-data-driven-design.md`](../adr/ADR-004-data-driven-design.md)
- Roadmap: [`README.md`](README.md) → kế: [`08-codegen-pipeline.md`](08-codegen-pipeline.md)
