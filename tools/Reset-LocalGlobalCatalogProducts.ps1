#Requires -Version 5.1
<#
.SYNOPSIS
  Deletes all rows from catalog.global_products (and dependent composition rows) on the local Platform DB only.

.DESCRIPTION
  SAFETY:
  - Dot-sources tools/LocalValidation.stack.ps1
  - Requires ASPNETCORE_ENVIRONMENT or -Environment to be Development or LocalValidation
  - Allowlists database name exits_platform only; host 127.0.0.1 / localhost / platform-db
  - Default is dry-run (use -Execute to delete)
  - Before/after row-count report
  - Does not touch POS DB or audit tables
  - Uses docker exec + psql against the Local Validation platform DB container (no embedded Npgsql)

  Reseed global products via Platform Admin create/import (no product seed exists).

.EXAMPLE
  .\tools\Reset-LocalGlobalCatalogProducts.ps1
.EXAMPLE
  .\tools\Reset-LocalGlobalCatalogProducts.ps1 -Execute
#>
[CmdletBinding()]
param(
    [ValidateSet('Development', 'LocalValidation')]
    [string]$Environment,
    [switch]$Execute,
    [string]$ConnectionString,
    [string]$ContainerName
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LocalValidation.stack.ps1')

function Write-Step([string]$Message) { Write-Host "[global-catalog-reset] $Message" -ForegroundColor Cyan }
function Write-Ok([string]$Message) { Write-Host "[global-catalog-reset] OK  $Message" -ForegroundColor Green }
function Write-Note([string]$Message) { Write-Host "[global-catalog-reset] NOTE $Message" -ForegroundColor Yellow }

$effectiveEnv = if (-not [string]::IsNullOrWhiteSpace($Environment)) {
    $Environment
}
else {
    $env:ASPNETCORE_ENVIRONMENT
}

if ([string]::IsNullOrWhiteSpace($effectiveEnv)) {
    $effectiveEnv = $env:DOTNET_ENVIRONMENT
}

if ($effectiveEnv -notin @('Development', 'LocalValidation')) {
    throw "Refusing reset: environment must be Development or LocalValidation (current: '$effectiveEnv'). Pass -Environment explicitly if needed."
}

if (-not $Execute) {
    Write-Note 'Dry run only. Pass -Execute to delete global catalog products.'
}

function Get-ConnectionPart([string]$ConnectionString, [string]$Key) {
    foreach ($part in ($ConnectionString -split ';')) {
        $kv = $part.Trim()
        if ($kv.Length -eq 0) { continue }
        $idx = $kv.IndexOf('=')
        if ($idx -lt 1) { continue }
        $k = $kv.Substring(0, $idx).Trim()
        if ($k -eq $Key -or ($Key -eq 'Username' -and $k -in @('Username', 'User ID', 'User'))) {
            return $kv.Substring($idx + 1).Trim()
        }
    }
    return $null
}

$cs = $ConnectionString
if ([string]::IsNullOrWhiteSpace($cs)) {
    $cs = $env:ConnectionStrings__PlatformDatabase
}

if ([string]::IsNullOrWhiteSpace($cs)) {
    $envFile = Join-Path (Split-Path $PSScriptRoot -Parent) 'deploy\docker\.env.local-validation'
    if (-not (Test-Path -LiteralPath $envFile)) {
        throw "Refusing reset: no ConnectionStrings__PlatformDatabase and missing $envFile. Start Local Validation or pass -ConnectionString."
    }

    $envMap = @{}
    Get-Content -LiteralPath $envFile | ForEach-Object {
        $line = $_.Trim()
        if ($line.Length -eq 0 -or $line.StartsWith('#')) { return }
        $eq = $line.IndexOf('=')
        if ($eq -lt 1) { return }
        $envMap[$line.Substring(0, $eq).Trim()] = $line.Substring($eq + 1).Trim()
    }

    $user = $envMap['LOCAL_VALIDATION_PLATFORM_DB_USER']
    $pass = $envMap['LOCAL_VALIDATION_PLATFORM_DB_PASSWORD']
    $port = if ($envMap.ContainsKey('LOCAL_VALIDATION_PLATFORM_DB_HOST_PORT')) {
        $envMap['LOCAL_VALIDATION_PLATFORM_DB_HOST_PORT']
    }
    else {
        $script:LocalValidationStack.DefaultPlatformDbPort
    }

    if ([string]::IsNullOrWhiteSpace($user) -or [string]::IsNullOrWhiteSpace($pass)) {
        throw 'Refusing reset: LOCAL_VALIDATION_PLATFORM_DB_USER/PASSWORD missing from .env.local-validation.'
    }

    $cs = "Host=127.0.0.1;Port=$port;Database=$($script:LocalValidationStack.PlatformDbName);Username=$user;Password=$pass"
}

$summary = Get-LocalValidationConnectionSummary -ConnectionString $cs -Label 'Platform'
$dbUser = Get-ConnectionPart -ConnectionString $cs -Key 'Username'
if ([string]::IsNullOrWhiteSpace($dbUser)) {
    throw 'Refusing reset: connection string must include Username.'
}
$allowedHosts = @('127.0.0.1', 'localhost', 'platform-db')
if ($summary.Host -notin $allowedHosts) {
    throw "Refusing reset: host '$($summary.Host)' is not allowlisted (127.0.0.1 / localhost / platform-db)."
}

if ($summary.Database -ne $script:LocalValidationStack.PlatformDbName) {
    throw "Refusing reset: database must be '$($script:LocalValidationStack.PlatformDbName)' (current: '$($summary.Database)')."
}

$container = if (-not [string]::IsNullOrWhiteSpace($ContainerName)) {
    $ContainerName
}
else {
    $script:LocalValidationStack.PlatformDbContainer
}

Write-Step "Environment=$effectiveEnv Host=$($summary.Host) Port=$($summary.Port) Database=$($summary.Database) User=$dbUser Container=$container"
Write-Note 'WARNING: -Execute permanently deletes local global_products and dependent template composition rows.'

$running = (& docker inspect -f '{{.State.Running}}' $container 2>$null | Out-String).Trim()
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($running)) {
    throw "Refusing reset: Docker container '$container' was not found. Start Local Validation first."
}
if ($running -ne 'true') {
    throw "Refusing reset: Docker container '$container' is not running. Start Local Validation first."
}

function Invoke-PlatformSql([string]$Sql) {
    $output = & docker exec -i $container psql -U $dbUser -d $script:LocalValidationStack.PlatformDbName -v ON_ERROR_STOP=1 -t -A -c $Sql 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "psql failed: $output"
    }
    return ($output | Out-String).Trim()
}

function Get-Count([string]$Table) {
    $raw = Invoke-PlatformSql "SELECT COUNT(*) FROM catalog.$Table;"
    return [int]$raw
}

$before = [ordered]@{
    catalog_template_products     = Get-Count 'catalog_template_products'
    global_product_business_types = Get-Count 'global_product_business_types'
    global_products               = Get-Count 'global_products'
}

Write-Step 'Counts before:'
foreach ($pair in $before.GetEnumerator()) {
    Write-Host ("  {0}: {1}" -f $pair.Key, $pair.Value)
}

if (-not $Execute) {
    Write-Note 'No changes made (dry run).'
    Write-Ok 'Dry run complete.'
    return
}

Write-Step 'Deleting dependent composition rows then global_products inside a transaction...'
$deleteSql = @'
BEGIN;
DELETE FROM catalog.catalog_template_products;
DELETE FROM catalog.global_product_business_types;
DELETE FROM catalog.global_products;
COMMIT;
'@
$null = Invoke-PlatformSql $deleteSql

$after = [ordered]@{
    catalog_template_products     = Get-Count 'catalog_template_products'
    global_product_business_types = Get-Count 'global_product_business_types'
    global_products               = Get-Count 'global_products'
}

Write-Step 'Counts after:'
foreach ($pair in $after.GetEnumerator()) {
    Write-Host ("  {0}: {1}" -f $pair.Key, $pair.Value)
}

Write-Ok 'Local global catalog products reset complete. Recreate products via Admin UI or CSV import.'
Write-Note 'Schema and migrations preserved. Audit history was not deleted.'
