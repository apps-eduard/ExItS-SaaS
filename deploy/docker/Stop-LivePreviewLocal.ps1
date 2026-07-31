#Requires -Version 5.1
<#
.SYNOPSIS
  Stops live-preview Docker databases without deleting volumes.

.DESCRIPTION
  Does not remove exits_live_preview_*_db_data volumes.
  Does not start or remove packaging stacks.
  Close local Platform API / POS API / Admin windows separately.
#>
[CmdletBinding()]
param(
    [switch]$Down
)

$ErrorActionPreference = 'Stop'
$dockerDir = $PSScriptRoot
$envFile = Join-Path $dockerDir '.env.live-preview'
$composeFile = Join-Path $dockerDir 'compose.live-preview.yaml'

if (-not (Test-Path $envFile)) {
    throw "Missing $envFile."
}

if ($Down) {
    Write-Host 'Stopping live-preview compose project (volumes preserved; no -v)...'
    & docker compose -f $composeFile --env-file $envFile down
}
else {
    Write-Host 'Stopping live-preview database containers (volumes preserved)...'
    & docker compose -f $composeFile --env-file $envFile stop
}

if ($LASTEXITCODE -ne 0) { throw "docker compose stop/down failed ($LASTEXITCODE)." }

Write-Host 'Done. Named volumes exits_live_preview_platform_db_data / exits_live_preview_pos_db_data remain.'
Write-Host 'Close any local API/Admin PowerShell windows manually.'
