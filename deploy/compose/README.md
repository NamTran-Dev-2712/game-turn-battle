# `deploy/compose/` — docker-compose local

`docker-compose.yml`: dựng **Postgres 16 + Redis 7** (luôn bật, có healthcheck) và service **`api`** sau
`--profile api` (build từ `../../server/Dockerfile`), trên mạng `game-team-dev`, cho dev/integration test
cục bộ. Gọi qua `scripts/dev/up.*` / `down.*`. **Owner:** DevOps/dev-experience. Biến lấy từ `../../.env`
(copy `../../.env.example`). Không commit secret thật. **Chỉ dev — không dùng cho production** (k8s/prod ở
phase 55 / `../../docs/deployment/`).

Hướng dẫn chạy + troubleshooting: [`../../docs/deployment/README.md`](../../docs/deployment/README.md) → **Local development**.
