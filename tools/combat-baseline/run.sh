#!/usr/bin/env bash
# Entrypoint cho tool combat-baseline (Phase 26) + gate .github/workflows/ci-server.yml.
# Hop dong: run.sh <generate|check> [file.json ...]  (mac dinh: tat ca vector trong shared/combat-vectors).
# KHONG doi cwd. Build log ra STDERR de STDOUT chi chua report; exit code truyen nguyen (0=ok, 1=drift, 2=tool).
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CLI_PROJECT="$SCRIPT_DIR/GameTeam.CombatBaseline.Cli"
CLI_DLL="$CLI_PROJECT/bin/Release/net9.0/combat-baseline.dll"

# Build idempotent (restore theo mac dinh); moi log build day sang stderr.
dotnet build "$CLI_PROJECT" -c Release --nologo --verbosity quiet 1>&2

exec dotnet "$CLI_DLL" "$@"
