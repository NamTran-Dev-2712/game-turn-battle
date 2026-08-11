# Khoi dong dev stack (Postgres + Redis, tuy chon API) — Phase 04.
# Mac dinh: chi Postgres + Redis. Them -Api de bat profile `api` (build server/Dockerfile).
# Idempotent: chay lai nhieu lan khong pha stack (docker compose up -d).
#   Vi du: scripts\dev\up.ps1            # Postgres + Redis
#          scripts\dev\up.ps1 -Api       # + API, cho /health
param(
    [switch]$Api
)
$ErrorActionPreference = "Stop"

$root    = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$compose = Join-Path $root "deploy\compose\docker-compose.yml"

# --- Preflight: Docker daemon phai chay ---
docker info *> $null
if ($LASTEXITCODE -ne 0) {
    Write-Error "Docker khong san sang. Mo Docker Desktop roi chay lai."
    exit 1
}

# --- Nap API_PORT tu .env (neu co) de in dung URL health; mac dinh 8080 ---
$apiPort = if ($env:API_PORT) { $env:API_PORT } else { "8080" }
$envFile = Join-Path $root ".env"
if (Test-Path $envFile) {
    $m = Select-String -Path $envFile -Pattern '^\s*API_PORT\s*=\s*(\d+)' | Select-Object -First 1
    if ($m) { $apiPort = $m.Matches[0].Groups[1].Value }
}

# --- Chon service + profile ---
$profileArgs = @()
$services    = @("postgres", "redis")
if ($Api) {
    $profileArgs = @("--profile", "api")
    $services    = @("postgres", "redis", "api")
    Write-Host "Bat profile 'api' (build server/Dockerfile)." -ForegroundColor Cyan
}

Write-Host "Khoi dong: $($services -join ', ') ..." -ForegroundColor Cyan
docker compose -f $compose @profileArgs up -d
if ($LASTEXITCODE -ne 0) { Write-Error "docker compose up that bai."; exit 1 }

# --- Cho Postgres + Redis healthy (poll, khong sleep co dinh) ---
function Wait-Healthy {
    param([string]$Service, [int]$TimeoutSec = 90)
    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    while ((Get-Date) -lt $deadline) {
        $cid = (docker compose -f $compose @profileArgs ps -q $Service).Trim()
        if ($cid) {
            $status = (docker inspect --format '{{if .State.Health}}{{.State.Health.Status}}{{else}}none{{end}}' $cid 2>$null).Trim()
            if ($status -eq "healthy") { return $true }
            if ($status -eq "unhealthy") { Write-Error "Service '$Service' unhealthy."; return $false }
        }
        Start-Sleep -Seconds 2
    }
    Write-Error "Het thoi gian cho '$Service' healthy ($TimeoutSec s)."
    return $false
}

foreach ($svc in @("postgres", "redis")) {
    Write-Host "Cho '$svc' healthy ..." -ForegroundColor DarkGray
    if (-not (Wait-Healthy -Service $svc)) {
        docker compose -f $compose @profileArgs ps
        exit 1
    }
}

# --- Neu bat API: cho /health tra 200 ---
if ($Api) {
    $healthUrl = "http://localhost:$apiPort/health"
    Write-Host "Cho API '$healthUrl' ..." -ForegroundColor DarkGray
    $deadline = (Get-Date).AddSeconds(90)
    $ok = $false
    while ((Get-Date) -lt $deadline) {
        try {
            $resp = Invoke-WebRequest -Uri $healthUrl -UseBasicParsing -TimeoutSec 3
            if ($resp.StatusCode -eq 200) { $ok = $true; break }
        } catch { }
        Start-Sleep -Seconds 2
    }
    if (-not $ok) {
        Write-Error "API '/health' khong phan hoi 200 trong thoi gian cho."
        docker compose -f $compose @profileArgs logs api --tail 30
        exit 1
    }
}

# --- Trang thai + goi y ---
Write-Host ""
docker compose -f $compose @profileArgs ps
Write-Host ""
Write-Host "Dev stack san sang." -ForegroundColor Green
if ($Api) { Write-Host "Health: http://localhost:$apiPort/health -> {""status"":""ok""}" -ForegroundColor Green }
Write-Host "Dung stack: scripts\dev\down.ps1  (them -Volumes de xoa du lieu DB)"
