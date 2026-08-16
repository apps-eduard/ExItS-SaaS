#Requires -Version 7.0
<#
.SYNOPSIS
  Operator checklist wrapper for a non-production recovery drill.
  Does not target Production. Does not auto-cutover traffic.

  For Phase 29 WP14 local-validation → disposable restore containers, prefer:
    .\ops\backup\Invoke-ExItsP29Wp14DevRecoveryDrill.ps1
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $PlatformConnectionString,
    [Parameter(Mandatory)] [string] $PosConnectionString,
    [Parameter(Mandatory)] [string] $OutputDirectory
)
$ErrorActionPreference = 'Stop'
Write-Host '=== ExItS Recovery Drill (manual disposable targets) ==='
Write-Host '1) Ensure targets are disposable empty databases (not Production).'
Write-Host '2) Backup Platform'
& "$PSScriptRoot\Backup-ExItsDatabase.ps1" -DatabaseKind Platform -ConnectionString $PlatformConnectionString -OutputDirectory $OutputDirectory -EnvironmentClassification Testing
Write-Host '3) Backup POS'
& "$PSScriptRoot\Backup-ExItsDatabase.ps1" -DatabaseKind PinoyBusinessPos -ConnectionString $PosConnectionString -OutputDirectory $OutputDirectory -EnvironmentClassification Testing
Write-Host '4) Verify each manifest/artifact with Verify-ExItsBackup.ps1'
Write-Host '5) Restore Platform then POS into approved empty DBs (destructive flag only if required)'
Write-Host '6) Run automated tests: dotnet test tests/ExItS.BackupRestore.Tests -c Release'
Write-Host '7) P29-WP14 development drill (local-validation → disposable containers): Invoke-ExItsP29Wp14DevRecoveryDrill.ps1'
Write-Host '8) Record timings, sizes, and validation results in the WP report'
Write-Host 'DRILL_CHECKLIST_PRINTED'
exit 0
