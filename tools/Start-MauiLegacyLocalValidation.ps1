#Requires -Version 5.1
<#
.SYNOPSIS
  Starts the isolated LEGACY MAUI / Blazor Local Validation Docker stack.

.DESCRIPTION
  Starts ONLY exits-maui-local-validation (DBs, Mailpit, Platform/POS APIs, Blazor Admin/Org/Personal).
  Does NOT stop or modify the React Local Validation stack (8091/8092/8095/5177/15533/15534).
  Does NOT use docker compose down -v. Does NOT touch React volumes or network.

.PARAMETER PublicHost
  Tailscale/LAN host or IP only (no scheme/port). When set, AllowedHosts/CORS and
  Blazor ExItSWebHosts / AdminPublicBaseUrl use http://PublicHost:819x while
  localhost and 10.0.2.2 remain supported. DB ports stay host-local (16533/16534).

.EXAMPLE
  .\tools\Start-MauiLegacyLocalValidation.ps1
  .\tools\Start-MauiLegacyLocalValidation.ps1 -Build
  .\tools\Start-MauiLegacyLocalValidation.ps1 -PublicHost 100.x.x.x
#>
[CmdletBinding()]
param(
    [int]$PortWaitSeconds = 240,
    [int]$DbHealthySeconds = 90,
    [ValidateSet('PlatformAdministratorsOnly', 'Full')]
    [string]$SeedScope = 'PlatformAdministratorsOnly',
    [string]$PublicHost = '',
    [switch]$Build,
    [switch]$CleanBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'MauiLocalValidation.stack.ps1')

function Write-Step([string]$Message) { Write-Host "[maui-local-validation] $Message" -ForegroundColor Cyan }
function Write-Ok([string]$Message) { Write-Host "[maui-local-validation] OK  $Message" -ForegroundColor Green }
function Write-Note([string]$Message) { Write-Host "[maui-local-validation] NOTE $Message" -ForegroundColor Yellow }

function Test-TcpPortOpen {
    param([string]$HostName, [int]$Port, [int]$TimeoutMs = 800)
    try {
        $client = New-Object System.Net.Sockets.TcpClient
        $result = $client.BeginConnect($HostName, $Port, $null, $null)
        if (-not $result.AsyncWaitHandle.WaitOne($TimeoutMs)) {
            $client.Close()
            return $false
        }
        $client.EndConnect($result)
        $client.Close()
        return $true
    }
    catch {
        return $false
    }
}

function Wait-TcpPort {
    param([string]$Label, [int]$Port, [int]$TimeoutSeconds)
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if (Test-TcpPortOpen -HostName '127.0.0.1' -Port $Port) {
            Write-Ok "$Label is listening on 127.0.0.1:$Port"
            return
        }
        Start-Sleep -Milliseconds 750
    }
    throw "$Label did not listen on 127.0.0.1:$Port within ${TimeoutSeconds}s."
}

function Wait-ContainerHealthy {
    param([string]$Name, [int]$TimeoutSeconds)
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

function Wait-HttpEndpoint {
    param([string]$Label, [string]$Url, [int]$TimeoutSeconds)
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    $lastError = ''
    while ((Get-Date) -lt $deadline) {
        try {
            $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 10
            if ([int]$response.StatusCode -ge 200 -and [int]$response.StatusCode -lt 500) {
                Write-Ok ("{0} -> HTTP {1}" -f $Label, [int]$response.StatusCode)
                return
            }
        }
        catch {
            $lastError = $_.Exception.Message
        }
        Start-Sleep -Seconds 2
    }
    throw "$Label failed at $Url within ${TimeoutSeconds}s. Last error: $lastError"
}

function Resolve-MauiPublicHostValue([string]$Value) {
    if ([string]::IsNullOrWhiteSpace($Value)) { return '' }
    $hostName = $Value.Trim()
    if ($hostName -match '://' -or
        $hostName.Contains('/') -or
        $hostName.Contains('\') -or
        $hostName.Contains(' ') -or
        $hostName -match ':\d+$') {
        throw 'PublicHost must be a host or IP only (no scheme, port, path, or spaces). Example: -PublicHost 100.120.79.81'
    }
    return $hostName
}

function Assert-MauiPortsDisjointFromReact {
    foreach ($reactPort in @($MauiLocalValidationStack.ForbiddenReactPorts)) {
        foreach ($mauiPort in @(
            $MauiLocalValidationStack.DefaultAdminPort,
            $MauiLocalValidationStack.DefaultPlatformApiPort,
            $MauiLocalValidationStack.DefaultPosApiPort,
            $MauiLocalValidationStack.DefaultOrgWebPort,
            $MauiLocalValidationStack.DefaultPersonalWebPort,
            $MauiLocalValidationStack.DefaultPlatformDbPort,
            $MauiLocalValidationStack.DefaultPosDbPort,
            $MauiLocalValidationStack.DefaultMailpitUiPort,
            $MauiLocalValidationStack.DefaultMailpitSmtpPort
        )) {
            if ($mauiPort -eq $reactPort) {
                throw "MAUI port $mauiPort collides with React forbidden port $reactPort."
            }
        }
    }
}

function Write-MauiPhysicalDeviceAppsettingsHint {
    param(
        [Parameter(Mandatory)][string]$RepoRoot,
        [Parameter(Mandatory)][string]$PublicHostValue,
        [Parameter(Mandatory)][int]$PlatformApiPort,
        [Parameter(Mandatory)][int]$PosApiPort
    )
    $path = Join-Path $RepoRoot 'src\Products\PinoyBusinessPOS\ExItS.PinoyBusinessPOS.Maui\wwwroot\appsettings.LocalValidation.PhysicalDevice.json'
    Write-Note "MAUI physical device should use:"
    Write-Host "  PosApi BaseUrl:         http://${PublicHostValue}:$PlatformApiPort"
    Write-Host "  PosBusinessApi BaseUrl: http://${PublicHostValue}:$PosApiPort"
    if (Test-Path -LiteralPath $path) {
        Write-Note "Update LocalValidation.PublicHost in: $path"
    }
}

$repoRoot = Get-MauiLocalValidationRepoRoot
$dockerDir = Join-Path $repoRoot 'deploy\docker'
$envFile = Join-Path $dockerDir $MauiLocalValidationStack.EnvFileName
$envExample = Join-Path $dockerDir $MauiLocalValidationStack.EnvExampleFileName

Write-Step "LEGACY-MAUI-ISO-01 - starting isolated MAUI/Blazor Local Validation"
Write-Note "React stack (8091/8092/8095/5177/15533/15534) is left alone."
Assert-MauiPortsDisjointFromReact
Test-MauiLocalValidationDockerAvailable

if (-not (Test-Path -LiteralPath $envFile)) {
    if (-not (Test-Path -LiteralPath $envExample)) {
        throw "Missing $envExample"
    }
    Copy-Item -LiteralPath $envExample -Destination $envFile
    Write-Note "Created $envFile from example - fill REPLACE_* secrets, then re-run."
    throw "Edit $envFile and set MAUI_LOCAL_VALIDATION_* passwords, then re-run."
}

$envMap = Import-MauiLocalValidationDotEnv -Path $envFile
Require-MauiLocalValidationEnvKey -Map $envMap -Key 'MAUI_LOCAL_VALIDATION_PLATFORM_DB_USER'
Require-MauiLocalValidationEnvKey -Map $envMap -Key 'MAUI_LOCAL_VALIDATION_PLATFORM_DB_PASSWORD'
Require-MauiLocalValidationEnvKey -Map $envMap -Key 'MAUI_LOCAL_VALIDATION_POS_DB_USER'
Require-MauiLocalValidationEnvKey -Map $envMap -Key 'MAUI_LOCAL_VALIDATION_POS_DB_PASSWORD'
Require-MauiLocalValidationEnvKey -Map $envMap -Key 'MAUI_LOCAL_VALIDATION_SHARED_PASSWORD'

$adminPort = if ($envMap['MAUI_LOCAL_VALIDATION_ADMIN_HOST_PORT']) { [int]$envMap['MAUI_LOCAL_VALIDATION_ADMIN_HOST_PORT'] } else { $MauiLocalValidationStack.DefaultAdminPort }
$platformApiPort = if ($envMap['MAUI_LOCAL_VALIDATION_PLATFORM_API_HOST_PORT']) { [int]$envMap['MAUI_LOCAL_VALIDATION_PLATFORM_API_HOST_PORT'] } else { $MauiLocalValidationStack.DefaultPlatformApiPort }
$posApiPort = if ($envMap['MAUI_LOCAL_VALIDATION_POS_API_HOST_PORT']) { [int]$envMap['MAUI_LOCAL_VALIDATION_POS_API_HOST_PORT'] } else { $MauiLocalValidationStack.DefaultPosApiPort }
$orgPort = if ($envMap['MAUI_LOCAL_VALIDATION_ORG_WEB_HOST_PORT']) { [int]$envMap['MAUI_LOCAL_VALIDATION_ORG_WEB_HOST_PORT'] } else { $MauiLocalValidationStack.DefaultOrgWebPort }
$personalPort = if ($envMap['MAUI_LOCAL_VALIDATION_PERSONAL_WEB_HOST_PORT']) { [int]$envMap['MAUI_LOCAL_VALIDATION_PERSONAL_WEB_HOST_PORT'] } else { $MauiLocalValidationStack.DefaultPersonalWebPort }
$platformDbPort = if ($envMap['MAUI_LOCAL_VALIDATION_PLATFORM_DB_HOST_PORT']) { [int]$envMap['MAUI_LOCAL_VALIDATION_PLATFORM_DB_HOST_PORT'] } else { $MauiLocalValidationStack.DefaultPlatformDbPort }
$posDbPort = if ($envMap['MAUI_LOCAL_VALIDATION_POS_DB_HOST_PORT']) { [int]$envMap['MAUI_LOCAL_VALIDATION_POS_DB_HOST_PORT'] } else { $MauiLocalValidationStack.DefaultPosDbPort }
$mailpitUiPort = if ($envMap['MAUI_LOCAL_VALIDATION_MAILPIT_UI_HOST_PORT']) { [int]$envMap['MAUI_LOCAL_VALIDATION_MAILPIT_UI_HOST_PORT'] } else { $MauiLocalValidationStack.DefaultMailpitUiPort }

$resolvedPublicHost = Resolve-MauiPublicHostValue -Value $PublicHost
if (-not $resolvedPublicHost) {
    $resolvedPublicHost = Resolve-MauiPublicHostValue -Value ([string]$envMap['MAUI_LOCAL_VALIDATION_PUBLIC_HOST'])
}
if ($resolvedPublicHost) {
    Write-Ok "PublicHost: $resolvedPublicHost (Tailscale/LAN)"
}
else {
    Write-Note "No PublicHost (localhost URLs). Pass -PublicHost <ip> for Tailscale/LAN."
}

$loopbackAdminUrl = "http://127.0.0.1:$adminPort"
$loopbackPlatformApiUrl = "http://127.0.0.1:$platformApiPort"
$loopbackPosApiUrl = "http://127.0.0.1:$posApiPort"
$loopbackOrgUrl = "http://127.0.0.1:$orgPort"
$loopbackPersonalUrl = "http://127.0.0.1:$personalPort"

$publicAdminUrl = if ($resolvedPublicHost) { "http://${resolvedPublicHost}:$adminPort" } else { $null }
$publicPlatformApiUrl = if ($resolvedPublicHost) { "http://${resolvedPublicHost}:$platformApiPort" } else { $null }
$publicPosApiUrl = if ($resolvedPublicHost) { "http://${resolvedPublicHost}:$posApiPort" } else { $null }
$publicOrgUrl = if ($resolvedPublicHost) { "http://${resolvedPublicHost}:$orgPort" } else { $null }
$publicPersonalUrl = if ($resolvedPublicHost) { "http://${resolvedPublicHost}:$personalPort" } else { $null }

# Compose substitutes these env vars into AllowedHosts / CORS / ExItSWebHosts / AdminPublicBaseUrl.
$baseAllowed = if ($envMap['MAUI_LOCAL_VALIDATION_ALLOWED_HOSTS']) {
    [string]$envMap['MAUI_LOCAL_VALIDATION_ALLOWED_HOSTS']
} else {
    'localhost;127.0.0.1;10.0.2.2;maui-platform-api;maui-admin-web;maui-pos-api;maui-org-web;maui-personal-web'
}
if ($resolvedPublicHost -and ($baseAllowed -notlike "*${resolvedPublicHost}*")) {
    $baseAllowed = "$baseAllowed;$resolvedPublicHost"
}
$env:MAUI_LOCAL_VALIDATION_ALLOWED_HOSTS = $baseAllowed
$env:MAUI_LOCAL_VALIDATION_SEED_SCOPE = $SeedScope

if ($resolvedPublicHost) {
    $env:MAUI_LOCAL_VALIDATION_ADMIN_ORIGIN = "http://${resolvedPublicHost}:$adminPort"
    $env:MAUI_LOCAL_VALIDATION_ORG_WEB_ORIGIN = "http://${resolvedPublicHost}:$orgPort"
    $env:MAUI_LOCAL_VALIDATION_PERSONAL_WEB_ORIGIN = "http://${resolvedPublicHost}:$personalPort"
    # Extra CORS slots beyond compose defaults (localhost retained in __0..__5).
    $env:MAUI_LOCAL_VALIDATION_CORS_PUBLIC_ADMIN = "http://${resolvedPublicHost}:$adminPort"
    $env:MAUI_LOCAL_VALIDATION_CORS_PUBLIC_ORG = "http://${resolvedPublicHost}:$orgPort"
    $env:MAUI_LOCAL_VALIDATION_CORS_PUBLIC_PERSONAL = "http://${resolvedPublicHost}:$personalPort"
}
else {
    $env:MAUI_LOCAL_VALIDATION_ADMIN_ORIGIN = if ($envMap['MAUI_LOCAL_VALIDATION_ADMIN_ORIGIN']) { [string]$envMap['MAUI_LOCAL_VALIDATION_ADMIN_ORIGIN'] } else { "http://localhost:$adminPort" }
    $env:MAUI_LOCAL_VALIDATION_ORG_WEB_ORIGIN = if ($envMap['MAUI_LOCAL_VALIDATION_ORG_WEB_ORIGIN']) { [string]$envMap['MAUI_LOCAL_VALIDATION_ORG_WEB_ORIGIN'] } else { "http://localhost:$orgPort" }
    $env:MAUI_LOCAL_VALIDATION_PERSONAL_WEB_ORIGIN = if ($envMap['MAUI_LOCAL_VALIDATION_PERSONAL_WEB_ORIGIN']) { [string]$envMap['MAUI_LOCAL_VALIDATION_PERSONAL_WEB_ORIGIN'] } else { "http://localhost:$personalPort" }
}

Write-Ok "AllowedHosts: $env:MAUI_LOCAL_VALIDATION_ALLOWED_HOSTS"
Write-Ok "Admin/Org/Personal origins: $env:MAUI_LOCAL_VALIDATION_ADMIN_ORIGIN | $env:MAUI_LOCAL_VALIDATION_ORG_WEB_ORIGIN | $env:MAUI_LOCAL_VALIDATION_PERSONAL_WEB_ORIGIN"

$composeArgs = Get-MauiLocalValidationComposeArgs -RepoRoot $repoRoot -EnvFile $envFile

Write-Step "Starting MAUI compose project $($MauiLocalValidationStack.ComposeProjectName) (volumes preserved)..."
$upArgs = $composeArgs + @('up', '-d')
if ($CleanBuild) {
    $upArgs += '--build'
    $upArgs += '--force-recreate'
}
elseif ($Build) {
    $upArgs += '--build'
}
# Recreate app containers when PublicHost changes so env substitutions apply (keep DB volumes).
if ($resolvedPublicHost -and -not $CleanBuild) {
    $appServices = @(
        'maui-platform-api', 'maui-pos-api', 'maui-admin-web', 'maui-org-web', 'maui-personal-web'
    )
    Write-Note "Recreating MAUI app containers so PublicHost AllowedHosts/CORS/origins apply."
    $null = Invoke-MauiLocalValidationDocker -DockerArgs ($composeArgs + @('up', '-d', '--force-recreate') + $appServices)
}

$exit = Invoke-MauiLocalValidationDocker -DockerArgs $upArgs
if ($exit -ne 0) {
    throw "docker compose up failed with exit code $exit"
}

Write-Step "Waiting for MAUI databases..."
Wait-ContainerHealthy -Name $MauiLocalValidationStack.PlatformDbContainer -TimeoutSeconds $DbHealthySeconds
Wait-ContainerHealthy -Name $MauiLocalValidationStack.PosDbContainer -TimeoutSeconds $DbHealthySeconds

Write-Step "Waiting for MAUI APIs and Blazor hosts..."
Wait-TcpPort -Label 'MAUI Platform API' -Port $platformApiPort -TimeoutSeconds $PortWaitSeconds
Wait-TcpPort -Label 'MAUI POS API' -Port $posApiPort -TimeoutSeconds $PortWaitSeconds
Wait-TcpPort -Label 'MAUI Blazor Admin' -Port $adminPort -TimeoutSeconds $PortWaitSeconds
Wait-TcpPort -Label 'MAUI Org Web' -Port $orgPort -TimeoutSeconds $PortWaitSeconds
Wait-TcpPort -Label 'MAUI Personal Web' -Port $personalPort -TimeoutSeconds $PortWaitSeconds

Wait-HttpEndpoint -Label 'MAUI Platform /health' -Url "http://127.0.0.1:$platformApiPort/health" -TimeoutSeconds 60
Wait-HttpEndpoint -Label 'MAUI POS /health' -Url "http://127.0.0.1:$posApiPort/health" -TimeoutSeconds 60
Wait-HttpEndpoint -Label 'MAUI Admin' -Url "http://127.0.0.1:$adminPort/" -TimeoutSeconds 90
Wait-HttpEndpoint -Label 'MAUI Org Web' -Url "http://127.0.0.1:$orgPort/" -TimeoutSeconds 90
Wait-HttpEndpoint -Label 'MAUI Personal Web' -Url "http://127.0.0.1:$personalPort/" -TimeoutSeconds 90

Write-Note "React coexistence check (informational):"
$reactChecks = @(
    @{ N = 'React Platform API'; P = 8091 },
    @{ N = 'React POS API'; P = 8092 },
    @{ N = 'React Admin'; P = 8095 },
    @{ N = 'React POS Vite'; P = 5177 }
)
foreach ($pair in $reactChecks) {
    if (Test-TcpPortOpen -HostName '127.0.0.1' -Port $pair.P) {
        Write-Ok "$($pair.N) still listening on $($pair.P) (untouched)"
    }
    else {
        Write-Note "$($pair.N) not listening on $($pair.P) (React stack may be stopped - OK)"
    }
}

Write-Host ""
Write-Ok "MAUI/Blazor Local Validation is up."
Write-Host "  Compose project:  $($MauiLocalValidationStack.ComposeProjectName)"
Write-Host ""
Write-Ok "LOCAL URLs"
Write-Host "  Admin:            $loopbackAdminUrl"
Write-Host "  Platform API:     $loopbackPlatformApiUrl"
Write-Host "  POS API:          $loopbackPosApiUrl"
Write-Host "  Org Web:          $loopbackOrgUrl"
Write-Host "  Personal Web:     $loopbackPersonalUrl"
Write-Host "  Platform DB:      127.0.0.1:$platformDbPort / $($MauiLocalValidationStack.PlatformDbName) (host-local only)"
Write-Host "  POS DB:           127.0.0.1:$posDbPort / $($MauiLocalValidationStack.PosDbName) (host-local only)"
Write-Host "  Mailpit:          http://127.0.0.1:$mailpitUiPort"
Write-Host "  MAUI emulator:    http://10.0.2.2:$platformApiPort and :$posApiPort"
if ($resolvedPublicHost) {
    Write-Host ""
    Write-Ok "PUBLIC URLs (Tailscale/LAN via $resolvedPublicHost)"
    Write-Host "  Admin:            $publicAdminUrl"
    Write-Host "  Platform API:     $publicPlatformApiUrl"
    Write-Host "  POS API:          $publicPosApiUrl"
    Write-Host "  Org Web:          $publicOrgUrl"
    Write-Host "  Personal Web:     $publicPersonalUrl"
    Write-MauiPhysicalDeviceAppsettingsHint -RepoRoot $repoRoot -PublicHostValue $resolvedPublicHost -PlatformApiPort $platformApiPort -PosApiPort $posApiPort
}
Write-Host "  Stop:             .\tools\Stop-MauiLegacyLocalValidation.ps1"
Write-Host "  DATABASES_NOT_PUBLICLY_EXPOSED=YES"
Write-Host "  REACT_STACK_ISOLATED=YES"
Write-Host ""
