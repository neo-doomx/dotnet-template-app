#Requires -Version 7.0
<#
.SYNOPSIS
    Stops the containers.
.PARAMETER Volumes
    Also delete the SQL Server data volume. Required after changing SQL_SA_PASSWORD in .env.
.EXAMPLE
    ./scripts/docker-down.ps1
    ./scripts/docker-down.ps1 -Volumes
#>

param(
    [switch] $Volumes
)

$ErrorActionPreference = 'Stop'
$composeFile = Join-Path (Split-Path $PSScriptRoot -Parent) 'docker-compose.yml'

if ($Volumes) {
    docker compose --file $composeFile down --volumes
}
else {
    docker compose --file $composeFile down
}
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
