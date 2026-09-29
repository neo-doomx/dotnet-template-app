#Requires -Version 7.0
<#
.SYNOPSIS
    Creates every local secret the template needs. Safe to run again: existing values in .env are kept.

    1. .env                      passwords for the Docker containers (gitignored)
    2. dotnet user-secrets       connection strings for 'dotnet run'
    3. .certs/template-api.pfx   HTTPS certificate for the API container (gitignored)
.EXAMPLE
    ./scripts/setup-secrets.ps1
#>

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$envFile = Join-Path $root '.env'
$apiProject = Join-Path $root 'src/Template.Api/Template.Api.csproj'
$certFile = Join-Path $root '.certs/template-api.pfx'

function New-Secret([int] $Length = 32) {
    # Letters and digits only, so the value is safe inside connection strings and compose files.
    $chars = [char[]]'abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789'
    do {
        $value = -join (1..$Length | ForEach-Object {
            $chars[[System.Security.Cryptography.RandomNumberGenerator]::GetInt32($chars.Length)]
        })
    } until ($value -cmatch '[a-z]' -and $value -cmatch '[A-Z]' -and $value -match '\d')
    return $value
}

function Invoke-Native([scriptblock] $Command, [string] $ErrorMessage) {
    & $Command
    if ($LASTEXITCODE -ne 0) { throw $ErrorMessage }
}

# 1. .env
$values = [ordered]@{}
if (Test-Path $envFile) {
    foreach ($line in Get-Content $envFile) {
        if ($line -match '^\s*([A-Z_]+)\s*=\s*(.*)$') { $values[$Matches[1]] = $Matches[2] }
    }
}

foreach ($key in 'SQL_SA_PASSWORD', 'REDIS_PASSWORD', 'CERT_PASSWORD', 'DASHBOARD_BROWSER_TOKEN') {
    if (-not $values[$key]) { $values[$key] = New-Secret }
}

$values.GetEnumerator() | ForEach-Object { "$($_.Key)=$($_.Value)" } | Set-Content $envFile
Write-Host "[1/3] Wrote $envFile" -ForegroundColor Green

# 2. User secrets for 'dotnet run'. The API runs on the host and reaches the containers on 127.0.0.1.
#    Not 'localhost': it can resolve to IPv6 (::1) first, and the containers only listen on IPv4.
$database = "Server=127.0.0.1,1433;Database=TemplateDb;User Id=sa;Password=$($values.SQL_SA_PASSWORD);TrustServerCertificate=True"
$redis = "127.0.0.1:6379,password=$($values.REDIS_PASSWORD)"

Invoke-Native { dotnet user-secrets set 'ConnectionStrings:Database' $database --project $apiProject | Out-Null } 'Failed to set the Database user secret.'
Invoke-Native { dotnet user-secrets set 'ConnectionStrings:Redis' $redis --project $apiProject | Out-Null } 'Failed to set the Redis user secret.'
Write-Host '[2/3] Stored connection strings in user secrets' -ForegroundColor Green

# 3. HTTPS certificate. Trusting it may show a confirmation dialog.
dotnet dev-certs https --check --trust *> $null
if ($LASTEXITCODE -ne 0) {
    Invoke-Native { dotnet dev-certs https --trust } 'Failed to trust the HTTPS development certificate.'
}

New-Item -ItemType Directory -Force (Split-Path $certFile) | Out-Null
Invoke-Native { dotnet dev-certs https --export-path $certFile --password $values.CERT_PASSWORD | Out-Null } 'Failed to export the HTTPS certificate.'
Write-Host "[3/3] Exported HTTPS certificate to $certFile" -ForegroundColor Green
