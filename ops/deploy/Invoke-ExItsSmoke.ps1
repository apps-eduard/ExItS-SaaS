#Requires -Version 5.1
<#
.SYNOPSIS
  Repeatable smoke checks for ExItS Platform and PinoyBusinessPOS.
.DESCRIPTION
  HealthOnly: liveness/readiness (safe for StagingPilot).
  Full: includes Development/Testing identity-header business path probes — refused outside Dev/Testing.
  Phase: P9-WP05-pilot-and-deployment
#>
[CmdletBinding()]
param(
    [ValidateSet('HealthOnly', 'Full')]
    [string]$Mode = 'HealthOnly',
    [string]$PlatformBaseUrl = 'http://127.0.0.1:5288',
    [string]$PosBaseUrl = 'http://127.0.0.1:5290',
    [string]$AspNetEnvironment = ''
)

$ErrorActionPreference = 'Stop'

function Assert-Ok([string]$Name, [string]$Url, [hashtable]$Headers = @{}) {
    Write-Host "[smoke] $Name => $Url"
    $response = Invoke-WebRequest -Uri $Url -Headers $Headers -Method GET -TimeoutSec 15 -UseBasicParsing
    if ($response.StatusCode -lt 200 -or $response.StatusCode -ge 300) {
        throw "Smoke failed: $Name status=$($response.StatusCode)"
    }
}

function Get-StatusCode([string]$Url) {
    try {
        $response = Invoke-WebRequest -Uri $Url -Method GET -TimeoutSec 15 -UseBasicParsing
        return [int]$response.StatusCode
    }
    catch {
        if ($_.Exception.Response -and $_.Exception.Response.StatusCode) {
            return [int]$_.Exception.Response.StatusCode
        }
        throw
    }
}

Assert-Ok 'platform.health_liveness' "$PlatformBaseUrl/health"
Assert-Ok 'platform.health_readiness' "$PlatformBaseUrl/health/ready"
Assert-Ok 'platform.process_startup' "$PlatformBaseUrl/"
Assert-Ok 'pos.health_liveness' "$PosBaseUrl/health"
Assert-Ok 'pos.health_readiness' "$PosBaseUrl/health/ready"

if ($Mode -eq 'Full') {
    $envName = if ($AspNetEnvironment) { $AspNetEnvironment } else { $env:ASPNETCORE_ENVIRONMENT }
    if (@('Development', 'Testing') -notcontains $envName) {
        throw "Full smoke requires ASPNETCORE_ENVIRONMENT Development or Testing (got '$envName')."
    }

    Write-Host '[smoke] platform.denied_access_behavior (expect non-success without identity)'
    $deniedCode = Get-StatusCode "$PlatformBaseUrl/organizations"
    if ($deniedCode -lt 400) {
        throw 'Expected denied/unauthorized-style response for Platform organizations without identity.'
    }

    Write-Host '[smoke] pos.denied_without_commercial_headers (expect non-success)'
    $posDeniedCode = Get-StatusCode "$PosBaseUrl/catalog/products"
    if ($posDeniedCode -lt 400) {
        throw 'Expected denied response for POS catalog without commercial/identity headers.'
    }

    Write-Host '[smoke] Full mode: remaining contracts (cash sale, utang, inventory, expenses, idempotency) are covered by IntegrationTests on disposable databases — do not mutate real Production data.'
}

Write-Host '[smoke] OK'
exit 0
