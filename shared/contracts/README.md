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

> **Phase 05 (đã chốt):** `openapi.json` được **sinh tự động từ `server/GameTeam.Contracts`** lúc build
> (.NET 9 OpenAPI + `Microsoft.Extensions.ApiDescription.Server`). Đây là **nguồn duy nhất** cho codegen
> client (Phase 08) — **KHÔNG sửa tay**. Đổi contract ⇒ sửa DTO/enum trong `GameTeam.Contracts`, rebuild để
> regenerate, chạy doc-sync. CI `ci-server` có bước *OpenAPI drift guard* chặn spec commit bị lệch code.
> Quy ước versioning + error envelope: `../../docs/backend/api-and-versioning.md`.
