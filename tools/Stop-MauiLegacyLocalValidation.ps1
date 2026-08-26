#Requires -Version 5.1
<#
.SYNOPSIS
  Stops the isolated LEGACY MAUI / Blazor Local Validation Docker stack.

.DESCRIPTION
  Stops ONLY exits-maui-local-validation containers.
  Preserves MAUI DB volumes (never down -v).
  Does NOT stop React Local Validation (exits-local-validation / 8091/8092/8095/5177).

.EXAMPLE
  .\tools\Stop-MauiLegacyLocalValidation.ps1
#>
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'MauiLocalValidation.stack.ps1')

function Write-Step([string]$Message) { Write-Host "[maui-local-validation] $Message" -ForegroundColor Cyan }
function Write-Ok([string]$Message) { Write-Host "[maui-local-validation] OK  $Message" -ForegroundColor Green }
function Write-Note([string]$Message) { Write-Host "[maui-local-validation] NOTE $Message" -ForegroundColor Yellow }

$repoRoot = Get-MauiLocalValidationRepoRoot
$envFile = Join-Path $repoRoot "deploy\docker\$($MauiLocalValidationStack.EnvFileName)"
if (-not (Test-Path -LiteralPath $envFile)) {
    $envFile = Join-Path $repoRoot "deploy\docker\$($MauiLocalValidationStack.EnvExampleFileName)"
}

Test-MauiLocalValidationDockerAvailable
$composeArgs = Get-MauiLocalValidationComposeArgs -RepoRoot $repoRoot -EnvFile $envFile

Write-Step "Stopping MAUI compose project $($MauiLocalValidationStack.ComposeProjectName) (volumes preserved)..."
Write-Note "React stack (exits-local-validation) is not touched. No docker compose down -v."
$exit = Invoke-MauiLocalValidationDocker -DockerArgs ($composeArgs + @('stop'))
if ($exit -ne 0) {
    # Fallback: stop by project name if env missing mid-flight
    $null = Invoke-MauiLocalValidationDocker -DockerArgs @(
        'compose', '-p', $MauiLocalValidationStack.ComposeProjectName, 'stop'
    )
}

Write-Ok "MAUI/Blazor Local Validation containers stopped. Volumes retained:"
Write-Host "  $($MauiLocalValidationStack.PlatformDbVolume)"
Write-Host "  $($MauiLocalValidationStack.PosDbVolume)"
