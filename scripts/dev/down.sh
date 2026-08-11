#!/usr/bin/env bash
# Dung & don dev stack — Phase 04.
# Mac dinh: GIU volume (du lieu Postgres khong bi xoa).
# Them -v (hoac --volumes) de xoa luon volume (mat du lieu DB dev).
#   Vi du: scripts/dev/down.sh          # dung, giu du lieu
#          scripts/dev/down.sh -v       # dung + xoa volume
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
COMPOSE="$ROOT/deploy/compose/docker-compose.yml"

REMOVE_VOLUMES=0
while [ $# -gt 0 ]; do
  case "$1" in
    -v|--volumes) REMOVE_VOLUMES=1; shift ;;
    *) echo "Tham so khong ro: $1" >&2; exit 2 ;;
  esac
done

# Bao gom profile 'api' de container api (neu dang chay) cung bi don.
DOWN_ARGS=(--profile api down)
if [ "$REMOVE_VOLUMES" -eq 1 ]; then
  echo "XOA volume: du lieu Postgres dev se mat."
  DOWN_ARGS+=(-v)
else
  echo "Dung stack, GIU volume (du lieu Postgres duoc bao toan)."
fi

docker compose -f "$COMPOSE" "${DOWN_ARGS[@]}"

echo "Da dung dev stack."
if [ "$REMOVE_VOLUMES" -eq 0 ]; then
  echo "Volume 'game-team-dev_pgdata' van con. Xoa bang: scripts/dev/down.sh -v"
fi
