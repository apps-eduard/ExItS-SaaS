#Requires -Version 5.1
<#
.SYNOPSIS
  Stops only the Cloudflare Local Preview tunnel container.

.DESCRIPTION
  Removes exits-local-validation-cloudflared.
  Does not stop application containers, databases, or Mailpit.
  Does not remove volumes and does not delete the compose project.
#>
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Step([string]$Message) { Write-Host "[cloudflare-preview] $Message" -ForegroundColor Cyan }
function Write-Ok([string]$Message) { Write-Host "[cloudflare-preview] OK  $Message" -ForegroundColor Green }
function Write-Warn([string]$Message) { Write-Host "[cloudflare-preview] $Message" -ForegroundColor Yellow }

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    throw 'Docker CLI not found. Install/start Docker Desktop.'
}

$container = 'exits-local-validation-cloudflared'
$exists = & docker inspect -f '{{.State.Running}}' $container 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Ok "$container is not running."
}
else {
    Write-Step "Stopping $container."
    & docker stop $container | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "docker stop $container failed."
    }
    & docker rm $container | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "docker rm $container failed."
    }
    Write-Ok 'Cloudflare preview container removed.'
}

Write-Warn 'Application containers, databases, and volumes were left running.'
Write-Warn 'Public web origins stay on the preview hostnames until you run .\tools\Start-DockerLocalValidation.ps1 without the overlay.'
Write-Warn 'LOCAL VALIDATION / NOT PRODUCTION'
