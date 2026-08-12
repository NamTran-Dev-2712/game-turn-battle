# 0005 — Config validator standardized (Phase 07)

- Date: 2026-08-12
- Scope: tooling
- Status: Active

## Decision

Config correctness is enforced by ONE tool: **`tools/config-validator`** — a **.NET 9 console**
(`JsonSchema.Net`/json-everything, draft 2020-12; CPM per ADR-010; solution tách rời `server/GameTeam.sln`).
Cấu trúc: **core lib** `GameTeam.ConfigValidator` (`SchemaSet` nạp+đăng ký schema MỘT LẦN, memo theo thư mục;
`ConfigFileMapper` map thư mục SỐ NHIỀU→schema SỐ ÍT; `ConfigLoader`; `IdIndex` tra cứu O(1); `SchemaValidator`;
`ReferenceValidator`; `VersionValidator`; `ConfigValidationRunner`) + **CLI mỏng** + **xUnit tests**. Validate
`config/**` cho: (1) JSON Schema, (2) referential integrity chéo (hero→skill; stage→hero/reward/stage;
gacha→hero; shop→reward; quest→reward; `reward.entries[].ref_id` đa hình theo `reward_type`), (3) `schema_version`
(hỗ trợ = `1`). Lỗi được **gom** (không dừng ở lỗi đầu), in `file:jsonpath:CODE message` với mã ổn định
`JSON001`/`MAP001`/`SCH001`/`VER001`/`VER002`/`REF001`/`REF002` (exit `0`/`1`/`2`). **GATE CI bắt buộc**:
`.github/workflows/validate-config.yml` gọi `tools/config-validator/run.sh config shared/config-schema`
(setup-dotnet theo `global.json`; `run.sh` exec `100755`; path filter thêm `tools/config-validator/**`, `global.json`).

## Why

Schema (Phase 06) chỉ ràng buộc *cấu trúc + định dạng* ID từng file; **không** kiểm *tồn tại* ID chéo hay
`schema_version` khớp. ADR-004/005 yêu cầu "CI fails on invalid config" — cần validator biến kiểm-tra ad-hoc
(Phase 06 dùng `jsonschema` thủ công) thành **gate tái lập được, có test**. Chọn **.NET** để đồng nhất codebase
server, tái dùng xUnit/CI, và để **Config Service (Phase 21, cũng .NET) project-reference thẳng core** và gọi
`ConfigValidationRunner.Run(...)` trước khi publish bundle — tránh viết lại logic (rủi ro trùng lặp nêu ở Phase 07).
Verified (SDK 9.0.306, Windows): build 0 error, **45 test xanh**, config thật exit 0, negative test (REF001) đỏ rồi
revert sạch. `run.sh` giải `$id`/`$ref` cục bộ (không fetch mạng) qua registry của json-everything.

## Not this

- **Python `jsonschema` + pytag** (công cụ Phase 06 dùng để lấy bằng chứng): nhẹ hơn nhưng thêm **toolchain mới**
  (repo chưa có project Python) và Phase 21 (.NET) **không** tái dùng được code — reuse chỉ ở mức đặc tả. Loại.
- **Node/AJV**: đã bị loại từ Phase 06 (`.memory/0004`) — repo là .NET + Python.
- **Nhét gate vào `ci-server.yml`**: giữ gate ở workflow riêng `validate-config.yml` (path-filtered) như thiết kế
  Phase 03; job `config-validate` cũ ở `ci-server.yml` chuyển thành con trỏ "MOVED".
- **Kiểm tồn tại `ref_id` cho `reward_type` = fragment/item**: chưa có loại config tương ứng → chỉ kiểm định dạng
  (giới hạn có chủ đích, tránh phát minh quan hệ; README §Known limitations). **Migration**: chưa có (mọi schema v1);
  phiên bản lạ = `VER002`, KHÔNG migrate (thuộc quy trình khi có breaking change).

Liên quan: [[0004-config-schema-standardized]] (schema Phase 06). Canonical how-to + mã lỗi:
`tools/config-validator/README.md`. Bám `docs/gameplay/configuration-and-data.md`, ADR-004/005.
