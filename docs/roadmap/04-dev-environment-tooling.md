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

- [ ] Rà `docker-compose.yml`: image pin (`postgres:16-alpine`, `redis:7-alpine`), healthcheck, volume, network `game-team-dev`.
- [ ] Profile `api` build từ `server/Dockerfile`, phụ thuộc DB healthy, inject connection strings.
- [ ] `scripts/dev/up.ps1|sh`: `docker compose up -d` (+`--profile api` tuỳ cờ), chờ healthy, in trạng thái.
- [ ] `scripts/dev/down.ps1|sh`: `docker compose down` (giữ/volume tuỳ cờ `-v`).
- [ ] Đồng bộ `.env.example` với biến compose cần; kiểm `.gitignore` chặn `.env`.
- [ ] Thử `up` → gọi `GET /health` trả `{status:"ok"}` khi bật profile api.
- [ ] Viết mục hướng dẫn chạy local + troubleshooting cổng bận.

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

---

## Liên kết
- [`../deployment/README.md`](../deployment/README.md) · [`../deployment/ci-cd-pipeline.md`](../deployment/ci-cd-pipeline.md) · [`../architecture/project-structure.md`](../architecture/project-structure.md)
- ADR: [`../adr/ADR-003-backend-architecture.md`](../adr/ADR-003-backend-architecture.md)
- Roadmap: [`README.md`](README.md) → kế: [`05-shared-contracts-skeleton.md`](05-shared-contracts-skeleton.md)
