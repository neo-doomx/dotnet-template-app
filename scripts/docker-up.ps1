#Requires -Version 7.0
<#
.SYNOPSIS
    Starts the containers.
.PARAMETER InfraOnly
    Start SQL Server, Redis and the Aspire dashboard only. Use this when you run the API with 'dotnet run'.
.EXAMPLE
    ./scripts/docker-up.ps1             # everything, API on https://localhost:8443
    ./scripts/docker-up.ps1 -InfraOnly  # dependencies only
#>

param(
    [switch] $InfraOnly
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$envFile = Join-Path $root '.env'
$composeFile = Join-Path $root 'docker-compose.yml'

if (-not (Test-Path $envFile)) {
    throw 'Missing .env. Run ./scripts/setup-secrets.ps1 first.'
}

if ($InfraOnly) {
    docker compose --file $composeFile up --detach --wait sqlserver redis aspire-dashboard
}
else {
    docker compose --file $composeFile up --detach --wait --build
}
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$token = (Get-Content $envFile | Where-Object { $_ -like 'DASHBOARD_BROWSER_TOKEN=*' }) -replace '^DASHBOARD_BROWSER_TOKEN=', ''

Write-Host ''
Write-Host "Aspire dashboard: http://localhost:18888/login?t=$token" -ForegroundColor Green
if (-not $InfraOnly) {
    Write-Host 'API (Scalar):     https://localhost:8443/scalar' -ForegroundColor Green
}
