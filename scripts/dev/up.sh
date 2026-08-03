#!/usr/bin/env bash
# Khởi động dev stack (Postgres + Redis + API) — bootstrap stub.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
docker compose -f "$ROOT/deploy/compose/docker-compose.yml" up -d
echo "Dev stack đang chạy. Dừng bằng: scripts/dev/down.sh"
