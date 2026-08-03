# `config/_versions/` — Metadata phiên bản config

| Mục | Nội dung |
|---|---|
| **Purpose** | Theo dõi phiên bản bundle config bất biến (`config@vN`) + `schema_version` (ADR-005). |
| **Responsibilities** | Ghi metadata mỗi lần publish bundle; hỗ trợ migration an toàn khi cập nhật live. |
| **Allowed** | File metadata JSON (theo `../../shared/config-schema/config-bundle.schema.json`). |
| **Not allowed** | ❌ dữ liệu gameplay. |
| **Dependencies** | Pipeline publish + Configuration Service. |
| **Owner** | Platform/LiveOps. |
| **Future expansion** | Lịch sử version, rollback bundle. |

Nguồn: `../../docs/mvp/08-*` (TE4), `../../docs/adr/ADR-005-configuration-strategy.md`. **Bootstrap:** chưa có bundle.
