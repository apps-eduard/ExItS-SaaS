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

# PublicHost must NOT force Mailpit links onto Tailscale (network exposure only).
$withPublicHost = Resolve-LocalValidationAuthPublicBaseUrl -EnvMap @{
    LOCAL_VALIDATION_REACT_ADMIN_ORIGIN = 'http://127.0.0.1:8095'
} -ResolvedPublicHost '100.120.79.81' -ReactAdminPort 8095
Assert-True ($withPublicHost -eq 'http://127.0.0.1:8095') `
    "PublicHost must NOT override local auth email base, got $withPublicHost"

$previousOverride = [string]$env:EXITS_ADMIN_PUBLIC_BASE_URL
try {
    $env:EXITS_ADMIN_PUBLIC_BASE_URL = 'http://100.120.79.81:8095'
    $explicit = Resolve-LocalValidationAuthPublicBaseUrl -EnvMap @{} -ResolvedPublicHost '' -ReactAdminPort 8095
    Assert-True ($explicit -eq 'http://100.120.79.81:8095') `
        "EXITS_ADMIN_PUBLIC_BASE_URL must win for intentional Tailscale Mailpit links, got $explicit"
}
finally {
    if ([string]::IsNullOrWhiteSpace($previousOverride)) {
        Remove-Item Env:EXITS_ADMIN_PUBLIC_BASE_URL -ErrorAction SilentlyContinue
    } else {
        $env:EXITS_ADMIN_PUBLIC_BASE_URL = $previousOverride
    }
}

$cors = Add-LocalValidationReactCorsOrigins -CorsOrigins @('http://localhost:8090') -EnvMap @{} -ResolvedPublicHost '100.120.79.81'
Assert-True ($cors -contains 'http://127.0.0.1:5177') 'CORS missing React POS 127.0.0.1:5177'
Assert-True ($cors -contains 'http://localhost:5177') 'CORS missing React POS localhost:5177'
Assert-True ($cors -contains 'http://127.0.0.1:8095') 'CORS missing React Admin 127.0.0.1:8095'
Assert-True ($cors -contains 'http://localhost:8095') 'CORS missing React Admin localhost:8095'
Assert-True ($cors -contains 'http://100.120.79.81:5177') 'CORS missing PublicHost React POS'
Assert-True ($cors -contains 'http://100.120.79.81:8095') 'CORS missing PublicHost React Admin'

$startScript = Get-Content -LiteralPath (Join-Path $repoRoot 'tools\Start-LocalValidation.ps1') -Raw
Assert-True ($startScript -match 'PlatformEmail__AdminPublicBaseUrl = \$authPublicBaseUrl') `
    'Start-LocalValidation must set AdminPublicBaseUrl from $authPublicBaseUrl (React Admin), not Blazor $publicAdminUrl'
Assert-True ($startScript -match 'PlatformEmail__SmtpHost') 'Start-LocalValidation must configure SMTP host'
Assert-True ($startScript -match 'Resolve-LocalValidationAuthPublicBaseUrl') 'Start-LocalValidation must resolve auth public base'

$apiOnly = Get-Content -LiteralPath (Join-Path $repoRoot 'tools\Start-PlatformApiOnly.ps1') -Raw
Assert-True ($apiOnly -match 'PlatformEmail__AdminPublicBaseUrl') 'Start-PlatformApiOnly must set AdminPublicBaseUrl'
Assert-True ($apiOnly -match '8095') 'Start-PlatformApiOnly must target React Admin :8095'
Assert-True ($apiOnly -notmatch 'elseif \(\$publicHost\)') 'Start-PlatformApiOnly must not force Mailpit links from PUBLIC_HOST'

$frontends = Get-Content -LiteralPath (Join-Path $repoRoot 'tools\Start-ReactFrontends.ps1') -Raw
Assert-True ($frontends -match 'PlatformWeb-local-access') 'Start-ReactFrontends must document local-access Admin default'
Assert-True ($frontends -match 'EXITS_REACT_ADMIN_WEB_PATH|-AdminWebPath') 'Start-ReactFrontends must support explicit Admin path'
Assert-True ($frontends -match 'same-origin') 'Start-ReactFrontends must document same-origin API routing'

$example = Get-Content -LiteralPath (Join-Path $repoRoot 'deploy\docker\.env.local-validation.example') -Raw
Assert-True ($example -match 'LOCAL_VALIDATION_REACT_ADMIN_ORIGIN') 'env example must document React Admin origin'
Assert-True ($example -match '8095') 'env example must mention port 8095'

if ($failures.Count -gt 0) {
    Write-Host 'FAIL Local Validation auth/Mailpit config contract:' -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    exit 1
}

Write-Host 'PASS Local Validation auth/Mailpit config contract (local Mailpit default; PublicHost = network only).' -ForegroundColor Green
exit 0
