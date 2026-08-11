# 04 — Môi trường dev & tooling

> Mục đích: Đảm bảo môi trường phát triển local **một lệnh chạy được** (Postgres + Redis + API) và các script tiện ích ổn định, để mọi phase sau có nơi chạy/thử thật.

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 0 Nền tảng & Chuẩn hoá | P0 | S0 | hạ tầng |

# Mục tiêu

`deploy/compose/docker-compose.yml` + `scripts/dev/up|down` khởi động được stack dev (Postgres 16, Redis 7, API service qua profile), nạp biến từ `.env` (template `.env.example`), healthcheck xanh.

# Lý do

Các phase backend (auth/save/config service, nhóm 2–4) cần Postgres+Redis chạy được cục bộ và trong CI integration. Chuẩn hoá dev env sớm tránh "chạy trên máy tôi thì được".

# Phụ thuộc

- **Trước:** 01, 02.
- **Sau:** 11 (EF/Postgres), 12 (Redis), 18–22 (auth/save/config service), 48/54 (integration/smoke).

# Phạm vi

- Hoàn thiện `docker-compose.yml`: Postgres+Redis luôn bật (có healthcheck), `api` sau `--profile api`, inject `ConnectionStrings__Postgres/__Redis`.
- `scripts/dev/up.{ps1,sh}`, `down.{ps1,sh}` idempotent, chạy trên Windows (PowerShell) + *nix (bash).
- `.env.example` đầy đủ khoá; `.env` chỉ dev, không commit.
- Ghi tài liệu "cách chạy local" ngắn gọn.

# Không thuộc phạm vi

- k8s / hạ tầng production (phase 55 / deploy docs).
- Migration schema thực (phase 11) — chỉ đảm bảo DB container sẵn sàng.
- Seed dữ liệu nghiệp vụ.

# Deliverables

- Stack dev khởi động bằng 1 lệnh; healthcheck Postgres+Redis xanh.
- Script up/down chạy đúng trên PowerShell và bash.
- `.env.example` cập nhật + ghi chú biến.
- Mục "Local development" trong [`../deployment/README.md`](../deployment/README.md) hoặc root `SETUP.md`.

# Công việc cần thực hiện

- [x] Rà `docker-compose.yml`: image pin (`postgres:16-alpine`, `redis:7-alpine`), healthcheck, volume, network `game-team-dev`. → thêm network cấp cao `game-team-dev` (trước chỉ có `name:` project ⇒ mạng là `game-team-dev_default`); `docker compose config` xác nhận network đúng tên; `docker network ls` khi chạy = `game-team-dev`.
- [x] Profile `api` build từ `server/Dockerfile`, phụ thuộc DB healthy, inject connection strings. → build image thành công (dotnet publish trong container); `depends_on: condition: service_healthy`; `docker exec printenv` xác nhận `ConnectionStrings__Postgres=Host=postgres;…` + `ConnectionStrings__Redis=redis:6379` (dùng tên service, không `localhost`).
- [x] `scripts/dev/up.ps1|sh`: `docker compose up -d` (+`--profile api` tuỳ cờ `-Api`/`--api`), chờ healthy (poll `docker inspect .State.Health`, không sleep cứng), in `docker compose ps`. → chạy thật cả PowerShell lẫn bash: Postgres+Redis `(healthy)`, exit 0.
- [x] `scripts/dev/down.ps1|sh`: `docker compose down` mặc định **giữ** volume; cờ `-Volumes`/`-v` xoá volume; idempotent. → verify: down thường giữ `game-team-dev_pgdata`; `-v` xoá; chạy lại khi rỗng vẫn exit 0.
- [x] Đồng bộ `.env.example` với biến compose cần; kiểm `.gitignore` chặn `.env`. → 7/7 biến compose (`POSTGRES_*`, `REDIS_PORT`, `ASPNETCORE_ENVIRONMENT`, `API_PORT`) có đủ; `git check-ignore .env` → exit 0 (ignored), `.env.example` committable.
- [x] Thử `up` → gọi `GET /health` trả `{status:"ok"}` khi bật profile api. → `up -Api`/`--api`: `curl /health` → **HTTP 200** body `{"status":"ok"}` (script tự chờ /health=200 mới báo xong).
- [x] Viết mục hướng dẫn chạy local + troubleshooting cổng bận. → mục **Local development** trong [`../deployment/README.md`](../deployment/README.md) (prereq → up → api → health → down → xoá volume → override cổng → bảng troubleshooting 5432/6379/8080 + Docker/unhealthy/connect); đồng bộ `SETUP.md`, `scripts/dev/README.md`, `deploy/compose/README.md`.

# Tiêu chí hoàn thành

- `scripts/dev/up` khởi động Postgres+Redis healthy < ~30s; `down` dọn sạch.
- Bật `--profile api` → `/health` = 200 `{status:"ok"}`.
- Script chạy được cả PowerShell lẫn bash (không phụ thuộc lệnh chỉ có ở 1 OS).
- `.env` không bị commit; `.env.example` đủ khoá.

# Cách kiểm tra

- Windows: `pwsh scripts/dev/up.ps1` → `docker ps` thấy healthy → `curl http://localhost:8080/health`.
- *nix: `bash scripts/dev/up.sh` tương tự.
- `scripts/dev/down.*` → `docker ps` trống.
- `git check-ignore .env` xác nhận bị ignore.

# Rủi ro

- **Cổng 5432/6379/8080 bận** → cho phép override qua `.env`; ghi troubleshooting.
- **Khác biệt line-ending script** (`.ps1` CRLF, `.sh` LF) → theo `.gitattributes`.
- **Healthcheck sai** khiến api start sớm → dùng `depends_on: condition: service_healthy`.

# Ghi chú

`.env` chỉ cho dev (ghi ở [`../audit/bootstrap-audit.md`](../audit/bootstrap-audit.md) §4). Không đưa secret thật vào compose. Cân nhắc Git LFS cho asset nặng (ngoài phạm vi phase này).

# Technical Debt Review

- **Maintainability:** một lệnh dựng môi trường; giảm ma sát onboarding.
- **Scalability:** compose chỉ cho dev; production ở phase 55/deploy docs.
- **Testing:** nền cho integration test dùng Testcontainers sau này.
- **Security:** secret chỉ dev; production secret quản lý riêng.
- **Nợ:** seed & migration thực để phase 11.

# Phase Review

Đóng khi stack dev lên bằng 1 lệnh, healthcheck xanh, `/health` phản hồi khi bật api, script chạy đa nền tảng.

**Đủ điều kiện đóng (eligible to close).** Toàn bộ 7 `# Công việc cần thực hiện` và 4 `# Tiêu chí hoàn thành`
đã đo được và xác minh chạy thật với Docker Desktop (Engine 28.5.1) trên cùng host Windows.

## Bằng chứng xác minh (chạy thật, Phase 04)

- **Compose:** `docker compose -f deploy/compose/docker-compose.yml config` hợp lệ; network = `game-team-dev`
  (đã thêm network cấp cao — trước đó chỉ là `game-team-dev_default`). Image pin `postgres:16-alpine` +
  `redis:7-alpine`; volume `pgdata`; healthcheck `pg_isready` / `redis-cli ping`.
- **up (base):** `scripts\dev\up.ps1` **và** `bash scripts/dev/up.sh` → Postgres+Redis `Up (healthy)` < ~30s,
  in `docker compose ps`, exit 0. Script poll `.State.Health` (không sleep cứng).
- **up (api):** `up.ps1 -Api` / `up.sh --api` → build `server/Dockerfile` (dotnet publish OK), chờ deps
  healthy rồi chờ `/health`; `curl http://localhost:<API_PORT>/health` → **200 `{"status":"ok"}`**.
  `docker exec … printenv` xác nhận `ConnectionStrings__Postgres/__Redis` inject đúng (tên service).
- **Override cổng:** cổng host **8080 đang bận** (PID khác) ⇒ `up -Api` **fail rõ ràng** (exit 1, in lỗi
  bind + log api). Đặt `API_PORT=18080` (cơ chế `.env`/env) ⇒ api chạy `18080->8080`, `/health`=200 →
  chứng minh troubleshooting "cổng bận" hoạt động.
- **down:** `down.*` mặc định xoá container+network, **giữ** volume `game-team-dev_pgdata`;
  `down.ps1 -Volumes` / `down.sh -v` xoá volume; chạy lại khi rỗng vẫn exit 0 (idempotent).
- **.env:** `git check-ignore .env` → exit 0 (ignored); `.env.example` committable, đủ 7 biến compose.
- **Đa nền tảng:** PowerShell **và** Git Bash (bash thật) **đều chạy thật** trên host này với Docker CLI
  Windows. *Lưu ý trung thực:* chưa chạy trên **Linux kernel** thật (Git Bash là môi trường bash trên
  Windows) — cú pháp `.sh` đã `bash -n` sạch, không dùng lệnh chỉ có ở 1 OS; CI integration Linux ở phase sau.
- **Sửa trong lúc verify:** `down.sh -v` ban đầu exit 1 (dòng cuối `[ test ] && echo` dưới `set -e`) → đổi
  thành `if`; re-verify exit 0.

**Kết luận:** **PASS.** Đủ điều kiện đóng theo Strict Phase Gate §5 (mọi hạng mục xác minh chạy thật, không
còn TODO/blocker). Không nợ kỹ thuật mở trong phạm vi phase 04.

---

## Liên kết
- [`../deployment/README.md`](../deployment/README.md) · [`../deployment/ci-cd-pipeline.md`](../deployment/ci-cd-pipeline.md) · [`../architecture/project-structure.md`](../architecture/project-structure.md)
- ADR: [`../adr/ADR-003-backend-architecture.md`](../adr/ADR-003-backend-architecture.md)
- Roadmap: [`README.md`](README.md) → kế: [`05-shared-contracts-skeleton.md`](05-shared-contracts-skeleton.md)
