#Requires -Version 5.1
<#
.SYNOPSIS
  Allowlisted Local Validation control actions for the loopback supervisor.

.DESCRIPTION
  Only semantic actions: Status | Restart | RestartAll | Reset.
  Never accepts arbitrary shell/commands. Not Production.

.EXAMPLE
  .\tools\Invoke-LocalValidationControl.ps1 -Action Status

.EXAMPLE
  .\tools\Invoke-LocalValidationControl.ps1 -Action Restart -ServiceKey pos-api

.EXAMPLE
  .\tools\Invoke-LocalValidationControl.ps1 -Action RestartAll

.EXAMPLE
  .\tools\Invoke-LocalValidationControl.ps1 -Action Reset -ConfirmReset
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('Status', 'Restart', 'RestartAll', 'Reset')]
    [string]$Action,

    [string]$ServiceKey = '',

    [switch]$ConfirmReset,

    [int]$PortWaitSeconds = 120,

    [string]$ProgressFile = '',

    [ValidateSet('validation', 'local-dev')]
    [string]$Profile = 'validation'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LocalValidation.stack.ps1')
. (Join-Path $PSScriptRoot 'LocalValidation.host-apps.ps1')
. (Join-Path $PSScriptRoot 'LocalDev.stack.ps1')

function Write-ProgressStatus([string]$Message) {
    # Prefer stderr so supervisor can keep stdout JSON-only.
    Write-Host "[local-validation-control] $Message" -ForegroundColor Cyan
    try { [Console]::Error.WriteLine("[local-validation-control] $Message") } catch { }
    if (-not [string]::IsNullOrWhiteSpace($ProgressFile)) {
        try {
            Set-Content -LiteralPath $ProgressFile -Value $Message -Encoding utf8
        } catch { }
    }
}

function Write-JsonResult($Object) {
    # Final stdout payload for the supervisor parser (ignore host chatter).
    $json = $Object | ConvertTo-Json -Depth 6 -Compress
    Write-Output "___LV_CONTROL_JSON___$json"
}

Assert-LocalValidationControlNotProduction

$repoRoot = Get-LocalValidationRepoRoot
$startScript = Join-Path $repoRoot 'tools\Start-LocalValidation.ps1'
$stopScript = Join-Path $repoRoot 'tools\Stop-LocalValidation.ps1'
$localStartScript = Join-Path $repoRoot 'tools\Start-LocalDev.ps1'
$localStopScript = Join-Path $repoRoot 'tools\Stop-LocalDev.ps1'
$resetScript = Join-Path $repoRoot 'tools\Reset-LocalValidation.ps1'

function Get-LocalDevHealthOverrides {
    return @{
        'platform-api' = (Get-LocalDevServicePort -ServiceKey 'platform-api')
        'pos-api' = (Get-LocalDevServicePort -ServiceKey 'pos-api')
        'react-pos' = (Get-LocalDevServicePort -ServiceKey 'react-pos')
        'platform-admin' = (Get-LocalDevServicePort -ServiceKey 'platform-admin')
    }
}

switch ($Action) {
    'Status' {
        if ($Profile -eq 'local-dev') {
            $snapshot = Get-LocalValidationControlHealthSnapshot -PortOverrides (Get-LocalDevHealthOverrides)
        } else {
            $snapshot = Get-LocalValidationControlHealthSnapshot
        }
        Write-JsonResult $snapshot
        exit 0
    }

    'Restart' {
        if ([string]::IsNullOrWhiteSpace($ServiceKey)) {
            throw 'Restart requires -ServiceKey.'
        }
        if ($Profile -eq 'local-dev') {
            $port = Get-LocalDevServicePort -ServiceKey $ServiceKey
            if ($ServiceKey.Trim().ToLowerInvariant() -in @('platform-db', 'pos-db')) {
                throw "Service '$ServiceKey' is not restartable (database health-only)."
            }
            Write-ProgressStatus "Restarting local $ServiceKey on port $port..."
            & $localStopScript -OnlyServices @($ServiceKey)
            if ($LASTEXITCODE -ne 0) {
                throw "Stop-LocalDev -OnlyServices $ServiceKey failed ($LASTEXITCODE)."
            }
            Write-ProgressStatus "Starting local $ServiceKey on port $port..."
            & $localStartScript -OnlyServices @($ServiceKey) -PortWaitSeconds $PortWaitSeconds
            if ($LASTEXITCODE -ne 0) {
                throw "Start-LocalDev -OnlyServices $ServiceKey failed ($LASTEXITCODE)."
            }
            Write-JsonResult ([pscustomobject]@{
                    ok = $true
                    action = 'Restart'
                    profile = 'local-dev'
                    serviceKey = $ServiceKey.Trim().ToLowerInvariant()
                    port = $port
                    message = "$ServiceKey restarted on port $port."
                })
            exit 0
        }
        $svc = Resolve-LocalValidationCatalogService -ServiceKey $ServiceKey
        if (-not $svc.Restartable) {
            throw "Service '$($svc.Key)' is not restartable (database/infra health-only)."
        }
        Write-ProgressStatus "Restarting $($svc.Label)..."
        Stop-LocalValidationKnownService -ServiceKey $svc.Key -RepoRoot $repoRoot
        Write-ProgressStatus "Starting $($svc.Label)..."
        & $startScript -OnlyServices @($svc.Key) -PortWaitSeconds $PortWaitSeconds
        if ($LASTEXITCODE -ne 0) {
            throw "Start-LocalValidation -OnlyServices $($svc.Key) failed ($LASTEXITCODE)."
        }
        Write-ProgressStatus "$($svc.Label) restarted."
        Write-JsonResult ([pscustomobject]@{
                ok = $true
                action = 'Restart'
                serviceKey = $svc.Key
                label = $svc.Label
                message = "$($svc.Label) restarted successfully."
            })
        exit 0
    }

    'RestartAll' {
        if ($Profile -eq 'local-dev') {
            Write-ProgressStatus 'Restarting local applications on 5288, 5290, 5178, and 5195...'
            & $localStopScript
            if ($LASTEXITCODE -ne 0) {
                throw "Stop-LocalDev failed ($LASTEXITCODE)."
            }
            & $localStartScript -PortWaitSeconds $PortWaitSeconds
            if ($LASTEXITCODE -ne 0) {
                throw "Start-LocalDev failed ($LASTEXITCODE)."
            }
            Write-JsonResult ([pscustomobject]@{
                    ok = $true
                    action = 'RestartAll'
                    profile = 'local-dev'
                    message = 'Local applications restarted on ports 5288, 5290, 5178, and 5195.'
                })
            exit 0
        }
        $keys = @(Get-LocalValidationRestartableServiceKeys)
        Write-ProgressStatus "Restarting applications (0 of $($keys.Count))..."
        & $stopScript -KeepSupervisor
        if ($LASTEXITCODE -ne 0) {
            throw "Stop-LocalValidation -KeepSupervisor failed ($LASTEXITCODE)."
        }
        Write-ProgressStatus 'Starting Local Validation applications...'
        & $startScript -PortWaitSeconds $PortWaitSeconds -SkipSupervisorStart
        if ($LASTEXITCODE -ne 0) {
            throw "Start-LocalValidation failed ($LASTEXITCODE)."
        }
        # Ensure supervisor health after apps return (may already be running).
        $null = Start-LocalValidationSupervisorHost -RepoRoot $repoRoot -WaitSeconds ([Math]::Min(60, $PortWaitSeconds))
        Write-ProgressStatus 'Local Validation applications restarted.'
        Write-JsonResult ([pscustomobject]@{
                ok = $true
                action = 'RestartAll'
                message = 'Local Validation applications restarted.'
            })
        exit 0
    }

    'Reset' {
        if (-not $ConfirmReset) {
            throw 'Reset requires -ConfirmReset (canonical Local Validation wipe + reseed).'
        }
        Write-ProgressStatus 'Resetting Local Validation test data...'
        # Keep supervisor alive across Stop inside Reset; Start will bring apps back.
        & $resetScript -ConfirmReset -KeepSupervisor
        if ($LASTEXITCODE -ne 0) {
            throw "Reset-LocalValidation failed ($LASTEXITCODE)."
        }
        $null = Start-LocalValidationSupervisorHost -RepoRoot $repoRoot -WaitSeconds ([Math]::Min(60, $PortWaitSeconds))
        Write-ProgressStatus 'Local Validation reset complete. Olivia and Rafael restored.'
        Write-JsonResult ([pscustomobject]@{
                ok = $true
                action = 'Reset'
                message = 'Local Validation reset complete. Olivia and Rafael restored.'
                baselineUsers = @('Olivia Mendoza', 'Rafael Torres')
            })
        exit 0
    }
}
