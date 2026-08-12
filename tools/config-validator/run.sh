#!/usr/bin/env bash
# Entrypoint GATE cho .github/workflows/validate-config.yml (Phase 07).
# Ký hợp đồng: run.sh <config-dir> [schema-dir]  (mặc định schema-dir = shared/config-schema).
# KHÔNG đổi cwd → tham số đường dẫn giữ tương đối theo nơi gọi (gốc repo ở CI/local).
# Build log ra STDERR để STDOUT chỉ chứa report của validator; exit code truyền nguyên (0=ok, 1=fail, 2=tool).
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CLI_PROJECT="$SCRIPT_DIR/GameTeam.ConfigValidator.Cli"
CLI_DLL="$CLI_PROJECT/bin/Release/net9.0/config-validator.dll"

# Build idempotent (restore theo mặc định); mọi log build đẩy sang stderr.
dotnet build "$CLI_PROJECT" -c Release --nologo --verbosity quiet 1>&2

exec dotnet "$CLI_DLL" "$@"
