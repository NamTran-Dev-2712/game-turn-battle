# Dung & don dev stack — Phase 04.
# Mac dinh: GIU volume (du lieu Postgres khong bi xoa).
# Them -Volumes de xoa luon volume (mat du lieu DB dev).
#   Vi du: scripts\dev\down.ps1             # dung, giu du lieu
#          scripts\dev\down.ps1 -Volumes    # dung + xoa volume
param(
    [switch]$Volumes
)
$ErrorActionPreference = "Stop"

$root    = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$compose = Join-Path $root "deploy\compose\docker-compose.yml"

# Bao gom profile 'api' de container api (neu dang chay) cung bi don.
$downArgs = @("--profile", "api", "down")
if ($Volumes) {
    Write-Host "XOA volume: du lieu Postgres dev se mat." -ForegroundColor Yellow
    $downArgs += "-v"
} else {
    Write-Host "Dung stack, GIU volume (du lieu Postgres duoc bao toan)." -ForegroundColor Cyan
}

docker compose -f $compose @downArgs
if ($LASTEXITCODE -ne 0) { Write-Error "docker compose down that bai."; exit 1 }

Write-Host "Da dung dev stack." -ForegroundColor Green
if (-not $Volumes) { Write-Host "Volume 'game-team-dev_pgdata' van con. Xoa bang: scripts\dev\down.ps1 -Volumes" }
