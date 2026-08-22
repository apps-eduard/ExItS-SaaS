#Requires -Version 5.1
<#
.SYNOPSIS
  Starts the full Local Validation application stack in Docker.

.DESCRIPTION
  Stops repo-scoped host apps, preserves Local Validation database volumes, starts
  infrastructure, and starts all five application services under the apps profile.
  Migrations remain application hosted services when the APIs start.
#>
[CmdletBinding()]
param(
    [int]$PortWaitSeconds = 180,
    [int]$DbHealthySeconds = 90,
    [ValidateSet('PlatformAdministratorsOnly', 'Full')]
    [string]$SeedScope = 'PlatformAdministratorsOnly',
    [switch]$PurgeTransactional,
    [string]$PublicHost = '',
    [switch]$Build,
    [switch]$CleanBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LocalValidation.stack.ps1')

function Write-Step([string]$Message) { Write-Host "[local-validation] $Message" -ForegroundColor Cyan }
function Write-Ok([string]$Message) { Write-Host "[local-validation] OK  $Message" -ForegroundColor Green }
function Write-Note([string]$Message) { Write-Host "[local-validation] NOTE $Message" -ForegroundColor Yellow }

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
            if ([int]$response.StatusCode -ge 200 -and [int]$response.StatusCode -lt 400) {
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

function Set-ComposeEnvironment {
    param([string]$Name, [string]$Value)
    Set-Item -LiteralPath "Env:$Name" -Value $Value
}

$repoRoot = Get-LocalValidationRepoRoot
$dockerDir = Join-Path $repoRoot 'deploy\docker'
$envFile = Join-Path $dockerDir $LocalValidationStack.EnvFileName
$composeFile = Join-Path $dockerDir $LocalValidationStack.ComposeFileName
$stateDir = Join-Path $env:LOCALAPPDATA 'ExItS\LocalValidation'
$stateFile = Join-Path $stateDir 'launcher-state.json'

Write-Step "Repository: $repoRoot"
Test-LocalValidationDockerAvailable
Write-Ok 'Docker Desktop is available'

if (-not (Test-Path -LiteralPath $envFile)) {
    throw "Missing $envFile. Copy deploy/docker/.env.local-validation.example and fill REPLACE_* values."
}
if (-not (Test-Path -LiteralPath $composeFile)) {
    throw "Missing $composeFile"
}

$envMap = Import-LocalValidationDotEnv -Path $envFile
foreach ($requiredKey in @(
    'LOCAL_VALIDATION_PLATFORM_DB_USER',
    'LOCAL_VALIDATION_PLATFORM_DB_PASSWORD',
    'LOCAL_VALIDATION_POS_DB_USER',
    'LOCAL_VALIDATION_POS_DB_PASSWORD',
    'LOCAL_VALIDATION_SHARED_PASSWORD'
)) {
    Require-LocalValidationEnvKey -Map $envMap -Key $requiredKey
}

$adminPort = if ($envMap['LOCAL_VALIDATION_ADMIN_HOST_PORT']) { [int]$envMap['LOCAL_VALIDATION_ADMIN_HOST_PORT'] } else { [int]$LocalValidationStack.DefaultAdminPort }
$platformApiPort = if ($envMap['LOCAL_VALIDATION_PLATFORM_API_HOST_PORT']) { [int]$envMap['LOCAL_VALIDATION_PLATFORM_API_HOST_PORT'] } else { [int]$LocalValidationStack.DefaultPlatformApiPort }
$posApiPort = if ($envMap['LOCAL_VALIDATION_POS_API_HOST_PORT']) { [int]$envMap['LOCAL_VALIDATION_POS_API_HOST_PORT'] } else { [int]$LocalValidationStack.DefaultPosApiPort }
$orgWebPort = if ($envMap['LOCAL_VALIDATION_ORG_WEB_HOST_PORT']) { [int]$envMap['LOCAL_VALIDATION_ORG_WEB_HOST_PORT'] } else { [int]$LocalValidationStack.DefaultOrgWebPort }
$personalWebPort = if ($envMap['LOCAL_VALIDATION_PERSONAL_WEB_HOST_PORT']) { [int]$envMap['LOCAL_VALIDATION_PERSONAL_WEB_HOST_PORT'] } else { [int]$LocalValidationStack.DefaultPersonalWebPort }
$mailpitUiPort = if ($envMap['LOCAL_VALIDATION_MAILPIT_UI_HOST_PORT']) { [int]$envMap['LOCAL_VALIDATION_MAILPIT_UI_HOST_PORT'] } else { 8025 }

New-Item -ItemType Directory -Force -Path $stateDir | Out-Null
$resolvedPublicHost = Resolve-LocalValidationEffectivePublicHost -ParamValue $PublicHost -EnvMap $envMap -StateFilePath $stateFile
$browserHost = if ($resolvedPublicHost) { $resolvedPublicHost } else { 'localhost' }
$adminOrigin = "http://${browserHost}:$adminPort"
$platformApiPublicUrl = "http://${browserHost}:$platformApiPort"
$posApiPublicUrl = "http://${browserHost}:$posApiPort"
$orgWebOrigin = "http://${browserHost}:$orgWebPort"
$personalWebOrigin = "http://${browserHost}:$personalWebPort"
$allowedHosts = Get-LocalValidationAllowedHostsList -PublicHostValue $resolvedPublicHost -EnvMap $envMap

Set-ComposeEnvironment -Name 'LOCAL_VALIDATION_ALLOWED_HOSTS' -Value $allowedHosts
Set-ComposeEnvironment -Name 'LOCAL_VALIDATION_ADMIN_ORIGIN' -Value $adminOrigin
Set-ComposeEnvironment -Name 'LOCAL_VALIDATION_ORG_WEB_ORIGIN' -Value $orgWebOrigin
Set-ComposeEnvironment -Name 'LOCAL_VALIDATION_PERSONAL_WEB_ORIGIN' -Value $personalWebOrigin
Set-ComposeEnvironment -Name 'LOCAL_VALIDATION_PLATFORM_API_PUBLIC_URL' -Value $platformApiPublicUrl
Set-ComposeEnvironment -Name 'LOCAL_VALIDATION_PLATFORM_API_INTERNAL_URL' -Value 'http://platform-api:8080'
Set-ComposeEnvironment -Name 'LOCAL_VALIDATION_POS_API_INTERNAL_URL' -Value 'http://pos-api:8080'
Set-ComposeEnvironment -Name 'LOCAL_VALIDATION_SEED_SCOPE' -Value $SeedScope
Set-ComposeEnvironment -Name 'LOCAL_VALIDATION_PURGE_TRANSACTIONAL' -Value $(if ($PurgeTransactional) { 'true' } else { 'false' })

Write-Step 'Inspecting Local Validation port/runtime provenance (all ExItS worktrees)...'
$dockerAppPortLabels = @{
    $adminPort        = 'Platform Admin'
    $platformApiPort   = 'Platform API'
    $posApiPort        = 'POS API'
    $orgWebPort        = 'Organization Web'
    $personalWebPort   = 'Personal Web'
}
Write-LocalValidationRuntimeProvenanceTable -PortLabels $dockerAppPortLabels -ExpectedRepoRoot $repoRoot

Write-Step 'Stopping repo-scoped host applications before Docker app mode...'
$null = Stop-LocalValidationCrossWorktreeHostApps -RepoRoot $repoRoot
Write-Step 'Stopping any existing Docker app services before the port safety check...'
$null = Stop-LocalValidationDockerAppServices -ComposeFile $composeFile -EnvFile $envFile
$conflicts = @(Report-LocalValidationPortConflictsWithProvenance -PortLabels $dockerAppPortLabels -ExpectedRepoRoot $repoRoot)
if ($conflicts.Count -gt 0) {
    throw 'Local Validation app ports remain occupied by unknown processes. Free them and retry.'
}

Write-Step 'Starting PostgreSQL and Mailpit (volumes preserved)...'
Start-LocalValidationInfrastructure -ComposeFile $composeFile -EnvFile $envFile
Wait-ContainerHealthy -Name $LocalValidationStack.PlatformDbContainer -TimeoutSeconds $DbHealthySeconds
Wait-ContainerHealthy -Name $LocalValidationStack.PosDbContainer -TimeoutSeconds $DbHealthySeconds

$composeBaseArgs = @(
    'compose', '-p', $LocalValidationStack.ComposeProjectName,
    '-f', $composeFile, '--env-file', $envFile
)

if ($CleanBuild) {
    Write-Step 'Building application images without cache (volumes preserved)...'
    $buildExit = Invoke-LocalValidationDocker -DockerArgs ($composeBaseArgs + @('build', '--no-cache') + $LocalValidationStack.AppComposeServices)
    if ($buildExit -ne 0) { throw "Docker clean build failed ($buildExit)." }
}

$upArgs = $composeBaseArgs + @('--profile', 'apps', 'up', '-d')
if ($Build -or $CleanBuild) { $upArgs += '--build' }
Write-Step 'Starting full Docker application stack...'
$upExit = Invoke-LocalValidationDocker -DockerArgs $upArgs
if ($upExit -ne 0) { throw "Docker application startup failed ($upExit)." }

Wait-TcpPort -Label 'Platform API' -Port $platformApiPort -TimeoutSeconds $PortWaitSeconds
Wait-TcpPort -Label 'POS API' -Port $posApiPort -TimeoutSeconds $PortWaitSeconds
Wait-TcpPort -Label 'Platform Admin' -Port $adminPort -TimeoutSeconds $PortWaitSeconds
Wait-TcpPort -Label 'Organization Web' -Port $orgWebPort -TimeoutSeconds $PortWaitSeconds
Wait-TcpPort -Label 'Personal Web' -Port $personalWebPort -TimeoutSeconds $PortWaitSeconds

Wait-HttpEndpoint -Label 'Platform API /health' -Url "http://127.0.0.1:$platformApiPort/health" -TimeoutSeconds $PortWaitSeconds
Wait-HttpEndpoint -Label 'POS API /health' -Url "http://127.0.0.1:$posApiPort/health" -TimeoutSeconds $PortWaitSeconds
Wait-HttpEndpoint -Label 'Admin /admin/login' -Url "http://127.0.0.1:$adminPort/admin/login" -TimeoutSeconds $PortWaitSeconds
Wait-HttpEndpoint -Label 'Organization Web /health' -Url "http://127.0.0.1:$orgWebPort/health" -TimeoutSeconds $PortWaitSeconds
Wait-HttpEndpoint -Label 'Personal Web /health' -Url "http://127.0.0.1:$personalWebPort/health" -TimeoutSeconds $PortWaitSeconds

$state = @{
    Mode = 'DockerApps'
    RepoRoot = $repoRoot
    WindowPids = @()
    StartedAtUtc = (Get-Date).ToUniversalTime().ToString('o')
    PublicHost = $resolvedPublicHost
    SeedScope = $SeedScope
    PurgeTransactionalOnSeed = [bool]$PurgeTransactional
    ComposeProjectName = $LocalValidationStack.ComposeProjectName
    ComposeFile = $composeFile
    Ports = @{
        Admin = $adminPort
        PlatformApi = $platformApiPort
        PosApi = $posApiPort
        OrgWeb = $orgWebPort
        PersonalWeb = $personalWebPort
    }
}
$state | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $stateFile -Encoding UTF8

Write-Host ''
Write-Host '======== Local Validation Docker ready ========' -ForegroundColor Green
Write-Host "  Admin:        $adminOrigin"
Write-Host "  Platform API: $platformApiPublicUrl"
Write-Host "  POS API:      $posApiPublicUrl"
Write-Host "  Org Web:      $orgWebOrigin"
Write-Host "  Personal Web: $personalWebOrigin"
Write-Host "  Mailpit:      http://localhost:$mailpitUiPort"
Write-Host '  Migrations:   API-hosted LocalValidation services'
Write-Host '  Volumes:      preserved'
Write-Host '================================================' -ForegroundColor Green
Write-Note 'Stop apps: .\tools\Stop-DockerLocalValidation.ps1'
Write-LocalValidationRuntimeSummary -PortLabels $dockerAppPortLabels -ExpectedRepoRoot $repoRoot -Mode 'DockerApps'
Assert-LocalValidationPortsOwnedByExpectedWorktree -PortLabels $dockerAppPortLabels -ExpectedRepoRoot $repoRoot
Write-Ok 'All Docker Local Validation health checks passed.'
