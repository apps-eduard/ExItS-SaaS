#Requires -Version 5.1
<#
.SYNOPSIS
  Starts PA-COM-07 mixed-worktree Local Validation: Platform from this repo, POS from feat/pos-react-client.

.DESCRIPTION
  Platform API + React Admin run from the Agent 2 worktree (PA-COM-07 branch).
  POS API runs from the external POS worktree at the required SHA with CommercialValidation:Strict=true.

  Does NOT merge Agent 1 or Agent 3 branches.

.EXAMPLE
  .\tools\Start-PaCom07MixedValidation.ps1
#>
[CmdletBinding()]
param(
    [string]$PlatformRepoRoot = "",
    [string]$PosRepoRoot = "C:\Users\speed\Desktop\ExItS-SaaS-pos-react-client",
    [string]$ExpectedPosSha = "7e8256b2aa6ae1e44e615a272939a7a796aeb89e",
    [string]$EnvFile = "",
    [int]$PortWaitSeconds = 180
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "LocalValidation.stack.ps1")

function Write-Step([string]$Message) { Write-Host "[pa-com-07] $Message" -ForegroundColor Cyan }
function Write-Ok([string]$Message) { Write-Host "[pa-com-07] OK $Message" -ForegroundColor Green }
function Write-Fail([string]$Message) { Write-Host "[pa-com-07] FAIL $Message" -ForegroundColor Red }

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

if (-not $PlatformRepoRoot) {
    $PlatformRepoRoot = Get-LocalValidationRepoRoot -StartPath $PSScriptRoot
}

$platformSha = Get-GitHead -RepoRoot $PlatformRepoRoot
Push-Location $PosRepoRoot
try {
    $posSha = (git rev-parse HEAD).Trim()
    if ($posSha -ne $ExpectedPosSha) {
        Write-Step "POS worktree at $posSha - checking out validation SHA $ExpectedPosSha (runtime only; no Platform merge)."
        $prev = $ErrorActionPreference
        $ErrorActionPreference = "Continue"
        try {
            git checkout --detach $ExpectedPosSha 2>&1 | ForEach-Object { Write-Host $_ }
            if ($LASTEXITCODE -ne 0) {
                throw "Unable to checkout POS validation SHA $ExpectedPosSha in $PosRepoRoot"
            }
        }
        finally {
            $ErrorActionPreference = $prev
        }
        $posSha = (git rev-parse HEAD).Trim()
    }
}
finally {
    Pop-Location
}
if ($posSha -ne $ExpectedPosSha) {
    throw "POS repo HEAD $posSha does not match required $ExpectedPosSha"
}

Write-Step "Platform repo: $PlatformRepoRoot at $platformSha"
Write-Step "POS repo:      $PosRepoRoot at $posSha"

$envFile = Resolve-EnvFilePath -PlatformRoot $PlatformRepoRoot -Override $EnvFile
$composeFile = Join-Path $PlatformRepoRoot "deploy\docker\$($LocalValidationStack.ComposeFileName)"
$dockerDir = Split-Path -Parent $envFile
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

$null = Stop-LocalValidationRepoScopedHostApps -RepoRoot $PlatformRepoRoot
$null = Stop-LocalValidationRepoScopedHostApps -RepoRoot $PosRepoRoot
$blocked = @(Report-LocalValidationPortConflicts -Ports @($platformApiPort, $posApiPort, $adminWebReactPort))
if ($blocked.Count -gt 0) { throw "Required ports are occupied." }

Write-Step "Starting PostgreSQL + Mailpit..."
Start-LocalValidationInfrastructure -ComposeFile $composeFile -EnvFile $envFile
Wait-TcpPort -Label "Platform DB" -HostName "127.0.0.1" -Port $platformDbPort -TimeoutSeconds 60
Wait-TcpPort -Label "POS DB" -HostName "127.0.0.1" -Port $posDbPort -TimeoutSeconds 60

$platformCs = "Host=127.0.0.1;Port=$platformDbPort;Database=$($LocalValidationStack.PlatformDbName);Username=$($envMap["LOCAL_VALIDATION_PLATFORM_DB_USER"]);Password=$($envMap["LOCAL_VALIDATION_PLATFORM_DB_PASSWORD"])"
$posCs = "Host=127.0.0.1;Port=$posDbPort;Database=$($LocalValidationStack.PosDbName);Username=$($envMap["LOCAL_VALIDATION_POS_DB_USER"]);Password=$($envMap["LOCAL_VALIDATION_POS_DB_PASSWORD"])"
$loopbackPlatformApiUrl = "http://127.0.0.1:$platformApiPort"
$loopbackPosApiUrl = "http://127.0.0.1:$posApiPort"
$publicAdminWebReactUrl = "http://127.0.0.1:$adminWebReactPort"
$allowedHosts = Get-LocalValidationAllowedHostsList -PublicHostValue "" -EnvMap $envMap
$corsOrigins = @(
    "http://localhost:$adminWebReactPort",
    "http://127.0.0.1:$adminWebReactPort"
)

$platformProject = Join-Path $PlatformRepoRoot "src\Platform\ExItS.Platform.Api\ExItS.Platform.Api.csproj"
$posProject = Join-Path $PosRepoRoot "src\Products\PinoyBusinessPOS\ExItS.PinoyBusinessPOS.Api\ExItS.PinoyBusinessPOS.Api.csproj"

Write-Step "Starting Platform API from Agent 2 worktree..."
$platformEnv = @{
    ASPNETCORE_ENVIRONMENT = "Testing"
    ASPNETCORE_URLS = "http://0.0.0.0:$platformApiPort"
    ConnectionStrings__PlatformDatabase = $platformCs
    AllowedHosts = $allowedHosts
    Security__EnforceHttps = "false"
    LocalValidation__Enabled = "true"
    LocalValidation__SeedScope = "PlatformAdministratorsOnly"
    LocalValidation__SharedPassword = [string]$envMap["LOCAL_VALIDATION_SHARED_PASSWORD"]
    PlatformAuthentication__Lifecycle__ExposeDebugTokens = "true"
    PlatformAuthentication__External__TestingEndpointEnabled = "true"
    PlatformAuthentication__Password__MinimumLength = "1"
    PlatformAuthentication__Password__RequireUppercase = "false"
    PlatformAuthentication__Password__RequireLowercase = "false"
    PlatformAuthentication__Password__RequireDigit = "false"
    PlatformAuthentication__Password__RequireNonAlphanumeric = "false"
}
for ($i = 0; $i -lt $corsOrigins.Count; $i++) {
    $platformEnv["Cors__AllowedOrigins__$i"] = $corsOrigins[$i]
}
$windowPids = @(
    (Start-AppWindow -Title "PA-COM-07 Platform API" -RepoRoot $PlatformRepoRoot -Project $platformProject -EnvMap $platformEnv)
)
Wait-TcpPort -Label "Platform API" -HostName "127.0.0.1" -Port $platformApiPort -TimeoutSeconds $PortWaitSeconds

Write-Step "Starting POS API from Agent 1 worktree (strict commercial)..."
$posEnv = @{
    ASPNETCORE_ENVIRONMENT = "Staging"
    ASPNETCORE_URLS = "http://0.0.0.0:$posApiPort"
    ConnectionStrings__PosDatabase = $posCs
    AllowedHosts = $allowedHosts
    Security__EnforceHttps = "false"
    LocalValidation__Enabled = "true"
    LocalValidation__PlatformApiBaseUrl = $loopbackPlatformApiUrl
    PlatformAuth__BaseUrl = $loopbackPlatformApiUrl
    CommercialValidation__Strict = "true"
}
for ($i = 0; $i -lt $corsOrigins.Count; $i++) {
    $posEnv["Cors__AllowedOrigins__$i"] = $corsOrigins[$i]
}
$windowPids += Start-AppWindow -Title "PA-COM-07 POS API (strict)" -RepoRoot $PosRepoRoot -Project $posProject -EnvMap $posEnv
Wait-TcpPort -Label "POS API" -HostName "127.0.0.1" -Port $posApiPort -TimeoutSeconds $PortWaitSeconds

Write-Step "Starting React Platform Admin container on $adminWebReactPort..."
Set-Item -LiteralPath "Env:LOCAL_VALIDATION_PLATFORM_API_PUBLIC_URL" -Value $loopbackPlatformApiUrl
Set-Item -LiteralPath "Env:LOCAL_VALIDATION_ADMIN_WEB_REACT_ORIGIN" -Value $publicAdminWebReactUrl
$reactUpExit = Invoke-LocalValidationDocker -DockerArgs @(
    "compose", "-p", $LocalValidationStack.ComposeProjectName,
    "-f", $composeFile, "--env-file", $envFile,
    "--profile", "apps", "up", "-d", "--build", "admin-web-react"
)
if ($reactUpExit -ne 0) { throw "admin-web-react compose up failed ($reactUpExit)." }
Wait-TcpPort -Label "React Platform Admin" -HostName "127.0.0.1" -Port $adminWebReactPort -TimeoutSeconds $PortWaitSeconds

$stateDir = Join-Path $env:LOCALAPPDATA "ExItS\LocalValidation"
New-Item -ItemType Directory -Force -Path $stateDir | Out-Null
$provenance = [ordered]@{
    package = "PA-COM-07"
    platformAdminRuntimeSha = $platformSha
    platformApiRuntimeSha = $platformSha
    posApiRuntimeSha = $posSha
    strictCommercialValidation = "ON"
    developmentGrantMerge = "OFF"
    platformApiUrl = $loopbackPlatformApiUrl
    posApiUrl = $loopbackPosApiUrl
    reactAdminUrl = $publicAdminWebReactUrl
    envFile = $envFile
    startedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
}
$provenancePath = Join-Path $stateDir "pa-com-07-provenance.json"
$provenance | ConvertTo-Json | Set-Content -LiteralPath $provenancePath -Encoding UTF8
$launcherState = [ordered]@{
    Mode = "PaCom07Mixed"
    PlatformRepoRoot = $PlatformRepoRoot
    PosRepoRoot = $PosRepoRoot
    WindowPids = $windowPids
    ProvenancePath = $provenancePath
}
$launcherState | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $stateDir "pa-com-07-launcher-state.json") -Encoding UTF8

Write-Ok "React Platform Admin: $publicAdminWebReactUrl/admin"
Write-Ok "Platform API:         $loopbackPlatformApiUrl"
Write-Ok "POS API (strict):     $loopbackPosApiUrl"
Write-Ok "Provenance:           $provenancePath"
Write-Ok "STRICT_COMMERCIAL_VALIDATION=ON DEVELOPMENT_GRANT_MERGE=OFF"
Write-Host @"

Run joined integration:

  .\tools\Invoke-PaCom07JoinedIntegration.ps1

"@
