#Requires -Version 5.1
# Thin forwarder — preferred entrypoint is tools/Stop-LocalValidation.ps1 from repo root.
$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
& (Join-Path $repoRoot 'tools\Stop-LocalValidation.ps1') @args
exit $LASTEXITCODE
