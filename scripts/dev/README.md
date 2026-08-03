# `scripts/dev/` — Chạy môi trường local

| Script | Việc |
|---|---|
| `up.ps1` / `up.sh` | Khởi động Postgres + Redis + API bằng `deploy/compose/docker-compose.yml`. |
| `down.ps1` / `down.sh` | Dừng & dọn stack local. |

**Chạy server (dev):** `dotnet run --project ../../server/src/GameTeam.Api`
**Mở client:** mở `../../client/project.godot` bằng Godot 4.x.

> Cần Docker Desktop. Biến môi trường lấy từ `../../.env` (copy từ `../../.env.example`).
