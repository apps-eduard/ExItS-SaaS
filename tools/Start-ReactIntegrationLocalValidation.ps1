#Requires -Version 5.1
<#
.SYNOPSIS
  Local Validation launcher for the React Platform Admin integration branch + current React POS.

.DESCRIPTION
  Starts a host-app Local Validation stack for PA-INTEGRATION-01:

  - PostgreSQL + Mailpit (Docker volumes preserved; never compose down -v)
  - Platform API :8091 from THIS integration worktree
  - POS API :8092 from the current feat/pos-react-client HEAD (no detach/downgrade)
  - React Platform Admin Vite :8095 from THIS worktree (never Blazor Admin as React Admin)
  - React POS Vite :5177 from the POS worktree (separate window; adb reverse when emulator present)

  Does NOT merge to main. Does NOT start Blazor Admin as the React Admin surface.

.EXAMPLE
  .\tools\Start-ReactIntegrationLocalValidation.ps1
#>
[CmdletBinding()]
param(
    [string]$PlatformRepoRoot = "",
    [string]$PosRepoRoot = "C:\Users\speed\Desktop\ExItS-SaaS-pos-react-client",
    [string]$EnvFile = "",
    [int]$PortWaitSeconds = 180,
    [switch]$SkipReactPos
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "LocalValidation.stack.ps1")

function Write-Step([string]$Message) { Write-Host "[pa-integration] $Message" -ForegroundColor Cyan }
function Write-Ok([string]$Message) { Write-Host "[pa-integration] OK $Message" -ForegroundColor Green }
function Write-Note([string]$Message) { Write-Host "[pa-integration] NOTE $Message" -ForegroundColor Yellow }

function ConvertTo-EnvAssignments([hashtable]$EnvMap) {
    ($EnvMap.GetEnumerator() | ForEach-Object {
        '$env:{0}=''{1}''' -f $_.Key, ([string]$_.Value).Replace("'", "''")
    }) -join "; "
}

function Wait-TcpPort {
    param(
        [Parameter(Mandatory)][string]$Label,
        [Parameter(Mandatory)][string]$HostName,
        [Parameter(Mandatory)][int]$Port,
        [Parameter(Mandatory)][int]$TimeoutSeconds
    )
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            $client = New-Object System.Net.Sockets.TcpClient
            $client.Connect($HostName, $Port)
            $client.Close()
            Write-Ok "$Label listening on ${HostName}:$Port"
            return
        }
        catch {
            Start-Sleep -Seconds 2
        }
    }
    throw "$Label did not listen on ${HostName}:$Port within ${TimeoutSeconds}s."
}

function Start-DotNetWatchWindow {
    param(
        [string]$Title,
        [string]$RepoRoot,
        [string]$Project,
        [hashtable]$EnvMap
    )
    $prefix = ConvertTo-EnvAssignments -EnvMap $EnvMap
    $run = @"
`$Host.UI.RawUI.WindowTitle = '$Title';
Remove-Item Env:DOTNET_ENVIRONMENT -ErrorAction SilentlyContinue;
Remove-Item Env:ASPNETCORE_ENVIRONMENT -ErrorAction SilentlyContinue;
$prefix
if (-not [string]::IsNullOrWhiteSpace(`$env:ASPNETCORE_ENVIRONMENT)) { `$env:DOTNET_ENVIRONMENT = `$env:ASPNETCORE_ENVIRONMENT }
Set-Location '$RepoRoot';
Write-Host ('=== {0} (ASPNETCORE_ENVIRONMENT={1}) ===' -f '$Title', `$env:ASPNETCORE_ENVIRONMENT) -ForegroundColor Cyan;
dotnet watch --project '$Project' run --no-launch-profile --non-interactive
"@
    $proc = Start-Process -FilePath "powershell.exe" -PassThru -ArgumentList @(
        "-NoExit", "-NoProfile", "-ExecutionPolicy", "Bypass", "-Command", $run
    )
    return $proc.Id
}

function Start-NpmDevWindow {
    param(
        [string]$Title,
        [string]$WorkingDirectory,
        [hashtable]$EnvMap,
        [string]$NpmScript = "dev"
    )
    $prefix = ConvertTo-EnvAssignments -EnvMap $EnvMap
    $run = @"
`$Host.UI.RawUI.WindowTitle = '$Title';
$prefix
Set-Location '$WorkingDirectory';
Write-Host ('=== {0} ===' -f '$Title') -ForegroundColor Cyan;
npm run $NpmScript
"@
    $proc = Start-Process -FilePath "powershell.exe" -PassThru -ArgumentList @(
        "-NoExit", "-NoProfile", "-ExecutionPolicy", "Bypass", "-Command", $run
    )
    return $proc.Id
}

function Resolve-EnvFilePath {
    param([string]$PlatformRoot, [string]$Override)
    if ($Override) { return $Override }
    $local = Join-Path $PlatformRoot "deploy\docker\$($LocalValidationStack.EnvFileName)"
    if (Test-Path -LiteralPath $local) { return $local }
    $fallback = "C:\Users\speed\Desktop\ExItS-SaaS\deploy\docker\$($LocalValidationStack.EnvFileName)"
    if (Test-Path -LiteralPath $fallback) { return $fallback }
    throw "Missing $($LocalValidationStack.EnvFileName). Provide -EnvFile or copy deploy/docker/.env.local-validation.example."
}

function Get-GitHead([string]$RepoRoot) {
    Push-Location $RepoRoot
    try { return (git rev-parse HEAD).Trim() }
    finally { Pop-Location }
}

function Get-GitBranch([string]$RepoRoot) {
    Push-Location $RepoRoot
    try { return (git branch --show-current).Trim() }
    finally { Pop-Location }
}

function Ensure-AdbReverse([int]$Port) {
    $adb = Get-Command adb -ErrorAction SilentlyContinue
    if (-not $adb) {
        Write-Note "adb not found; skip tcp:$Port reverse"
        return $false
    }
    $devices = & adb devices 2>$null | Select-String -Pattern "emulator-\d+\s+device"
    if (-not $devices) {
        Write-Note "No Android emulator device; skip adb reverse tcp:$Port"
        return $false
    }
    & adb reverse "tcp:$Port" "tcp:$Port" | Out-Null
    Write-Ok "adb reverse tcp:$Port tcp:$Port"
    return $true
}

if (-not $PlatformRepoRoot) {
    $PlatformRepoRoot = Get-LocalValidationRepoRoot -StartPath $PSScriptRoot
}

if (-not (Test-Path -LiteralPath $PosRepoRoot)) {
    throw "POS worktree not found: $PosRepoRoot"
}

$platformBranch = Get-GitBranch -RepoRoot $PlatformRepoRoot
$platformSha = Get-GitHead -RepoRoot $PlatformRepoRoot
$posBranch = Get-GitBranch -RepoRoot $PosRepoRoot
$posSha = Get-GitHead -RepoRoot $PosRepoRoot

if ($platformBranch -ne "feat/platform-admin-react-integration" -and $platformBranch -ne "") {
    Write-Note "Platform branch is '$platformBranch' (expected feat/platform-admin-react-integration)."
}

Write-Step "Platform: $PlatformRepoRoot"
Write-Host "  branch=$platformBranch"
Write-Host "  sha=$platformSha"
Write-Step "POS:      $PosRepoRoot"
Write-Host "  branch=$posBranch"
Write-Host "  sha=$posSha"
Write-Note "POS HEAD is used as-is (no detach/downgrade)."

$envFile = Resolve-EnvFilePath -PlatformRoot $PlatformRepoRoot -Override $EnvFile
$composeFile = Join-Path $PlatformRepoRoot "deploy\docker\$($LocalValidationStack.ComposeFileName)"
if (-not (Test-Path -LiteralPath $composeFile)) {
    throw "Missing compose file: $composeFile"
}

Test-LocalValidationDockerAvailable
$envMap = Import-LocalValidationDotEnv -Path $envFile
Require-LocalValidationEnvKey -Map $envMap -Key "LOCAL_VALIDATION_PLATFORM_DB_USER"
Require-LocalValidationEnvKey -Map $envMap -Key "LOCAL_VALIDATION_PLATFORM_DB_PASSWORD"
Require-LocalValidationEnvKey -Map $envMap -Key "LOCAL_VALIDATION_POS_DB_USER"
Require-LocalValidationEnvKey -Map $envMap -Key "LOCAL_VALIDATION_POS_DB_PASSWORD"
Require-LocalValidationEnvKey -Map $envMap -Key "LOCAL_VALIDATION_SHARED_PASSWORD"

$platformDbPort = if ($envMap["LOCAL_VALIDATION_PLATFORM_DB_HOST_PORT"]) { [int]$envMap["LOCAL_VALIDATION_PLATFORM_DB_HOST_PORT"] } else { [int]$LocalValidationStack.DefaultPlatformDbPort }
$posDbPort = if ($envMap["LOCAL_VALIDATION_POS_DB_HOST_PORT"]) { [int]$envMap["LOCAL_VALIDATION_POS_DB_HOST_PORT"] } else { [int]$LocalValidationStack.DefaultPosDbPort }
$platformApiPort = if ($envMap["LOCAL_VALIDATION_PLATFORM_API_HOST_PORT"]) { [int]$envMap["LOCAL_VALIDATION_PLATFORM_API_HOST_PORT"] } else { [int]$LocalValidationStack.DefaultPlatformApiPort }
$posApiPort = if ($envMap["LOCAL_VALIDATION_POS_API_HOST_PORT"]) { [int]$envMap["LOCAL_VALIDATION_POS_API_HOST_PORT"] } else { [int]$LocalValidationStack.DefaultPosApiPort }
$adminWebReactPort = if ($envMap["LOCAL_VALIDATION_ADMIN_WEB_REACT_HOST_PORT"]) { [int]$envMap["LOCAL_VALIDATION_ADMIN_WEB_REACT_HOST_PORT"] } else { [int]$LocalValidationStack.DefaultAdminWebReactPort }
$reactPosPort = 5177

Write-Step "Stopping conflicting repo-scoped host apps..."
$null = Stop-LocalValidationRepoScopedHostApps -RepoRoot $PlatformRepoRoot
$null = Stop-LocalValidationRepoScopedHostApps -RepoRoot $PosRepoRoot

# Stop stale Docker React Admin container if it holds :8095 — host Vite is the integration surface.
$composeProject = $LocalValidationStack.ComposeProjectName
$prev = $ErrorActionPreference
$ErrorActionPreference = "Continue"
try {
    docker compose -p $composeProject -f $composeFile --env-file $envFile stop admin-web-react 2>$null | Out-Null
    docker compose -p $composeProject -f $composeFile --env-file $envFile rm -f admin-web-react 2>$null | Out-Null
}
finally {
    $ErrorActionPreference = $prev
}

$blocked = @(Report-LocalValidationPortConflicts -Ports @($platformApiPort, $posApiPort, $adminWebReactPort, $reactPosPort))
if ($blocked.Count -gt 0) {
    throw "Required ports occupied: $($blocked -join ', '). Free them and retry."
}

Write-Step "Starting PostgreSQL + Mailpit (volumes preserved)..."
Start-LocalValidationInfrastructure -ComposeFile $composeFile -EnvFile $envFile
Wait-TcpPort -Label "Platform DB" -HostName "127.0.0.1" -Port $platformDbPort -TimeoutSeconds 60
Wait-TcpPort -Label "POS DB" -HostName "127.0.0.1" -Port $posDbPort -TimeoutSeconds 60

$platformCs = "Host=127.0.0.1;Port=$platformDbPort;Database=$($LocalValidationStack.PlatformDbName);Username=$($envMap["LOCAL_VALIDATION_PLATFORM_DB_USER"]);Password=$($envMap["LOCAL_VALIDATION_PLATFORM_DB_PASSWORD"])"
$posCs = "Host=127.0.0.1;Port=$posDbPort;Database=$($LocalValidationStack.PosDbName);Username=$($envMap["LOCAL_VALIDATION_POS_DB_USER"]);Password=$($envMap["LOCAL_VALIDATION_POS_DB_PASSWORD"])"
$loopbackPlatformApiUrl = "http://127.0.0.1:$platformApiPort"
$loopbackPosApiUrl = "http://127.0.0.1:$posApiPort"
$reactAdminUrl = "http://127.0.0.1:$adminWebReactPort"
$reactPosUrl = "http://127.0.0.1:$reactPosPort"
$allowedHosts = Get-LocalValidationAllowedHostsList -PublicHostValue "" -EnvMap $envMap
$corsOrigins = @(
    "http://localhost:$adminWebReactPort",
    "http://127.0.0.1:$adminWebReactPort",
    "http://localhost:$reactPosPort",
    "http://127.0.0.1:$reactPosPort"
)

$platformProject = Join-Path $PlatformRepoRoot "src\Platform\ExItS.Platform.Api\ExItS.Platform.Api.csproj"
$posProject = Join-Path $PosRepoRoot "src\Products\PinoyBusinessPOS\ExItS.PinoyBusinessPOS.Api\ExItS.PinoyBusinessPOS.Api.csproj"
$adminWebDir = Join-Path $PlatformRepoRoot "src\Platform\ExItS.Platform.Admin.Web"
$posClientDir = Join-Path $PosRepoRoot "src\Products\PinoyBusinessPOS\ExItS.PinoyBusinessPOS.Client"

if (-not (Test-Path -LiteralPath $adminWebDir)) { throw "Missing React Admin project: $adminWebDir" }
if (-not (Test-Path -LiteralPath $posProject)) { throw "Missing POS API project: $posProject" }
if (-not $SkipReactPos -and -not (Test-Path -LiteralPath $posClientDir)) {
    throw "Missing React POS client: $posClientDir"
}

Write-Step "Starting Platform API from integration worktree (Staging + LocalValidation)..."
$platformEnv = @{
    ASPNETCORE_ENVIRONMENT = "Staging"
    ASPNETCORE_URLS = "http://0.0.0.0:$platformApiPort"
    ConnectionStrings__PlatformDatabase = $platformCs
    AllowedHosts = $allowedHosts
    Security__EnforceHttps = "false"
    LocalValidation__Enabled = "true"
    LocalValidation__SeedScope = "PlatformAdministratorsOnly"
    LocalValidation__SharedPassword = [string]$envMap["LOCAL_VALIDATION_SHARED_PASSWORD"]
    PlatformAuthentication__Lifecycle__ExposeDebugTokens = "false"
    PlatformAuthentication__External__TestingEndpointEnabled = "false"
    PlatformAuthentication__Password__MinimumLength = "1"
    PlatformAuthentication__Password__RequireUppercase = "false"
    PlatformAuthentication__Password__RequireLowercase = "false"
    PlatformAuthentication__Password__RequireDigit = "false"
    PlatformAuthentication__Password__RequireNonAlphanumeric = "false"
    # Host-run API (not Docker network): Mailpit is on the loopback published ports.
    PlatformEmail__SmtpHost = "127.0.0.1"
    PlatformEmail__SmtpPort = "1025"
    PlatformEmail__UseSsl = "false"
    PlatformEmail__FromAddress = "noreply@exits.local"
    PlatformEmail__FromDisplayName = "ExItS Local Validation"
    PlatformEmail__AdminPublicBaseUrl = $reactAdminUrl
}
for ($i = 0; $i -lt $corsOrigins.Count; $i++) {
    $platformEnv["Cors__AllowedOrigins__$i"] = $corsOrigins[$i]
}
$windowPids = @(
    (Start-DotNetWatchWindow -Title "PA-INTEGRATION Platform API" -RepoRoot $PlatformRepoRoot -Project $platformProject -EnvMap $platformEnv)
)
Wait-TcpPort -Label "Platform API" -HostName "127.0.0.1" -Port $platformApiPort -TimeoutSeconds $PortWaitSeconds

Write-Step "Starting POS API from current POS worktree HEAD..."
$posEnv = @{
    ASPNETCORE_ENVIRONMENT = "Staging"
    ASPNETCORE_URLS = "http://0.0.0.0:$posApiPort"
    ConnectionStrings__PosDatabase = $posCs
    AllowedHosts = $allowedHosts
    Security__EnforceHttps = "false"
    LocalValidation__Enabled = "true"
    LocalValidation__PlatformApiBaseUrl = $loopbackPlatformApiUrl
    PlatformAuth__BaseUrl = $loopbackPlatformApiUrl
}
for ($i = 0; $i -lt $corsOrigins.Count; $i++) {
    $posEnv["Cors__AllowedOrigins__$i"] = $corsOrigins[$i]
}
$windowPids += Start-DotNetWatchWindow -Title "PA-INTEGRATION POS API" -RepoRoot $PosRepoRoot -Project $posProject -EnvMap $posEnv
Wait-TcpPort -Label "POS API" -HostName "127.0.0.1" -Port $posApiPort -TimeoutSeconds $PortWaitSeconds

Write-Step "Starting React Platform Admin Vite on :$adminWebReactPort (host, not Blazor, not Docker)..."
$adminEnv = @{
    VITE_PLATFORM_API_PROXY_TARGET = $loopbackPlatformApiUrl
    VITE_BUILD_SHA = $platformSha
    EXITS_GIT_SHA = $platformSha
    # Enables weak-password client policy + Development Test User (via Vite /config.js plugin).
    LOCAL_VALIDATION_TOOLS_ENABLED = "true"
    PLATFORM_API_SAME_ORIGIN = "true"
}
# Empty API base URL => browser same-origin /api via Vite proxy (cookie-friendly for Local Validation HTTP).
$windowPids += Start-NpmDevWindow -Title "PA-INTEGRATION React Admin" -WorkingDirectory $adminWebDir -EnvMap $adminEnv -NpmScript "dev"
Wait-TcpPort -Label "React Admin" -HostName "127.0.0.1" -Port $adminWebReactPort -TimeoutSeconds $PortWaitSeconds

if (-not $SkipReactPos) {
    Write-Step "Starting React POS Vite on :$reactPosPort..."
    $posClientEnv = @{
        VITE_POS_BUILD_SHA = $posSha
    }
    $windowPids += Start-NpmDevWindow -Title "PA-INTEGRATION React POS" -WorkingDirectory $posClientDir -EnvMap $posClientEnv -NpmScript "dev"
    Wait-TcpPort -Label "React POS" -HostName "127.0.0.1" -Port $reactPosPort -TimeoutSeconds $PortWaitSeconds
    $null = Ensure-AdbReverse -Port $reactPosPort
    $null = Ensure-AdbReverse -Port $platformApiPort
}
else {
    Write-Note "SkipReactPos set - start React POS manually: cd $posClientDir; npm run dev"
}

$stateDir = Join-Path $env:LOCALAPPDATA "ExItS\LocalValidation"
New-Item -ItemType Directory -Force -Path $stateDir | Out-Null
$provenance = [ordered]@{
    package = "PA-INTEGRATION-01"
    platformBranch = $platformBranch
    platformRuntimeSha = $platformSha
    posBranch = $posBranch
    posRuntimeSha = $posSha
    reactAdminUrl = "$reactAdminUrl/admin"
    platformApiUrl = $loopbackPlatformApiUrl
    posApiUrl = $loopbackPosApiUrl
    reactPosUrl = $reactPosUrl
    blazorAdminUsedAsReactAdmin = $false
    posDowngraded = $false
    envFile = $envFile
    startedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
}
$provenancePath = Join-Path $stateDir "pa-integration-provenance.json"
$provenance | ConvertTo-Json | Set-Content -LiteralPath $provenancePath -Encoding UTF8
$launcherState = [ordered]@{
    Mode = "ReactIntegration"
    PlatformRepoRoot = $PlatformRepoRoot
    PosRepoRoot = $PosRepoRoot
    WindowPids = $windowPids
    ProvenancePath = $provenancePath
}
$launcherState | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $stateDir "pa-integration-launcher-state.json") -Encoding UTF8

Write-Host ""
Write-Ok "React Admin:  $reactAdminUrl/admin"
Write-Ok "Platform API: $loopbackPlatformApiUrl"
Write-Ok "POS API:      $loopbackPosApiUrl"
Write-Ok "React POS:    $reactPosUrl"
Write-Ok "Emulator POS: http://127.0.0.1:$reactPosPort  (after adb reverse)"
Write-Ok "Provenance:   $provenancePath"
Write-Host ""
Write-Host "RUNTIME PROVENANCE"
Write-Host "  Platform branch/SHA: $platformBranch / $platformSha"
Write-Host "  POS branch/SHA:      $posBranch / $posSha"
Write-Host "  OLD_BLAZOR_ADMIN_USED_AS_REACT_ADMIN=NO"
Write-Host "  POS_DOWNGRADED=NO"
Write-Host ""

