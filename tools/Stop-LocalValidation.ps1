#Requires -Version 5.1
<#
.SYNOPSIS
  Stops local Local Validation ExItS apps started by tools/Start-LocalValidation.ps1.

.DESCRIPTION
  - Stops only repo-scoped ExItS.Platform.Api / ExItS.PinoyBusinessPOS.Api / ExItS.Platform.Admin
    and launcher PowerShell windows recorded in launcher-state.json.
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
    $markers = @(
        'ExItS.Platform.Api',
        'ExItS.PinoyBusinessPOS.Api',
        'ExItS.Platform.Admin'
    )
    $rootNorm = $RepoRoot.Replace('/', '\').TrimEnd('\')
    $results = @()
    foreach ($p in Get-CimInstance Win32_Process -Filter "Name = 'dotnet.exe'" -ErrorAction SilentlyContinue) {
        $cmd = [string]$p.CommandLine
        if ([string]::IsNullOrWhiteSpace($cmd)) { continue }
        if ($cmd.IndexOf($rootNorm, [StringComparison]::OrdinalIgnoreCase) -lt 0) { continue }
        foreach ($m in $markers) {
            if ($cmd.IndexOf($m, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
                $results += $p
                break
            }
        }
    }
    return $results
}

$repoRoot = Get-RepoRoot
$stateDir = Join-Path $env:LOCALAPPDATA 'ExItS\LocalValidation'
$stateFile = Join-Path $stateDir 'launcher-state.json'
$dockerDir = Join-Path $repoRoot 'deploy\docker'
$envFile = Join-Path $dockerDir $LocalValidationStack.EnvFileName
$composeFile = Join-Path $dockerDir $LocalValidationStack.ComposeFileName

Write-Step "Repository: $repoRoot"

if (Test-Path -LiteralPath $stateFile) {
    try {
        $state = Get-Content -LiteralPath $stateFile -Raw | ConvertFrom-Json
        foreach ($windowPid in @($state.WindowPids)) {
            if ($windowPid -and (Get-Process -Id $windowPid -ErrorAction SilentlyContinue)) {
                Write-Step "Stopping launcher window PID $windowPid"
                Stop-Process -Id $windowPid -Force -ErrorAction SilentlyContinue
            }
        }
    } catch {
        Write-Step "Could not read $stateFile - continuing with process scan."
    }
}

$apps = @(Get-RepoScopedAppProcesses -RepoRoot $repoRoot)
foreach ($p in $apps) {
    Write-Step ("Stopping app PID {0}" -f $p.ProcessId)
    Stop-Process -Id $p.ProcessId -Force -ErrorAction SilentlyContinue
}

if (Test-Path -LiteralPath $stateFile) {
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
