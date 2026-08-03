#Requires -Version 5.1
<#
.SYNOPSIS
  One-command local Local Validation: Docker DBs only + host Platform/POS/Admin.

.DESCRIPTION
  - Resolves repo root from this script location (run from any directory).
  - Starts only exits-local-validation platform-db + pos-db (never compose down with -v).
  - Stops stale ExItS.Platform.Api / ExItS.PinoyBusinessPOS.Api / ExItS.Platform.Admin
    processes that belong to this repository only.
  - Starts apps with dotnet watch in separate PowerShell windows, in order.
  - Uses deploy/docker/.env.local-validation (gitignored) - no secrets committed.
  - Admin DataProtection keys: %LOCALAPPDATA%\ExItS\LocalValidation\DataProtectionKeys
  - Sign in with approved Local Validation identities via the login dropdown (server-side normal
    Platform /auth/login) or manual credentials. Password from LOCAL_VALIDATION_SHARED_PASSWORD
    (never commit the secret; never exposed to the browser).
  - Not Production. Does not start P14-WP03.

.EXAMPLE
  .\tools\Start-LocalValidation.ps1
#>
[CmdletBinding()]
param(
    [int]$PortWaitSeconds = 120,
    [int]$DbHealthySeconds = 90,
    [ValidateSet('Full', 'PlatformAdministratorsOnly')]
    [string]$SeedScope = 'Full'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Step([string]$Message) { Write-Host "[local-validation] $Message" -ForegroundColor Cyan }
function Write-Ok([string]$Message) { Write-Host "[local-validation] OK  $Message" -ForegroundColor Green }
function Write-Fail([string]$Message) { Write-Host "[local-validation] FAIL $Message" -ForegroundColor Red }
function Write-Note([string]$Message) { Write-Host "[local-validation] NOTE $Message" -ForegroundColor Yellow }

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

function Require-EnvKey($Map, [string]$Key) {
    if (-not $Map.ContainsKey($Key) -or [string]::IsNullOrWhiteSpace([string]$Map[$Key]) -or ([string]$Map[$Key]).StartsWith('REPLACE_')) {
        throw "Set a real value for $Key in deploy/docker/.env.local-validation (not REPLACE_*)."
    }
}

function Test-DockerAvailable {
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
        throw 'Docker CLI not found. Install/start Docker Desktop.'
    }
    & docker info 1>$null 2>$null
    if ($LASTEXITCODE -ne 0) {
        throw 'Docker Desktop is not available (docker info failed). Start Docker Desktop and retry.'
    }
}

function Get-ListeningOwner([int]$Port) {
    $conns = @(Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue)
    if ($conns.Count -eq 0) { return $null }
    $procId = $conns[0].OwningProcess
    $proc = Get-Process -Id $procId -ErrorAction SilentlyContinue
    $cmd = $null
    try {
        $cmd = (Get-CimInstance Win32_Process -Filter "ProcessId = $procId" -ErrorAction SilentlyContinue).CommandLine
    } catch { }
    return [pscustomobject]@{
        Port = $Port
        ProcessId = $procId
        ProcessName = if ($proc) { $proc.ProcessName } else { '?' }
        CommandLine = $cmd
    }
}

function Test-TcpPortOpen([string]$HostName, [int]$Port, [int]$TimeoutMs = 800) {
    try {
        $client = New-Object System.Net.Sockets.TcpClient
        $iar = $client.BeginConnect($HostName, $Port, $null, $null)
        if (-not $iar.AsyncWaitHandle.WaitOne($TimeoutMs)) {
            $client.Close()
            return $false
        }
        $client.EndConnect($iar)
        $client.Close()
        return $true
    } catch {
        return $false
    }
}

function Wait-TcpPort([string]$Label, [string]$HostName, [int]$Port, [int]$TimeoutSeconds) {
    Write-Step "Waiting for $Label on ${HostName}:${Port} (up to ${TimeoutSeconds}s)..."
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if (Test-TcpPortOpen -HostName $HostName -Port $Port) {
            Write-Ok "$Label is listening on ${HostName}:${Port}"
            return
        }
        Start-Sleep -Milliseconds 750
    }
    $owner = Get-ListeningOwner -Port $Port
    if ($owner) {
        Write-Fail ("Port {0} occupied by PID {1} ({2})" -f $Port, $owner.ProcessId, $owner.ProcessName)
    }
    throw "$Label did not become ready on ${HostName}:${Port} within ${TimeoutSeconds}s."
}

function Get-RepoScopedAppProcesses([string]$RepoRoot) {
    $markers = @(
        'ExItS.Platform.Api',
        'ExItS.PinoyBusinessPOS.Api',
        'ExItS.Platform.Admin'
    )
    $rootNorm = $RepoRoot.Replace('/', '\').TrimEnd('\')
    $results = @()
    foreach ($p in Get-CimInstance Win32_Process -Filter "Name = 'dotnet.exe'" -ErrorAction SilentlyContinue) {
        $cmd = [string]$p.CommandLine
        if ([string]::IsNullOrWhiteSpace($cmd)) { continue }
        if ($cmd.IndexOf($rootNorm, [StringComparison]::OrdinalIgnoreCase) -lt 0) { continue }
        foreach ($m in $markers) {
            if ($cmd.IndexOf($m, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
                $results += $p
                break
            }
        }
    }
    return $results
}

function Stop-RepoScopedApps([string]$RepoRoot) {
    $procs = @(Get-RepoScopedAppProcesses -RepoRoot $RepoRoot)
    if ($procs.Count -eq 0) {
        Write-Step 'No stale repo-scoped ExItS app processes found.'
        return
    }
    foreach ($p in $procs) {
        $snippet = $p.CommandLine.Substring(0, [Math]::Min(120, $p.CommandLine.Length))
        Write-Step ("Stopping stale PID {0}: {1}" -f $p.ProcessId, $snippet)
        Stop-Process -Id $p.ProcessId -Force -ErrorAction SilentlyContinue
    }
    Start-Sleep -Seconds 1
}

function Report-PortConflicts([int[]]$Ports) {
    $blocked = @()
    foreach ($port in $Ports) {
        $owner = Get-ListeningOwner -Port $port
        if ($owner) {
            Write-Fail ("Port {0} in use by PID {1} ({2})" -f $owner.Port, $owner.ProcessId, $owner.ProcessName)
            if ($owner.CommandLine) { Write-Host "         $($owner.CommandLine)" }
            $blocked += $owner
        }
    }
    return $blocked
}

function Wait-ContainerHealthy([string]$Name, [int]$TimeoutSeconds) {
    Write-Step "Waiting for container $Name healthy (up to ${TimeoutSeconds}s)..."
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        $status = (& docker inspect -f '{{.State.Health.Status}}' $Name 2>$null)
        if ($status -eq 'healthy') {
            Write-Ok "$Name is healthy"
            return
        }
        Start-Sleep -Seconds 2
    }
    throw "Container $Name did not become healthy within ${TimeoutSeconds}s."
}

function Invoke-HttpCheck([string]$Label, [string]$Url) {
    try {
        $resp = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 10
        Write-Ok ("{0} -> HTTP {1} ({2})" -f $Label, [int]$resp.StatusCode, $Url)
        return $true
    } catch {
        Write-Fail ("{0} -> {1} ({2})" -f $Label, $_.Exception.Message, $Url)
        return $false
    }
}

function ConvertTo-EnvAssignments($EnvMap) {
    ($EnvMap.GetEnumerator() | ForEach-Object {
        $escaped = ([string]$_.Value) -replace "'", "''"
        "`$env:$($_.Key) = '$escaped'; "
    }) -join ''
}

function Start-AppWindow {
    param(
        [string]$Title,
        [string]$RepoRoot,
        [string]$Project,
        [hashtable]$EnvMap
    )
    $prefix = ConvertTo-EnvAssignments -EnvMap $EnvMap
    $run = @"
`$Host.UI.RawUI.WindowTitle = '$Title';
# Parent shells often leave DOTNET_ENVIRONMENT=Testing (integration tests) or Staging.
# Clear both so ASPNETCORE_ENVIRONMENT from EnvMap is authoritative for the child host.
Remove-Item Env:DOTNET_ENVIRONMENT -ErrorAction SilentlyContinue;
Remove-Item Env:ASPNETCORE_ENVIRONMENT -ErrorAction SilentlyContinue;
# Prevent a polluted parent shell from breaking MapStaticAssets package/_framework resolution.
Remove-Item Env:ReloadStaticAssetsAtRuntime -ErrorAction SilentlyContinue;
$prefix
# Keep DOTNET_ENVIRONMENT aligned with ASPNETCORE_ENVIRONMENT when the latter is set.
if (-not [string]::IsNullOrWhiteSpace(`$env:ASPNETCORE_ENVIRONMENT)) { `$env:DOTNET_ENVIRONMENT = `$env:ASPNETCORE_ENVIRONMENT }
Set-Location '$RepoRoot';
Write-Host ('=== {0} (ASPNETCORE_ENVIRONMENT={1}) ===' -f '$Title', `$env:ASPNETCORE_ENVIRONMENT) -ForegroundColor Cyan;
dotnet watch --project '$Project' run --no-launch-profile --non-interactive
"@
    $proc = Start-Process -FilePath 'powershell.exe' -PassThru -ArgumentList @(
        '-NoExit',
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        '-Command', $run
    )
    return $proc.Id
}

# --- main ---
$repoRoot = Get-RepoRoot
$dockerDir = Join-Path $repoRoot 'deploy\docker'
$envFile = Join-Path $dockerDir '.env.local-validation'
$composeFile = Join-Path $dockerDir 'compose.local-validation.yaml'
$stateDir = Join-Path $env:LOCALAPPDATA 'ExItS\LocalValidation'
$dpKeys = Join-Path $stateDir 'DataProtectionKeys'
$stateFile = Join-Path $stateDir 'launcher-state.json'

Write-Step "Repository: $repoRoot"
Test-DockerAvailable
Write-Ok 'Docker Desktop is available'

if (-not (Test-Path -LiteralPath $envFile)) {
    throw "Missing $envFile. Copy deploy/docker/.env.local-validation.example and fill REPLACE_* values."
}
if (-not (Test-Path -LiteralPath $composeFile)) {
    throw "Missing $composeFile"
}

$envMap = Import-DotEnv -Path $envFile
Require-EnvKey $envMap 'LOCAL_VALIDATION_PLATFORM_DB_USER'
Require-EnvKey $envMap 'LOCAL_VALIDATION_PLATFORM_DB_PASSWORD'
Require-EnvKey $envMap 'LOCAL_VALIDATION_POS_DB_USER'
Require-EnvKey $envMap 'LOCAL_VALIDATION_POS_DB_PASSWORD'
Require-EnvKey $envMap 'LOCAL_VALIDATION_SHARED_PASSWORD'

$platformDbPort = if ($envMap['LOCAL_VALIDATION_PLATFORM_DB_HOST_PORT']) { [int]$envMap['LOCAL_VALIDATION_PLATFORM_DB_HOST_PORT'] } else { 15533 }
$posDbPort = if ($envMap['LOCAL_VALIDATION_POS_DB_HOST_PORT']) { [int]$envMap['LOCAL_VALIDATION_POS_DB_HOST_PORT'] } else { 15534 }
$adminPort = if ($envMap['LOCAL_VALIDATION_ADMIN_HOST_PORT']) { [int]$envMap['LOCAL_VALIDATION_ADMIN_HOST_PORT'] } else { 8090 }
$platformApiPort = if ($envMap['LOCAL_VALIDATION_PLATFORM_API_HOST_PORT']) { [int]$envMap['LOCAL_VALIDATION_PLATFORM_API_HOST_PORT'] } else { 8091 }
$posApiPort = if ($envMap['LOCAL_VALIDATION_POS_API_HOST_PORT']) { [int]$envMap['LOCAL_VALIDATION_POS_API_HOST_PORT'] } else { 8092 }
$mailpitUiPort = if ($envMap['LOCAL_VALIDATION_MAILPIT_UI_HOST_PORT']) { [int]$envMap['LOCAL_VALIDATION_MAILPIT_UI_HOST_PORT'] } else { 8025 }
$mailpitSmtpPort = if ($envMap['LOCAL_VALIDATION_MAILPIT_SMTP_HOST_PORT']) { [int]$envMap['LOCAL_VALIDATION_MAILPIT_SMTP_HOST_PORT'] } else { 1025 }
$adminOrigin = if ($envMap['LOCAL_VALIDATION_ADMIN_ORIGIN']) { [string]$envMap['LOCAL_VALIDATION_ADMIN_ORIGIN'] } else { "http://localhost:$adminPort" }

New-Item -ItemType Directory -Force -Path $dpKeys | Out-Null
Write-Ok "DataProtection keys directory: $dpKeys"

Write-Step 'Stopping stale repo-scoped local ExItS apps (Api/Admin only; DBs untouched)...'
Stop-RepoScopedApps -RepoRoot $repoRoot

$conflicts = @(Report-PortConflicts -Ports @($adminPort, $platformApiPort, $posApiPort))
if ($conflicts.Count -gt 0) {
    throw 'Ports 8090/8091/8092 still occupied after stopping repo-scoped apps. Free them and retry.'
}
Write-Ok 'App ports 8090/8091/8092 are free'

Write-Step 'Starting local-validation PostgreSQL + Mailpit (volumes preserved; never compose down with -v)...'
& docker compose -f $composeFile --env-file $envFile up -d platform-db pos-db mailpit
if ($LASTEXITCODE -ne 0) { throw "docker compose up platform-db pos-db mailpit failed ($LASTEXITCODE)." }

Wait-ContainerHealthy -Name 'exits-local-validation-platform-db' -TimeoutSeconds $DbHealthySeconds
Wait-ContainerHealthy -Name 'exits-local-validation-pos-db' -TimeoutSeconds $DbHealthySeconds
Wait-TcpPort -Label 'Platform DB' -HostName '127.0.0.1' -Port $platformDbPort -TimeoutSeconds 30
Wait-TcpPort -Label 'POS DB' -HostName '127.0.0.1' -Port $posDbPort -TimeoutSeconds 30
Wait-TcpPort -Label 'Mailpit SMTP' -HostName '127.0.0.1' -Port $mailpitSmtpPort -TimeoutSeconds 30
Wait-TcpPort -Label 'Mailpit UI' -HostName '127.0.0.1' -Port $mailpitUiPort -TimeoutSeconds 30
Write-Ok "Mailpit UI: http://localhost:$mailpitUiPort"
$platformCs = "Host=127.0.0.1;Port=$platformDbPort;Database=exits_platform;Username=$($envMap['LOCAL_VALIDATION_PLATFORM_DB_USER']);Password=$($envMap['LOCAL_VALIDATION_PLATFORM_DB_PASSWORD'])"
$posCs = "Host=127.0.0.1;Port=$posDbPort;Database=exits_pos;Username=$($envMap['LOCAL_VALIDATION_POS_DB_USER']);Password=$($envMap['LOCAL_VALIDATION_POS_DB_PASSWORD'])"
$platformApiUrl = "http://localhost:$platformApiPort"
$posApiUrl = "http://localhost:$posApiPort"
$adminUrl = "http://localhost:$adminPort"

$platformProject = Join-Path $repoRoot 'src\Platform\ExItS.Platform.Api\ExItS.Platform.Api.csproj'
$posProject = Join-Path $repoRoot 'src\Products\PinoyBusinessPOS\ExItS.PinoyBusinessPOS.Api\ExItS.PinoyBusinessPOS.Api.csproj'
$adminProject = Join-Path $repoRoot 'src\Platform\ExItS.Platform.Admin\ExItS.Platform.Admin.csproj'

$windowPids = @()

Write-Step 'Starting Platform API (dotnet watch)...'
$seedScopeValue = if ([string]::IsNullOrWhiteSpace($env:LocalValidation__SeedScope)) { $SeedScope } else { [string]$env:LocalValidation__SeedScope }
$windowPids += Start-AppWindow -Title 'ExItS LocalValidation - Platform API' -RepoRoot $repoRoot -Project $platformProject -EnvMap @{
    ASPNETCORE_ENVIRONMENT = 'Staging'
    ASPNETCORE_URLS = $platformApiUrl
    ConnectionStrings__PlatformDatabase = $platformCs
    AllowedHosts = 'localhost;127.0.0.1'
    Cors__AllowedOrigins__0 = $adminOrigin
    Security__EnforceHttps = 'false'
    LocalValidation__Enabled = 'true'
    LocalValidation__SeedScope = $seedScopeValue
    LocalValidation__SharedPassword = [string]$envMap['LOCAL_VALIDATION_SHARED_PASSWORD']
    PlatformEmail__SmtpHost = '127.0.0.1'
    PlatformEmail__SmtpPort = "$mailpitSmtpPort"
    PlatformEmail__UseSsl = 'false'
    PlatformEmail__FromAddress = 'noreply@exits.local'
    PlatformEmail__FromDisplayName = 'ExItS Local Validation'
    PlatformEmail__AdminPublicBaseUrl = $adminOrigin
}
Wait-TcpPort -Label 'Platform API' -HostName '127.0.0.1' -Port $platformApiPort -TimeoutSeconds $PortWaitSeconds

Write-Step 'Starting POS API (dotnet watch)...'
$windowPids += Start-AppWindow -Title 'ExItS LocalValidation - POS API' -RepoRoot $repoRoot -Project $posProject -EnvMap @{
    ASPNETCORE_ENVIRONMENT = 'Staging'
    ASPNETCORE_URLS = $posApiUrl
    ConnectionStrings__PosDatabase = $posCs
    AllowedHosts = 'localhost;127.0.0.1'
    Cors__AllowedOrigins__0 = $adminOrigin
    Security__EnforceHttps = 'false'
    LocalValidation__Enabled = 'true'
    LocalValidation__PlatformApiBaseUrl = $platformApiUrl
    PlatformAuth__BaseUrl = $platformApiUrl
}
Wait-TcpPort -Label 'POS API' -HostName '127.0.0.1' -Port $posApiPort -TimeoutSeconds $PortWaitSeconds

Write-Step 'Starting Platform Admin (dotnet watch)...'
# Admin runs Development so Ant Design / Blazor static assets load without Staging SWA hacks.
# Local Validation identity dropdown uses normal Platform /auth/login server-side
# (SharedPassword stays in Admin process env — never sent to the browser).
$windowPids += Start-AppWindow -Title 'ExItS LocalValidation - Admin' -RepoRoot $repoRoot -Project $adminProject -EnvMap @{
    ASPNETCORE_ENVIRONMENT = 'Development'
    ASPNETCORE_URLS = $adminUrl
    AllowedHosts = 'localhost;127.0.0.1'
    PlatformApi__BaseUrl = $platformApiUrl
    PlatformApi__TimeoutSeconds = '30'
    LocalValidation__Enabled = 'true'
    LocalValidation__SharedPassword = [string]$envMap['LOCAL_VALIDATION_SHARED_PASSWORD']
}
Wait-TcpPort -Label 'Platform Admin' -HostName '127.0.0.1' -Port $adminPort -TimeoutSeconds $PortWaitSeconds

$state = @{
    RepoRoot = $repoRoot
    WindowPids = $windowPids
    StartedAtUtc = (Get-Date).ToUniversalTime().ToString('o')
    Ports = @{ Admin = $adminPort; PlatformApi = $platformApiPort; PosApi = $posApiPort }
}
$state | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $stateFile -Encoding UTF8

$healthOk = $true
$healthOk = (Invoke-HttpCheck -Label 'Platform API /health' -Url "$platformApiUrl/health") -and $healthOk
$healthOk = (Invoke-HttpCheck -Label 'POS API /health' -Url "$posApiUrl/health") -and $healthOk
$healthOk = (Invoke-HttpCheck -Label 'Admin /admin/login' -Url "$adminUrl/admin/login") -and $healthOk

Write-Host ''
Write-Host '======== Local Validation local ready ========' -ForegroundColor Green
Write-Host "  Admin:        $adminUrl"
Write-Host "  Platform API: $platformApiUrl"
Write-Host "  POS API:      $posApiUrl"
Write-Host "  Platform DB:  127.0.0.1:$platformDbPort"
Write-Host "  POS DB:       127.0.0.1:$posDbPort"
Write-Host "  Mailpit UI:   http://localhost:$mailpitUiPort"
Write-Host "  Mailpit SMTP: 127.0.0.1:$mailpitSmtpPort"
Write-Host "  DP keys:      $dpKeys"
Write-Host '=========================================' -ForegroundColor Green
Write-Note 'If the browser still has an old localhost antiforgery cookie, open an Incognito window or clear localhost site data once.'
Write-Host 'Stop apps:  .\tools\Stop-LocalValidation.ps1'
Write-Host 'Stop DBs:   .\tools\Stop-LocalValidation.ps1 -StopDatabases   (volumes preserved)'

if (-not $healthOk) {
    Write-Fail 'One or more health checks failed - see messages above.'
    exit 1
}

Write-Ok 'All health checks passed.'
exit 0
