#Requires -Version 7.0
<#
.SYNOPSIS
  Evaluate retention. Dry-run by default. Never deletes the latest complete backup.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $BackupDirectory,
    [switch] $ExecuteDeletes
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$cli = Join-Path $repoRoot 'tools\ExItS.BackupRestore.Cli\ExItS.BackupRestore.Cli.csproj'
& dotnet build $cli -c Release --nologo -v q | Out-Null
$argList = @('run', '--project', $cli, '-c', 'Release', '--no-build', '--', 'retention', $BackupDirectory)
if ($ExecuteDeletes) { $argList += '--execute' }
& dotnet @argList
exit $LASTEXITCODE
