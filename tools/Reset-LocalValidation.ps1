#Requires -Version 5.1
<#
.SYNOPSIS
  Destructive reset of Local Validation databases only, then reseed via Start-LocalValidation.

.DESCRIPTION
  SAFETY (all required):
  - Explicit -ConfirmReset switch (operator must opt in)
  - Rejects if ASPNETCORE_ENVIRONMENT / DOTNET_ENVIRONMENT is Production
  - Rejects Production-looking connection strings in the environment
  - Removes only named volumes: exits_local_validation_platform_db_data, exits_local_validation_pos_db_data
  - Never runs against packaging/production compose projects
  - Never uses automatic broad deletion on ordinary application startup

  Flow:
  1. Stop Local Validation apps (+ DB containers)
  2. Remove Local Validation Docker volumes only
  3. Optionally clear Admin DataProtection keys for Local Validation
  4. Start Local Validation (migrate + seed exact dataset)
  5. Verify seed-identities returns 8 approved identities

.EXAMPLE
  .\tools\Reset-LocalValidation.ps1 -ConfirmReset
#>
[CmdletBinding()]
param(
    [switch]$ConfirmReset,
    [switch]$SkipStart,
    [int]$VerifySeconds = 180
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Step([string]$Message) { Write-Host "[local-validation-reset] $Message" -ForegroundColor Cyan }
function Write-Ok([string]$Message) { Write-Host "[local-validation-reset] OK  $Message" -ForegroundColor Green }
function Write-Fail([string]$Message) { Write-Host "[local-validation-reset] FAIL $Message" -ForegroundColor Red }
function Write-Note([string]$Message) { Write-Host "[local-validation-reset] NOTE $Message" -ForegroundColor Yellow }

function Get-RepoRoot {
    $dir = (Resolve-Path -LiteralPath $PSScriptRoot).Path
    $probe = Get-Item -LiteralPath $dir
    while ($null -ne $probe) {
        if (Test-Path -LiteralPath (Join-Path $probe.FullName 'ExItS.slnx')) {
            return $probe.FullName
        }
        $probe = $probe.Parent
    }
    throw "Could not locate ExItS.slnx above $PSScriptRoot."
}

function Assert-NotProductionEnvironment {
    $candidates = @(
        [Environment]::GetEnvironmentVariable('ASPNETCORE_ENVIRONMENT'),
        [Environment]::GetEnvironmentVariable('DOTNET_ENVIRONMENT'),
        $env:ASPNETCORE_ENVIRONMENT,
        $env:DOTNET_ENVIRONMENT
    ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

    foreach ($value in $candidates) {
        if ([string]::Equals($value, 'Production', [StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing Local Validation reset: environment is Production ($value)."
        }
    }

    $connCandidates = @(
        [Environment]::GetEnvironmentVariable('ConnectionStrings__PlatformDatabase'),
        [Environment]::GetEnvironmentVariable('ConnectionStrings__PosDatabase'),
        $env:ConnectionStrings__PlatformDatabase,
        $env:ConnectionStrings__PosDatabase
    ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

    foreach ($cs in $connCandidates) {
        if ($cs -match '(?i)production|prod-|azure\.com|amazonaws\.com|cloud\.sql') {
            throw "Refusing Local Validation reset: connection string looks like Production ($cs)."
        }
        if ($cs -match '(?i)Host\s*=\s*(?!127\.0\.0\.1|localhost|platform-db|pos-db)[^\s;]+' `
            -and $cs -notmatch '(?i)15533|15534') {
            Write-Note "Unusual host in connection string; continuing only because ports/volumes are Local Validation scoped."
        }
    }
}

if (-not $ConfirmReset) {
    Write-Fail 'Refusing to run: pass -ConfirmReset to intentionally wipe Local Validation databases.'
    Write-Host @'
Usage:
  .\tools\Reset-LocalValidation.ps1 -ConfirmReset

This removes ONLY:
  - docker volume exits_local_validation_platform_db_data
  - docker volume exits_local_validation_pos_db_data

It does NOT touch Production, packaging volumes, or ordinary app startup.
'@
    exit 2
}

Assert-NotProductionEnvironment

$repoRoot = Get-RepoRoot
$dockerDir = Join-Path $repoRoot 'deploy\docker'
$envFile = Join-Path $dockerDir '.env.local-validation'
$composeFile = Join-Path $dockerDir 'compose.local-validation.yaml'
$stopScript = Join-Path $repoRoot 'tools\Stop-LocalValidation.ps1'
$startScript = Join-Path $repoRoot 'tools\Start-LocalValidation.ps1'
$volumePlatform = 'exits_local_validation_platform_db_data'
$volumePos = 'exits_local_validation_pos_db_data'

Write-Step "Repository: $repoRoot"
Write-Step 'Destructive Local Validation reset confirmed by operator (-ConfirmReset).'
Write-Step 'Target volumes: exits_local_validation_platform_db_data, exits_local_validation_pos_db_data'

if (-not (Test-Path -LiteralPath $envFile)) {
    throw "Missing $envFile — copy from .env.local-validation.example first."
}
if (-not (Test-Path -LiteralPath $composeFile)) {
    throw "Missing $composeFile"
}

Write-Step 'Stopping Local Validation apps and database containers...'
& $stopScript -StopDatabases
if ($LASTEXITCODE -ne 0) { throw "Stop-LocalValidation.ps1 failed ($LASTEXITCODE)." }

Write-Step 'Removing Local Validation Docker volumes only (explicit volume rm; not compose down -v)...'
foreach ($volume in @($volumePlatform, $volumePos)) {
    $exists = & docker volume inspect $volume 2>$null
    if ($LASTEXITCODE -eq 0) {
        & docker volume rm $volume
        if ($LASTEXITCODE -ne 0) { throw "Failed to remove volume $volume ($LASTEXITCODE)." }
        Write-Ok "Removed volume $volume"
    }
    else {
        Write-Note "Volume $volume already absent."
    }
}

$dpKeys = Join-Path $env:LOCALAPPDATA 'ExItS\LocalValidation\DataProtectionKeys'
if (Test-Path -LiteralPath $dpKeys) {
    Write-Step "Clearing Local Validation DataProtection keys: $dpKeys"
    Remove-Item -LiteralPath $dpKeys -Recurse -Force -ErrorAction SilentlyContinue
}

if ($SkipStart) {
    Write-Ok 'Volumes removed. Skipping Start (-SkipStart). Run .\tools\Start-LocalValidation.ps1 when ready.'
    exit 0
}

Write-Step 'Starting Local Validation (migrate + seed exact dataset)...'
& $startScript
if ($LASTEXITCODE -ne 0) { throw "Start-LocalValidation.ps1 failed ($LASTEXITCODE)." }

Write-Step "Verifying seed identities at http://localhost:8091 (up to $VerifySeconds s)..."
$deadline = [DateTime]::UtcNow.AddSeconds($VerifySeconds)
$verified = $false
while ([DateTime]::UtcNow -lt $deadline) {
    try {
        $response = Invoke-WebRequest -Uri 'http://localhost:8091/api/v1/platform/local-validation/seed-identities' -UseBasicParsing -TimeoutSec 5
        if ($response.StatusCode -eq 200) {
            $json = $response.Content | ConvertFrom-Json
            $items = @($json)
            if ($json.items) { $items = @($json.items) }
            if ($json.Count -and -not $json.items) { $items = @($json) }
            $count = @($items).Count
            if ($count -ge 8) {
                Write-Ok "Seed identities available: $count"
                $emails = @($items | ForEach-Object { $_.email })
                foreach ($required in @(
                        'olivia.mendoza@exits.local',
                        'rafael.torres@exits.local',
                        'maria.santos@exits.local',
                        'carlo.reyes@exits.local',
                        'ana.cruz@exits.local',
                        'daniel.garcia@exits.local',
                        'luis.navarro@exits.local',
                        'sofia.ramos@exits.local'
                    )) {
                    if (-not ($emails | Where-Object { $_ -eq $required })) {
                        throw "Missing required seed identity email: $required"
                    }
                }
                $verified = $true
                break
            }
            Write-Note "Seed identities not ready yet (count=$count)."
        }
    }
    catch {
        Write-Note "Waiting for Platform API seed endpoint: $($_.Exception.Message)"
    }
    Start-Sleep -Seconds 5
}

if (-not $verified) {
    Write-Fail 'Could not verify seed identities within timeout. Apps may still be starting — check Platform API logs.'
    exit 1
}

Write-Ok 'Local Validation reset and reseed completed.'
Write-Host 'Organizations: ABC Sari-Sari Store (abc-sari-sari), XYZ Mini Grocery (xyz-mini-grocery)'
Write-Host 'Identities: 2 Platform + 4 Organization + 2 Personal'
Write-Host 'Admin: http://localhost:8090/'
