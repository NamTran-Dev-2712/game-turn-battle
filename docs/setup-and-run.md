# Setup & Run Guide

> Hướng dẫn cài đặt, cấu hình và chạy dự án **game-team** — backend .NET 9 + client Godot 4.7 +
> hạ tầng Postgres/Redis. Mọi giá trị trong tài liệu này được rút trực tiếp từ mã nguồn
> (`global.json`, `deploy/compose/docker-compose.yml`, `appsettings.json`, `.github/workflows/`,
> `client/project.godot`). Runbook thao tác nhanh (verify Phase 17) xem thêm [`../setup-and-run.md`](../setup-and-run.md)
> ở gốc repo; chi tiết hạ tầng: [`deployment/README.md`](deployment/README.md).

---

## 1. Overview & Tech Stack

Mobile turn-battle RPG, **server-authoritative + deterministic**. Kiến trúc 3 khối:

- **`client/`** — Godot 4.7 (GDScript). Client chỉ hiển thị; không phải nơi quyết định kết quả/kinh tế.
- **`server/`** — .NET 9, Clean Architecture + CQRS (MediatR). Nguồn chân lý combat/economy.
- **`shared/`** — hợp đồng dùng chung (OpenAPI sinh từ code, JSON Schema config, codegen GDScript).

| Lớp | Công nghệ | Ghi chú |
|---|---|---|
| Client | **Godot 4.7** (GDScript) | `client/project.godot`; main scene `res://src/ui/app_root.tscn` |
| Client test | **gdUnit4 6.2.0** | Vendored tại `client/addons/gdUnit4/` — không cần cài thêm |
| Backend | **.NET 9** (C#), ASP.NET Core, MediatR, FluentValidation, Asp.Versioning | Solution `server/GameTeam.sln`; host `server/src/GameTeam.Api` |
| Database | **PostgreSQL 16** (`postgres:16-alpine`) | EF Core 9 + Npgsql; migration `Initial` seed `schema_metadata` version=1 |
| Cache | **Redis 7** (`redis:7-alpine`) | StackExchange.Redis; graceful degradation (Redis tắt ⇒ `/health` = `degraded`, vẫn 200) |
| Message broker | *(không có)* | Chưa dùng ở giai đoạn hiện tại |
| Contracts | OpenAPI first-party → `shared/contracts/openapi.json`; JSON Schema → `shared/config-schema/` | Client DTO **sinh tự động** vào `client/src/data/generated/` |
| Container | Docker + Docker Compose | `deploy/compose/docker-compose.yml`, `server/Dockerfile` (multi-stage) |

---

## 2. Prerequisites

| Công cụ | Phiên bản | Nguồn pin | Ghi chú |
|---|---|---|---|
| **.NET SDK** | **9.0.306** (`rollForward: latestFeature`) | [`global.json`](../global.json) | Bắt buộc cho backend |
| **Godot** | **4.7** | `client/project.godot` feature `"4.7"` + `GODOT_VERSION` trong CI | Local đã verify `4.7.1-stable` |
| gdUnit4 | 6.2.0 | `GDUNIT4_VERSION` trong CI | Đã commit sẵn — bỏ qua nếu chỉ chạy game |
| **Docker Desktop** | mới nhất | — | Postgres + Redis local; phải chạy *trước* `scripts/dev/up` |
| Git | 2.30+ | — | Hooks: `git config core.hooksPath .githooks` |

Kiểm tra nhanh:

```bash
dotnet --version      # → 9.0.306 (hoặc feature mới hơn của 9.0.3xx)
docker info           # daemon phải chạy
```

---

## 3. Environment Configuration

Biến môi trường local nằm ở **`.env`** (git-ignored) — copy từ **`.env.example`**:

```bash
cp .env.example .env
```

Các biến do Compose đọc (mặc định trong `docker-compose.yml`):

| Biến | Mặc định | Ý nghĩa |
|---|---|---|
| `POSTGRES_USER` | `gameteam` | User Postgres |
| `POSTGRES_PASSWORD` | `devpassword` | Mật khẩu dev (**không phải secret thật**) |
| `POSTGRES_DB` | `gameteam` | Tên database |
| `POSTGRES_PORT` | `5432` | Cổng Postgres map ra host |
| `REDIS_PORT` | `6379` | Cổng Redis map ra host |
| `API_PORT` | `8080` | Cổng API map ra host (khi chạy profile `api`) |
| `ASPNETCORE_ENVIRONMENT` | `Development` | Môi trường ASP.NET Core |

**Connection string** (backend đọc từ `appsettings.json`, override bằng env dạng `__`):

| Config key | Env override | Mặc định (appsettings) |
|---|---|---|
| `ConnectionStrings:Postgres` | `ConnectionStrings__Postgres` | `Host=localhost;Port=5432;Database=gameteam;Username=gameteam;Password=devpassword` |
| `ConnectionStrings:Redis` | `ConnectionStrings__Redis` | `localhost:6379` |

> Trong container, Compose tự set các key này trỏ tới service `postgres`/`redis` — không cần chỉnh tay.

**Client** đọc base URL từ biến môi trường **`GAME_TEAM_API_BASE_URL`** (mặc định `http://localhost:8080`).
Đặt biến này trước khi chạy client nếu API ở cổng khác.

---

## 4. Installation & Running

### Option 1 — Local (Development mode)

Chạy hạ tầng bằng Docker, còn API + client chạy trực tiếp trên host.

```bash
# 1) Hạ tầng: Postgres + Redis (chờ healthy rồi in trạng thái)
scripts/dev/up.sh                 # Windows: scripts\dev\up.ps1

# 2) Backend: restore + build + test
dotnet build server/GameTeam.sln
dotnet test  server/GameTeam.sln

# 3) Chạy API (Kestrel)
#    Mặc định .NET 9 bind http://localhost:5000. Để khớp default của client (8080),
#    ép cổng bằng ASPNETCORE_URLS:
ASPNETCORE_URLS=http://localhost:8080 dotnet run --project server/src/GameTeam.Api
```

```powershell
# PowerShell (Windows) tương đương bước 3:
$env:ASPNETCORE_URLS = "http://localhost:8080"
dotnet run --project server/src/GameTeam.Api
```

**Client (Godot):**

1. Mở `client/project.godot` bằng Godot 4.7 (lần đầu Godot import asset).
2. Bấm **F5** để chạy. Nếu API không ở `http://localhost:8080`, set `GAME_TEAM_API_BASE_URL` trước khi mở Godot.

Dừng hạ tầng:

```bash
scripts/dev/down.sh               # giữ dữ liệu (volume pgdata)
scripts/dev/down.sh -v            # xoá luôn volume DB   (PowerShell: down.ps1 -Volumes)
```

### Option 2 — Docker / Docker Compose

Mặc định Compose chỉ dựng **db + redis** (API để trong profile `api`). Bật cả API:

```bash
# Cách gọn: script bật profile api + chờ /health
scripts/dev/up.sh --api           # Windows: scripts\dev\up.ps1 -Api

# Hoặc gọi compose trực tiếp:
docker compose -f deploy/compose/docker-compose.yml --profile api up -d --build
```

- API build từ [`server/Dockerfile`](../server/Dockerfile) (multi-stage, `EXPOSE 8080`, `ASPNETCORE_URLS=http://+:8080`).
- Chỉ hạ tầng (không API): bỏ `--profile api` / `--api`.
- Dừng: `docker compose -f deploy/compose/docker-compose.yml --profile api down` (thêm `-v` để xoá volume).

---

## 5. Verification & Preview

Với API chạy ở `http://localhost:8080`:

| Mục đích | URL / Endpoint | Kỳ vọng |
|---|---|---|
| **Health check** | `GET /health` | `{"status":"ok"}` — hoặc `"degraded"` khi Redis tắt (**vẫn HTTP 200**) |
| Ping mẫu | `GET /api/v1/ping` | 200 |
| Server time | `GET /api/v1/server-time` | 200, `utc_now` |
| **OpenAPI JSON** | `GET /openapi/v1.json` | Spec (single source cho codegen) |
| **Swagger UI** | `GET /swagger` | UI (chỉ bật ở môi trường Development) |
| Client | Godot F5 → main scene | Boot → `/health` OK → config (best-effort) → **main hub** |

Kiểm nhanh:

```bash
curl http://localhost:8080/health
# {"status":"ok"}
```

| Cổng mặc định | Dịch vụ |
|---|---|
| `8080` | API (host `dotnet run` cần `ASPNETCORE_URLS`; container map từ `API_PORT`) |
| `5432` | PostgreSQL |
| `6379` | Redis |

**Seed / tài khoản test:** migration `Initial` seed hàng neo `schema_metadata` (version=1) — không có
bảng nghiệp vụ hay tài khoản người dùng mẫu ở giai đoạn này (đăng nhập/auth là phase sau). Thư mục
`config/` hiện chỉ chứa README (chưa có dữ liệu config game). Áp migration lên DB dev:

```bash
dotnet ef database update --project server/src/GameTeam.Infrastructure \
  --startup-project server/src/GameTeam.Api
```

---

## 6. Troubleshooting

| Triệu chứng | Nguyên nhân & cách xử lý |
|---|---|
| **Sai SDK .NET** (`error NETSDK...` / version mismatch) | Cài .NET **9.0.306** (xem `global.json`, `rollForward: latestFeature`). |
| **Docker chưa chạy** (`Docker khong san sang` / `Cannot connect to the Docker daemon`) | Mở Docker Desktop **trước** khi chạy `scripts/dev/up`. |
| **Xung đột cổng** (5432 / 6379 / 8080 đang bận) | Đổi `POSTGRES_PORT` / `REDIS_PORT` / `API_PORT` trong `.env`. Nếu đổi cổng API, set `GAME_TEAM_API_BASE_URL` cho client. |
| **Client luôn ra màn lỗi boot** | API chưa chạy hoặc sai cổng. Kiểm `GET /health` → 200; xác nhận `GAME_TEAM_API_BASE_URL` khớp cổng API. |
| **Gọi API ở host không thấy cổng 8080** | `dotnet run` không có `launchSettings` → Kestrel mặc định `:5000`. Ép bằng `ASPNETCORE_URLS=http://localhost:8080`. |
| **Thiếu migration / bảng chưa tạo** | Chạy `dotnet ef database update` (mục 5). Đảm bảo Postgres đã healthy (`docker compose ... ps`). |
| **Container `unhealthy` / `up` treo** | `docker compose -f deploy/compose/docker-compose.yml ps` + `logs <service>` để xem lỗi healthcheck. |
| **Godot version không khớp** | Dùng đúng Godot **4.7** (khớp feature `"4.7"` trong `project.godot` và CI). |
| **`Headless mode is not supported!` khi chạy gdUnit4** | Thêm `--ignoreHeadlessMode` (Windows) hoặc chạy qua `xvfb-run` (Linux) — xem [`../setup-and-run.md`](../setup-and-run.md) §6. |
