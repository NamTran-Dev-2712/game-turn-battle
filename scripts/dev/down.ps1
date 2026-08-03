# Dừng & dọn dev stack — bootstrap stub.
$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..\..")
docker compose -f (Join-Path $root "deploy\compose\docker-compose.yml") down
