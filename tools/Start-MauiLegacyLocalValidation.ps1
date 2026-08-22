#Requires -Version 5.1
<#
.SYNOPSIS
  Starts the isolated LEGACY MAUI / Blazor Local Validation Docker stack.

.DESCRIPTION
  Starts ONLY exits-maui-local-validation (DBs, Mailpit, Platform/POS APIs, Blazor Admin/Org/Personal).
  Does NOT stop or modify the React Local Validation stack (8091/8092/8095/5177/15533/15534).
  Does NOT use docker compose down -v. Does NOT touch React volumes or network.

.EXAMPLE
  .\tools\Start-MauiLegacyLocalValidation.ps1
  .\tools\Start-MauiLegacyLocalValidation.ps1 -Build
#>
[CmdletBinding()]
param(
    [int]$PortWaitSeconds = 240,
    [int]$DbHealthySeconds = 90,
    [ValidateSet('PlatformAdministratorsOnly', 'Full')]
    [string]$SeedScope = 'PlatformAdministratorsOnly',
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

$env:MAUI_LOCAL_VALIDATION_SEED_SCOPE = $SeedScope

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
Write-Host "  Admin:            http://127.0.0.1:$adminPort"
Write-Host "  Platform API:     http://127.0.0.1:$platformApiPort"
Write-Host "  POS API:          http://127.0.0.1:$posApiPort"
Write-Host "  Org Web:          http://127.0.0.1:$orgPort"
Write-Host "  Personal Web:     http://127.0.0.1:$personalPort"
Write-Host "  Platform DB:      127.0.0.1:$($MauiLocalValidationStack.DefaultPlatformDbPort) / $($MauiLocalValidationStack.PlatformDbName)"
Write-Host "  POS DB:           127.0.0.1:$($MauiLocalValidationStack.DefaultPosDbPort) / $($MauiLocalValidationStack.PosDbName)"
Write-Host "  Mailpit:          http://127.0.0.1:$($MauiLocalValidationStack.DefaultMailpitUiPort)"
Write-Host "  MAUI emulator:    http://10.0.2.2:$platformApiPort and :$posApiPort"
Write-Host "  Stop:             .\tools\Stop-MauiLegacyLocalValidation.ps1"
Write-Host ""