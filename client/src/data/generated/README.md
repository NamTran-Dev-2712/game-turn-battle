# `client/src/data/generated/` — Model client SINH TỰ ĐỘNG (Phase 08)

> ⚠️ **KHÔNG SỬA TAY.** Mọi file `*.gd` ở đây do `shared/codegen/` sinh ra từ hợp đồng
> `shared/contracts/openapi.json`. Sửa tay sẽ bị ghi đè ở lần regenerate kế tiếp.

| Mục | Nội dung |
|---|---|
| **Nguồn** | `shared/contracts/openapi.json` (sinh từ `server/GameTeam.Contracts` — nguồn DUY NHẤT, ADR-008). |
| **Sinh bởi** | `shared/codegen/` (chạy `bash shared/codegen/run.sh`). |
| **Nội dung** | Enum dùng chung (`Faction`/`Class`/`Element`/`Role`/`Rarity`/`Currency`) + DTO nền (read-model). |
| **Không chứa** | ❌ logic; ❌ mạng/parse (Phase 15); ❌ DTO client tự định nghĩa trùng. |

## Đổi contract?

1. Sửa DTO/enum trong `server/GameTeam.Contracts` → rebuild (regenerate `openapi.json`).
2. Chạy `bash shared/codegen/run.sh` → regenerate thư mục này.
3. Commit diff generated; CI `codegen-check` chặn nếu generated bị lệch (drift).

Chi tiết: [`../../../../shared/codegen/README.md`](../../../../shared/codegen/README.md).
