#!/usr/bin/env bash
# Khoi dong dev stack (Postgres + Redis, tuy chon API) — Phase 04.
# Mac dinh: chi Postgres + Redis. Them --api (hoac --profile api) de bat profile `api`.
# Idempotent: chay lai nhieu lan khong pha stack (docker compose up -d).
#   Vi du: scripts/dev/up.sh            # Postgres + Redis
#          scripts/dev/up.sh --api      # + API, cho /health
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
COMPOSE="$ROOT/deploy/compose/docker-compose.yml"

# --- Parse co ---
API=0
while [ $# -gt 0 ]; do
  case "$1" in
    --api) API=1; shift ;;
    --profile) [ "${2:-}" = "api" ] && API=1; shift 2 ;;
    *) echo "Tham so khong ro: $1" >&2; exit 2 ;;
  esac
done

# --- Preflight: Docker daemon phai chay ---
if ! docker info >/dev/null 2>&1; then
  echo "Docker khong san sang. Khoi dong Docker (Desktop/daemon) roi chay lai." >&2
  exit 1
fi

# --- API_PORT tu .env (neu co) de in dung URL health; mac dinh 8080 ---
API_PORT="${API_PORT:-8080}"
if [ -f "$ROOT/.env" ]; then
  ENV_PORT="$(grep -E '^\s*API_PORT\s*=' "$ROOT/.env" | tail -n1 | sed -E 's/^[^=]*=\s*//' | tr -d '[:space:]' || true)"
  [ -n "${ENV_PORT:-}" ] && API_PORT="$ENV_PORT"
fi

# --- Chon service + profile ---
PROFILE_ARGS=()
SERVICES="postgres redis"
if [ "$API" -eq 1 ]; then
  PROFILE_ARGS=(--profile api)
  SERVICES="postgres redis api"
  echo "Bat profile 'api' (build server/Dockerfile)."
fi

echo "Khoi dong: ${SERVICES} ..."
docker compose -f "$COMPOSE" "${PROFILE_ARGS[@]}" up -d

# --- Cho service healthy (poll, khong sleep co dinh) ---
wait_healthy() {
  local service="$1" timeout="${2:-90}" waited=0 cid status
  while [ "$waited" -lt "$timeout" ]; do
    cid="$(docker compose -f "$COMPOSE" "${PROFILE_ARGS[@]}" ps -q "$service" || true)"
    if [ -n "$cid" ]; then
      status="$(docker inspect --format '{{if .State.Health}}{{.State.Health.Status}}{{else}}none{{end}}' "$cid" 2>/dev/null || echo none)"
      [ "$status" = "healthy" ] && return 0
      if [ "$status" = "unhealthy" ]; then echo "Service '$service' unhealthy." >&2; return 1; fi
    fi
    sleep 2; waited=$((waited + 2))
  done
  echo "Het thoi gian cho '$service' healthy (${timeout}s)." >&2
  return 1
}

for svc in postgres redis; do
  echo "Cho '$svc' healthy ..."
  if ! wait_healthy "$svc"; then
    docker compose -f "$COMPOSE" "${PROFILE_ARGS[@]}" ps
    exit 1
  fi
done

# --- Neu bat API: cho /health tra 200 ---
if [ "$API" -eq 1 ]; then
  HEALTH_URL="http://localhost:${API_PORT}/health"
  echo "Cho API '$HEALTH_URL' ..."
  waited=0; ok=0
  while [ "$waited" -lt 90 ]; do
    if curl -fsS -o /dev/null "$HEALTH_URL" 2>/dev/null; then ok=1; break; fi
    sleep 2; waited=$((waited + 2))
  done
  if [ "$ok" -ne 1 ]; then
    echo "API '/health' khong phan hoi 200 trong thoi gian cho." >&2
    docker compose -f "$COMPOSE" "${PROFILE_ARGS[@]}" logs api --tail 30
    exit 1
  fi
fi

# --- Trang thai + goi y ---
echo
docker compose -f "$COMPOSE" "${PROFILE_ARGS[@]}" ps
echo
echo "Dev stack san sang."
[ "$API" -eq 1 ] && echo "Health: http://localhost:${API_PORT}/health -> {\"status\":\"ok\"}"
echo "Dung stack: scripts/dev/down.sh  (them -v de xoa du lieu DB)"
