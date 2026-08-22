#Requires -Version 5.1
<#
.SYNOPSIS
  Local Validation launcher for the React Platform Admin integration branch + current React POS.

.DESCRIPTION
  Starts a host-app Local Validation stack for PA-INTEGRATION (01/04):

  - PostgreSQL + Mailpit (Docker volumes preserved; never compose down -v)
  - Platform API :8091 from THIS integration worktree
  - POS API :8092 from the current feat/pos-react-client HEAD (no detach/downgrade)
  - React Platform Admin Vite :8095 from THIS worktree (never Blazor Admin as React Admin)
  - React POS :5177 from the POS worktree:
      default = npm run dev (Vite)
      -ReactPosDocker = Docker nginx SPA built from POS worktree (not this Platform tree)
  - Does not touch MAUI isolated Local Validation ports 8190-8194

  Does NOT merge to main. Does NOT start Blazor Admin as the React Admin surface.
  Does NOT copy or downgrade POS source into the Platform integration worktree.

.PARAMETER PublicHost
  Tailscale/LAN host or IP only (no scheme/port). When set, APIs/Vite bind 0.0.0.0,
  AllowedHosts/CORS include PublicHost, and the launcher prints PUBLIC URLs.
  Localhost/127.0.0.1 and Android emulator 10.0.2.2 remain supported.

.EXAMPLE
  .\tools\Start-ReactIntegrationLocalValidation.ps1

.EXAMPLE
  .\tools\Start-ReactIntegrationLocalValidation.ps1 -PublicHost 100.x.x.x

.EXAMPLE
  .\tools\Start-ReactIntegrationLocalValidation.ps1 -ReactPosDocker
#>
[CmdletBinding()]
param(
    [string]$PlatformRepoRoot = "",
    [string]$PosRepoRoot = "C:\Users\speed\Desktop\ExItS-SaaS-pos-react-client",
    [string]$EnvFile = "",
    [int]$PortWaitSeconds = 180,
    [string]$PublicHost = "",
    [switch]$SkipReactPos,
    [switch]$ReactPosDocker,
    [switch]$ReactPosDockerRebuild
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
        [string]$NpmScript = "dev",
        [string]$ExtraNpmArgs = ""
    )
    $prefix = ConvertTo-EnvAssignments -EnvMap $EnvMap
    $extra = if ([string]::IsNullOrWhiteSpace($ExtraNpmArgs)) { "" } else { " -- $ExtraNpmArgs" }
    $run = @"
`$Host.UI.RawUI.WindowTitle = '$Title';
$prefix
Set-Location '$WorkingDirectory';
Write-Host ('=== {0} ===' -f '$Title') -ForegroundColor Cyan;
npm run $NpmScript$extra
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

function Wait-HttpEndpoint {
    param(
        [Parameter(Mandatory)][string]$Label,
        [Parameter(Mandatory)][string]$Url,
        [Parameter(Mandatory)][int]$TimeoutSeconds
    )
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            $response = Invoke-WebRequest -Uri $Url -Method GET -UseBasicParsing -TimeoutSec 5
            if ([int]$response.StatusCode -ge 200 -and [int]$response.StatusCode -lt 500) {
                Write-Ok "$Label responded HTTP $([int]$response.StatusCode)"
                return
            }
        }
        catch {
            Start-Sleep -Seconds 2
        }
    }
    throw "$Label did not become ready at $Url within ${TimeoutSeconds}s."
}

function Stop-ReactPosDockerContainer {
    param(
        [Parameter(Mandatory)][string]$PosComposeFile,
        [Parameter(Mandatory)][string]$PosEnvFile
    )
    $composeProject = $LocalValidationStack.ComposeProjectName
    $prev = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        docker compose -p $composeProject -f $PosComposeFile --env-file $PosEnvFile --profile react-pos stop react-pos 2>$null | Out-Null
        docker compose -p $composeProject -f $PosComposeFile --env-file $PosEnvFile --profile react-pos rm -f react-pos 2>$null | Out-Null
        docker rm -f exits-local-validation-react-pos 2>$null | Out-Null
    }
    finally {
        $ErrorActionPreference = $prev
    }
}

function Start-ReactPosDockerFromPosWorktree {
    param(
        [Parameter(Mandatory)][string]$PosRepoRoot,
        [Parameter(Mandatory)][string]$PosComposeFile,
        [Parameter(Mandatory)][string]$PosEnvFile,
        [Parameter(Mandatory)][int]$PlatformApiPort,
        [Parameter(Mandatory)][int]$PosApiPort,
        [Parameter(Mandatory)][int]$ReactPosPort,
        [Parameter(Mandatory)][string]$PosSha,
        [string]$PublicHost = "",
        [switch]$Rebuild
    )

    Write-Step "Building/starting React POS Docker from POS worktree (context=$PosRepoRoot)..."
    Write-Host "  compose=$PosComposeFile"
    Write-Host "  image source SHA=$PosSha"
    Write-Note "Upstream APIs: host.docker.internal :$PlatformApiPort / :$PosApiPort (not Platform-tree POS sources)."

    $env:REACT_POS_PLATFORM_API_UPSTREAM = "http://host.docker.internal:$PlatformApiPort"
    $env:REACT_POS_POS_API_UPSTREAM = "http://host.docker.internal:$PosApiPort"
    $env:REACT_POS_PLATFORM_API_PROXY_HOST = "127.0.0.1:$PlatformApiPort"
    $env:REACT_POS_POS_API_PROXY_HOST = "127.0.0.1:$PosApiPort"
    $env:LOCAL_VALIDATION_REACT_POS_HOST_PORT = "$ReactPosPort"
    $env:LOCAL_VALIDATION_REACT_POS_ORIGIN = "http://127.0.0.1:$ReactPosPort"
    $env:LOCAL_VALIDATION_REACT_POS_ORIGIN_LOCALHOST = "http://localhost:$ReactPosPort"
    $env:LOCAL_VALIDATION_REACT_POS_ORIGIN_EMULATOR = "http://10.0.2.2:$ReactPosPort"
    if (-not [string]::IsNullOrWhiteSpace($PublicHost)) {
        $env:LOCAL_VALIDATION_REACT_POS_ORIGIN_PUBLIC = "http://${PublicHost}:$ReactPosPort"
    }
    else {
        Remove-Item Env:LOCAL_VALIDATION_REACT_POS_ORIGIN_PUBLIC -ErrorAction SilentlyContinue
    }

    Stop-ReactPosDockerContainer -PosComposeFile $PosComposeFile -PosEnvFile $PosEnvFile

    $composeBaseArgs = @(
        "compose", "-p", $LocalValidationStack.ComposeProjectName,
        "-f", $PosComposeFile, "--env-file", $PosEnvFile,
        "--profile", "react-pos"
    )

    if ($Rebuild) {
        $buildExit = Invoke-LocalValidationDocker -DockerArgs ($composeBaseArgs + @("build", "--no-cache", "react-pos"))
        if ($buildExit -ne 0) { throw "React POS Docker clean build failed ($buildExit)." }
    }

    $upArgs = $composeBaseArgs + @("up", "-d", "--build", "react-pos")
    $upExit = Invoke-LocalValidationDocker -DockerArgs $upArgs
    if ($upExit -ne 0) { throw "React POS Docker startup failed ($upExit)." }

    Wait-TcpPort -Label "React POS Docker" -HostName "127.0.0.1" -Port $ReactPosPort -TimeoutSeconds $PortWaitSeconds
    Wait-HttpEndpoint -Label "React POS /" -Url "http://127.0.0.1:$ReactPosPort/" -TimeoutSeconds $PortWaitSeconds
    Wait-HttpEndpoint -Label "React POS /sign-in" -Url "http://127.0.0.1:$ReactPosPort/sign-in" -TimeoutSeconds $PortWaitSeconds
}

function Test-ReactPosProxyLoginChain {
    param(
        [Parameter(Mandatory)][string]$ReactPosBaseUrl,
        [Parameter(Mandatory)][string]$SharedPassword
    )

    # Prefer a known Local Validation personal identity when seeded; fall back to short shared-password probe user.
    $candidates = @(
        @{ Username = "kizy@gmail.com"; Password = $SharedPassword },
        @{ Username = "kizy@gmail.com"; Password = "1" }
    )

    $lastError = $null
    foreach ($candidate in $candidates) {
        try {
            $session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
            $loginBody = @{ usernameOrEmail = $candidate.Username; password = $candidate.Password } | ConvertTo-Json
            $login = Invoke-WebRequest -Uri "$ReactPosBaseUrl/platform-api/api/v1/platform/auth/login" `
                -Method POST -ContentType "application/json" -Body $loginBody `
                -WebSession $session -UseBasicParsing -TimeoutSec 30
            if ([int]$login.StatusCode -ne 200) {
                throw "login HTTP $([int]$login.StatusCode)"
            }
            $me = Invoke-WebRequest -Uri "$ReactPosBaseUrl/platform-api/api/v1/platform/auth/me" `
                -Method GET -WebSession $session -UseBasicParsing -TimeoutSec 30
            if ([int]$me.StatusCode -ne 200) {
                throw "auth/me HTTP $([int]$me.StatusCode)"
            }
            $platformHealth = Invoke-WebRequest -Uri "$ReactPosBaseUrl/platform-api/health" `
                -Method GET -UseBasicParsing -TimeoutSec 15
            $posHealth = Invoke-WebRequest -Uri "$ReactPosBaseUrl/pos-api/health" `
                -Method GET -UseBasicParsing -TimeoutSec 15
            return [pscustomobject]@{
                Login = "PASS"
                AuthMe = "PASS"
                PlatformApiProxy = $(if ([int]$platformHealth.StatusCode -eq 200) { "PASS" } else { "FAIL" })
                PosApiProxy = $(if ([int]$posHealth.StatusCode -eq 200) { "PASS" } else { "FAIL" })
                Username = $candidate.Username
            }
        }
        catch {
            $lastError = $_
        }
    }
    throw "React POS proxy login chain failed. Last error: $lastError"
}

if (-not $PlatformRepoRoot) {
    $PlatformRepoRoot = Get-LocalValidationRepoRoot -StartPath $PSScriptRoot
}

if (-not (Test-Path -LiteralPath $PosRepoRoot)) {
    throw "POS worktree not found: $PosRepoRoot"
}

if ($SkipReactPos -and $ReactPosDocker) {
    throw "Use either -SkipReactPos or -ReactPosDocker, not both."
}

$platformBranch = Get-GitBranch -RepoRoot $PlatformRepoRoot
$platformSha = Get-GitHead -RepoRoot $PlatformRepoRoot
$posBranch = Get-GitBranch -RepoRoot $PosRepoRoot
$posSha = Get-GitHead -RepoRoot $PosRepoRoot

$expectedPlatformBranches = @(
    "feat/platform-admin-react-integration",
    "feat/tailscale-access-01"
)
if ($platformBranch -and ($expectedPlatformBranches -notcontains $platformBranch)) {
    Write-Note "Platform branch is '$platformBranch' (expected one of: $($expectedPlatformBranches -join ', '))."
}

if ($posBranch -ne "feat/pos-react-client") {
    throw "POS worktree must be on feat/pos-react-client (found '$posBranch' at $PosRepoRoot)."
}

Write-Step "Platform: $PlatformRepoRoot"
Write-Host "  branch=$platformBranch"
Write-Host "  sha=$platformSha"
Write-Step "POS:      $PosRepoRoot"
Write-Host "  branch=$posBranch"
Write-Host "  sha=$posSha"
Write-Note "POS HEAD is used as-is (no detach/downgrade)."
if ($ReactPosDocker) {
    Write-Note "React POS mode=Docker (built from POS worktree; exclusive with npm run dev on :5177)."
}
else {
    Write-Note "React POS mode=Vite npm run dev (default)."
}

$envFile = Resolve-EnvFilePath -PlatformRoot $PlatformRepoRoot -Override $EnvFile
$composeFile = Join-Path $PlatformRepoRoot "deploy\docker\$($LocalValidationStack.ComposeFileName)"
if (-not (Test-Path -LiteralPath $composeFile)) {
    throw "Missing compose file: $composeFile"
}

$posComposeFile = Join-Path $PosRepoRoot "deploy\docker\$($LocalValidationStack.ComposeFileName)"
$posEnvFile = Join-Path $PosRepoRoot "deploy\docker\$($LocalValidationStack.EnvFileName)"
if ($ReactPosDocker) {
    if (-not (Test-Path -LiteralPath $posComposeFile)) {
        throw "Missing POS compose file for React POS Docker: $posComposeFile"
    }
    if (-not (Test-Path -LiteralPath $posEnvFile)) {
        Write-Note "POS env file missing at $posEnvFile - using Platform env file for compose substitution."
        $posEnvFile = $envFile
    }
    if (-not (Test-Path -LiteralPath (Join-Path $PosRepoRoot "deploy\docker\Dockerfile.pos-react"))) {
        throw "Missing POS Dockerfile.pos-react under $PosRepoRoot (approved React POS Docker image must come from POS worktree)."
    }
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

# Stop stale Docker React Admin container if it holds :8095 - host Vite is the integration surface.
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

# Always clear React POS Docker binder when using Vite mode; Docker mode rebuilds it below.
if (-not $ReactPosDocker -and (Test-Path -LiteralPath $posComposeFile)) {
    $posEnvForStop = if (Test-Path -LiteralPath $posEnvFile) { $posEnvFile } else { $envFile }
    Stop-ReactPosDockerContainer -PosComposeFile $posComposeFile -PosEnvFile $posEnvForStop
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
$stateDir = Join-Path $env:LOCALAPPDATA "ExItS\LocalValidation"
New-Item -ItemType Directory -Force -Path $stateDir | Out-Null
$stateFile = Join-Path $stateDir "pa-integration-launcher-state.json"
$resolvedPublicHost = Resolve-LocalValidationEffectivePublicHost -ParamValue $PublicHost -EnvMap $envMap -StateFilePath $stateFile
if ($resolvedPublicHost) {
    Write-Ok "PublicHost: $resolvedPublicHost (Tailscale/LAN)"
}
else {
    Write-Note "No PublicHost (localhost-only URLs). Pass -PublicHost <ip> for Tailscale/LAN."
}

$loopbackPlatformApiUrl = "http://127.0.0.1:$platformApiPort"
$loopbackPosApiUrl = "http://127.0.0.1:$posApiPort"
$reactAdminUrl = "http://127.0.0.1:$adminWebReactPort"
$reactPosUrl = "http://127.0.0.1:$reactPosPort"
$publicReactAdminUrl = if ($resolvedPublicHost) { "http://${resolvedPublicHost}:$adminWebReactPort" } else { $null }
$publicReactPosUrl = if ($resolvedPublicHost) { "http://${resolvedPublicHost}:$reactPosPort" } else { $null }
$publicPlatformApiUrl = if ($resolvedPublicHost) { "http://${resolvedPublicHost}:$platformApiPort" } else { $null }
$publicPosApiUrl = if ($resolvedPublicHost) { "http://${resolvedPublicHost}:$posApiPort" } else { $null }
# Email magic links / AdminPublicBaseUrl: prefer PublicHost so phones on Tailscale open the right origin.
$adminPublicBaseUrl = if ($resolvedPublicHost) { $publicReactAdminUrl } else { $reactAdminUrl }

$allowedHosts = Get-LocalValidationAllowedHostsList -PublicHostValue $resolvedPublicHost -EnvMap $envMap
$corsOrigins = @(
    "http://localhost:$adminWebReactPort",
    "http://127.0.0.1:$adminWebReactPort",
    "http://localhost:$reactPosPort",
    "http://127.0.0.1:$reactPosPort"
)
if ($resolvedPublicHost) {
    $corsOrigins += "http://${resolvedPublicHost}:$adminWebReactPort"
    $corsOrigins += "http://${resolvedPublicHost}:$reactPosPort"
}
Write-Ok "AllowedHosts: $allowedHosts"
Write-Ok ("CORS origins: {0}" -f ($corsOrigins -join ', '))

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
    PlatformEmail__AdminPublicBaseUrl = $adminPublicBaseUrl
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
# host 0.0.0.0 so Tailscale/LAN can reach Admin; localhost still works.
$windowPids += Start-NpmDevWindow -Title "PA-INTEGRATION React Admin" -WorkingDirectory $adminWebDir -EnvMap $adminEnv -NpmScript "dev" -ExtraNpmArgs "--host 0.0.0.0 --port $adminWebReactPort"
Wait-TcpPort -Label "React Admin" -HostName "127.0.0.1" -Port $adminWebReactPort -TimeoutSeconds $PortWaitSeconds

if (-not $SkipReactPos) {
    if ($ReactPosDocker) {
        Start-ReactPosDockerFromPosWorktree `
            -PosRepoRoot $PosRepoRoot `
            -PosComposeFile $posComposeFile `
            -PosEnvFile $posEnvFile `
            -PlatformApiPort $platformApiPort `
            -PosApiPort $posApiPort `
            -ReactPosPort $reactPosPort `
            -PosSha $posSha `
            -PublicHost $resolvedPublicHost `
            -Rebuild:$ReactPosDockerRebuild
        $null = Ensure-AdbReverse -Port $reactPosPort
        $null = Ensure-AdbReverse -Port $platformApiPort
    }
    else {
        Write-Step "Starting React POS Vite on :$reactPosPort (0.0.0.0 for Tailscale/LAN)..."
        $posClientEnv = @{
            VITE_POS_BUILD_SHA = $posSha
        }
        $windowPids += Start-NpmDevWindow -Title "PA-INTEGRATION React POS" -WorkingDirectory $posClientDir -EnvMap $posClientEnv -NpmScript "dev" -ExtraNpmArgs "--host 0.0.0.0 --port $reactPosPort"
        Wait-TcpPort -Label "React POS" -HostName "127.0.0.1" -Port $reactPosPort -TimeoutSeconds $PortWaitSeconds
        $null = Ensure-AdbReverse -Port $reactPosPort
        $null = Ensure-AdbReverse -Port $platformApiPort
    }
}
else {
    Write-Note "SkipReactPos set - start React POS manually: cd $posClientDir; npm run dev"
}

$reactPosMode = if ($SkipReactPos) { "Skipped" } elseif ($ReactPosDocker) { "Docker" } else { "Vite" }
$proxyValidation = $null
if ($ReactPosDocker -and -not $SkipReactPos) {
    Write-Step "Validating React POS Docker login + /auth/me + API proxies..."
    $proxyValidation = Test-ReactPosProxyLoginChain `
        -ReactPosBaseUrl $reactPosUrl `
        -SharedPassword ([string]$envMap["LOCAL_VALIDATION_SHARED_PASSWORD"])
    Write-Ok "LOGIN=$($proxyValidation.Login) AUTH_ME=$($proxyValidation.AuthMe) /platform-api=$($proxyValidation.PlatformApiProxy) /pos-api=$($proxyValidation.PosApiProxy)"
}

$stateDir = Join-Path $env:LOCALAPPDATA "ExItS\LocalValidation"
New-Item -ItemType Directory -Force -Path $stateDir | Out-Null
$provenance = [ordered]@{
    package = "TAILSCALE-ACCESS-01"
    platformBranch = $platformBranch
    platformRuntimeSha = $platformSha
    posBranch = $posBranch
    posRuntimeSha = $posSha
    publicHost = $resolvedPublicHost
    posDockerFromPosWorktree = [bool]$ReactPosDocker
    reactPosMode = $reactPosMode
    reactAdminUrl = "$reactAdminUrl/admin"
    platformApiUrl = $loopbackPlatformApiUrl
    posApiUrl = $loopbackPosApiUrl
    reactPosUrl = $reactPosUrl
    publicReactAdminUrl = $(if ($publicReactAdminUrl) { "$publicReactAdminUrl/admin" } else { $null })
    publicPlatformApiUrl = $publicPlatformApiUrl
    publicPosApiUrl = $publicPosApiUrl
    publicReactPosUrl = $publicReactPosUrl
    blazorAdminUsedAsReactAdmin = $false
    posDowngraded = $false
    mauiStackPortsUntouched = @("8190", "8191", "8192", "8193", "8194")
    databasePortsHostLocalOnly = @("15533", "15534")
    envFile = $envFile
    posComposeFile = $(if ($ReactPosDocker) { $posComposeFile } else { $null })
    proxyValidation = $proxyValidation
    startedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
}
$provenancePath = Join-Path $stateDir "pa-integration-provenance.json"
$provenance | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $provenancePath -Encoding UTF8
$launcherState = [ordered]@{
    Mode = "ReactIntegration"
    Package = "TAILSCALE-ACCESS-01"
    PlatformRepoRoot = $PlatformRepoRoot
    PosRepoRoot = $PosRepoRoot
    PublicHost = $resolvedPublicHost
    ReactPosMode = $reactPosMode
    WindowPids = $windowPids
    ProvenancePath = $provenancePath
}
$launcherState | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $stateDir "pa-integration-launcher-state.json") -Encoding UTF8

Write-Host ""
Write-Ok "LOCAL URLs"
Write-Ok "  React Admin:  $reactAdminUrl/admin"
Write-Ok "  Platform API: $loopbackPlatformApiUrl"
Write-Ok "  POS API:      $loopbackPosApiUrl"
Write-Ok "  React POS:    $reactPosUrl  (mode=$reactPosMode)"
Write-Ok "  Emulator POS: http://127.0.0.1:$reactPosPort  (after adb reverse) / 10.0.2.2 via reverse"
if ($resolvedPublicHost) {
    Write-Host ""
    Write-Ok "PUBLIC URLs (Tailscale/LAN via $resolvedPublicHost)"
    Write-Ok "  React Admin:  $publicReactAdminUrl/admin"
    Write-Ok "  Platform API: $publicPlatformApiUrl"
    Write-Ok "  POS API:      $publicPosApiUrl"
    Write-Ok "  React POS:    $publicReactPosUrl"
}
Write-Ok "DB ports stay host-local: 127.0.0.1:$platformDbPort / 127.0.0.1:$posDbPort (not Tailscale-published)"
Write-Ok "Provenance:   $provenancePath"
Write-Host ""
Write-Host "RUNTIME PROVENANCE"
Write-Host "  Platform branch/SHA: $platformBranch / $platformSha"
Write-Host "  POS branch/SHA:      $posBranch / $posSha"
Write-Host "  PUBLIC_HOST=$(if ($resolvedPublicHost) { $resolvedPublicHost } else { '(none)' })"
Write-Host "  POS_DOCKER_FROM_POS_WORKTREE=$(if ($ReactPosDocker) { 'YES' } else { 'NO' })"
Write-Host "  REACT_POS_MODE=$reactPosMode"
Write-Host "  OLD_BLAZOR_ADMIN_USED_AS_REACT_ADMIN=NO"
Write-Host "  POS_DOWNGRADED=NO"
Write-Host "  MAUI_STACK_UNCHANGED=YES (8190-8194 untouched)"
Write-Host "  DATABASES_NOT_PUBLICLY_EXPOSED=YES"
if ($proxyValidation) {
    Write-Host "  LOGIN=$($proxyValidation.Login)"
    Write-Host "  AUTH_ME=$($proxyValidation.AuthMe)"
    Write-Host "  PLATFORM_API_PROXY=$($proxyValidation.PlatformApiProxy)"
    Write-Host "  POS_API_PROXY=$($proxyValidation.PosApiProxy)"
}
Write-Host ""

