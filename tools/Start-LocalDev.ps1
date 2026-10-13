#Requires -Version 5.1
<#
.SYNOPSIS
  Fast local development against the existing Local Validation PostgreSQL containers.

.DESCRIPTION
  Starts platform-db, pos-db, and Mailpit if they are not already running.
  Does not build or start production-like application images.
  Platform API and POS API run with dotnet watch.
  Personal, Organization, and POS UI run as the canonical React Vite app.
  Platform Admin runs as the React Vite app.
  Host ports are separate from Docker validation ports (8091, 8092, 8095, 5177).
  Public preview containers and this local dev process use the same database volumes.
  Both can run together. Preview keeps serving the last built images until
  .\tools\Start-DockerLocalValidation.ps1 -Build refreshes them.
  This script does not apply EF migrations and does not delete volumes.

.PARAMETER IncludeLoanManager
  Also start PinoyLoanManager React Vite on its existing port 5176.

.EXAMPLE
  .\tools\Start-LocalDev.ps1
#>
[CmdletBinding()]
param(
    [int]$PortWaitSeconds = 180,
    [switch]$IncludeLoanManager,
    [string[]]$OnlyServices = @()
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LocalValidation.stack.ps1')
. (Join-Path $PSScriptRoot 'LocalValidation.host-apps.ps1')
. (Join-Path $PSScriptRoot 'LocalDev.stack.ps1')

function Write-Step([string]$Message) { Write-Host "[local-dev] $Message" -ForegroundColor Cyan }
function Write-Ok([string]$Message) { Write-Host "[local-dev] OK  $Message" -ForegroundColor Green }
function Write-Fail([string]$Message) { Write-Host "[local-dev] FAIL $Message" -ForegroundColor Red }

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

function Assert-PortFree([int]$Port, [string]$Label) {
    $owner = Get-ListeningOwner -Port $Port
    if ($null -eq $owner) { return }
    Write-Fail "$Label port $Port is already in use."
    Write-Fail ("Process: {0} (PID {1})" -f $owner.ProcessName, $owner.ProcessId)
    if (-not [string]::IsNullOrWhiteSpace([string]$owner.CommandLine)) {
        Write-Fail ("Command: {0}" -f $owner.CommandLine)
    }
    throw "Free port $Port and retry. This launcher does not stop unrelated processes."
}

function Assert-DockerAvailable {
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
        throw 'Docker CLI not found. Install/start Docker Desktop.'
    }
    & docker info 1>$null 2>$null
    if ($LASTEXITCODE -ne 0) {
        throw 'Docker Desktop is not available (docker info failed). Start Docker Desktop and retry.'
    }
}

function Test-ShouldStartLocalDevService([string]$Key) {
    if ($null -eq $OnlyServices -or @($OnlyServices).Count -eq 0) { return $true }
    $normalized = @($OnlyServices | ForEach-Object { ([string]$_).Trim().ToLowerInvariant() })
    return $normalized -contains $Key.Trim().ToLowerInvariant()
}

function Add-CorsOrigin([System.Collections.Generic.List[string]]$Origins, [string]$Origin) {
    if ([string]::IsNullOrWhiteSpace($Origin)) { return }
    if ($Origin.Trim() -eq '*') {
        throw 'Fast dev refuses wildcard CORS.'
    }
    if (-not $Origins.Contains($Origin)) {
        $Origins.Add($Origin)
    }
}

Test-LocalDevPortsDoNotOverlapValidation

$repoRoot = Get-LocalValidationRepoRoot
$dockerDir = Join-Path $repoRoot 'deploy\docker'
$envFile = Join-Path $dockerDir $LocalValidationStack.EnvFileName
$composeFile = Join-Path $dockerDir $LocalValidationStack.ComposeFileName
if (-not (Test-Path -LiteralPath $envFile)) {
    throw "Missing $envFile. Copy deploy/docker/.env.local-validation.example and fill REPLACE_* values. Do not commit that file."
}
$envMap = Import-DotEnv -Path $envFile
Require-EnvKey $envMap 'LOCAL_VALIDATION_PLATFORM_DB_USER'
Require-EnvKey $envMap 'LOCAL_VALIDATION_PLATFORM_DB_PASSWORD'
Require-EnvKey $envMap 'LOCAL_VALIDATION_POS_DB_USER'
Require-EnvKey $envMap 'LOCAL_VALIDATION_POS_DB_PASSWORD'
Require-EnvKey $envMap 'LOCAL_VALIDATION_SHARED_PASSWORD'

$platformDbPort = if ($envMap['LOCAL_VALIDATION_PLATFORM_DB_HOST_PORT']) { [int]$envMap['LOCAL_VALIDATION_PLATFORM_DB_HOST_PORT'] } else { [int]$LocalValidationStack.DefaultPlatformDbPort }
$posDbPort = if ($envMap['LOCAL_VALIDATION_POS_DB_HOST_PORT']) { [int]$envMap['LOCAL_VALIDATION_POS_DB_HOST_PORT'] } else { [int]$LocalValidationStack.DefaultPosDbPort }
$mailpitSmtpPort = if ($envMap['LOCAL_VALIDATION_MAILPIT_SMTP_HOST_PORT']) { [int]$envMap['LOCAL_VALIDATION_MAILPIT_SMTP_HOST_PORT'] } else { 1025 }

$platformApiPort = [int]$LocalDevStack.PlatformApiPort
$posApiPort = [int]$LocalDevStack.PosApiPort
$reactPosPort = [int]$LocalDevStack.ReactPosPort
$adminPort = [int]$LocalDevStack.AdminPort

Write-Step "Repository: $repoRoot"
Write-Step 'Checking Docker and fast-dev ports...'
Assert-DockerAvailable
Write-Ok 'Fast dev uses the same databases as the public preview. Preview containers can stay running.'

$partial = @($OnlyServices).Count -gt 0
foreach ($entry in @(
    @{ Key = 'platform-api'; Port = $platformApiPort; Label = 'Platform API' },
    @{ Key = 'pos-api'; Port = $posApiPort; Label = 'POS API' },
    @{ Key = 'react-pos'; Port = $reactPosPort; Label = 'React Personal/Organization/POS' },
    @{ Key = 'platform-admin'; Port = $adminPort; Label = 'Platform Admin' }
)) {
    if (-not (Test-ShouldStartLocalDevService ([string]$entry.Key))) { continue }
    Assert-PortFree -Port ([int]$entry.Port) -Label ([string]$entry.Label)
}
if ($IncludeLoanManager -and -not $partial) {
    Assert-PortFree -Port ([int]$LocalDevStack.LoanPort) -Label 'PinoyLoanManager'
}
Write-Ok 'Fast-dev ports are free'

Write-Step 'Starting shared PostgreSQL and Mailpit (no image build, volumes unchanged)...'
$composeExit = Invoke-LocalValidationDocker -DockerArgs @(
    'compose', '-p', $LocalValidationStack.ComposeProjectName,
    '-f', $composeFile, '--env-file', $envFile,
    'up', '-d', '--no-build', 'platform-db', 'pos-db', 'mailpit'
)
if ($composeExit -ne 0) {
    throw 'Failed to start platform-db, pos-db, and mailpit.'
}

$deadline = (Get-Date).AddSeconds(90)
while ((Get-Date) -lt $deadline) {
    $platformUp = Test-LocalValidationTcpOpen -Port $platformDbPort
    $posUp = Test-LocalValidationTcpOpen -Port $posDbPort
    if ($platformUp -and $posUp) { break }
    Start-Sleep -Seconds 2
}
if (-not (Test-LocalValidationTcpOpen -Port $platformDbPort) -or -not (Test-LocalValidationTcpOpen -Port $posDbPort)) {
    throw "Shared databases did not accept connections on 127.0.0.1:${platformDbPort} and 127.0.0.1:${posDbPort}."
}
Write-Ok "Shared databases ready on 127.0.0.1:${platformDbPort} (platform) and 127.0.0.1:${posDbPort} (pos)"
Write-Ok ("Volumes: {0}, {1}" -f $LocalValidationStack.PlatformDbVolume, $LocalValidationStack.PosDbVolume)

$platformCs = "Host=127.0.0.1;Port=$platformDbPort;Database=$($LocalValidationStack.PlatformDbName);Username=$($envMap['LOCAL_VALIDATION_PLATFORM_DB_USER']);Password=$($envMap['LOCAL_VALIDATION_PLATFORM_DB_PASSWORD'])"
$posCs = "Host=127.0.0.1;Port=$posDbPort;Database=$($LocalValidationStack.PosDbName);Username=$($envMap['LOCAL_VALIDATION_POS_DB_USER']);Password=$($envMap['LOCAL_VALIDATION_POS_DB_PASSWORD'])"

$origins = [System.Collections.Generic.List[string]]::new()
foreach ($origin in @(
    "http://127.0.0.1:$adminPort",
    "http://localhost:$adminPort",
    "http://127.0.0.1:$reactPosPort",
    "http://localhost:$reactPosPort",
    "http://10.0.2.2:$reactPosPort"
)) {
    Add-CorsOrigin -Origins $origins -Origin $origin
}
if ($IncludeLoanManager) {
    $loanPort = [int]$LocalDevStack.LoanPort
    Add-CorsOrigin -Origins $origins -Origin "http://127.0.0.1:$loanPort"
    Add-CorsOrigin -Origins $origins -Origin "http://localhost:$loanPort"
}

$dpKeys = Join-Path $env:LOCALAPPDATA $LocalDevStack.DataProtectionDirectory
New-Item -ItemType Directory -Force -Path $dpKeys | Out-Null
$stateDir = Join-Path $env:LOCALAPPDATA $LocalDevStack.StateDirectory
New-Item -ItemType Directory -Force -Path $stateDir | Out-Null

$platformProject = Join-Path $repoRoot 'src\Platform\ExItS.Platform.Api\ExItS.Platform.Api.csproj'
$posProject = Join-Path $repoRoot 'src\Products\PinoyBusinessPOS\ExItS.PinoyBusinessPOS.Api\ExItS.PinoyBusinessPOS.Api.csproj'
$reactDir = Join-Path $repoRoot 'src\Products\PinoyBusinessPOS\ExItS.PinoyBusinessPOS.React'
$adminDir = Join-Path $repoRoot 'src\Platform\ExItS.Platform.Admin.Web'

$platformEnv = @{
    ASPNETCORE_ENVIRONMENT = 'Development'
    ASPNETCORE_URLS = "http://127.0.0.1:$platformApiPort"
    ConnectionStrings__PlatformDatabase = $platformCs
    AllowedHosts = 'localhost;127.0.0.1;10.0.2.2'
    Security__EnforceHttps = 'false'
    LocalValidation__Enabled = 'true'
    LocalValidation__SeedScope = [string]$LocalValidationStack.DefaultSeedScope
    LocalValidation__PurgeTransactionalOnSeed = 'false'
    LocalValidation__SharedPassword = [string]$envMap['LOCAL_VALIDATION_SHARED_PASSWORD']
    PlatformAuthentication__Password__MinimumLength = '1'
    PlatformAuthentication__Password__RequireUppercase = 'false'
    PlatformAuthentication__Password__RequireLowercase = 'false'
    PlatformAuthentication__Password__RequireDigit = 'false'
    PlatformAuthentication__Password__RequireNonAlphanumeric = 'false'
    PlatformEmail__SmtpHost = '127.0.0.1'
    PlatformEmail__SmtpPort = "$mailpitSmtpPort"
    PlatformEmail__UseSsl = 'false'
    PlatformEmail__FromAddress = 'noreply@exits.local'
    PlatformEmail__FromDisplayName = 'ExItS Local Dev'
    PlatformEmail__AdminPublicBaseUrl = "http://127.0.0.1:$adminPort"
    PlatformEmail__PinoyBusinessPosPublicBaseUrl = "http://127.0.0.1:$reactPosPort"
    PlatformEmail__AllowHttpLoopbackPublicUrls = 'true'
    DataProtection__KeysPath = $dpKeys
    PosProductApi__BaseUrl = "http://127.0.0.1:$posApiPort"
}
if ($envMap.ContainsKey('LOCAL_VALIDATION_GOOGLE_CLIENT_ID') -and -not ([string]$envMap['LOCAL_VALIDATION_GOOGLE_CLIENT_ID']).StartsWith('REPLACE_')) {
    $platformEnv['PlatformAuthentication__External__Google__Enabled'] = 'true'
    $platformEnv['PlatformAuthentication__External__Google__ClientId'] = [string]$envMap['LOCAL_VALIDATION_GOOGLE_CLIENT_ID']
    $platformEnv['PlatformAuthentication__External__Google__ClientSecret'] = [string]$envMap['LOCAL_VALIDATION_GOOGLE_CLIENT_SECRET']
    # Google must redirect to the Vite origin. The registered redirect URI is
    # http://127.0.0.1:<ReactPosPort>/platform-api/api/v1/platform/auth/external/google/callback
    $platformEnv['PlatformAuthentication__External__PublicBrowserOrigin'] = "http://127.0.0.1:$reactPosPort"
    $platformEnv['PlatformAuthentication__External__TrustedProxyHost'] = '127.0.0.1'
}
$payMongoSecret = ''
if ($envMap.ContainsKey('LOCAL_VALIDATION_PAYMONGO_SECRET_KEY')) {
    $payMongoSecret = [string]$envMap['LOCAL_VALIDATION_PAYMONGO_SECRET_KEY']
}
if (-not [string]::IsNullOrWhiteSpace($payMongoSecret) -and -not $payMongoSecret.StartsWith('REPLACE_')) {
    $platformEnv['PayMongo__SecretKey'] = $payMongoSecret
    $platformEnv['PayMongo__PublicAppBaseUrl'] = "http://127.0.0.1:$reactPosPort"
    $payMongoWebhook = ''
    if ($envMap.ContainsKey('LOCAL_VALIDATION_PAYMONGO_WEBHOOK_SECRET')) {
        $payMongoWebhook = [string]$envMap['LOCAL_VALIDATION_PAYMONGO_WEBHOOK_SECRET']
    }
    if (-not [string]::IsNullOrWhiteSpace($payMongoWebhook) -and -not $payMongoWebhook.StartsWith('REPLACE_')) {
        $platformEnv['PayMongo__WebhookSecret'] = $payMongoWebhook
    }
    Write-Ok "PayMongo checkout is enabled for http://127.0.0.1:$reactPosPort"
} else {
    Write-Host '[local-dev] NOTE PayMongo secret is not set. Subscription checkout stays unavailable.' -ForegroundColor Yellow
}
for ($i = 0; $i -lt $origins.Count; $i++) {
    $platformEnv["Cors__AllowedOrigins__$i"] = $origins[$i]
}

$windowPids = @()
if (Test-ShouldStartLocalDevService 'platform-api') {
Write-Step "Starting Platform API with dotnet watch on $platformApiPort..."
$platformLaunch = Start-LocalValidationAppWindow `
    -Title 'ExItS LocalDev - Platform API' `
    -RepoRoot $repoRoot `
    -Project $platformProject `
    -EnvMap $platformEnv `
    -Mode Watch `
    -ServiceKey 'localdev-platform-api'
$windowPids += $platformLaunch.WindowProcessId
$platformReady = Wait-LocalServiceReady `
    -ServiceName 'Platform API' `
    -HealthUri "http://127.0.0.1:$platformApiPort/health" `
    -TimeoutSeconds $PortWaitSeconds `
    -WindowProcessId $platformLaunch.WindowProcessId `
    -ExitMarkerPath $platformLaunch.ExitMarkerPath
Write-Ok ("Platform API ready ({0}s) http://127.0.0.1:{1}/health" -f $platformReady.ReadyInSeconds, $platformApiPort)
}

$posEnv = @{
    ASPNETCORE_ENVIRONMENT = 'Development'
    ASPNETCORE_URLS = "http://127.0.0.1:$posApiPort"
    ConnectionStrings__PosDatabase = $posCs
    AllowedHosts = 'localhost;127.0.0.1;10.0.2.2'
    Security__EnforceHttps = 'false'
    LocalValidation__Enabled = 'true'
    LocalValidation__PlatformApiBaseUrl = "http://127.0.0.1:$platformApiPort"
    PlatformAuth__BaseUrl = "http://127.0.0.1:$platformApiPort"
    PosDeviceAuthorization__EnforcementEnabled = 'false'
    DataProtection__KeysPath = $dpKeys
}
for ($i = 0; $i -lt $origins.Count; $i++) {
    $posEnv["Cors__AllowedOrigins__$i"] = $origins[$i]
}

if (Test-ShouldStartLocalDevService 'pos-api') {
Write-Step "Starting POS API with dotnet watch on $posApiPort..."
$posLaunch = Start-LocalValidationAppWindow `
    -Title 'ExItS LocalDev - POS API' `
    -RepoRoot $repoRoot `
    -Project $posProject `
    -EnvMap $posEnv `
    -Mode Watch `
    -ServiceKey 'localdev-pos-api'
$windowPids += $posLaunch.WindowProcessId
$posReady = Wait-LocalServiceReady `
    -ServiceName 'POS API' `
    -HealthUri "http://127.0.0.1:$posApiPort/health" `
    -TimeoutSeconds $PortWaitSeconds `
    -WindowProcessId $posLaunch.WindowProcessId `
    -ExitMarkerPath $posLaunch.ExitMarkerPath
Write-Ok ("POS API ready ({0}s) http://127.0.0.1:{1}/health" -f $posReady.ReadyInSeconds, $posApiPort)
}

$reactEnv = @{
    POS_DEV_PORT = "$reactPosPort"
    EXITS_PLATFORM_API_PROXY_TARGET = "http://127.0.0.1:$platformApiPort"
    EXITS_POS_API_PROXY_TARGET = "http://127.0.0.1:$posApiPort"
}
if (Test-ShouldStartLocalDevService 'react-pos') {
Write-Step "Starting React Personal, Organization, and POS Vite on $reactPosPort..."
$windowPids += Start-LocalValidationNpmDevWindow `
    -Title 'ExItS LocalDev - React POS' `
    -WorkingDirectory $reactDir `
    -EnvMap $reactEnv
}
$adminEnv = @{
    ADMIN_DEV_PORT = "$adminPort"
    VITE_PLATFORM_API_PROXY_TARGET = "http://127.0.0.1:$platformApiPort"
}
if (Test-ShouldStartLocalDevService 'platform-admin') {
Write-Step "Starting Platform Admin Vite on $adminPort..."
$windowPids += Start-LocalValidationNpmDevWindow `
    -Title 'ExItS LocalDev - Platform Admin' `
    -WorkingDirectory $adminDir `
    -EnvMap $adminEnv
}

if ($IncludeLoanManager -and -not $partial) {
    $loanDir = Join-Path $repoRoot 'src\Products\PinoyLoanManager\ExItS.PinoyLoanManager.Client'
    $loanEnv = @{
        EXITS_PLATFORM_API_PROXY_TARGET = "http://127.0.0.1:$platformApiPort"
    }
    Write-Step "Starting PinoyLoanManager Vite on $($LocalDevStack.LoanPort)..."
    $windowPids += Start-LocalValidationNpmDevWindow `
        -Title 'ExItS LocalDev - Loan Manager' `
        -WorkingDirectory $loanDir `
        -EnvMap $loanEnv
}

$frontPorts = @()
if (Test-ShouldStartLocalDevService 'react-pos') { $frontPorts += $reactPosPort }
if (Test-ShouldStartLocalDevService 'platform-admin') { $frontPorts += $adminPort }
if ($IncludeLoanManager -and -not $partial) { $frontPorts += [int]$LocalDevStack.LoanPort }
foreach ($port in $frontPorts) {
    $ready = $false
    $frontDeadline = (Get-Date).AddSeconds($PortWaitSeconds)
    while ((Get-Date) -lt $frontDeadline) {
        if (Test-LocalValidationTcpOpen -Port $port) { $ready = $true; break }
        Start-Sleep -Seconds 1
    }
    if (-not $ready) {
        throw "Vite did not listen on 127.0.0.1:$port. Check the LocalDev window."
    }
    Write-Ok "Vite ready on http://127.0.0.1:$port"
}

if (-not $partial) {
$state = [pscustomobject]@{
    Mode = 'LocalDev'
    StartedAtUtc = (Get-Date).ToUniversalTime().ToString('o')
    WindowPids = @($windowPids)
    PlatformApiPort = $platformApiPort
    PosApiPort = $posApiPort
    ReactPosPort = $reactPosPort
    AdminPort = $adminPort
    IncludeLoanManager = [bool]$IncludeLoanManager
    PlatformDbPort = $platformDbPort
    PosDbPort = $posDbPort
    PlatformDbVolume = [string]$LocalValidationStack.PlatformDbVolume
    PosDbVolume = [string]$LocalValidationStack.PosDbVolume
}
$state | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $stateDir $LocalDevStack.StateFileName) -Encoding utf8
}

Write-Host ''
Write-Ok 'Fast dev is running. Code edits do not rebuild Docker images.'
Write-Host ("  Platform API     http://127.0.0.1:{0}" -f $platformApiPort)
Write-Host ("  POS API          http://127.0.0.1:{0}" -f $posApiPort)
Write-Host ("  Personal/Org/POS http://127.0.0.1:{0}" -f $reactPosPort)
Write-Host ("  Platform Admin   http://127.0.0.1:{0}" -f $adminPort)
Write-Host ("  Mailpit          http://127.0.0.1:8025")
Write-Host ("  Platform DB      127.0.0.1:{0} volume {1}" -f $platformDbPort, $LocalValidationStack.PlatformDbVolume)
Write-Host ("  POS DB           127.0.0.1:{0} volume {1}" -f $posDbPort, $LocalValidationStack.PosDbVolume)
Write-Host '  Stop local apps: .\tools\Stop-LocalDev.ps1'
Write-Host '  Refresh public preview images: .\tools\Start-DockerLocalValidation.ps1 -Build'
exit 0
