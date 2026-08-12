#!/usr/bin/env bash
# Entrypoint codegen client (Phase 08) — dùng cho CI (.github/workflows/codegen-check.yml) và local.
# Ký hợp đồng: run.sh [openapi-path] [output-dir]
#   mặc định: openapi-path = shared/contracts/openapi.json ; output-dir = client/src/data/generated
# KHÔNG đổi cwd → tham số đường dẫn giữ tương đối theo nơi gọi (gốc repo ở CI/local).
# Build log ra STDERR để STDOUT chỉ chứa report; exit code truyền nguyên (0=ok, 2=lỗi tool/contract).
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CLI_PROJECT="$SCRIPT_DIR/GameTeam.Codegen.Cli"
CLI_DLL="$CLI_PROJECT/bin/Release/net9.0/codegen.dll"

# Build idempotent (restore theo mặc định); mọi log build đẩy sang stderr.
dotnet build "$CLI_PROJECT" -c Release --nologo --verbosity quiet 1>&2

exec dotnet "$CLI_DLL" "$@"
