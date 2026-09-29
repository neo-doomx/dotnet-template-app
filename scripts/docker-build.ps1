#Requires -Version 7.0
<#
.SYNOPSIS
    Builds the API Docker image.
.EXAMPLE
    ./scripts/docker-build.ps1
    ./scripts/docker-build.ps1 -Tag 1.2.0
#>

param(
    [string] $Tag = 'latest'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent

docker build --file (Join-Path $root 'src/Template.Api/Dockerfile') --tag "template-api:$Tag" $root
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Built template-api:$Tag" -ForegroundColor Green
