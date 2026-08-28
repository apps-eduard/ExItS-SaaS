#Requires -Version 5.1
<#
.SYNOPSIS
  Stops POS API processes that lock ExItS.PinoyBusinessPOS.Api bin DLLs (MSB3027).

.DESCRIPTION
  Windows keeps Application.dll / Infrastructure.dll open while ExItS.PinoyBusinessPOS.Api.exe
  (or a repo-scoped dotnet host for that project) is running. Debug builds then fail with
  MSB3027/MSB3021 and Local Validation can leave inventory on a stale process.

  Called automatically from the POS Api Debug build. Safe no-op when nothing is locked.
  Prefer tools/Start-LocalValidation.ps1 for day-to-day API hosting (dotnet watch).

.EXAMPLE
  powershell -NoProfile -File .\tools\Unlock-PosApiBuild.ps1
#>
[CmdletBinding()]
param(
    [string]$RepoRoot = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
}

$rootNorm = $RepoRoot.Replace('/', '\').TrimEnd('\')
$marker = 'ExItS.PinoyBusinessPOS.Api'
$stopped = @()

function Test-PosApiLockCandidate([string]$Haystack) {
    if ([string]::IsNullOrWhiteSpace($Haystack)) { return $false }
    $norm = $Haystack.Replace('/', '\')
    if ($norm.IndexOf($rootNorm, [StringComparison]::OrdinalIgnoreCase) -lt 0) { return $false }
    return $norm.IndexOf($marker, [StringComparison]::OrdinalIgnoreCase) -ge 0
}

foreach ($process in Get-CimInstance Win32_Process -Filter "Name = '$marker.exe'" -ErrorAction SilentlyContinue) {
    $haystack = "{0}|{1}" -f [string]$process.CommandLine, [string]$process.ExecutablePath
    if (-not (Test-PosApiLockCandidate $haystack)) { continue }
    Write-Host "[unlock-pos-api] Stopping apphost PID $($process.ProcessId) (DLL lock prevention)"
    Stop-Process -Id $process.ProcessId -Force -ErrorAction SilentlyContinue
    $stopped += [int]$process.ProcessId
}

foreach ($process in Get-CimInstance Win32_Process -Filter "Name = 'dotnet.exe'" -ErrorAction SilentlyContinue) {
    $commandLine = [string]$process.CommandLine
    if (-not (Test-PosApiLockCandidate $commandLine)) { continue }
    # Only stop run/watch hosts for this project — not arbitrary dotnet test/build.
    if ($commandLine -notmatch '(?i)(\brun\b|\bwatch\b)') { continue }
    Write-Host "[unlock-pos-api] Stopping dotnet host PID $($process.ProcessId) (DLL lock prevention)"
    Stop-Process -Id $process.ProcessId -Force -ErrorAction SilentlyContinue
    $stopped += [int]$process.ProcessId
}

if ($stopped.Count -gt 0) {
    Start-Sleep -Milliseconds 750
}

exit 0
