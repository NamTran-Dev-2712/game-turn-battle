# Cài đặt môi trường (Setup)

## Yêu cầu
| Công cụ | Phiên bản | Ghi chú |
|---|---|---|
| .NET SDK | **9.0** (pin trong [global.json](global.json)) | Backend |
| Godot | **4.x** (khớp `GODOT_VERSION` trong CI) | Client, mở `client/project.godot` |
| Docker Desktop | mới nhất | Postgres + Redis local |
| Git | 2.30+ | Bật hooks: `git config core.hooksPath .githooks` |

## Các bước
```bash
# 1) Clone & vào thư mục
git clone <repo-url> && cd game-team

# 2) Biến môi trường local
cp .env.example .env          # chỉnh nếu cần (giá trị dev, KHÔNG phải secret thật)

# 3) Backend
dotnet build server/GameTeam.sln
dotnet test  server/GameTeam.sln

# 4) Hạ tầng local (Postgres + Redis)
scripts/dev/up.sh             # Windows: scripts\dev\up.ps1
#   dừng: scripts/dev/down.sh

# 5) Chạy API
dotnet run --project server/src/GameTeam.Api    # GET /health -> {"status":"ok"}

# 6) Client: mở client/project.godot bằng Godot 4.x
```

## Xác minh nhanh
- `dotnet test server/GameTeam.sln` → tất cả xanh.
- `/health` trả `200 OK`.
- Godot mở project không lỗi.

## Sự cố thường gặp
| Triệu chứng | Cách xử lý |
|---|---|
| Sai SDK .NET | Cài .NET 9; `global.json` sẽ pin |
| Docker chưa chạy | Mở Docker Desktop trước `scripts/dev/up` |
| Cổng bận (5432/6379/8080) | Sửa cổng trong `.env` |

Chi tiết môi trường & CI: [docs/deployment/README.md](docs/deployment/README.md).
