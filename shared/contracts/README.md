# `shared/contracts/` — Hợp đồng API

| Mục | Nội dung |
|---|---|
| **Purpose** | Đặc tả API (OpenAPI) + enum/hằng dùng chung → client & server không lệch (ADR-008). |
| **Responsibilities** | Định nghĩa request/response/versioned endpoint; là nguồn cho `../codegen/`. |
| **Allowed** | `.yaml`/`.json` OpenAPI, file enum/hằng. |
| **Not allowed** | ❌ logic; ❌ DTO chỉ dùng nội bộ server (đặt ở `GameTeam.Contracts`). |
| **Dependencies** | Sinh từ / đồng bộ với `server/GameTeam.Contracts`. |
| **Owner** | Platform team. |
| **Future expansion** | `/api/vN` versioning (ADR-008). |

> **Bootstrap:** hiện là placeholder. Đặc tả thật thêm ở phase Core Framework (S1). Xem `../../docs/backend/api-and-versioning.md`.
