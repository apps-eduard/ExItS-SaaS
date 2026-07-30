#Requires -Version 5.1
<#
.SYNOPSIS
  Pre-deployment backup create + verify for Platform and POS (blocks deploy on failure).
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$OutputDir,
    [string]$EnvironmentClass = 'StagingPilot'
)

$ErrorActionPreference = 'Stop'
$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')

if (-not $env:EXITS_PLATFORM_DATABASE) { throw 'EXITS_PLATFORM_DATABASE is required.' }
if (-not $env:EXITS_POS_DATABASE) { throw 'EXITS_POS_DATABASE is required.' }

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
$backupCli = Join-Path $RepoRoot 'tools\ExItS.BackupRestore.Cli\ExItS.BackupRestore.Cli.csproj'
$commit = (git -C $RepoRoot rev-parse HEAD).Trim()

Write-Host '[pre-deploy-backup] Platform backup...'
& dotnet run --project $backupCli -c Release -- backup Platform $OutputDir $EnvironmentClass --commit $commit
if ($LASTEXITCODE -ne 0) { throw 'Platform backup failed.' }

Write-Host '[pre-deploy-backup] POS backup...'
& dotnet run --project $backupCli -c Release -- backup PinoyBusinessPos $OutputDir $EnvironmentClass --commit $commit
if ($LASTEXITCODE -ne 0) { throw 'POS backup failed.' }

Write-Host '[pre-deploy-backup] Complete. Run Verify-ExItsBackup.ps1 on each artifact, then export:'
Write-Host '  EXITS_PLATFORM_BACKUP_VERIFIED=true'
Write-Host '  EXITS_POS_BACKUP_VERIFIED=true'
Write-Host '  EXITS_PLATFORM_BACKUP_SET=<id>'
Write-Host '  EXITS_POS_BACKUP_SET=<id>'
Write-Host '  EXITS_BACKUP_VERIFIED=true'
exit 0
