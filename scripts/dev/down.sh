#!/usr/bin/env bash
# Dừng & dọn dev stack — bootstrap stub.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
docker compose -f "$ROOT/deploy/compose/docker-compose.yml" down
