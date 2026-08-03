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

> **Bootstrap:** mỗi tool hiện là **README + TODO stub** — hiện thực thật ở phase Core Framework (config-validator ưu tiên, cần cho CI `validate-config.yml`).

## Thư mục con
`config-validator/` · `codegen/` · `content-importer/`
