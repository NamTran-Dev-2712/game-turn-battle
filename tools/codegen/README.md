# `tools/codegen/` — ĐÃ CHUYỂN sang `shared/codegen/` (Phase 08)

> Pipeline codegen client (OpenAPI → GDScript) được hiện thực tại **`shared/codegen/`** theo đúng
> `docs/roadmap/08-codegen-pipeline.md` (§Mục tiêu: pipeline nằm ở `shared/codegen/`). Thư mục này
> **không còn là nơi chạy generator** — giữ lại chỉ để tương thích tham chiếu cũ.

| Mục | Nội dung |
|---|---|
| **Vị trí thật** | [`../../shared/codegen/`](../../shared/codegen/README.md) (bộ chạy .NET + template + `run.sh`). |
| **Nguồn** | `shared/contracts/openapi.json` (sinh từ `server/GameTeam.Contracts`). |
| **Output** | `client/src/data/generated/` (committed — dùng cho drift check). |
| **CI gate** | `.github/workflows/codegen-check.yml` (regenerate → `git diff --exit-code`). |

Xem hướng dẫn đầy đủ: [`../../shared/codegen/README.md`](../../shared/codegen/README.md).
