#Requires -Version 5.1
<#
.SYNOPSIS
  Deletes all rows from catalog.global_products (and dependent composition rows) on the local Platform DB only.

.DESCRIPTION
  SAFETY:
  - Dot-sources tools/LocalValidation.stack.ps1
  - Requires ASPNETCORE_ENVIRONMENT or -Environment to be Development or LocalValidation
  - Allowlists database name exits_platform only; host 127.0.0.1 / localhost
  - Default -DryRun (use -Execute to delete)
  - Transaction with before/after counts
  - Does not touch POS DB or audit tables

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
    [string]$ConnectionString
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

$cs = $ConnectionString
if ([string]::IsNullOrWhiteSpace($cs)) {
    $cs = $env:ConnectionStrings__PlatformDatabase
}

if ([string]::IsNullOrWhiteSpace($cs)) {
    $port = $script:LocalValidationStack.DefaultPlatformDbPort
    $cs = "Host=127.0.0.1;Port=$port;Database=$($script:LocalValidationStack.PlatformDbName);Username=postgres;Password=postgres"
}

$summary = Get-LocalValidationConnectionSummary -ConnectionString $cs -Label 'Platform'
$allowedHosts = @('127.0.0.1', 'localhost', 'platform-db')
if ($summary.Host -notin $allowedHosts) {
    throw "Refusing reset: host '$($summary.Host)' is not allowlisted (127.0.0.1 / localhost / platform-db)."
}

if ($summary.Database -ne $script:LocalValidationStack.PlatformDbName) {
    throw "Refusing reset: database must be '$($script:LocalValidationStack.PlatformDbName)' (current: '$($summary.Database)')."
}

Write-Step "Environment=$effectiveEnv Host=$($summary.Host) Port=$($summary.Port) Database=$($summary.Database)"

Add-Type -AssemblyName 'Npgsql'
$conn = New-Object Npgsql.NpgsqlConnection($cs)
$conn.Open()
try {
    function Get-Count([string]$sql) {
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = $sql
        return [int]$cmd.ExecuteScalar()
    }

    $before = [ordered]@{
        catalog_template_products = Get-Count 'SELECT COUNT(*) FROM catalog.catalog_template_products'
        global_product_business_types = Get-Count 'SELECT COUNT(*) FROM catalog.global_product_business_types'
        global_products = Get-Count 'SELECT COUNT(*) FROM catalog.global_products'
    }

    Write-Step 'Counts before:'
    foreach ($pair in $before.GetEnumerator()) {
        Write-Host ("  {0}: {1}" -f $pair.Key, $pair.Value)
    }

    if (-not $Execute) {
        Write-Note 'No changes made (dry run).'
        return
    }

    $tx = $conn.BeginTransaction()
    try {
        $deleteSql = @'
DELETE FROM catalog.catalog_template_products;
DELETE FROM catalog.global_product_business_types;
DELETE FROM catalog.global_products;
'@
        $cmd = $conn.CreateCommand()
        $cmd.Transaction = $tx
        $cmd.CommandText = $deleteSql
        [void]$cmd.ExecuteNonQuery()
        $tx.Commit()
    }
    catch {
        $tx.Rollback()
        throw
    }

    $after = [ordered]@{
        catalog_template_products = Get-Count 'SELECT COUNT(*) FROM catalog.catalog_template_products'
        global_product_business_types = Get-Count 'SELECT COUNT(*) FROM catalog.global_product_business_types'
        global_products = Get-Count 'SELECT COUNT(*) FROM catalog.global_products'
    }

    Write-Step 'Counts after:'
    foreach ($pair in $after.GetEnumerator()) {
        Write-Host ("  {0}: {1}" -f $pair.Key, $pair.Value)
    }

    Write-Ok 'Global catalog products cleared. Reseed via Platform Admin or CSV import.'
    Write-Note 'Catalog templates remain; composition rows referencing products were removed.'
}
finally {
    $conn.Close()
    $conn.Dispose()
}
