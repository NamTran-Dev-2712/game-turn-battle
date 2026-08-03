# `tools/config-validator/` — Validate config (TODO stub)

| Mục | Nội dung |
|---|---|
| **Purpose** | Validate mọi file `config/**` theo `shared/config-schema/**` + kiểm tra **referential integrity** (ref hero↔skill↔stage…). |
| **Responsibilities** | Chạy ở CI (`validate-config.yml`) làm **gate merge**; fail nếu config sai (ADR-005). |
| **Allowed** | Mã validator + test của nó. |
| **Not allowed** | ❌ sửa dữ liệu config; chỉ đọc/validate. |
| **Dependencies** | `shared/config-schema`, `config/`. |
| **Owner** | Platform/content-tools. |
| **Future expansion** | Kiểm tra version/migration, cảnh báo cân bằng. |

## TODO (chưa hiện thực ở bootstrap)
- [ ] Chọn runtime tool (khuyến nghị .NET console tái dùng codebase server, hoặc script).
- [ ] Load & validate JSON Schema (draft 2020-12).
- [ ] Kiểm tra referential integrity giữa các loại config.
- [ ] Exit code khác 0 khi lỗi (gate CI).

Chi tiết: `../../docs/testing/README.md` §4, `../../docs/adr/ADR-005-configuration-strategy.md`.
