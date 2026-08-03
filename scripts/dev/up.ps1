# Khởi động dev stack (Postgres + Redis + API) — bootstrap stub.
$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..\..")
docker compose -f (Join-Path $root "deploy\compose\docker-compose.yml") up -d
Write-Host "Dev stack dang chay. Dung bang: scripts\dev\down.ps1"
