#Requires -Version 5.1
<#
.SYNOPSIS
  One-command local Local Validation: Docker DBs only + host Platform/POS/Admin/Org Web/Personal Web.

.DESCRIPTION
  - Resolves repo root from this script location (run from any directory).
  - Starts only exits-local-validation platform-db + pos-db (never compose down with -v).
  - Stops stale ExItS.Platform.Api / ExItS.PinoyBusinessPOS.Api / ExItS.Platform.Admin /
    ExItS.PinoyBusinessPOS.Web / ExItS.Personal.Web processes that belong to this repository only.
  - Default BackendMode=Run: prebuilds backends, then starts with
    `dotnet run --no-build --no-launch-profile` (deterministic; readiness timeout covers app startup only).
  - Optional BackendMode=Watch: `dotnet watch` for backend hot reload (slower first readiness).
  - Starts apps in separate PowerShell windows, in order: Platform → seed-identities → POS → web/React.
  - Starts canonical React POS Vite on :5177 (ExItS.PinoyBusinessPOS.React) via npm run dev.
  - Uses deploy/docker/.env.local-validation (gitignored) - no secrets committed.
  - Admin DataProtection keys: %LOCALAPPDATA%\ExItS\LocalValidation\DataProtectionKeys
  - Platform API DataProtection keys: same directory (shared path, distinct application name)
  - Sign in with approved Local Validation identities via the login dropdown (server-side normal
    Platform /auth/login) or manual credentials. Password from LOCAL_VALIDATION_SHARED_PASSWORD
    (never commit the secret; never exposed to the browser).
  - Not Production. Does not start P14-WP03.

.PARAMETER SeedScope
  Local Validation seed scope. Default PlatformAdministratorsOnly (Olivia + Rafael only).
  Pass Full only when you explicitly want the legacy eight-identity catalog.

.PARAMETER PurgeTransactional
  When set with PlatformAdministratorsOnly, purge transactional rows before seed.
  Reset enables this; ordinary Start does not (preserves app-created users/orgs).

.PARAMETER PublicHost
  Optional LAN/Tailscale host (IP or DNS name, no scheme/port). Binds remain 0.0.0.0;
  printed browser URLs, CORS, AllowedHosts, and Admin PlatformApi base URL use this host.
  If omitted, resolves in order: LOCAL_VALIDATION_PUBLIC_HOST from .env.local-validation,
  last PublicHost from launcher-state.json, then an active Tailscale 100.x address when present.

.PARAMETER BackendMode
  Run (default): build once, then `dotnet run --no-build`. Watch: `dotnet watch` (optional HMR).

.PARAMETER PortWaitSeconds
  Application-startup readiness safety timeout per service (HTTP /health or equivalent).
  Does NOT cover compile time in Run mode (builds happen before process start).

.EXAMPLE
  .\tools\Start-LocalValidation.ps1

.EXAMPLE
  .\tools\Start-LocalValidation.ps1 -BackendMode Watch

.EXAMPLE
  .\tools\Start-LocalValidation.ps1 -PublicHost 100.120.79.81

.EXAMPLE
  .\tools\Start-LocalValidation.ps1 -SeedScope Full
#>
[CmdletBinding()]
param(
    [int]$PortWaitSeconds = 120,
    [int]$DbHealthySeconds = 90,
    [ValidateSet('Full', 'PlatformAdministratorsOnly')]
    [string]$SeedScope = 'PlatformAdministratorsOnly',
    [switch]$PurgeTransactional,
    [string]$PublicHost = '',
    [ValidateSet('Run', 'Watch')]
    [string]$BackendMode = 'Run'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LocalValidation.stack.ps1')

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
    return @(Get-LocalValidationRepoScopedAppProcesses -RepoRoot $RepoRoot)
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

function Resolve-PublicHost([string]$Value) {
    if ([string]::IsNullOrWhiteSpace($Value)) { return '' }
    $hostName = $Value.Trim()
    if ($hostName -match '://') {
        throw "PublicHost must be a host or IP only (no scheme). Example: -PublicHost 100.120.79.81"
    }
    if ($hostName.Contains('/') -or $hostName.Contains('\') -or $hostName.Contains(' ')) {
        throw "PublicHost must be a host or IP only (no path/spaces). Example: -PublicHost 100.120.79.81"
    }
    if ($hostName -match ':\d+$') {
        throw "PublicHost must not include a port; ports come from LOCAL_VALIDATION_*_HOST_PORT."
    }
    return $hostName
}

function Get-PersistedPublicHost([string]$StateFilePath) {
    if (-not (Test-Path -LiteralPath $StateFilePath)) { return '' }
    try {
        $state = Get-Content -LiteralPath $StateFilePath -Raw | ConvertFrom-Json
        return Resolve-PublicHost -Value ([string]$state.PublicHost)
    } catch {
        return ''
    }
}

function Get-TailscalePublicHost {
    try {
        $addr = Get-NetIPAddress -AddressFamily IPv4 -ErrorAction SilentlyContinue |
            Where-Object {
                $_.IPAddress -like '100.*' -and
                (
                    $_.InterfaceAlias -match '(?i)tailscale' -or
                    $_.PrefixOrigin -eq 'Manual'
                )
            } |
            Select-Object -First 1 -ExpandProperty IPAddress
        if (-not [string]::IsNullOrWhiteSpace($addr)) {
            return Resolve-PublicHost -Value $addr
        }
    } catch { }
    return ''
}

function Resolve-EffectivePublicHost {
    param(
        [string]$ParamValue,
        $EnvMap,
        [string]$StateFilePath
    )
    $fromParam = Resolve-PublicHost -Value $ParamValue
    if ($fromParam) {
        Write-Ok "PublicHost from -PublicHost: $fromParam"
        return $fromParam
    }
    $fromEnv = Resolve-PublicHost -Value ([string]$EnvMap['LOCAL_VALIDATION_PUBLIC_HOST'])
    if ($fromEnv) {
        Write-Ok "PublicHost from LOCAL_VALIDATION_PUBLIC_HOST: $fromEnv"
        return $fromEnv
    }
    $fromState = Get-PersistedPublicHost -StateFilePath $StateFilePath
    if ($fromState) {
        Write-Ok "PublicHost from previous launcher-state: $fromState"
        return $fromState
    }
    $fromTailscale = Get-TailscalePublicHost
    if ($fromTailscale) {
        Write-Ok "PublicHost auto-detected Tailscale address: $fromTailscale"
        return $fromTailscale
    }
    Write-Note 'No PublicHost resolved (localhost-only AllowedHosts). For Tailscale use -PublicHost or set LOCAL_VALIDATION_PUBLIC_HOST.'
    return ''
}

function Get-LocalValidationAllowedHosts([string]$PublicHostValue, $EnvMap) {
    $hosts = New-Object 'System.Collections.Generic.List[string]'
    # 10.0.2.2 = Android emulator host-loopback alias used by MAUI Local Validation clients.
    foreach ($h in @('localhost', '127.0.0.1', '10.0.2.2')) { $hosts.Add($h) }
    if (-not [string]::IsNullOrWhiteSpace($PublicHostValue) -and -not $hosts.Contains($PublicHostValue)) {
        $hosts.Add($PublicHostValue)
    }
    $fromEnv = [string]$EnvMap['LOCAL_VALIDATION_ALLOWED_HOSTS']
    if (-not [string]::IsNullOrWhiteSpace($fromEnv)) {
        foreach ($part in ($fromEnv -split ';')) {
            $trimmed = $part.Trim()
            if ($trimmed.Length -gt 0 -and -not $hosts.Contains($trimmed)) {
                $hosts.Add($trimmed)
            }
        }
    }
    return ($hosts -join ';')
}

function Show-LocalValidationFirewallGuidance {
    Write-Note 'Windows Firewall: allow inbound TCP 8090/8091/8092/8093/8094/8095/5177 for Tailscale/LAN (Blazor Admin, APIs, Org/Personal Web, React Admin, React POS). Mailpit UI 8025 is optional. Prefer the Private profile. Do not open 15533/15534 (DB). This launcher does not create firewall rules.'
    Write-Host @'
  New-NetFirewallRule -DisplayName "ExItS Local Validation Admin 8090" -Direction Inbound -Protocol TCP -LocalPort 8090 -Action Allow -Profile Private
  New-NetFirewallRule -DisplayName "ExItS Local Validation Platform API 8091" -Direction Inbound -Protocol TCP -LocalPort 8091 -Action Allow -Profile Private
  New-NetFirewallRule -DisplayName "ExItS Local Validation POS API 8092" -Direction Inbound -Protocol TCP -LocalPort 8092 -Action Allow -Profile Private
  New-NetFirewallRule -DisplayName "ExItS Local Validation Org Web 8093" -Direction Inbound -Protocol TCP -LocalPort 8093 -Action Allow -Profile Private
  New-NetFirewallRule -DisplayName "ExItS Local Validation Personal Web 8094" -Direction Inbound -Protocol TCP -LocalPort 8094 -Action Allow -Profile Private
  New-NetFirewallRule -DisplayName "ExItS Local Validation React Admin 8095" -Direction Inbound -Protocol TCP -LocalPort 8095 -Action Allow -Profile Private
  # Optional (Mailpit UI for Tailscale devices). Prefer Private. Do not use Profile Any.
  New-NetFirewallRule -DisplayName "ExItS Local Validation Mailpit 8025" -Direction Inbound -Protocol TCP -LocalPort 8025 -Action Allow -Profile Private
'@
}

function Start-AppWindow {
    param(
        [string]$Title,
        [string]$RepoRoot,
        [string]$Project,
        [hashtable]$EnvMap,
        [ValidateSet('Run', 'Watch')]
        [string]$Mode = 'Run',
        [string]$ServiceKey = '',
        [string]$Configuration = 'Debug'
    )
    $prefix = ConvertTo-EnvAssignments -EnvMap $EnvMap
    $key = if ([string]::IsNullOrWhiteSpace($ServiceKey)) {
        [IO.Path]::GetFileNameWithoutExtension($Project)
    } else {
        $ServiceKey
    }
    $exitMarker = Clear-LocalValidationExitMarker -ServiceKey $key
    $escapedMarker = $exitMarker -replace "'", "''"
    if ($Mode -eq 'Run') {
        $dotnetCmd = "dotnet run --project '$Project' --no-build --no-launch-profile --configuration $Configuration"
    } else {
        $dotnetCmd = "dotnet watch --project '$Project' run --no-launch-profile --non-interactive"
    }
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
Write-Host ('=== {0} (ASPNETCORE_ENVIRONMENT={1}; BackendMode={2}) ===' -f '$Title', `$env:ASPNETCORE_ENVIRONMENT, '$Mode') -ForegroundColor Cyan;
`$exitCode = 1
try {
  $dotnetCmd
  if (`$null -ne `$LASTEXITCODE) { `$exitCode = [int]`$LASTEXITCODE } else { `$exitCode = 0 }
} catch {
  Write-Host `$_ -ForegroundColor Red
  `$exitCode = 1
}
Set-Content -LiteralPath '$escapedMarker' -Value ([string]`$exitCode) -Encoding ascii
Write-Host ('Process exited with code {0}. Window stays open for inspection. Marker={1}' -f `$exitCode, '$escapedMarker') -ForegroundColor Yellow
"@
    $proc = Start-Process -FilePath 'powershell.exe' -PassThru -ArgumentList @(
        '-NoExit',
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        '-Command', $run
    )
    return [pscustomobject]@{
        WindowProcessId = $proc.Id
        ServiceKey = $key
        ExitMarkerPath = $exitMarker
        Mode = $Mode
    }
}

function Start-NpmDevWindow {
    param(
        [string]$Title,
        [string]$WorkingDirectory,
        [hashtable]$EnvMap,
        [string]$NpmScript = 'dev',
        [string]$ExtraNpmArgs = ''
    )
    $prefix = ConvertTo-EnvAssignments -EnvMap $EnvMap
    $extra = if ([string]::IsNullOrWhiteSpace($ExtraNpmArgs)) { '' } else { " -- $ExtraNpmArgs" }
    $run = @"
`$Host.UI.RawUI.WindowTitle = '$Title';
$prefix
Set-Location '$WorkingDirectory';
Write-Host ('=== {0} ===' -f '$Title') -ForegroundColor Cyan;
if (-not (Test-Path -LiteralPath 'node_modules')) {
    Write-Host 'node_modules missing - running npm ci...' -ForegroundColor Yellow;
    npm ci;
    if (`$LASTEXITCODE -ne 0) { throw 'npm ci failed for $Title' }
}
npm run $NpmScript$extra
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
$repoRoot = Get-LocalValidationRepoRoot
$dockerDir = Join-Path $repoRoot 'deploy\docker'
$envFile = Join-Path $dockerDir $LocalValidationStack.EnvFileName
$composeFile = Join-Path $dockerDir $LocalValidationStack.ComposeFileName
$stateDir = Join-Path $env:LOCALAPPDATA 'ExItS\LocalValidation'
$dpKeys = Join-Path $stateDir 'DataProtectionKeys'
$stateFile = Join-Path $stateDir 'launcher-state.json'

Write-Step "Repository: $repoRoot"
Write-Ok ("Compose project={0} file={1}" -f $LocalValidationStack.ComposeProjectName, $LocalValidationStack.ComposeFileName)
Test-LocalValidationDockerAvailable
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

$platformDbPort = if ($envMap['LOCAL_VALIDATION_PLATFORM_DB_HOST_PORT']) { [int]$envMap['LOCAL_VALIDATION_PLATFORM_DB_HOST_PORT'] } else { [int]$LocalValidationStack.DefaultPlatformDbPort }
$posDbPort = if ($envMap['LOCAL_VALIDATION_POS_DB_HOST_PORT']) { [int]$envMap['LOCAL_VALIDATION_POS_DB_HOST_PORT'] } else { [int]$LocalValidationStack.DefaultPosDbPort }
$adminPort = if ($envMap['LOCAL_VALIDATION_ADMIN_HOST_PORT']) { [int]$envMap['LOCAL_VALIDATION_ADMIN_HOST_PORT'] } else { [int]$LocalValidationStack.DefaultAdminPort }
$platformApiPort = if ($envMap['LOCAL_VALIDATION_PLATFORM_API_HOST_PORT']) { [int]$envMap['LOCAL_VALIDATION_PLATFORM_API_HOST_PORT'] } else { [int]$LocalValidationStack.DefaultPlatformApiPort }
$posApiPort = if ($envMap['LOCAL_VALIDATION_POS_API_HOST_PORT']) { [int]$envMap['LOCAL_VALIDATION_POS_API_HOST_PORT'] } else { [int]$LocalValidationStack.DefaultPosApiPort }
$orgWebPort = if ($envMap['LOCAL_VALIDATION_ORG_WEB_HOST_PORT']) { [int]$envMap['LOCAL_VALIDATION_ORG_WEB_HOST_PORT'] } else { [int]$LocalValidationStack.DefaultOrgWebPort }
$personalWebPort = if ($envMap['LOCAL_VALIDATION_PERSONAL_WEB_HOST_PORT']) { [int]$envMap['LOCAL_VALIDATION_PERSONAL_WEB_HOST_PORT'] } else { [int]$LocalValidationStack.DefaultPersonalWebPort }
$adminWebReactPort = if ($envMap['LOCAL_VALIDATION_ADMIN_WEB_REACT_HOST_PORT']) { [int]$envMap['LOCAL_VALIDATION_ADMIN_WEB_REACT_HOST_PORT'] } else { [int]$LocalValidationStack.DefaultAdminWebReactPort }
$reactPosPortEarly = if ($envMap['LOCAL_VALIDATION_REACT_POS_HOST_PORT']) { [int]$envMap['LOCAL_VALIDATION_REACT_POS_HOST_PORT'] } else { [int]$LocalValidationStack.DefaultReactPosPort }
$mailpitUiPort = if ($envMap['LOCAL_VALIDATION_MAILPIT_UI_HOST_PORT']) { [int]$envMap['LOCAL_VALIDATION_MAILPIT_UI_HOST_PORT'] } else { 8025 }
$mailpitSmtpPort = if ($envMap['LOCAL_VALIDATION_MAILPIT_SMTP_HOST_PORT']) { [int]$envMap['LOCAL_VALIDATION_MAILPIT_SMTP_HOST_PORT'] } else { 1025 }

New-Item -ItemType Directory -Force -Path $dpKeys | Out-Null
Write-Ok "DataProtection keys directory: $dpKeys"

$appPortLabels = @{
    $adminPort         = 'Platform Admin'
    $platformApiPort   = 'Platform API'
    $posApiPort        = 'POS API'
    $orgWebPort        = 'Organization Web'
    $personalWebPort   = 'Personal Web'
    $adminWebReactPort = 'React Platform Admin'
    $reactPosPortEarly = 'React POS'
}

Write-Step 'Inspecting Local Validation port/runtime provenance (all ExItS worktrees)...'
Write-LocalValidationRuntimeProvenanceTable -PortLabels $appPortLabels -ExpectedRepoRoot $repoRoot

Write-Step 'Stopping Docker app services before host mode (infrastructure and volumes preserved)...'
$null = Stop-LocalValidationDockerAppServices -ComposeFile $composeFile -EnvFile $envFile

Write-Step 'Stopping stale cross-worktree ExItS host apps (DBs untouched)...'
$null = Stop-LocalValidationCrossWorktreeHostApps -RepoRoot $repoRoot

# React POS is Vite/node (not dotnet AppMarkers). Free :5177 so restart is reliable.
Write-Step "Freeing React POS port $reactPosPortEarly if still held by Vite/node..."
$null = Stop-LocalValidationPortListeners -Port $reactPosPortEarly -Label 'React POS'

# Apphosts (ExItS.*.Api.exe) can survive after parent dotnet.exe exits and keep ports/DLLs locked.
Write-Step 'Freeing Local Validation host app ports if still held by leftover apphosts...'
foreach ($port in @($adminPort, $platformApiPort, $posApiPort, $orgWebPort, $personalWebPort, $adminWebReactPort)) {
    $owner = Get-LocalValidationListeningOwner -Port $port
    if ($null -eq $owner) { continue }
    $name = [string]$owner.ProcessName
    $isAppMarker = $false
    foreach ($marker in $LocalValidationStack.AppMarkers) {
        if ($name.Equals($marker, [StringComparison]::OrdinalIgnoreCase)) {
            $isAppMarker = $true
            break
        }
    }
    if ($isAppMarker -or $name.Equals('dotnet', [StringComparison]::OrdinalIgnoreCase)) {
        $null = Stop-LocalValidationPortListeners -Port $port -Label $appPortLabels[$port]
    }
}

$conflicts = @(Report-LocalValidationPortConflictsWithProvenance -PortLabels $appPortLabels -ExpectedRepoRoot $repoRoot)
if ($conflicts.Count -gt 0) {
    throw 'Ports 8090/8091/8092/8093/8094/8095/5177 still occupied after stopping cross-worktree apps and Docker app services. Free them and retry.'
}
Write-Ok 'App ports 8090/8091/8092/8093/8094/8095/5177 are free'

Write-Step 'Starting local-validation PostgreSQL + Mailpit (volumes preserved)...'
Start-LocalValidationInfrastructure -ComposeFile $composeFile -EnvFile $envFile

Wait-ContainerHealthy -Name $LocalValidationStack.PlatformDbContainer -TimeoutSeconds $DbHealthySeconds
Wait-ContainerHealthy -Name $LocalValidationStack.PosDbContainer -TimeoutSeconds $DbHealthySeconds
Wait-TcpPort -Label 'Platform DB' -HostName '127.0.0.1' -Port $platformDbPort -TimeoutSeconds 30
Wait-TcpPort -Label 'POS DB' -HostName '127.0.0.1' -Port $posDbPort -TimeoutSeconds 30
Wait-TcpPort -Label 'Mailpit SMTP' -HostName '127.0.0.1' -Port $mailpitSmtpPort -TimeoutSeconds 30
Wait-TcpPort -Label 'Mailpit UI' -HostName '127.0.0.1' -Port $mailpitUiPort -TimeoutSeconds 30
Write-Ok "Mailpit UI: http://localhost:$mailpitUiPort"
$platformCs = "Host=127.0.0.1;Port=$platformDbPort;Database=$($LocalValidationStack.PlatformDbName);Username=$($envMap['LOCAL_VALIDATION_PLATFORM_DB_USER']);Password=$($envMap['LOCAL_VALIDATION_PLATFORM_DB_PASSWORD'])"
$posCs = "Host=127.0.0.1;Port=$posDbPort;Database=$($LocalValidationStack.PosDbName);Username=$($envMap['LOCAL_VALIDATION_POS_DB_USER']);Password=$($envMap['LOCAL_VALIDATION_POS_DB_PASSWORD'])"
$platformCsSummary = Get-LocalValidationConnectionSummary -ConnectionString $platformCs -Label 'Platform'
$posCsSummary = Get-LocalValidationConnectionSummary -ConnectionString $posCs -Label 'POS'

# Bind all interfaces so Tailscale/LAN can reach apps; localhost/127.0.0.1 still work.
$resolvedPublicHost = Resolve-EffectivePublicHost -ParamValue $PublicHost -EnvMap $envMap -StateFilePath $stateFile
$bindAdminUrl = "http://0.0.0.0:$adminPort"
$bindPlatformApiUrl = "http://0.0.0.0:$platformApiPort"
$bindPosApiUrl = "http://0.0.0.0:$posApiPort"
$bindOrgWebUrl = "http://0.0.0.0:$orgWebPort"
$bindPersonalWebUrl = "http://0.0.0.0:$personalWebPort"
$loopbackAdminUrl = "http://127.0.0.1:$adminPort"
$loopbackPlatformApiUrl = "http://127.0.0.1:$platformApiPort"
$loopbackPosApiUrl = "http://127.0.0.1:$posApiPort"
$loopbackOrgWebUrl = "http://127.0.0.1:$orgWebPort"
$loopbackPersonalWebUrl = "http://127.0.0.1:$personalWebPort"
$loopbackAdminWebReactUrl = "http://127.0.0.1:$adminWebReactPort"
if ($resolvedPublicHost) {
    $publicAdminUrl = "http://${resolvedPublicHost}:$adminPort"
    $publicPlatformApiUrl = "http://${resolvedPublicHost}:$platformApiPort"
    $publicPosApiUrl = "http://${resolvedPublicHost}:$posApiPort"
    $publicOrgWebUrl = "http://${resolvedPublicHost}:$orgWebPort"
    $publicPersonalWebUrl = "http://${resolvedPublicHost}:$personalWebPort"
    $publicAdminWebReactUrl = "http://${resolvedPublicHost}:$adminWebReactPort"
}
else {
    $publicAdminUrl = "http://localhost:$adminPort"
    $publicPlatformApiUrl = "http://localhost:$platformApiPort"
    $publicPosApiUrl = "http://localhost:$posApiPort"
    $publicOrgWebUrl = "http://localhost:$orgWebPort"
    $publicPersonalWebUrl = "http://localhost:$personalWebPort"
    $publicAdminWebReactUrl = "http://localhost:$adminWebReactPort"
}

$allowedHosts = Get-LocalValidationAllowedHosts -PublicHostValue $resolvedPublicHost -EnvMap $envMap
$corsOrigins = @(
    "http://localhost:$adminPort",
    "http://127.0.0.1:$adminPort",
    "http://localhost:$orgWebPort",
    "http://127.0.0.1:$orgWebPort",
    "http://localhost:$personalWebPort",
    "http://127.0.0.1:$personalWebPort",
    "http://localhost:$adminWebReactPort",
    "http://127.0.0.1:$adminWebReactPort"
)
if ($resolvedPublicHost) {
    $corsOrigins += "http://${resolvedPublicHost}:$adminPort"
    $corsOrigins += "http://${resolvedPublicHost}:$orgWebPort"
    $corsOrigins += "http://${resolvedPublicHost}:$personalWebPort"
    $corsOrigins += "http://${resolvedPublicHost}:$adminWebReactPort"
}

$reactAdminPort = if ($envMap['LOCAL_VALIDATION_REACT_ADMIN_HOST_PORT']) {
    [int]$envMap['LOCAL_VALIDATION_REACT_ADMIN_HOST_PORT']
} else {
    [int]$LocalValidationStack.DefaultReactAdminPort
}
$reactPosPort = if ($envMap['LOCAL_VALIDATION_REACT_POS_HOST_PORT']) {
    [int]$envMap['LOCAL_VALIDATION_REACT_POS_HOST_PORT']
} else {
    [int]$LocalValidationStack.DefaultReactPosPort
}

# Auth emails (activate/reset) must open React Admin Vite pages, not Blazor :8090.
$authPublicBaseUrl = Resolve-LocalValidationAuthPublicBaseUrl `
    -EnvMap $envMap `
    -ResolvedPublicHost $resolvedPublicHost `
    -ReactAdminPort $reactAdminPort

$corsOrigins = Add-LocalValidationReactCorsOrigins `
    -CorsOrigins $corsOrigins `
    -EnvMap $envMap `
    -ResolvedPublicHost $resolvedPublicHost `
    -ReactPosPort $reactPosPort `
    -ReactAdminPort $reactAdminPort

Write-Ok "Kestrel bind URLs: $bindAdminUrl | $bindPlatformApiUrl | $bindPosApiUrl | $bindOrgWebUrl | $bindPersonalWebUrl"
Write-Ok "AllowedHosts: $allowedHosts"
Write-Ok ("CORS origins: {0}" -f ($corsOrigins -join ', '))
Write-Ok "Auth email / activation base URL: $authPublicBaseUrl"
if ($resolvedPublicHost) {
    Write-Ok "PublicHost browser URLs use $resolvedPublicHost"
}

$platformProject = Join-Path $repoRoot 'src\Platform\ExItS.Platform.Api\ExItS.Platform.Api.csproj'
$posProject = Join-Path $repoRoot 'src\Products\PinoyBusinessPOS\ExItS.PinoyBusinessPOS.Api\ExItS.PinoyBusinessPOS.Api.csproj'
$adminProject = Join-Path $repoRoot 'src\Platform\ExItS.Platform.Admin\ExItS.Platform.Admin.csproj'
$orgWebProject = Join-Path $repoRoot 'src\Products\PinoyBusinessPOS\ExItS.PinoyBusinessPOS.Web\ExItS.PinoyBusinessPOS.Web.csproj'
$personalWebProject = Join-Path $repoRoot 'src\Platform\ExItS.Personal.Web\ExItS.Personal.Web.csproj'

foreach ($proj in @($platformProject, $posProject, $adminProject, $orgWebProject, $personalWebProject)) {
    if (-not (Test-Path -LiteralPath $proj)) {
        throw "Missing project file: $proj"
    }
}

$windowPids = @()
$timing = @{
    PlatformBuildSeconds = $null
    PosBuildSeconds = $null
    PlatformReadySeconds = $null
    PosReadySeconds = $null
}

Write-Host ''
Write-Host ("BACKEND MODE: {0}" -f $BackendMode.ToUpperInvariant()) -ForegroundColor Cyan
if ($BackendMode -eq 'Watch') {
    Write-Note 'Watch mode uses dotnet watch — first readiness may include compile time. Prefer default Run for deterministic validation.'
}
Write-Note ("PortWaitSeconds={0} is an application readiness safety timeout (not build time)." -f $PortWaitSeconds)

if ($BackendMode -eq 'Run') {
    Write-Host ''
    Write-Host '[build] Prebuilding backends (once) before process start...' -ForegroundColor Cyan
    $timing.PlatformBuildSeconds = Invoke-LocalValidationDotnetBuild -Label 'Platform API' -ProjectPath $platformProject
    $timing.PosBuildSeconds = Invoke-LocalValidationDotnetBuild -Label 'POS API' -ProjectPath $posProject
    $null = Invoke-LocalValidationDotnetBuild -Label 'Platform Admin' -ProjectPath $adminProject
    $null = Invoke-LocalValidationDotnetBuild -Label 'Organization Web' -ProjectPath $orgWebProject
    $null = Invoke-LocalValidationDotnetBuild -Label 'Personal Web' -ProjectPath $personalWebProject
}

Write-Step 'Starting Platform API...'
# -SeedScope parameter is authoritative. Do not let a polluted parent shell (e.g. leftover Full)
# override the default PlatformAdministratorsOnly baseline after Reset.
$seedScopeValue = $SeedScope
$inheritedSeedScope = [string]$env:LocalValidation__SeedScope
if (-not [string]::IsNullOrWhiteSpace($inheritedSeedScope) -and $inheritedSeedScope -ne $seedScopeValue) {
    Write-Note "Ignoring parent LocalValidation__SeedScope='$inheritedSeedScope'; using -SeedScope '$seedScopeValue'."
}
$purgeTransactional = [bool]$PurgeTransactional
Write-Ok "LocalValidation SeedScope=$seedScopeValue PurgeTransactionalOnSeed=$purgeTransactional"
Write-LocalValidationStartupDiagnostics `
    -AspNetCoreEnvironment 'Staging' `
    -SeedScope $seedScopeValue `
    -PlatformCsSummary $platformCsSummary `
    -PosCsSummary $posCsSummary `
    -ComposeProjectName $LocalValidationStack.ComposeProjectName `
    -PlatformDbContainer $LocalValidationStack.PlatformDbContainer `
    -PosDbContainer $LocalValidationStack.PosDbContainer `
    -PlatformDbVolume $LocalValidationStack.PlatformDbVolume `
    -PosDbVolume $LocalValidationStack.PosDbVolume `
    -WindowPids @()
$platformEnv = @{
    ASPNETCORE_ENVIRONMENT = 'Staging'
    ASPNETCORE_URLS = $bindPlatformApiUrl
    ConnectionStrings__PlatformDatabase = $platformCs
    AllowedHosts = $allowedHosts
    Security__EnforceHttps = 'false'
    LocalValidation__Enabled = 'true'
    LocalValidation__SeedScope = $seedScopeValue
    LocalValidation__PurgeTransactionalOnSeed = $(if ($purgeTransactional) { 'true' } else { 'false' })
    LocalValidation__SharedPassword = [string]$envMap['LOCAL_VALIDATION_SHARED_PASSWORD']
    # Local Validation only: allow weak passwords (e.g. 123) for registration/activation testing.
    PlatformAuthentication__Password__MinimumLength = '1'
    PlatformAuthentication__Password__MaximumLength = '128'
    PlatformAuthentication__Password__RequireUppercase = 'false'
    PlatformAuthentication__Password__RequireLowercase = 'false'
    PlatformAuthentication__Password__RequireDigit = 'false'
    PlatformAuthentication__Password__RequireNonAlphanumeric = 'false'
    PlatformEmail__SmtpHost = '127.0.0.1'
    PlatformEmail__SmtpPort = "$mailpitSmtpPort"
    PlatformEmail__UseSsl = 'false'
    PlatformEmail__FromAddress = 'noreply@exits.local'
    PlatformEmail__FromDisplayName = 'ExItS Local Validation'
    # Must match React Admin (:8095) - hosts /admin/activate-account and /admin/reset-password.
    PlatformEmail__AdminPublicBaseUrl = $authPublicBaseUrl
    PlatformEmail__PinoyLoanManagerPublicBaseUrl = 'http://localhost:4176'
    # Personal/POS React Vite (:5177) hosts /activate-account and /reset-password.
    PlatformEmail__PinoyBusinessPosPublicBaseUrl = "http://localhost:$reactPosPort"
    PlatformEmail__AllowHttpLoopbackPublicUrls = 'true'
    DataProtection__KeysPath = $dpKeys
}
for ($i = 0; $i -lt $corsOrigins.Count; $i++) {
    $platformEnv["Cors__AllowedOrigins__$i"] = $corsOrigins[$i]
}
$platformLaunch = Start-AppWindow `
    -Title 'ExItS LocalValidation - Platform API' `
    -RepoRoot $repoRoot `
    -Project $platformProject `
    -EnvMap $platformEnv `
    -Mode $BackendMode `
    -ServiceKey 'platform-api'
$windowPids += $platformLaunch.WindowProcessId
try {
    $seedBaseUrl = $loopbackPlatformApiUrl
    $platformReady = Wait-LocalServiceReady `
        -ServiceName 'Platform API' `
        -HealthUri "$loopbackPlatformApiUrl/health" `
        -TimeoutSeconds $PortWaitSeconds `
        -WindowProcessId $platformLaunch.WindowProcessId `
        -ExitMarkerPath $platformLaunch.ExitMarkerPath `
        -OptionalDependencyName 'local-validation seed-identities' `
        -OptionalDependencyProbe ({ Test-LocalValidationSeedIdentitiesReady -PlatformApiBaseUrl $seedBaseUrl }.GetNewClosure())
    $timing.PlatformReadySeconds = $platformReady.ReadyInSeconds
    Write-Ok ("Platform API health READY ({0}s)" -f $platformReady.ReadyInSeconds)
    Write-Ok 'Platform local-validation seed-identities READY'
}
catch {
    Write-Fail 'Platform API did not become ready. Check the "ExItS LocalValidation - Platform API" window.'
    Write-Fail "Project: $platformProject"
    Write-Fail "Exit marker: $($platformLaunch.ExitMarkerPath)"
    throw
}

Write-Step 'Starting POS API (after Platform seed-identities)...'
$posEnv = @{
    ASPNETCORE_ENVIRONMENT = 'Staging'
    ASPNETCORE_URLS = $bindPosApiUrl
    ConnectionStrings__PosDatabase = $posCs
    AllowedHosts = $allowedHosts
    Security__EnforceHttps = 'false'
    LocalValidation__Enabled = 'true'
    # Server-to-server on the same host: keep loopback (DB also stays on localhost).
    LocalValidation__PlatformApiBaseUrl = $loopbackPlatformApiUrl
    PlatformAuth__BaseUrl = $loopbackPlatformApiUrl
    # Temporary React PWA preview: pause installation-device transaction gate (re-enable for Capacitor).
    PosDeviceAuthorization__EnforcementEnabled = 'false'
}
for ($i = 0; $i -lt $corsOrigins.Count; $i++) {
    $posEnv["Cors__AllowedOrigins__$i"] = $corsOrigins[$i]
}
$posLaunch = Start-AppWindow `
    -Title 'ExItS LocalValidation - POS API' `
    -RepoRoot $repoRoot `
    -Project $posProject `
    -EnvMap $posEnv `
    -Mode $BackendMode `
    -ServiceKey 'pos-api'
$windowPids += $posLaunch.WindowProcessId
try {
    $posReady = Wait-LocalServiceReady `
        -ServiceName 'POS API' `
        -HealthUri "$loopbackPosApiUrl/health" `
        -TimeoutSeconds $PortWaitSeconds `
        -WindowProcessId $posLaunch.WindowProcessId `
        -ExitMarkerPath $posLaunch.ExitMarkerPath
    $timing.PosReadySeconds = $posReady.ReadyInSeconds
    Write-Ok ("POS API health READY ({0}s)" -f $posReady.ReadyInSeconds)
}
catch {
    Write-Fail 'POS API did not become ready. Check the "ExItS LocalValidation - POS API" window for migrate/startup errors (DB localhost:15534).'
    Write-Fail "Project: $posProject"
    Write-Fail "Exit marker: $($posLaunch.ExitMarkerPath)"
    throw
}

Write-Step 'Starting Platform Admin...'
# Admin runs Development so Ant Design / Blazor static assets load without Staging SWA hacks.
# Local Validation identity dropdown uses normal Platform /auth/login server-side
# (SharedPassword stays in Admin process env - never sent to the browser).
# PlatformApi__BaseUrl is browser-visible (OAuth challenge links) and server HttpClient base.
$adminEnv = @{
    ASPNETCORE_ENVIRONMENT = 'Development'
    ASPNETCORE_URLS = $bindAdminUrl
    AllowedHosts = $allowedHosts
    PlatformApi__BaseUrl = $publicPlatformApiUrl
    PlatformApi__TimeoutSeconds = '30'
    LocalValidation__Enabled = 'true'
    LocalValidation__SharedPassword = [string]$envMap['LOCAL_VALIDATION_SHARED_PASSWORD']
    ExItSWebHosts__PlatformAdmin = $publicAdminUrl
    ExItSWebHosts__OrganizationWeb = $publicOrgWebUrl
    ExItSWebHosts__PersonalWeb = $publicPersonalWebUrl
}
$adminLaunch = Start-AppWindow `
    -Title 'ExItS LocalValidation - Admin' `
    -RepoRoot $repoRoot `
    -Project $adminProject `
    -EnvMap $adminEnv `
    -Mode $BackendMode `
    -ServiceKey 'platform-admin'
$windowPids += $adminLaunch.WindowProcessId
Wait-LocalServiceReady `
    -ServiceName 'Platform Admin' `
    -HealthUri "$loopbackAdminUrl/admin/login" `
    -TimeoutSeconds $PortWaitSeconds `
    -WindowProcessId $adminLaunch.WindowProcessId `
    -ExitMarkerPath $adminLaunch.ExitMarkerPath | Out-Null
Write-Ok 'Platform Admin READY'

Write-Step 'Starting Organization Web Admin...'
$orgWebEnv = @{
    ASPNETCORE_ENVIRONMENT = 'Development'
    ASPNETCORE_URLS = $bindOrgWebUrl
    AllowedHosts = $allowedHosts
    LocalValidation__Enabled = 'true'
    Security__RequireHttpsApiUrls = 'false'
    PosApi__BaseUrl = $loopbackPlatformApiUrl
    PosBusinessApi__BaseUrl = $loopbackPosApiUrl
    ExItSWebHosts__PlatformAdmin = $publicAdminUrl
    ExItSWebHosts__OrganizationWeb = $publicOrgWebUrl
    ExItSWebHosts__PersonalWeb = $publicPersonalWebUrl
}
$orgLaunch = Start-AppWindow `
    -Title 'ExItS LocalValidation - Org Web' `
    -RepoRoot $repoRoot `
    -Project $orgWebProject `
    -EnvMap $orgWebEnv `
    -Mode $BackendMode `
    -ServiceKey 'org-web'
$windowPids += $orgLaunch.WindowProcessId
Wait-LocalServiceReady `
    -ServiceName 'Organization Web' `
    -HealthUri "$loopbackOrgWebUrl/health" `
    -TimeoutSeconds $PortWaitSeconds `
    -WindowProcessId $orgLaunch.WindowProcessId `
    -ExitMarkerPath $orgLaunch.ExitMarkerPath | Out-Null
Write-Ok 'Organization Web READY'

Write-Step 'Starting Personal Web...'
$personalWebEnv = @{
    ASPNETCORE_ENVIRONMENT = 'Development'
    ASPNETCORE_URLS = $bindPersonalWebUrl
    AllowedHosts = $allowedHosts
    LocalValidation__Enabled = 'true'
    PlatformApi__BaseUrl = $loopbackPlatformApiUrl
    ExItSWebHosts__PlatformAdmin = $publicAdminUrl
    ExItSWebHosts__OrganizationWeb = $publicOrgWebUrl
    ExItSWebHosts__PersonalWeb = $publicPersonalWebUrl
}
$personalLaunch = Start-AppWindow `
    -Title 'ExItS LocalValidation - Personal Web' `
    -RepoRoot $repoRoot `
    -Project $personalWebProject `
    -EnvMap $personalWebEnv `
    -Mode $BackendMode `
    -ServiceKey 'personal-web'
$windowPids += $personalLaunch.WindowProcessId
Wait-LocalServiceReady `
    -ServiceName 'Personal Web' `
    -HealthUri "$loopbackPersonalWebUrl/health" `
    -TimeoutSeconds $PortWaitSeconds `
    -WindowProcessId $personalLaunch.WindowProcessId `
    -ExitMarkerPath $personalLaunch.ExitMarkerPath | Out-Null
Write-Ok 'Personal Web READY'

Write-Step 'Starting React Platform Admin (Docker production build on 8095)...'
$gitSha = Get-LocalValidationGitSha -RepoRoot $repoRoot
$reactApiProxyTarget = "http://host.docker.internal:$platformApiPort"
Set-Item -LiteralPath 'Env:LOCAL_VALIDATION_PLATFORM_API_PUBLIC_URL' -Value $publicPlatformApiUrl
Set-Item -LiteralPath 'Env:LOCAL_VALIDATION_ADMIN_WEB_REACT_ORIGIN' -Value $publicAdminWebReactUrl
Set-Item -LiteralPath 'Env:LOCAL_VALIDATION_PLATFORM_API_SAME_ORIGIN' -Value 'true'
Set-Item -LiteralPath 'Env:LOCAL_VALIDATION_PLATFORM_API_PROXY_TARGET' -Value $reactApiProxyTarget
Set-Item -LiteralPath 'Env:EXITS_GIT_SHA' -Value $gitSha
$reactUpArgs = @(
    'compose', '-p', $LocalValidationStack.ComposeProjectName,
    '-f', $composeFile, '--env-file', $envFile,
    '--profile', 'apps', 'up', '-d', 'admin-web-react'
)
$reactExit = Invoke-LocalValidationDocker -DockerArgs $reactUpArgs
if ($reactExit -ne 0) { throw "React Platform Admin container startup failed ($reactExit)." }
Wait-TcpPort -Label 'React Platform Admin' -HostName '127.0.0.1' -Port $adminWebReactPort -TimeoutSeconds $PortWaitSeconds

$reactPosClientDir = Join-Path $repoRoot 'src\Products\PinoyBusinessPOS\ExItS.PinoyBusinessPOS.React'
if (-not (Test-Path -LiteralPath (Join-Path $reactPosClientDir 'package.json'))) {
    throw "Missing canonical React POS client: $reactPosClientDir"
}
Write-Step "Starting React POS Vite on :$reactPosPort (after POS API healthy; canonical ExItS.PinoyBusinessPOS.React)..."
$reactPosEnv = @{
    VITE_POS_BUILD_SHA = $gitSha
    EXITS_PLATFORM_API_PROXY_TARGET = $loopbackPlatformApiUrl
    EXITS_POS_API_PROXY_TARGET = $loopbackPosApiUrl
    # DEV-only: Offline PIN on Tailscale/LAN HTTP (crypto.subtle unavailable). Never for production builds.
    VITE_ALLOW_INSECURE_OFFLINE_PIN = 'true'
}
if ($resolvedPublicHost) {
    $reactPosEnv['POS_DEV_HOST'] = '0.0.0.0'
    $reactPosEnv['POS_DEV_PUBLIC_HOST'] = $resolvedPublicHost
}
$windowPids += Start-NpmDevWindow `
    -Title 'ExItS LocalValidation - React POS' `
    -WorkingDirectory $reactPosClientDir `
    -EnvMap $reactPosEnv `
    -NpmScript 'dev'
Wait-TcpPort -Label 'React POS' -HostName '127.0.0.1' -Port $reactPosPort -TimeoutSeconds $PortWaitSeconds
Write-Ok 'React POS Vite READY (HMR preserved)'

$state = @{
    Mode = 'HostApps'
    BackendMode = $BackendMode
    RepoRoot = $repoRoot
    WindowPids = $windowPids
    StartedAtUtc = (Get-Date).ToUniversalTime().ToString('o')
    PublicHost = $resolvedPublicHost
    SeedScope = $seedScopeValue
    PurgeTransactionalOnSeed = $purgeTransactional
    ComposeProjectName = $LocalValidationStack.ComposeProjectName
    ComposeFile = $composeFile
    PlatformDbContainer = $LocalValidationStack.PlatformDbContainer
    PosDbContainer = $LocalValidationStack.PosDbContainer
    PlatformDbVolume = $LocalValidationStack.PlatformDbVolume
    PosDbVolume = $LocalValidationStack.PosDbVolume
    Timing = $timing
    Ports = @{
        Admin = $adminPort
        PlatformApi = $platformApiPort
        PosApi = $posApiPort
        OrgWeb = $orgWebPort
        PersonalWeb = $personalWebPort
        AdminWebReact = $adminWebReactPort
        ReactPos = $reactPosPort
        PlatformDb = $platformDbPort
        PosDb = $posDbPort
    }
    Databases = @{
        Platform = @{ Host = '127.0.0.1'; Port = $platformDbPort; Name = $LocalValidationStack.PlatformDbName }
        Pos = @{ Host = '127.0.0.1'; Port = $posDbPort; Name = $LocalValidationStack.PosDbName }
    }
}
$state | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $stateFile -Encoding UTF8

Write-LocalValidationStartupDiagnostics `
    -AspNetCoreEnvironment 'Staging/Development(Admin)' `
    -SeedScope $seedScopeValue `
    -PlatformCsSummary $platformCsSummary `
    -PosCsSummary $posCsSummary `
    -ComposeProjectName $LocalValidationStack.ComposeProjectName `
    -PlatformDbContainer $LocalValidationStack.PlatformDbContainer `
    -PosDbContainer $LocalValidationStack.PosDbContainer `
    -PlatformDbVolume $LocalValidationStack.PlatformDbVolume `
    -PosDbVolume $LocalValidationStack.PosDbVolume `
    -WindowPids $windowPids

$healthOk = $true
$healthOk = (Invoke-HttpCheck -Label 'Platform API /health' -Url "$loopbackPlatformApiUrl/health") -and $healthOk
$healthOk = (Invoke-HttpCheck -Label 'POS API /health' -Url "$loopbackPosApiUrl/health") -and $healthOk
$healthOk = (Invoke-HttpCheck -Label 'Admin /admin/login' -Url "$loopbackAdminUrl/admin/login") -and $healthOk
$healthOk = (Invoke-HttpCheck -Label 'Organization Web /health' -Url "$loopbackOrgWebUrl/health") -and $healthOk
$healthOk = (Invoke-HttpCheck -Label 'Personal Web /health' -Url "$loopbackPersonalWebUrl/health") -and $healthOk
$healthOk = (Invoke-HttpCheck -Label 'React Admin /health' -Url "$loopbackAdminWebReactUrl/health") -and $healthOk
$healthOk = (Invoke-HttpCheck -Label 'React Admin /admin' -Url "$loopbackAdminWebReactUrl/admin") -and $healthOk
$healthOk = (Invoke-HttpCheck -Label 'React POS /' -Url "http://127.0.0.1:$reactPosPort/") -and $healthOk
$healthOk = (Invoke-HttpCheck -Label 'React POS /sign-in' -Url "http://127.0.0.1:$reactPosPort/sign-in") -and $healthOk

Write-Host ''
Write-Host '======== Local Validation local ready ========' -ForegroundColor Green
Write-Host ("  BackendMode:  {0}" -f $BackendMode)
if ($null -ne $timing.PlatformBuildSeconds) {
    Write-Host ("  Platform build: {0}s | POS build: {1}s" -f $timing.PlatformBuildSeconds, $timing.PosBuildSeconds)
}
if ($null -ne $timing.PlatformReadySeconds) {
    Write-Host ("  Platform ready after launch: {0}s | POS ready after launch: {1}s" -f $timing.PlatformReadySeconds, $timing.PosReadySeconds)
}
Write-Host "  Blazor Admin: $publicAdminUrl"
Write-Host "  React Admin:  $authPublicBaseUrl  (register / activate / reset)"
Write-Host "  React POS:    http://127.0.0.1:$reactPosPort"
Write-Host "  Platform API: $publicPlatformApiUrl"
Write-Host "  POS API:      $publicPosApiUrl"
Write-Host "  Org Web:      $publicOrgWebUrl"
Write-Host "  Personal Web: $publicPersonalWebUrl"
Write-Host "  React Admin:  $publicAdminWebReactUrl"
Write-LocalValidationReactAdminBanner -Port $adminWebReactPort -PublicHost $resolvedPublicHost -ApiDescription "same-origin /api (proxy $reactApiProxyTarget)" -GitSha $gitSha
Write-LocalValidationMailpitBanner -UiPort $mailpitUiPort -PublicHost $resolvedPublicHost -EmailLinkBaseUrl $publicAdminWebReactUrl
Write-Host "  Bind:         0.0.0.0:$adminPort / 0.0.0.0:$platformApiPort / 0.0.0.0:$posApiPort / 0.0.0.0:$orgWebPort / 0.0.0.0:$personalWebPort / 0.0.0.0:$adminWebReactPort / React POS :$reactPosPort"
Write-Host "  Platform DB:  127.0.0.1:$platformDbPort"
Write-Host "  POS DB:       127.0.0.1:$posDbPort"
Write-Host "  Mailpit UI:   http://localhost:$mailpitUiPort"
Write-Host "  Mailpit SMTP: 127.0.0.1:$mailpitSmtpPort"
Write-Host "  Auth emails:  $authPublicBaseUrl/admin/activate-account|reset-password"
Write-Host "  DP keys:      $dpKeys"
Write-Host '=========================================' -ForegroundColor Green
Show-LocalValidationFirewallGuidance
Write-Note 'Activation/reset Mailpit links default to React Admin http://127.0.0.1:8095 (not Tailscale). Override with EXITS_ADMIN_PUBLIC_BASE_URL when testing from another device.'
Write-Note 'If the browser still has an old localhost antiforgery cookie, open an Incognito window or clear localhost site data once.'
Write-Host 'Stop apps:  .\tools\Stop-LocalValidation.ps1'
Write-Host 'Stop DBs:   .\tools\Stop-LocalValidation.ps1 -StopDatabases   (volumes preserved)'
Write-Host 'API-only + Mailpit auth: .\tools\Start-PlatformApiOnly.ps1'
Write-Host 'Optional backend HMR: .\tools\Start-LocalValidation.ps1 -BackendMode Watch'

if (-not $healthOk) {
    Write-Fail 'One or more health checks failed - see messages above.'
    exit 1
}

Write-LocalValidationRuntimeSummary -PortLabels $appPortLabels -ExpectedRepoRoot $repoRoot -Mode 'HostApps'
Assert-LocalValidationPortsOwnedByExpectedWorktree -PortLabels $appPortLabels -ExpectedRepoRoot $repoRoot

Write-Ok 'All health checks passed.'
exit 0
