#Requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('Platform', 'PinoyBusinessPos')]
    [string] $DatabaseKind,
    [Parameter(Mandatory)] [string] $OutputDirectory,
    [string] $EnvironmentClassification = 'Development',
    [string] $ApplicationGitCommit = '',
    [string] $MigrationSchemaVersion = '',
    # Optional override; otherwise EXITS_PLATFORM_DATABASE / EXITS_POS_DATABASE.
    [string] $ConnectionString = '',
    # When set, run pg_dump inside this Docker container (preferred for Testcontainers / local-validation).
    [string] $DockerContainerId = ''
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$envName = if ($DatabaseKind -eq 'Platform') { 'EXITS_PLATFORM_DATABASE' } else { 'EXITS_POS_DATABASE' }
if (-not [string]::IsNullOrWhiteSpace($ConnectionString)) {
    [Environment]::SetEnvironmentVariable($envName, $ConnectionString, 'Process')
}
if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($envName))) {
    throw "Set $envName (or -ConnectionString) to a non-secret-host connection string. Do not commit it."
}
$cli = Join-Path $repoRoot 'tools\ExItS.BackupRestore.Cli\ExItS.BackupRestore.Cli.csproj'
& dotnet build $cli -c Release --nologo -v q | Out-Null
$argList = @('run', '--project', $cli, '-c', 'Release', '--no-build', '--', 'backup', $DatabaseKind, $OutputDirectory, $EnvironmentClassification)
if ($ApplicationGitCommit) { $argList += @('--commit', $ApplicationGitCommit) }
if ($MigrationSchemaVersion) { $argList += @('--migration', $MigrationSchemaVersion) }
if ($DockerContainerId) { $argList += @('--docker-container', $DockerContainerId) }
& dotnet @argList
exit $LASTEXITCODE
