#Requires -Version 5.1
<#
.SYNOPSIS
  Opt-in Cloudflare Tunnel preview for the FULL Docker Local Validation stack.

.DESCRIPTION
  LOCAL VALIDATION / NOT PRODUCTION.
  Starts the existing full Docker stack when it is not already running, then
  starts only the Cloudflare preview overlay. Does not print the tunnel token.
  Cloudflare Access must protect the public hostnames before outside use.
#>
[CmdletBinding()]
param(
    [int]$TunnelWaitSeconds = 90
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LocalValidation.stack.ps1')

function Write-Step([string]$Message) { Write-Host "[cloudflare-preview] $Message" -ForegroundColor Cyan }
function Write-Ok([string]$Message) { Write-Host "[cloudflare-preview] OK  $Message" -ForegroundColor Green }
function Write-Warn([string]$Message) { Write-Host "[cloudflare-preview] $Message" -ForegroundColor Yellow }

$repoRoot = Get-LocalValidationRepoRoot -StartPath $PSScriptRoot
$composeDir = Join-Path $repoRoot 'deploy\docker'
$baseCompose = Join-Path $composeDir 'compose.local-validation.yaml'
$overlayCompose = Join-Path $composeDir 'compose.cloudflare-preview.yaml'
$envFile = Join-Path $composeDir '.env.local-validation'

Test-LocalValidationDockerAvailable

if (-not (Test-Path -LiteralPath $envFile)) {
    throw "Missing $envFile. Copy deploy/docker/.env.local-validation.example and fill local values. Do not commit it."
}
if (-not (Test-Path -LiteralPath $overlayCompose)) {
    throw "Missing $overlayCompose."
}

$envMap = Import-LocalValidationDotEnv -Path $envFile
Require-LocalValidationEnvKey -Map $envMap -Key 'LOCAL_VALIDATION_CLOUDFLARE_TUNNEL_TOKEN'
Write-Ok 'Tunnel token is present in the local env file (value not shown).'

$appContainers = @(
    'exits-local-validation-platform-api',
    'exits-local-validation-pos-api',
    'exits-local-validation-admin-web',
    'exits-local-validation-react-pos'
)

$stackReady = $true
foreach ($name in $appContainers) {
    $running = & docker inspect -f '{{.State.Running}}' $name 2>$null
    if ($LASTEXITCODE -ne 0 -or $running -ne 'true') {
        $stackReady = $false
        break
    }
}

if (-not $stackReady) {
    Write-Step 'FULL Docker Local Validation is not running. Starting it with the existing launcher.'
    & (Join-Path $PSScriptRoot 'Start-DockerLocalValidation.ps1')
}
else {
    Write-Ok 'FULL Docker Local Validation is already running.'
}

$env:LOCAL_VALIDATION_CLOUDFLARE_TUNNEL_TOKEN = [string]$envMap['LOCAL_VALIDATION_CLOUDFLARE_TUNNEL_TOKEN']
Remove-Item Env:LOCAL_VALIDATION_CLOUDFLARE_TRUSTED_PROXY -ErrorAction SilentlyContinue

Write-Step 'Starting cloudflared on the Local Validation network.'
$tunnelArgs = @(
    'compose',
    '-p', 'exits-local-validation',
    '-f', $baseCompose,
    '-f', $overlayCompose,
    '--env-file', $envFile,
    'up', '-d', '--no-deps', '--force-recreate',
    'cloudflared'
)
$tunnelExit = Invoke-LocalValidationDocker -DockerArgs $tunnelArgs
if ($tunnelExit -ne 0) {
    throw "docker compose could not start cloudflared (exit $tunnelExit)."
}

Write-Step 'Refreshing API allowed hosts for the preview hostnames.'
$webArgs = @(
    'compose',
    '-p', 'exits-local-validation',
    '-f', $baseCompose,
    '-f', $overlayCompose,
    '--env-file', $envFile,
    'up', '-d', '--no-deps', '--force-recreate',
    'platform-api',
    'pos-api'
)
$exitCode = Invoke-LocalValidationDocker -DockerArgs $webArgs
if ($exitCode -ne 0) {
    throw "docker compose preview web refresh exited with code $exitCode."
}

$deadline = (Get-Date).AddSeconds($TunnelWaitSeconds)
$connected = $false
while ((Get-Date) -lt $deadline) {
    $running = & docker inspect -f '{{.State.Running}}' 'exits-local-validation-cloudflared' 2>$null
    if ($LASTEXITCODE -ne 0 -or $running -ne 'true') {
        throw 'cloudflared is not running. Check the container locally. Do not paste the tunnel token into chat or tickets.'
    }

    $prev = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $logs = & docker logs --tail 80 'exits-local-validation-cloudflared' 2>&1
    $ErrorActionPreference = $prev
    foreach ($line in @($logs)) {
        if ([string]$line -match 'Registered tunnel connection') {
            $connected = $true
            break
        }
    }
    if ($connected) { break }
    Start-Sleep -Seconds 2
}

if (-not $connected) {
    throw "cloudflared stayed up but did not report a registered connection within ${TunnelWaitSeconds}s. Inspect logs locally. Do not paste the tunnel token."
}

Write-Ok 'cloudflared reported a registered tunnel connection.'
Write-Host ''
Write-Warn 'LOCAL VALIDATION / NOT PRODUCTION'
Write-Warn 'CLOUDFLARE ACCESS MUST PROTECT THESE HOSTS'
Write-Host ''
Write-Host 'https://app.exitsapps.com'
Write-Host 'https://my.exitsapps.com'
Write-Host 'https://pos.exitsapps.com'
Write-Host 'https://admin.exitsapps.com'
Write-Host ''
Write-Host 'Application login is still required. Cloudflare Access is only the outer gate.'
Write-Host 'Stop preview with .\tools\Stop-CloudflareLocalPreview.ps1'
Write-Host 'That stops cloudflared only. Run .\tools\Start-DockerLocalValidation.ps1 afterward to restore localhost cross-links.'
