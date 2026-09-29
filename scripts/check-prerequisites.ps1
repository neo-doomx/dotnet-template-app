#Requires -Version 7.0
<#
.SYNOPSIS
    Checks that everything needed to build and run the template is installed.
.EXAMPLE
    ./scripts/check-prerequisites.ps1
#>

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$failures = 0

function Test-Requirement([string] $Name, [scriptblock] $Check, [string] $Fix) {
    $detail = $null
    try { $detail = & $Check } catch { }

    if ($detail) {
        Write-Host "[OK]   $Name ($detail)" -ForegroundColor Green
        return
    }

    Write-Host "[FAIL] $Name" -ForegroundColor Red
    Write-Host "       $Fix" -ForegroundColor Yellow
    $script:failures++
}

Push-Location $root
try {
    Test-Requirement '.NET SDK 10 (from global.json)' {
        $version = dotnet --version 2>$null
        if ($LASTEXITCODE -eq 0 -and $version -like '10.*') { $version }
    } 'Install the .NET 10 SDK: https://dotnet.microsoft.com/download/dotnet/10.0'

    Test-Requirement 'dotnet-ef local tool' {
        dotnet tool restore *> $null
        $version = dotnet ef --version 2>$null | Select-Object -Last 1
        if ($LASTEXITCODE -eq 0) { $version }
    } 'Run: dotnet tool restore'

    Test-Requirement 'HTTPS development certificate trusted' {
        dotnet dev-certs https --check --trust *> $null
        if ($LASTEXITCODE -eq 0) { 'trusted' }
    } 'Run: ./scripts/setup-secrets.ps1 (or: dotnet dev-certs https --trust)'

    Test-Requirement 'Docker CLI' {
        $version = docker --version 2>$null
        if ($LASTEXITCODE -eq 0) { $version -replace '^Docker version ', '' }
    } 'Install Docker Desktop: https://www.docker.com/products/docker-desktop'

    Test-Requirement 'Docker engine running' {
        $version = docker info --format '{{.ServerVersion}}' 2>$null
        if ($LASTEXITCODE -eq 0) { $version }
    } 'Start Docker Desktop and wait until the engine is running.'

    Test-Requirement 'Docker Compose v2' {
        $version = docker compose version --short 2>$null
        if ($LASTEXITCODE -eq 0) { $version }
    } 'Update Docker Desktop. Compose v2 ships with it.'

    Test-Requirement 'Local secrets (.env)' {
        if (Test-Path (Join-Path $root '.env')) { 'found' }
    } 'Run: ./scripts/setup-secrets.ps1'
}
finally {
    Pop-Location
}

Write-Host ''
if ($failures -gt 0) {
    Write-Host "$failures check(s) failed." -ForegroundColor Red
    exit 1
}

Write-Host 'All checks passed.' -ForegroundColor Green
