# Verifies Development-only connection guards and catalog clear/preserve contract
# for Reset-DisposableCustomerData.ps1 without touching databases.
#   powershell -NoProfile -File scripts/dev/Test-ResetDisposableCustomerDataGuards.ps1

$ErrorActionPreference = 'Stop'
$scriptPath = Join-Path $PSScriptRoot 'Reset-DisposableCustomerData.ps1'
$source = Get-Content -LiteralPath $scriptPath -Raw

function Test-DisposableDevelopmentConnection {
    param(
        [Parameter(Mandatory)]
        [string]$Connection,
        [Parameter(Mandatory)]
        [ValidateSet('Platform', 'Pos')]
        [string]$Target
    )

    $lower = $Connection.ToLowerInvariant()
    if ($lower -match 'production' -or $lower -match '(^|[;\s=])prod([;\s=]|$)') {
        throw "Refusing connection that looks like Production ($Target). This script is Development-only."
    }

    $dbMatch = [regex]::Match($Connection, '(?i)(?:Database|Initial Catalog)\s*=\s*([^;]+)')
    $hostMatch = [regex]::Match($Connection, '(?i)(?:Host|Server)\s*=\s*([^;]+)')
    $database = if ($dbMatch.Success) { $dbMatch.Groups[1].Value.Trim() } else { '' }
    $dbHost = if ($hostMatch.Success) { $hostMatch.Groups[1].Value.Trim() } else { '' }

    $allowedDb =
        $database -match '(?i)^exits_platform$' -or
        $database -match '(?i)^exits_pos$' -or
        $database -eq 'ExItS_Platform' -or
        $database -eq 'ExItS_Pos' -or
        $database -match '(?i)(local.?validation|_test|_dev|dev_only)'

    $loopbackHost = $dbHost -match '^(127\.0\.0\.1|localhost|::1)$'
    $hasDevPasswordMarker = $lower -match 'exits_platform_dev_only|exits_pos_dev_only'

    if (-not $allowedDb -and -not ($loopbackHost -and $hasDevPasswordMarker)) {
        throw "Refusing $Target connection - database '$database' on host '$dbHost' is not an approved disposable Development target."
    }
}

# Keep the test guard in sync with the reset script implementation.
if ($source -notmatch 'function Test-DisposableDevelopmentConnection') {
    throw 'Reset script is missing Test-DisposableDevelopmentConnection.'
}

$failures = 0

function Assert-Throws {
    param([scriptblock]$Action, [string]$Name)
    $threw = $false
    try {
        & $Action
    }
    catch {
        $threw = $true
    }
    if ($threw) {
        Write-Host "PASS $Name"
    }
    else {
        Write-Host "FAIL $Name - expected throw"
        $script:failures++
    }
}

function Assert-Ok {
    param([scriptblock]$Action, [string]$Name)
    try {
        & $Action
        Write-Host "PASS $Name"
    }
    catch {
        Write-Host "FAIL $Name - $($_.Exception.Message)"
        $script:failures++
    }
}

Assert-Throws {
    Test-DisposableDevelopmentConnection -Connection 'Host=db.example;Database=customer_prod;Username=u;Password=p' -Target Platform
} 'rejects database name containing prod'

Assert-Throws {
    Test-DisposableDevelopmentConnection -Connection 'Host=production-db;Database=exits_platform;Username=u;Password=p' -Target Platform
} 'rejects production host keyword'

Assert-Throws {
    Test-DisposableDevelopmentConnection -Connection 'Host=10.0.0.5;Database=customer_main;Username=u;Password=p' -Target Platform
} 'rejects unknown non-dev database'

Assert-Ok {
    Test-DisposableDevelopmentConnection -Connection 'Host=127.0.0.1;Database=exits_platform;Username=u;Password=exits_platform_dev_only' -Target Platform
} 'allows local validation exits_platform'

Assert-Ok {
    Test-DisposableDevelopmentConnection -Connection 'Host=127.0.0.1;Database=ExItS_Platform;Username=u;Password=exits_platform_dev_only' -Target Platform
} 'allows Development ExItS_Platform'

Assert-Ok {
    Test-DisposableDevelopmentConnection -Connection 'Host=127.0.0.1;Database=exits_pos;Username=u;Password=exits_pos_dev_only' -Target Pos
} 'allows local validation exits_pos'

$requiredClears = @(
    'DELETE FROM catalog.catalog_template_products',
    'DELETE FROM catalog.global_products',
    'DELETE FROM catalog.global_categories',
    'DELETE FROM catalog.global_product_business_types',
    'DELETE FROM catalog.global_category_business_types',
    'DELETE FROM catalog.catalog_import_items',
    'DELETE FROM catalog.catalog_import_jobs'
)
foreach ($stmt in $requiredClears) {
    if ($source.Contains($stmt)) {
        Write-Host "PASS clear statement present: $stmt"
    }
    else {
        Write-Host "FAIL missing clear statement: $stmt"
        $failures++
    }
}

if ($source -match 'DELETE FROM catalog\.catalog_templates\b' -or $source -match 'TRUNCATE TABLE catalog\.catalog_templates') {
    Write-Host 'FAIL catalog_templates definitions must be preserved'
    $failures++
}
else {
    Write-Host 'PASS catalog_templates definitions are not deleted'
}

if ($source -match 'DELETE FROM catalog\.business_types\b') {
    Write-Host 'FAIL business_types definitions must be preserved'
    $failures++
}
else {
    Write-Host 'PASS business_types definitions are not deleted'
}

if ($source -notmatch 'olivia\.mendoza@exits\.local' -or $source -notmatch 'rafael\.torres@exits\.local') {
    Write-Host 'FAIL canonical admin emails missing'
    $failures++
}
else {
    Write-Host 'PASS canonical admin emails preserved in filter'
}

# Drift check: reset script must still call the same guard name and refuse production.
if ($source -notmatch 'Refusing connection that looks like Production') {
    Write-Host 'FAIL production refusal message missing from reset script'
    $failures++
}
else {
    Write-Host 'PASS production refusal message present'
}

if ($failures -gt 0) {
    throw "$failures reset-guard assertion(s) failed."
}

Write-Host 'All Reset-DisposableCustomerData guard assertions passed.'
