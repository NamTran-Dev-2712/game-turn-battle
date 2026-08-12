# `tools/` — Công cụ nội bộ dev

> Công cụ hỗ trợ phát triển (không vào runtime game): validate config, sinh mã, import content.

| Mục | Nội dung |
|---|---|
| **Purpose** | Chứa tool nội bộ dùng ở dev/CI. |
| **Responsibilities** | `config-validator` chặn config sai; `codegen` đồng bộ hợp đồng; `content-importer` nhập bảng → JSON. |
| **Allowed** | Mã tool (script/CLI). |
| **Not allowed** | ❌ logic gameplay/runtime; ❌ secret. |
| **Dependencies** | `shared/config-schema`, `shared/contracts`, `config/`. |
| **Owner** | Platform/tooling team. |
| **Future expansion** | Thêm tool build/analyze. |

> **Trạng thái:** `config-validator` đã **hiện thực (Phase 07)** — .NET 9 CLI + core lib tái dùng, là **GATE
> CI bắt buộc** ở `validate-config.yml` (schema + referential integrity + `schema_version`); xem
> `config-validator/README.md`. `codegen` đã **hiện thực (Phase 08) ở `../shared/codegen/`** (OpenAPI → GDScript,
> GATE `codegen-check.yml`); thư mục `tools/codegen/` chỉ còn con trỏ "MOVED". `content-importer` (Post-MVP) vẫn là
> **README + TODO stub**.

## Thư mục con
`config-validator/` · `codegen/` · `content-importer/`
