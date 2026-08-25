#Requires -Version 5.1
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Contract checks for Local Validation auth/Mailpit launcher configuration.
# Run: powershell -File tools/Test-LocalValidationAuthMailpitConfig.ps1

$repoRoot = Split-Path -Parent $PSScriptRoot
. (Join-Path $PSScriptRoot 'LocalValidation.stack.ps1')

$failures = [System.Collections.Generic.List[string]]::new()

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { [void]$failures.Add($Message) }
}

Assert-True ($LocalValidationStack.DefaultReactAdminPort -eq 8095) 'DefaultReactAdminPort must be 8095'
Assert-True ($LocalValidationStack.DefaultReactPosPort -eq 5177) 'DefaultReactPosPort must be 5177'

$resolved = Resolve-LocalValidationAuthPublicBaseUrl -EnvMap @{} -ResolvedPublicHost '' -ReactAdminPort 8095
Assert-True ($resolved -eq 'http://127.0.0.1:8095') "Default auth base expected http://127.0.0.1:8095, got $resolved"

$withEnv = Resolve-LocalValidationAuthPublicBaseUrl -EnvMap @{
    LOCAL_VALIDATION_REACT_ADMIN_ORIGIN = 'http://localhost:8095'
} -ResolvedPublicHost '' -ReactAdminPort 8095
Assert-True ($withEnv -eq 'http://localhost:8095') "Env override failed: $withEnv"

$withPublicHost = Resolve-LocalValidationAuthPublicBaseUrl -EnvMap @{
    LOCAL_VALIDATION_REACT_ADMIN_ORIGIN = 'http://127.0.0.1:8095'
} -ResolvedPublicHost '100.120.79.81' -ReactAdminPort 8095
Assert-True ($withPublicHost -eq 'http://100.120.79.81:8095') `
    "PublicHost must win over localhost REACT_ADMIN_ORIGIN for Tailscale Mailpit links, got $withPublicHost"

$cors = Add-LocalValidationReactCorsOrigins -CorsOrigins @('http://localhost:8090') -EnvMap @{} -ResolvedPublicHost ''
Assert-True ($cors -contains 'http://127.0.0.1:5177') 'CORS missing React POS 127.0.0.1:5177'
Assert-True ($cors -contains 'http://localhost:5177') 'CORS missing React POS localhost:5177'
Assert-True ($cors -contains 'http://127.0.0.1:8095') 'CORS missing React Admin 127.0.0.1:8095'
Assert-True ($cors -contains 'http://localhost:8095') 'CORS missing React Admin localhost:8095'

$startScript = Get-Content -LiteralPath (Join-Path $repoRoot 'tools\Start-LocalValidation.ps1') -Raw
Assert-True ($startScript -match 'PlatformEmail__AdminPublicBaseUrl = \$authPublicBaseUrl') `
    'Start-LocalValidation must set AdminPublicBaseUrl from $authPublicBaseUrl (React Admin), not Blazor $publicAdminUrl'
Assert-True ($startScript -match 'PlatformEmail__SmtpHost') 'Start-LocalValidation must configure SMTP host'
Assert-True ($startScript -match 'Resolve-LocalValidationAuthPublicBaseUrl') 'Start-LocalValidation must resolve auth public base'

$apiOnly = Get-Content -LiteralPath (Join-Path $repoRoot 'tools\Start-PlatformApiOnly.ps1') -Raw
Assert-True ($apiOnly -match 'PlatformEmail__AdminPublicBaseUrl') 'Start-PlatformApiOnly must set AdminPublicBaseUrl'
Assert-True ($apiOnly -match '8095') 'Start-PlatformApiOnly must target React Admin :8095'

$example = Get-Content -LiteralPath (Join-Path $repoRoot 'deploy\docker\.env.local-validation.example') -Raw
Assert-True ($example -match 'LOCAL_VALIDATION_REACT_ADMIN_ORIGIN') 'env example must document React Admin origin'
Assert-True ($example -match '8095') 'env example must mention port 8095'

if ($failures.Count -gt 0) {
    Write-Host 'FAIL Local Validation auth/Mailpit config contract:' -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    exit 1
}

Write-Host 'PASS Local Validation auth/Mailpit config contract (React Admin :8095 + Mailpit SMTP).' -ForegroundColor Green
exit 0
