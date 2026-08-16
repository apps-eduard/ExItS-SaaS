#Requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('Platform', 'PinoyBusinessPos')]
    [string] $DatabaseKind,
    [Parameter(Mandatory)] [string] $ArtifactPath,
    [Parameter(Mandatory)] [string] $ManifestPath,
    [switch] $AllowDestructiveRestore,
    [string] $DestructiveConfirmation = '',
    [string] $ConnectionString = '',
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
    throw "Set $envName (or -ConnectionString)."
}
if ($AllowDestructiveRestore -and $DestructiveConfirmation -ne 'DESTROY_AND_RESTORE') {
    throw 'Destructive restore requires -DestructiveConfirmation DESTROY_AND_RESTORE.'
}
$cli = Join-Path $repoRoot 'tools\ExItS.BackupRestore.Cli\ExItS.BackupRestore.Cli.csproj'
& dotnet build $cli -c Release --nologo -v q | Out-Null
$argList = @('run', '--project', $cli, '-c', 'Release', '--no-build', '--', 'restore', $DatabaseKind, $ArtifactPath, $ManifestPath)
if ($AllowDestructiveRestore) { $argList += '--destructive' }
if ($DockerContainerId) { $argList += @('--docker-container', $DockerContainerId) }
& dotnet @argList
exit $LASTEXITCODE
