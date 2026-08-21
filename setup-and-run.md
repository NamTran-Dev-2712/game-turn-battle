# Setup & Run — Hướng dẫn cài đặt và chạy dự án

> Runbook tuyến tính để cài đặt, chạy backend + client, chạy test, và **xác minh Phase 17** (boot →
> health → config → main hub; server tắt → màn lỗi + retry). Dựa trên repo thực tế. Chi tiết môi trường
> hạ tầng: [`SETUP.md`](SETUP.md) + [`docs/deployment/README.md`](docs/deployment/README.md) (Local development).
> Nếu một giá trị **không** xác định được từ repo, mục đó ghi rõ `TODO/VERIFY` — không đoán.

---

## 1. Yêu cầu (Prerequisites)

| Công cụ | Phiên bản | Ghi chú |
|---|---|---|
| OS | Windows / macOS / Linux | Repo phát triển trên Windows 11 (PowerShell) + Git Bash; scripts có cả `.ps1` và `.sh` |
| .NET SDK | **9.0.306** (pin ở [`global.json`](global.json), `rollForward: latestFeature`) | Backend |
| Godot | **4.7** (khớp `GODOT_VERSION` trong [`.github/workflows/ci-client.yml`](.github/workflows/ci-client.yml); local đã verify 4.7.1-stable) | Client — mở `client/project.godot` |
| gdUnit4 | **6.2.0** (vendored, đã commit tại `client/addons/gdUnit4/`) | Test client — không cần cài thêm |
| Docker Desktop | mới nhất | Postgres + Redis local (Phase 04) |
| Git | 2.30+ | Hooks: `git config core.hooksPath .githooks` |

---

## 2. Cấu trúc repository

| Thư mục | Nội dung |
|---|---|
| `server/` | Backend .NET 9 (Clean Architecture). Solution `server/GameTeam.sln`; API = `server/src/GameTeam.Api`. |
| `client/` | Client Godot 4.7 (GDScript). Project = `client/project.godot`; source `client/src/`; test `client/tests/`; addon test `client/addons/gdUnit4/`. |
| `shared/` | Contract dùng chung: `shared/contracts/openapi.json`, `shared/config-schema/`, `shared/codegen/`. |
| `config/` | Dữ liệu config game (data-driven). |
| `scripts/dev/` | Script hạ tầng local: `up.{ps1,sh}` / `down.{ps1,sh}`. |
| `deploy/compose/` | `docker-compose.yml` (Postgres 16 + Redis 7, profile `api`). |
| `tools/` | Công cụ (vd `tools/config-validator`). |
| `docs/` | Tài liệu SSOT (`docs/roadmap/`, `docs/adr/`, `docs/godot/`, …). |

Client Phase 17 (boot/UI) nằm ở `client/src/ui/` (`app_root`, `boot/`, `main_hub/`, `base/`); test ở `client/tests/ui/`.

---

## 3. Cài đặt lần đầu (First-time setup)

```bash
# 1) Clone & vào thư mục
git clone <repo-url> && cd game-team

# 2) Biến môi trường local (giá trị dev, KHÔNG phải secret thật; .env đã gitignore)
cp .env.example .env
#    Cổng mặc định: POSTGRES_PORT=5432, REDIS_PORT=6379, API_PORT=8080

# 3) Backend: khôi phục + build + test
dotnet build server/GameTeam.sln
dotnet test  server/GameTeam.sln

# 4) Client: mở client/project.godot bằng Godot 4.7 (lần đầu Godot sẽ import asset).
#    Hoặc import bằng dòng lệnh (xem §6).
```

---

## 4. Chạy backend

```bash
# (khuyến nghị) Hạ tầng local Postgres + Redis — chờ healthy rồi in trạng thái:
scripts/dev/up.sh                 # Windows: scripts\dev\up.ps1

# Chạy API trên host:
dotnet run --project server/src/GameTeam.Api
```

- **Cổng:** `http://localhost:8080` (từ `API_PORT` trong `.env`).
- **Health:** `GET http://localhost:8080/health` → `{"status":"ok"}` (hoặc `"degraded"` khi Redis tắt — vẫn HTTP 200).
- Dừng hạ tầng (giữ dữ liệu): `scripts/dev/down.sh` (Windows: `down.ps1`); xoá volume DB: thêm `-v` / `-Volumes`.
- Chạy cả API trong container: `scripts/dev/up.sh --api` (Windows: `up.ps1 -Api`).

> Client mặc định gọi `http://localhost:8080`. Nếu chạy API cổng khác, đặt biến môi trường
> **`GAME_TEAM_API_BASE_URL`** trước khi chạy client (vd `http://127.0.0.1:5080`).

---

## 5. Chạy client

**Godot editor (khuyến nghị để xem UI):**
1. Mở `client/project.godot` bằng Godot 4.7.
2. Bấm **F5** (Run project). `run/main_scene = res://src/ui/app_root.tscn`.
3. Luồng boot: splash "Đang kết nối máy chủ..." → `/health` → tải config (best-effort) → **main hub**.

**Dòng lệnh (smoke, không cửa sổ):** thay `<godot>` bằng đường dẫn Godot 4.7 (vd trên Windows máy dev:
`D:\Godot_v4.7.1-stable_win64.exe\Godot_v4.7.1-stable_win64_console.exe`).

```bash
# Chạy main scene vài giây rồi thoát (kiểm không crash):
<godot> --headless --path client --quit-after 240
```

---

## 6. Chạy test

**Import gate (bắt buộc sạch trước khi test):**

```bash
<godot> --headless --import --path client        # exit 0, 0 error / 0 warning
```

**gdUnit4 — CI / Linux** (khớp [`ci-client.yml`](.github/workflows/ci-client.yml)):

```bash
chmod +x client/addons/gdUnit4/runtest.sh
cd client
xvfb-run --auto-servernum ./addons/gdUnit4/runtest.sh \
  --godot_binary "<godot>" \
  -a res://tests -rd reports
#   JUnit: client/reports/report_<n>/results.xml
```

**gdUnit4 — Windows local** (gdUnit4 từ chối `--headless` thuần → thêm `--ignoreHeadlessMode`; các test Phase 17
KHÔNG dùng InputEvent nên an toàn):

```powershell
& "<godot>" --headless --path client -d -s `
  res://addons/gdUnit4/bin/GdUnitCmdTool.gd --ignoreHeadlessMode -a res://tests
```

- **Kết quả mong đợi (local, Godot 4.7.1):** toàn bộ **48/48 test pass, 0 error/0 failure/0 orphan** (8 suite),
  gồm `tests/ui/base/` (3), `tests/ui/boot/` (5), và `tests/core/net/` (parse_health).
- `client/reports/` là artifact (đã gitignore) — không commit.

---

## 7. Xác minh Phase 17 (boot → hub; lỗi → retry)

**Happy path (server bật):**
1. `scripts/dev/up.sh` (hoặc `up.ps1`) → `dotnet run --project server/src/GameTeam.Api`. Kiểm `GET /health` → 200.
2. Mở client (F5). Quan sát: splash → health OK → config (best-effort) → **main hub** (tiêu đề "game team — Sảnh chính",
   nhãn `Config: … · online/offline`, các nút placeholder).

**Failure + retry (server tắt):**
3. Tắt API (Ctrl-C ở tiến trình `dotnet run`). Chạy lại client → hiện **màn lỗi boot** (thông báo an toàn) + nút **"Thử lại"**.
4. Bật lại API (`dotnet run …`) → bấm **"Thử lại"** → boot chạy lại sạch → vào **main hub**.

**Tự động (không cần server):** import gate sạch + gdUnit4 48/48 (§6) đã bao phủ happy-path (mock health+config → hub),
health fail → màn lỗi (không điều hướng), và retry → hub một lần (không trùng listener/điều hướng).

> Config Service thật = **phase 21**; hiện config load là **best-effort** (endpoint vắng ⇒ giữ cache, KHÔNG chặn boot).
> Đăng nhập thật chèn vào boot ở **phase 20**. Health là cổng kết nối bắt buộc.

---

## 8. Sự cố thường gặp (Troubleshooting)

| Triệu chứng | Cách xử lý |
|---|---|
| Sai SDK .NET | Cài .NET 9 (`global.json` pin `9.0.306`, roll-forward latestFeature). |
| Docker chưa chạy | Mở Docker Desktop **trước** `scripts/dev/up`. |
| Cổng bận (5432/6379/8080) | Sửa `POSTGRES_PORT`/`REDIS_PORT`/`API_PORT` trong `.env`; đổi cổng API thì set `GAME_TEAM_API_BASE_URL` cho client. |
| Client luôn ra màn lỗi boot | API chưa chạy / sai cổng / sai `GAME_TEAM_API_BASE_URL`. Kiểm `GET /health` → 200 rồi bấm "Thử lại". |
| Godot version không khớp | Dùng đúng Godot **4.7** (khớp `project.godot` feature "4.7" + CI). |
| `Headless mode is not supported!` khi test | gdUnit4 chặn `--headless` thuần: thêm `--ignoreHeadlessMode` (Windows) hoặc chạy qua `xvfb-run` (Linux). |
| Import lỗi / node path hỏng | Chạy lại `--headless --import --path client`; xoá `client/.godot/` (đã gitignore) rồi import lại. |
| `.uid` lạ trong git | `.uid` đã gitignore — không commit; nếu thấy, kiểm `.gitignore`. |
| Container unhealthy / `up` treo | `docker compose -f deploy/compose/docker-compose.yml ps` + `logs <service>`. |

---

## 9. Nguồn sự thật (versions/paths)

- .NET SDK: [`global.json`](global.json) → `9.0.306`.
- Godot / gdUnit4 pin: [`.github/workflows/ci-client.yml`](.github/workflows/ci-client.yml) (`GODOT_VERSION=4.7`, `GDUNIT4_VERSION=6.2.0`) + `client/project.godot` feature `"4.7"`.
- Cổng dev: [`.env.example`](.env.example) (Postgres 5432 / Redis 6379 / API 8080).
- API project: `server/src/GameTeam.Api`; solution `server/GameTeam.sln`.
- Client main scene: `client/project.godot` → `run/main_scene = res://src/ui/app_root.tscn`.
- Hạ tầng local chi tiết: [`docs/deployment/README.md`](docs/deployment/README.md) → Local development.
