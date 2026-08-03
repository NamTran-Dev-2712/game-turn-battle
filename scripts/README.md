# `scripts/` — Tự động hoá thao tác

> Script chuẩn hoá thao tác lặp (giảm lỗi người, hợp AI). Nhóm theo mục đích: `dev/`, `db/`, `ci/`.

| Mục | Nội dung |
|---|---|
| **Purpose** | Chứa script chạy local/CI: khởi động dev stack, migration/seed db, helper pipeline. |
| **Responsibilities** | Bọc lệnh phức tạp thành thao tác đơn giản, tài liệu hoá. |
| **Allowed** | `.ps1` (Windows) / `.sh` (bash) mỏng, gọi tool có sẵn. |
| **Not allowed** | ❌ logic gameplay; ❌ secret cứng (đọc từ `.env`/CI secrets). |
| **Dependencies** | Docker, dotnet, Godot CLI. |
| **Owner** | Platform/dev-experience team. |
| **Future expansion** | Thêm script build/export/release. |

## Thư mục con
- `dev/` — chạy local (client, server, db).
- `db/` — migration, seed.
- `ci/` — helper cho GitHub Actions.

> **Bootstrap:** stub mỏng minh hoạ; mở rộng khi có solution/DB thật.
