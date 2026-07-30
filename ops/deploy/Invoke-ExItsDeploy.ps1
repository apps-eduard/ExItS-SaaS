#Requires -Version 5.1
<#
.SYNOPSIS
  Orchestrates ExItS non-production pilot deployment rehearsal / deploy steps.
.DESCRIPTION
  Stops on failure. Refuses dirty trees for StagingPilot/Production. Never echoes secrets.
  Production actions require -ConfirmPhrase DEPLOY_PRODUCTION_CONFIRMED and remain blocked by config validators.
  Phase: P9-WP05-pilot-and-deployment
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('Development', 'Testing', 'StagingPilot', 'Production')]
    [string]$Environment,

    [Parameter(Mandatory)]
    [ValidateSet(
        'Plan',
        'ValidateConfig',
        'BackupGate',
        'Migrate',
        'WaitHealth',
        'SmokeHealth',
        'SmokeFull',
        'PackageVersion',
        'Evidence',
        'Rehearsal')]
    [string]$Action,

    [string]$ConfirmPhrase = '',
    [string]$PlatformBaseUrl = 'http://127.0.0.1:5288',
    [string]$PosBaseUrl = 'http://127.0.0.1:5290',
    [string]$BackupRoot = '',
    [string]$EvidenceDir = '',
    [int]$HealthTimeoutSeconds = 120,
    [switch]$AllowDirtyTree
)

$ErrorActionPreference = 'Stop'
$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
Set-Location $RepoRoot

function Write-ExItsInfo([string]$Message) {
    Write-Host "[exits-deploy] $Message"
}

function Get-GitCommit {
    $sha = (git rev-parse HEAD).Trim()
    if ([string]::IsNullOrWhiteSpace($sha)) { throw 'Unable to resolve Git HEAD.' }
    return $sha
}

function Assert-CleanTree {
    if ($AllowDirtyTree -and $Environment -in @('Development', 'Testing')) { return }
    $status = git status --porcelain
    if ($status) {
        throw "Working tree is not clean. StagingPilot/Production deployments refuse uncommitted changes.`n$status"
    }
}

function Assert-Confirmation {
    if ($Environment -eq 'Production') {
        if ($ConfirmPhrase -ne 'DEPLOY_PRODUCTION_CONFIRMED') {
            throw 'Production requires -ConfirmPhrase DEPLOY_PRODUCTION_CONFIRMED.'
        }
    }
    elseif ($Environment -eq 'StagingPilot') {
        if ($ConfirmPhrase -ne 'DEPLOY_PILOT_CONFIRMED') {
            throw 'StagingPilot requires -ConfirmPhrase DEPLOY_PILOT_CONFIRMED.'
        }
    }
}

function Invoke-DeploymentCli([string[]]$CliArgs) {
    $project = Join-Path $RepoRoot 'tools\ExItS.Deployment.Cli\ExItS.Deployment.Cli.csproj'
    & dotnet run --project $project -c Release -- @CliArgs
    if ($LASTEXITCODE -ne 0) { throw "Deployment CLI failed: $($CliArgs -join ' ')" }
}

function Test-HttpOk([string]$Url, [int]$TimeoutSec) {
    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSec)
    do {
        try {
            $response = Invoke-WebRequest -Uri $Url -Method GET -TimeoutSec 5 -UseBasicParsing
            if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 300) {
                return $true
            }
        }
        catch {
            # retry until timeout
        }
        Start-Sleep -Seconds 2
    } while ([DateTime]::UtcNow -lt $deadline)
    return $false
}

Assert-Confirmation

switch ($Action) {
    'Plan' {
        Write-ExItsInfo "Plan mode for $Environment (no mutations)."
        Invoke-DeploymentCli @('migration-order')
        Invoke-DeploymentCli @('smoke-catalog')
        Invoke-DeploymentCli @('phase-marker')
        break
    }
    'PackageVersion' {
        Assert-CleanTree
        $commit = Get-GitCommit
        Invoke-DeploymentCli @('package-version', '--commit', $commit, '--build', '1')
        break
    }
    'ValidateConfig' {
        Assert-CleanTree
        $commit = Get-GitCommit
        $cli = @(
            'validate-config',
            '--env', $Environment,
            '--commit', $commit,
            '--working-tree-clean', 'true'
        )
        if ($ConfirmPhrase) { $cli += @('--confirm', $ConfirmPhrase) }
        if ($Environment -in @('StagingPilot', 'Production')) {
            $cli += @(
                '--enforce-https', 'true',
                '--allowed-hosts', $(if ($env:PILOT_ALLOWED_HOSTS) { $env:PILOT_ALLOWED_HOSTS } else { 'pilot.example.internal' }),
                '--backup-verified', $(if ($env:EXITS_BACKUP_VERIFIED -eq 'true') { 'true' } else { 'false' }),
                '--platform-backup-set', $(if ($env:EXITS_PLATFORM_BACKUP_SET) { $env:EXITS_PLATFORM_BACKUP_SET } else { '' }),
                '--pos-backup-set', $(if ($env:EXITS_POS_BACKUP_SET) { $env:EXITS_POS_BACKUP_SET } else { '' }),
                '--maui-api', $(if ($env:EXITS_MAUI_POS_API_BASE_URL) { $env:EXITS_MAUI_POS_API_BASE_URL } else { 'https://pilot.example.internal/pos/' })
            )
        }
        Invoke-DeploymentCli $cli
        break
    }
    'BackupGate' {
        Invoke-DeploymentCli @(
            'backup-gate',
            '--platform-verified', $(if ($env:EXITS_PLATFORM_BACKUP_VERIFIED -eq 'true') { 'true' } else { 'false' }),
            '--pos-verified', $(if ($env:EXITS_POS_BACKUP_VERIFIED -eq 'true') { 'true' } else { 'false' }),
            '--platform-set', $(if ($env:EXITS_PLATFORM_BACKUP_SET) { $env:EXITS_PLATFORM_BACKUP_SET } else { '' }),
            '--pos-set', $(if ($env:EXITS_POS_BACKUP_SET) { $env:EXITS_POS_BACKUP_SET } else { '' })
        )
        break
    }
    'Migrate' {
        Assert-CleanTree
        Write-ExItsInfo 'Backup gate must pass before migrate.'
        & $PSCommandPath -Environment $Environment -Action BackupGate -ConfirmPhrase $ConfirmPhrase
        Write-ExItsInfo 'Applying Platform migrations...'
        & dotnet ef database update `
            --project (Join-Path $RepoRoot 'src\Platform\ExItS.Platform.Infrastructure\ExItS.Platform.Infrastructure.csproj') `
            --startup-project (Join-Path $RepoRoot 'src\Platform\ExItS.Platform.Api\ExItS.Platform.Api.csproj')
        if ($LASTEXITCODE -ne 0) { throw 'Platform migration failed — stop deployment.' }
        Write-ExItsInfo 'Applying POS migrations...'
        & dotnet ef database update `
            --project (Join-Path $RepoRoot 'src\Products\PinoyBusinessPOS\ExItS.PinoyBusinessPOS.Infrastructure\ExItS.PinoyBusinessPOS.Infrastructure.csproj') `
            --startup-project (Join-Path $RepoRoot 'src\Products\PinoyBusinessPOS\ExItS.PinoyBusinessPOS.Api\ExItS.PinoyBusinessPOS.Api.csproj')
        if ($LASTEXITCODE -ne 0) { throw 'POS migration failed — stop deployment; do not continue as success.' }
        Write-ExItsInfo 'Migrations applied (Platform then POS).'
        break
    }
    'WaitHealth' {
        $platformHealth = "$PlatformBaseUrl/health"
        $platformReady = "$PlatformBaseUrl/health/ready"
        $posHealth = "$PosBaseUrl/health"
        $posReady = "$PosBaseUrl/health/ready"
        if (-not (Test-HttpOk $platformHealth $HealthTimeoutSeconds)) { throw "Platform liveness timeout: $platformHealth" }
        if (-not (Test-HttpOk $platformReady $HealthTimeoutSeconds)) { throw "Platform readiness timeout: $platformReady" }
        if (-not (Test-HttpOk $posHealth $HealthTimeoutSeconds)) { throw "POS liveness timeout: $posHealth" }
        if (-not (Test-HttpOk $posReady $HealthTimeoutSeconds)) { throw "POS readiness timeout: $posReady" }
        Write-ExItsInfo 'Health and readiness OK.'
        break
    }
    'SmokeHealth' {
        & (Join-Path $PSScriptRoot 'Invoke-ExItsSmoke.ps1') -Mode HealthOnly -PlatformBaseUrl $PlatformBaseUrl -PosBaseUrl $PosBaseUrl
        break
    }
    'SmokeFull' {
        if ($Environment -notin @('Development', 'Testing')) {
            throw 'Full identity-header smoke is allowed only in Development/Testing. Use SmokeHealth for StagingPilot.'
        }
        & (Join-Path $PSScriptRoot 'Invoke-ExItsSmoke.ps1') -Mode Full -PlatformBaseUrl $PlatformBaseUrl -PosBaseUrl $PosBaseUrl
        break
    }
    'Evidence' {
        if (-not $EvidenceDir) { $EvidenceDir = Join-Path $RepoRoot 'artifacts\deploy-evidence' }
        New-Item -ItemType Directory -Force -Path $EvidenceDir | Out-Null
        $commit = Get-GitCommit
        $stamp = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
        $path = Join-Path $EvidenceDir "rehearsal-$stamp.json"
        $payload = @{
            phaseMarker = 'P9-WP05-pilot-and-deployment'
            environment = $Environment
            commit = $commit
            utc = $stamp
            note = 'Evidence stub — fill durations/backup-set IDs after rehearsal steps.'
            healthcareUntouched = $true
        } | ConvertTo-Json -Depth 4
        Set-Content -Path $path -Value $payload -Encoding utf8
        Write-ExItsInfo "Wrote $path"
        break
    }
    'Rehearsal' {
        Assert-CleanTree
        Write-ExItsInfo 'Starting non-production rehearsal (Plan → PackageVersion → Evidence).'
        & $PSCommandPath -Environment $Environment -Action Plan -ConfirmPhrase $ConfirmPhrase
        & $PSCommandPath -Environment $Environment -Action PackageVersion -ConfirmPhrase $ConfirmPhrase
        & $PSCommandPath -Environment $Environment -Action Evidence -ConfirmPhrase $ConfirmPhrase -EvidenceDir $EvidenceDir
        Write-ExItsInfo 'Core rehearsal scaffolding complete. Run ValidateConfig/BackupGate/Migrate/WaitHealth/Smoke against disposable targets as documented.'
        break
    }
}

Write-ExItsInfo "Action $Action completed for $Environment."
exit 0
