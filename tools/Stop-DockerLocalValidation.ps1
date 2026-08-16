#Requires -Version 5.1
<#
.SYNOPSIS
  Stops Local Validation Docker application services while preserving volumes.

.PARAMETER StopInfrastructure
  Also stops Platform PostgreSQL, POS PostgreSQL, and Mailpit. Volumes are preserved.
#>
[CmdletBinding()]
param(
    [switch]$StopInfrastructure
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LocalValidation.stack.ps1')

function Write-Step([string]$Message) { Write-Host "[local-validation] $Message" -ForegroundColor Cyan }
function Write-Ok([string]$Message) { Write-Host "[local-validation] OK  $Message" -ForegroundColor Green }

$repoRoot = Get-LocalValidationRepoRoot
$dockerDir = Join-Path $repoRoot 'deploy\docker'
$envFile = Join-Path $dockerDir $LocalValidationStack.EnvFileName
$composeFile = Join-Path $dockerDir $LocalValidationStack.ComposeFileName
$stateFile = Join-Path $env:LOCALAPPDATA 'ExItS\LocalValidation\launcher-state.json'

if (-not (Test-Path -LiteralPath $envFile)) {
    throw "Missing $envFile"
}

Test-LocalValidationDockerAvailable
Write-Step 'Stopping Docker application services (infrastructure and volumes preserved)...'
$null = Stop-LocalValidationDockerAppServices -ComposeFile $composeFile -EnvFile $envFile
Write-Ok 'Docker application services stopped.'

if ($StopInfrastructure) {
    Write-Step 'Stopping PostgreSQL and Mailpit containers (volumes preserved)...'
    $args = @(
        'compose', '-p', $LocalValidationStack.ComposeProjectName,
        '-f', $composeFile, '--env-file', $envFile,
        'stop'
    ) + $LocalValidationStack.InfraComposeServices
    $exitCode = Invoke-LocalValidationDocker -DockerArgs $args
    if ($exitCode -ne 0) { throw "Docker infrastructure stop failed ($exitCode)." }
    Write-Ok 'Docker infrastructure stopped; database volumes remain.'
}

if (Test-Path -LiteralPath $stateFile) {
    try {
        $state = Get-Content -LiteralPath $stateFile -Raw | ConvertFrom-Json
        if ([string]$state.Mode -eq 'DockerApps') {
            Remove-Item -LiteralPath $stateFile -Force -ErrorAction SilentlyContinue
        }
    }
    catch { }
}

Write-Host 'Restart: .\tools\Start-DockerLocalValidation.ps1'
