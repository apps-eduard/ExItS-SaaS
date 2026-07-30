#Requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $ArtifactPath,
    [Parameter(Mandatory)] [string] $EncryptedOutputPath,
    [Parameter(Mandatory)] [string] $KeyFilePath
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
[Environment]::SetEnvironmentVariable('EXITS_BACKUP_KEY_FILE', $KeyFilePath, 'Process')
$cli = Join-Path $repoRoot 'tools\ExItS.BackupRestore.Cli\ExItS.BackupRestore.Cli.csproj'
& dotnet build $cli -c Release --nologo -v q | Out-Null
& dotnet run --project $cli -c Release --no-build -- encrypt $ArtifactPath $EncryptedOutputPath
exit $LASTEXITCODE
