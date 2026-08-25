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
  5. Verify seed-identities returns exactly Olivia + Rafael (both Platform Administrator)

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
. (Join-Path $PSScriptRoot 'LocalValidation.stack.ps1')

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

function Import-DotEnv([string]$Path) {
    $map = @{}
    Get-Content -LiteralPath $Path | ForEach-Object {
        $line = $_.Trim()
        if ($line.Length -eq 0 -or $line.StartsWith('#')) { return }
        $idx = $line.IndexOf('=')
        if ($idx -lt 1) { return }
        $key = $line.Substring(0, $idx).Trim()
        $value = $line.Substring($idx + 1).Trim()
        if (($value.StartsWith('"') -and $value.EndsWith('"')) -or ($value.StartsWith("'") -and $value.EndsWith("'"))) {
            $value = $value.Substring(1, $value.Length - 2)
        }
        $map[$key] = $value
    }
    return $map
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
$envFile = Join-Path $dockerDir $LocalValidationStack.EnvFileName
$composeFile = Join-Path $dockerDir $LocalValidationStack.ComposeFileName
$stopScript = Join-Path $repoRoot 'tools\Stop-LocalValidation.ps1'
$startScript = Join-Path $repoRoot 'tools\Start-LocalValidation.ps1'
$volumePlatform = $LocalValidationStack.PlatformDbVolume
$volumePos = $LocalValidationStack.PosDbVolume

Write-Step "Repository: $repoRoot"
Write-Step 'Destructive Local Validation reset confirmed by operator (-ConfirmReset).'
Write-Step ("Compose project={0}; volumes={1}, {2}" -f $LocalValidationStack.ComposeProjectName, $volumePlatform, $volumePos)

if (-not (Test-Path -LiteralPath $envFile)) {
    throw "Missing $envFile - copy from .env.local-validation.example first."
}
if (-not (Test-Path -LiteralPath $composeFile)) {
    throw "Missing $composeFile"
}

$envMap = Import-DotEnv $envFile

# Start-LocalValidation injects LocalValidation__Enabled=true into app windows; dotenv may omit it.
# Require the shared password secret as proof this is a Local Validation operator env file.
if (-not $envMap.ContainsKey('LOCAL_VALIDATION_SHARED_PASSWORD') `
    -or [string]::IsNullOrWhiteSpace([string]$envMap['LOCAL_VALIDATION_SHARED_PASSWORD']) `
    -or [string]$envMap['LOCAL_VALIDATION_SHARED_PASSWORD'] -like 'REPLACE_*') {
    throw 'deploy/docker/.env.local-validation must set LOCAL_VALIDATION_SHARED_PASSWORD (not REPLACE_*).'
}

$lvEnabled = $false
foreach ($key in @('LocalValidation__Enabled', 'LOCAL_VALIDATION')) {
    if ($envMap.ContainsKey($key) -and [string]::Equals([string]$envMap[$key], 'true', [StringComparison]::OrdinalIgnoreCase)) {
        $lvEnabled = $true
        break
    }
}
if (-not $lvEnabled) {
    Write-Note 'Dotenv omits LocalValidation__Enabled; Start-LocalValidation will set LocalValidation__Enabled=true for app windows.'
}

Write-Note 'Catalog/plans/features/roles are recreated by Platform migrate+seed after volume wipe.'
Write-Note ("POS product DB resets via volume wipe of {0} (product-owned container migrate on start)." -f $volumePos)

Write-Step 'Stopping Local Validation apps and database containers...'
$previousEap = $ErrorActionPreference
$ErrorActionPreference = 'Continue'
& $stopScript -StopDatabases
$stopExit = $LASTEXITCODE
$ErrorActionPreference = $previousEap
if ($stopExit -ne 0) { throw "Stop-LocalValidation.ps1 failed ($stopExit)." }

Write-Step 'Removing Local Validation DB containers so volumes can be deleted...'
$rmExit = Invoke-LocalValidationDocker -DockerArgs @(
    'compose', '-p', $LocalValidationStack.ComposeProjectName,
    '-f', $composeFile, '--env-file', $envFile,
    'rm', '-f', '-s', '-v', 'platform-db', 'pos-db'
)
# Note: compose rm -v removes anonymous volumes only; named volumes are removed explicitly below.
if ($rmExit -ne 0) {
    Write-Note "compose rm returned $rmExit - continuing with explicit container/volume cleanup."
}
foreach ($name in @($LocalValidationStack.PlatformDbContainer, $LocalValidationStack.PosDbContainer)) {
    Invoke-LocalValidationDocker -DockerArgs @('rm', '-f', $name) | Out-Null
}

Write-Step 'Removing Local Validation Docker volumes only (explicit volume rm; not compose down -v)...'
foreach ($volume in @($volumePlatform, $volumePos)) {
    $inspectExit = Invoke-LocalValidationDocker -DockerArgs @('volume', 'inspect', $volume)
    if ($inspectExit -eq 0) {
        $volRm = Invoke-LocalValidationDocker -DockerArgs @('volume', 'rm', $volume)
        if ($volRm -ne 0) { throw "Failed to remove volume $volume ($volRm)." }
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

Write-Step 'Starting Local Validation (migrate + seed PlatformAdministratorsOnly + PurgeTransactional)...'
Remove-Item Env:LocalValidation__SeedScope -ErrorAction SilentlyContinue
& $startScript -SeedScope PlatformAdministratorsOnly -PurgeTransactional
if ($LASTEXITCODE -ne 0) { throw "Start-LocalValidation.ps1 failed ($LASTEXITCODE)." }

Write-Step "Verifying seed identities at http://localhost:8091 (up to $VerifySeconds s)..."
$deadline = [DateTime]::UtcNow.AddSeconds($VerifySeconds)
$verified = $false
while ([DateTime]::UtcNow -lt $deadline) {
    try {
        $response = Invoke-WebRequest -Uri 'http://localhost:8091/api/v1/platform/local-validation/seed-identities' -UseBasicParsing -TimeoutSec 5
        if ($response.StatusCode -eq 200) {
            $json = $response.Content | ConvertFrom-Json
            # Endpoint returns a JSON array (not { items: [...] }). Avoid $json.items —
            # with $ErrorActionPreference Stop that throws on arrays.
            $items = @(
                if ($json -is [System.Array]) { $json }
                elseif ($null -ne $json -and ($json.PSObject.Properties.Name -contains 'items')) { @($json.items) }
                elseif ($null -ne $json) { @($json) }
                else { @() }
            )
            $count = @($items).Count
            if ($count -eq 2) {
                Write-Ok "Seed identities available: $count"
                $emails = @($items | ForEach-Object { $_.email })
                foreach ($required in @(
                        'olivia.mendoza@exits.local',
                        'rafael.torres@exits.local'
                    )) {
                    if (-not ($emails | Where-Object { $_ -eq $required })) {
                        throw "Missing required seed identity email: $required"
                    }
                }
                $verified = $true
                break
            }
            Write-Note "Seed identities not ready yet (count=$count; expected 2 for PlatformAdministratorsOnly)."
        }
    }
    catch {
        Write-Note "Waiting for Platform API seed endpoint: $($_.Exception.Message)"
    }
    Start-Sleep -Seconds 5
}

if (-not $verified) {
    Write-Fail 'Could not verify seed identities within timeout. Apps may still be starting - check Platform API logs.'
    exit 1
}

Write-Ok 'Local Validation reset and reseed completed.'
Write-Host 'Seed scope: PlatformAdministratorsOnly'
Write-Host 'Baseline: Olivia Mendoza + Rafael Torres (both Platform Administrator)'
Write-Host 'Quick Login: exactly those 2 Platform accounts'
Write-Host 'Transactional orgs/customers/subscriptions/payments: cleared'
Write-Host 'Catalog/plans/features/built-in roles: retained via migrate+seed'
Write-Host 'Admin (Blazor): http://localhost:8090/'
Write-Host 'React Admin (activate/reset): http://127.0.0.1:8095/'
Write-Host 'React POS: http://127.0.0.1:5177/'
Write-Host 'Platform API: http://localhost:8091/'
Write-Host 'POS API: http://localhost:8092/'
Write-Host 'Mailpit: http://localhost:8025/'
Write-Host 'Auth emails use React Admin :8095 (PlatformEmail__AdminPublicBaseUrl). Start React Admin Vite if activation links must open.'
