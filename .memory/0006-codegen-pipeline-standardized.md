# 0006 — Codegen pipeline standardized (Phase 08)

- Date: 2026-08-12
- Scope: tooling
- Status: Active

## Decision

Model client (DTO/enum) được **sinh tự động, KHÔNG gõ tay**, từ hợp đồng duy nhất. ONE tool:
**`shared/codegen`** — **.NET 9 console**, **không gói ngoài** (chỉ `System.Text.Json`), solution/CPM riêng
(tách `server/` và `tools/config-validator/`), cấu trúc như Phase 07: **core lib** `GameTeam.Codegen`
(`OpenApiReader` giữ thứ tự khai báo; `GdTypeMapper` whitelist kiểu; `GdEmitter`; `CodegenRunner` ghi file +
dọn `.gd` stale) + **CLI mỏng** `codegen` + **xUnit tests**. Đọc **`shared/contracts/openapi.json`** → sinh
**GDScript** vào **`client/src/data/generated/`** (mỗi schema 1 file `.gd` snake_case): 6 enum dùng chung
(`class_name <Name>` + `enum {…}` **giữ đúng số C#**, kể cả `Rarity` có khoảng trống `0,3,4,5`) + 8 DTO nền
(`class_name <Name> extends Resource`, biến typed; ghi chú `## wire: <jsonKey>` cho parse Phase 15). Để enum
mang số qua spec, Phase 05 `ContractEnumsDocumentTransformer` **được mở rộng** phát `x-enum-varnames` +
`x-enum-values` (single-source, additive). Mỗi file có header `AUTO-GENERATED — DO NOT EDIT` + đường dẫn nguồn;
deterministic + idempotent (thứ tự cố định, LF, không timestamp). **GATE CI bắt buộc**:
`.github/workflows/codegen-check.yml` chạy `shared/codegen/run.sh` rồi `git diff --exit-code -- client/src/data/generated`
(generated lệch ⇒ FAIL); import Godot headless model do `ci-client.yml` (`--headless --import` trên `client/**`).

## Why

ADR-008 yêu cầu contract single-source + client codegen: tránh client gõ tay DTO rồi lệch khi server đổi. Sinh
từ `openapi.json` (đã có drift-guard ở `ci-server`) giữ **một nguồn**; đặt nền sớm để nhóm 3 (client core, Phase
14+) và NetworkClient (Phase 15) dùng model sinh ra. Chọn **.NET** để đồng nhất toolchain (server + Phase 07),
tái dùng xUnit/CI, `global.json` pin SDK. **Bỏ số enum khi serialize chuỗi** là rủi ro thật (OpenAPI string-enum
mất số): giải bằng enrich spec (`x-enum-values`) thay vì đọc chéo C# — giữ nguyên luật "input = OpenAPI".
Verified (SDK 9.0.306, Windows): **34 test codegen xanh** + **66 test server xanh** (Api 24 gồm test khoá
`x-enum-values` cho Rarity `0,3,4,5`); sinh 14 file; chạy 2 lần byte-identical (idempotent, LF, no CRLF);
**Godot 4.7 `--headless --import` exit 0, 0 lỗi, 14 class đăng ký** (`class_name Class` không đụng độ); drift test:
đổi `ProfileDto` (thêm field) → rebuild → regenerate → `profile_dto.gd` có `var drift_probe: int`, `git diff
--exit-code` đỏ → revert sạch (exit 0). File `.uid` do Godot import sinh ra được `.gitignore` (repo không track).

## Not this

- **Generator OpenAPI→GDScript có sẵn** (openapi-generator/NSwag): **không có target GDScript** đạt yêu cầu →
  viết template tối giản, deterministic (đúng "Rủi ro" Phase 08). Loại.
- **Python/Jinja hoặc Node**: thêm toolchain mới; đã loại ở Phase 06/07 (`.memory/0004`, `[[0005-config-validator-standardized]]`).
  Repo chuẩn .NET cho tool. Loại.
- **Ordinal enum từ mảng string OpenAPI** (RARITY.FIVE=3): sai số C# cho enum có khoảng trống → "renumber âm thầm".
  Chọn enrich `x-enum-values` để **giữ đúng số**.
- **Đọc trực tiếp `GameTeam.Contracts` C#** để lấy số enum: phá luật "nguồn = OpenAPI single-source". Loại — đưa số
  vào spec thay vì.
- **Output ở `shared/codegen/output/`** (git-ignored): drift check cần file **tracked** → output ở
  `client/src/data/generated/` (committed). `tools/codegen/` cũ chuyển thành con trỏ "MOVED → shared/codegen".
- **Commit `.tres`/model config-schema ở đây**: đó là họ model **khác** (config-driven Resource, `docs/godot/resources-and-assets.md`);
  Phase 08 chỉ sinh read-model **contract**.
- **Parse/mạng/round-trip**: là **Phase 15** — Phase 08 chỉ data model (không logic).

Liên quan: [[0003-shared-contracts-standardized]] (nguồn openapi.json), [[0005-config-validator-standardized]]
(khuôn tool .NET). Canonical how-to + bảng kiểu/giới hạn: `shared/codegen/README.md`. Bám ADR-008, ADR-002,
`docs/backend/api-and-versioning.md` §4.
