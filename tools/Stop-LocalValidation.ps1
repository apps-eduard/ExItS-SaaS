#Requires -Version 5.1
<#
.SYNOPSIS
  Stops local Local Validation ExItS apps started by tools/Start-LocalValidation.ps1.

.DESCRIPTION
  - Stops only repo-scoped ExItS.Platform.Api / ExItS.PinoyBusinessPOS.Api / ExItS.Platform.Admin /
    ExItS.PinoyBusinessPOS.Web / ExItS.Personal.Web, launcher PowerShell windows recorded in launcher-state.json,
    and React POS Vite listeners on :5177.
  - Leaves PostgreSQL containers running by default.
  - -StopDatabases stops DB containers without deleting volumes (never compose down with -v).
  - Not Production.

.EXAMPLE
  .\tools\Stop-LocalValidation.ps1

.EXAMPLE
  .\tools\Stop-LocalValidation.ps1 -StopDatabases
#>
[CmdletBinding()]
param(
    [switch]$StopDatabases
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LocalValidation.stack.ps1')

function Write-Step([string]$Message) { Write-Host "[local-validation] $Message" -ForegroundColor Cyan }
function Write-Ok([string]$Message) { Write-Host "[local-validation] OK  $Message" -ForegroundColor Green }

function Get-RepoRoot {
    $dir = (Resolve-Path -LiteralPath $PSScriptRoot).Path
    $probe = Get-Item -LiteralPath $dir
    while ($null -ne $probe) {
        if (Test-Path -LiteralPath (Join-Path $probe.FullName 'ExItS.slnx')) {
            return $probe.FullName
        }
        $probe = $probe.Parent
    }
    throw "Could not locate ExItS.slnx above $PSScriptRoot."
}

function Get-RepoScopedAppProcesses([string]$RepoRoot) {
    return @(Get-LocalValidationRepoScopedAppProcesses -RepoRoot $RepoRoot)
}

$repoRoot = Get-LocalValidationRepoRoot
$stateDir = Join-Path $env:LOCALAPPDATA 'ExItS\LocalValidation'
$stateFile = Join-Path $stateDir 'launcher-state.json'
$dockerDir = Join-Path $repoRoot 'deploy\docker'
$envFile = Join-Path $dockerDir $LocalValidationStack.EnvFileName
$composeFile = Join-Path $dockerDir $LocalValidationStack.ComposeFileName

Write-Step "Repository: $repoRoot"

$stateMode = ''
if (Test-Path -LiteralPath $stateFile) {
    try {
        $state = Get-Content -LiteralPath $stateFile -Raw | ConvertFrom-Json
        $stateMode = [string]$state.Mode
        if ($stateMode -ne 'DockerApps') {
            foreach ($windowPid in @($state.WindowPids)) {
                if ($windowPid -and (Get-Process -Id $windowPid -ErrorAction SilentlyContinue)) {
                    Write-Step "Stopping launcher window PID $windowPid"
                    Stop-Process -Id $windowPid -Force -ErrorAction SilentlyContinue
                }
            }
        }
    } catch {
        Write-Step "Could not read $stateFile - continuing with process scan."
    }
}

$null = Stop-LocalValidationRepoScopedHostApps -RepoRoot $repoRoot

Write-Step 'Stopping React POS Vite listeners on :5177 (if any)...'
$null = Stop-LocalValidationPortListeners -Port ([int]$LocalValidationStack.DefaultReactPosPort) -Label 'React POS'

if ($stateMode -ne 'DockerApps' -and (Test-Path -LiteralPath $envFile) -and (Test-Path -LiteralPath $composeFile)) {
    Write-Step 'Stopping React Platform Admin container (volumes preserved)...'
    $null = Invoke-LocalValidationDocker -DockerArgs @(
        'compose', '-p', $LocalValidationStack.ComposeProjectName,
        '-f', $composeFile, '--env-file', $envFile,
        'stop', 'admin-web-react'
    )
}

if ($stateMode -ne 'DockerApps' -and (Test-Path -LiteralPath $stateFile)) {
    Remove-Item -LiteralPath $stateFile -Force -ErrorAction SilentlyContinue
}

Write-Ok 'Local Local Validation app processes stopped (DBs left running by default).'

if ($StopDatabases) {
    if (-not (Test-Path -LiteralPath $envFile)) {
        throw "Missing $envFile"
    }
    Write-Step 'Stopping local-validation database containers (volumes preserved; never compose down with -v)...'
    $stopExit = Invoke-LocalValidationDocker -DockerArgs @(
        'compose', '-p', $LocalValidationStack.ComposeProjectName,
        '-f', $composeFile, '--env-file', $envFile,
        'stop', 'platform-db', 'pos-db'
    )
    if ($stopExit -ne 0) { throw "docker compose stop failed ($stopExit)." }
    Write-Ok ("Database containers stopped. Volumes {0}, {1} remain." -f $LocalValidationStack.PlatformDbVolume, $LocalValidationStack.PosDbVolume)
}

Write-Host 'Restart: .\tools\Start-LocalValidation.ps1'
