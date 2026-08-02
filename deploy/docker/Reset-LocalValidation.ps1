#Requires -Version 5.1
# Forwarder — prefer tools\Reset-LocalValidation.ps1 from repo root.
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
& (Join-Path $repoRoot 'tools\Reset-LocalValidation.ps1') @args
exit $LASTEXITCODE
