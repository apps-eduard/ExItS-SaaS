#Requires -Version 5.1
<#
.SYNOPSIS
  Runs the PA-COM-07 joined Platform→POS Playwright integration against a live mixed-worktree stack.
#>
[CmdletBinding()]
param(
    [string]$AdminWebPath = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "LocalValidation.stack.ps1")
$platformRepoRoot = Get-LocalValidationRepoRoot -StartPath $PSScriptRoot
if (-not $AdminWebPath) {
    $AdminWebPath = Join-Path $platformRepoRoot "src\Platform\ExItS.Platform.Admin.Web"
}

$provenancePath = Join-Path $env:LOCALAPPDATA "ExItS\LocalValidation\pa-com-07-provenance.json"
if (-not (Test-Path -LiteralPath $provenancePath)) {
    throw "Missing $provenancePath. Run .\tools\Start-PaCom07MixedValidation.ps1 first."
}

$provenance = Get-Content -LiteralPath $provenancePath -Raw | ConvertFrom-Json
$envFile = ""
if ($provenance.PSObject.Properties.Match("envFile").Count -gt 0) {
    $envFile = [string]$provenance.envFile
}
if ([string]::IsNullOrWhiteSpace($envFile)) {
    $candidate = Join-Path $platformRepoRoot "deploy\docker\$($LocalValidationStack.EnvFileName)"
    if (-not (Test-Path -LiteralPath $candidate)) {
        $candidate = "C:\Users\speed\Desktop\ExItS-SaaS\deploy\docker\$($LocalValidationStack.EnvFileName)"
    }
    $envFile = $candidate
}
if (Test-Path -LiteralPath $envFile) {
    $envMap = Import-LocalValidationDotEnv -Path $envFile
    if ($envMap.ContainsKey("LOCAL_VALIDATION_SHARED_PASSWORD")) {
        $env:LOCAL_VALIDATION_SHARED_PASSWORD = [string]$envMap["LOCAL_VALIDATION_SHARED_PASSWORD"]
    }
}
$env:PA_COM_07_JOINED = "1"
$env:PA_COM_07_ADMIN_BASE_URL = [string]$provenance.reactAdminUrl
$env:PA_COM_07_PLATFORM_API_URL = [string]$provenance.platformApiUrl
$env:PA_COM_07_POS_API_URL = [string]$provenance.posApiUrl
$env:PA_COM_07_PLATFORM_API_SHA = [string]$provenance.platformApiRuntimeSha
$env:PA_COM_07_POS_API_SHA = [string]$provenance.posApiRuntimeSha
$env:PA_COM_07_PLATFORM_ADMIN_SHA = [string]$provenance.platformAdminRuntimeSha

Push-Location $AdminWebPath
try {
    npm run test:e2e:joined
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
finally {
    Pop-Location
}
