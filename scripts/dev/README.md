# `scripts/dev/` — Chạy môi trường local

Script đa nền tảng, idempotent, chờ dịch vụ **healthy** trước khi báo xong (phase 04).

| Script | Việc | Cờ |
|---|---|---|
| `up.ps1` / `up.sh` | Khởi động Postgres + Redis (mặc định); chờ healthy; in trạng thái. | `-Api` (ps1) / `--api` hoặc `--profile api` (sh) → kèm API, chờ `/health`. |
| `down.ps1` / `down.sh` | Dừng & dọn stack; **giữ** volume dữ liệu. | `-Volumes` (ps1) / `-v` (sh) → xoá luôn volume (mất dữ liệu DB dev). |

**Ví dụ:**
```bash
bash scripts/dev/up.sh            # Postgres + Redis
bash scripts/dev/up.sh --api      # + API, chờ GET /health = 200
bash scripts/dev/down.sh          # dừng, giữ dữ liệu
bash scripts/dev/down.sh -v       # dừng + xoá volume
```
```powershell
pwsh scripts\dev\up.ps1
pwsh scripts\dev\up.ps1 -Api
pwsh scripts\dev\down.ps1
pwsh scripts\dev\down.ps1 -Volumes
```

**Chạy server trên host (không container):** `dotnet run --project ../../server/src/GameTeam.Api`
**Mở client:** mở `../../client/project.godot` bằng Godot 4.x.

> Cần Docker Desktop. Biến môi trường lấy từ `../../.env` (copy từ `../../.env.example`).
> Chi tiết + troubleshooting: [`../../docs/deployment/README.md`](../../docs/deployment/README.md) → **Local development**.
