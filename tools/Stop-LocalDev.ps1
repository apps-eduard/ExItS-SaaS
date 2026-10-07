#Requires -Version 5.1
<#
.SYNOPSIS
  Stops fast local dev apps. Leaves the shared PostgreSQL containers and volumes running.

.EXAMPLE
  .\tools\Stop-LocalDev.ps1
#>
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LocalValidation.stack.ps1')
. (Join-Path $PSScriptRoot 'LocalDev.stack.ps1')

function Write-Step([string]$Message) { Write-Host "[local-dev] $Message" -ForegroundColor Cyan }
function Write-Ok([string]$Message) { Write-Host "[local-dev] OK  $Message" -ForegroundColor Green }
function Write-Note([string]$Message) { Write-Host "[local-dev] NOTE $Message" -ForegroundColor Yellow }

function Get-ListeningOwner([int]$Port) {
    $conns = @(Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue)
    if ($conns.Count -eq 0) { return $null }
    $procId = $conns[0].OwningProcess
    $proc = Get-Process -Id $procId -ErrorAction SilentlyContinue
    $cmd = $null
    try {
        $cmd = (Get-CimInstance Win32_Process -Filter "ProcessId = $procId" -ErrorAction SilentlyContinue).CommandLine
    } catch { }
    return [pscustomobject]@{
        Port = $Port
        ProcessId = $procId
        ProcessName = if ($proc) { $proc.ProcessName } else { '?' }
        CommandLine = $cmd
    }
}

$repoRoot = Get-LocalValidationRepoRoot
$repoNorm = $repoRoot.Replace('/', '\').TrimEnd('\')
$statePath = Join-Path (Join-Path $env:LOCALAPPDATA $LocalDevStack.StateDirectory) $LocalDevStack.StateFileName
Write-Step "Repository: $repoRoot"

if (Test-Path -LiteralPath $statePath) {
    $state = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json
    foreach ($windowPid in @($state.WindowPids)) {
        if (-not $windowPid) { continue }
        $proc = Get-Process -Id $windowPid -ErrorAction SilentlyContinue
        if (-not $proc) { continue }
        Write-Step "Stopping launcher window PID $windowPid"
        Stop-Process -Id $windowPid -Force -ErrorAction SilentlyContinue
    }
}

$ports = @(
    [int]$LocalDevStack.PlatformApiPort,
    [int]$LocalDevStack.PosApiPort,
    [int]$LocalDevStack.ReactPosPort,
    [int]$LocalDevStack.AdminPort,
    [int]$LocalDevStack.LoanPort
)
$blocked = @()
foreach ($port in $ports) {
    $owner = Get-ListeningOwner -Port $port
    if ($null -eq $owner) { continue }
    $cmd = [string]$owner.CommandLine
    $repoOwned = -not [string]::IsNullOrWhiteSpace($cmd) -and $cmd.Replace('/', '\').IndexOf($repoNorm, [StringComparison]::OrdinalIgnoreCase) -ge 0
    if (-not $repoOwned) {
        $blocked += $owner
        Write-Note ("Port {0} is held by unrelated {1} (PID {2}). Not stopping it." -f $port, $owner.ProcessName, $owner.ProcessId)
        continue
    }
    Write-Step ("Stopping {0} PID {1} on port {2}" -f $owner.ProcessName, $owner.ProcessId, $port)
    Stop-Process -Id $owner.ProcessId -Force -ErrorAction SilentlyContinue
}

if (Test-Path -LiteralPath $statePath) {
    Remove-Item -LiteralPath $statePath -Force
}

if ($blocked.Count -gt 0) {
    throw 'One or more fast-dev ports are still held by unrelated processes. PostgreSQL was left running.'
}

Write-Ok 'Fast dev apps stopped. Shared PostgreSQL containers and volumes were left running.'
Write-Host 'Start production-like validation with .\tools\Start-DockerLocalValidation.ps1 when image validation is required.'
