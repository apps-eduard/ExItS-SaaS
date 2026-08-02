#Requires -Version 5.1
# Thin forwarder — preferred entrypoint is tools/Start-LocalValidation.ps1 from repo root.
$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
& (Join-Path $repoRoot 'tools\Start-LocalValidation.ps1') @args
exit $LASTEXITCODE
