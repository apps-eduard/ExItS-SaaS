#Requires -Version 5.1
# Shared Local Validation host-app start/stop helpers.
# Dot-source after LocalValidation.stack.ps1. Not Production.

function ConvertTo-LocalValidationEnvAssignments {
    param($EnvMap)

    ($EnvMap.GetEnumerator() | ForEach-Object {
        $escaped = ([string]$_.Value) -replace "'", "''"
        "`$env:$($_.Key) = '$escaped'; "
    }) -join ''
}

function Start-LocalValidationAppWindow {
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
    $prefix = ConvertTo-LocalValidationEnvAssignments -EnvMap $EnvMap
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
Remove-Item Env:DOTNET_ENVIRONMENT -ErrorAction SilentlyContinue;
Remove-Item Env:ASPNETCORE_ENVIRONMENT -ErrorAction SilentlyContinue;
Remove-Item Env:ReloadStaticAssetsAtRuntime -ErrorAction SilentlyContinue;
$prefix
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

function Start-LocalValidationNpmDevWindow {
    param(
        [string]$Title,
        [string]$WorkingDirectory,
        [hashtable]$EnvMap,
        [string]$NpmScript = 'dev',
        [string]$ExtraNpmArgs = ''
    )
    $prefix = ConvertTo-LocalValidationEnvAssignments -EnvMap $EnvMap
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

function Stop-LocalValidationKnownService {
    param(
        [Parameter(Mandatory)][string]$ServiceKey,
        [Parameter(Mandatory)][string]$RepoRoot
    )

    $svc = Resolve-LocalValidationCatalogService -ServiceKey $ServiceKey
    if (-not $svc.Restartable) {
        throw "Service '$($svc.Key)' is health-only and cannot be restarted from Local Validation controls."
    }

    $rootNorm = $RepoRoot.Replace('/', '\').TrimEnd('\')
    switch ($svc.Kind) {
        'dotnet' {
            $marker = [string]$svc.Marker
            foreach ($process in Get-CimInstance Win32_Process -Filter "Name = 'dotnet.exe'" -ErrorAction SilentlyContinue) {
                $cmd = [string]$process.CommandLine
                if ([string]::IsNullOrWhiteSpace($cmd)) { continue }
                if ($cmd.IndexOf($rootNorm, [StringComparison]::OrdinalIgnoreCase) -lt 0) { continue }
                if ($cmd.IndexOf($marker, [StringComparison]::OrdinalIgnoreCase) -lt 0) { continue }
                Write-Host ("[local-validation] Stopping {0} PID {1}" -f $svc.Label, $process.ProcessId) -ForegroundColor Cyan
                Stop-Process -Id $process.ProcessId -Force -ErrorAction SilentlyContinue
            }
            $exeName = "$marker.exe"
            foreach ($process in Get-CimInstance Win32_Process -Filter "Name = '$exeName'" -ErrorAction SilentlyContinue) {
                $haystack = ("{0}|{1}" -f $process.CommandLine, $process.ExecutablePath).Replace('/', '\')
                if ($haystack.IndexOf($rootNorm, [StringComparison]::OrdinalIgnoreCase) -lt 0) { continue }
                Write-Host ("[local-validation] Stopping {0} apphost PID {1}" -f $svc.Label, $process.ProcessId) -ForegroundColor Cyan
                Stop-Process -Id $process.ProcessId -Force -ErrorAction SilentlyContinue
            }
            Start-Sleep -Seconds 1
        }
        'npm' {
            # Vite/node is not in AppMarkers; free the known React POS port only.
            $null = Stop-LocalValidationPortListeners -Port ([int]$svc.Port) -Label $svc.Label
        }
        'docker' {
            $composeFile = Join-Path $RepoRoot 'deploy\docker\compose.local-validation.yaml'
            $envFile = Join-Path $RepoRoot 'deploy\docker\.env.local-validation'
            if (-not (Test-Path -LiteralPath $composeFile) -or -not (Test-Path -LiteralPath $envFile)) {
                throw "Missing compose/env for docker service $($svc.Key)."
            }
            $null = Invoke-LocalValidationDocker -DockerArgs @(
                'compose', '-p', $LocalValidationStack.ComposeProjectName,
                '-f', $composeFile, '--env-file', $envFile,
                'stop', [string]$svc.Marker
            )
        }
        default {
            throw "Unsupported service kind '$($svc.Kind)' for $($svc.Key)."
        }
    }
}

function Test-LocalValidationTcpOpen {
    param(
        [string]$HostName = '127.0.0.1',
        [Parameter(Mandatory)][int]$Port,
        [int]$TimeoutMs = 600
    )
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

function Get-LocalValidationControlHealthSnapshot {
    param(
        [hashtable]$PortOverrides = @{}
    )

    $rows = @()
    foreach ($svc in @(Get-LocalValidationServiceCatalog)) {
        $port = [int]$svc.Port
        if ($PortOverrides.ContainsKey($svc.Key)) {
            $port = [int]$PortOverrides[$svc.Key]
        }
        $up = Test-LocalValidationTcpOpen -Port $port
        $rows += [pscustomobject]@{
            key = $svc.Key
            label = $svc.Label
            port = $port
            status = if ($up) { 'Up' } else { 'Down' }
            restartable = [bool]$svc.Restartable
            kind = $svc.Kind
        }
    }
    return [pscustomobject]@{
        checkedAtUtc = (Get-Date).ToUniversalTime().ToString('o')
        services = $rows
        busy = $false
        operation = $null
    }
}

function Assert-LocalValidationControlNotProduction {
    $candidates = @(
        [Environment]::GetEnvironmentVariable('ASPNETCORE_ENVIRONMENT'),
        [Environment]::GetEnvironmentVariable('DOTNET_ENVIRONMENT'),
        $env:ASPNETCORE_ENVIRONMENT,
        $env:DOTNET_ENVIRONMENT
    ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

    foreach ($value in $candidates) {
        if ([string]::Equals($value, 'Production', [StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing Local Validation control action: environment is Production ($value)."
        }
    }
}

function Stop-LocalValidationSupervisorHost {
    param(
        [Parameter(Mandatory)][string]$RepoRoot,
        [int]$Port = 0,
        [switch]$KeepSupervisor
    )

    if ($KeepSupervisor) {
        return 0
    }

    if ($Port -le 0) { $Port = [int]$LocalValidationStack.DefaultSupervisorPort }
    $rootNorm = $RepoRoot.Replace('/', '\').TrimEnd('\')
    $stopped = 0
    $assembly = [string]$LocalValidationStack.SupervisorAssembly

    foreach ($process in Get-CimInstance Win32_Process -Filter "Name = 'dotnet.exe'" -ErrorAction SilentlyContinue) {
        $cmd = [string]$process.CommandLine
        if ([string]::IsNullOrWhiteSpace($cmd)) { continue }
        if ($cmd.IndexOf($rootNorm, [StringComparison]::OrdinalIgnoreCase) -lt 0) { continue }
        if ($cmd.IndexOf($assembly, [StringComparison]::OrdinalIgnoreCase) -lt 0) { continue }
        Write-Host ("[local-validation] Stopping supervisor dotnet PID {0}" -f $process.ProcessId) -ForegroundColor Cyan
        Stop-Process -Id $process.ProcessId -Force -ErrorAction SilentlyContinue
        $stopped++
    }

    $exeName = "$assembly.exe"
    foreach ($process in Get-CimInstance Win32_Process -Filter "Name = '$exeName'" -ErrorAction SilentlyContinue) {
        $haystack = ("{0}|{1}" -f $process.CommandLine, $process.ExecutablePath).Replace('/', '\')
        if ($haystack.IndexOf($rootNorm, [StringComparison]::OrdinalIgnoreCase) -lt 0) { continue }
        Write-Host ("[local-validation] Stopping supervisor apphost PID {0}" -f $process.ProcessId) -ForegroundColor Cyan
        Stop-Process -Id $process.ProcessId -Force -ErrorAction SilentlyContinue
        $stopped++
    }

    # PowerShell launcher windows titled for supervisor (state WindowPids also cover this).
    foreach ($proc in Get-Process -Name powershell, pwsh -ErrorAction SilentlyContinue) {
        try {
            if ($proc.MainWindowTitle -and $proc.MainWindowTitle.IndexOf('LocalValidation - Supervisor', [StringComparison]::OrdinalIgnoreCase) -ge 0) {
                Write-Host ("[local-validation] Stopping supervisor window PID {0}" -f $proc.Id) -ForegroundColor Cyan
                Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
                $stopped++
            }
        } catch { }
    }

    $owner = Get-LocalValidationListeningOwner -Port $Port
    if ($null -ne $owner) {
        $hay = ("{0}|{1}" -f $owner.ProcessName, $owner.CommandLine)
        if ($hay.IndexOf($assembly, [StringComparison]::OrdinalIgnoreCase) -ge 0 -or
            $hay.IndexOf('local-validation-supervisor', [StringComparison]::OrdinalIgnoreCase) -ge 0) {
            Write-Host ("[local-validation] Freeing supervisor port {0} (PID {1})" -f $Port, $owner.ProcessId) -ForegroundColor Cyan
            Stop-Process -Id $owner.ProcessId -Force -ErrorAction SilentlyContinue
            $stopped++
        }
    }

    if ($stopped -gt 0) { Start-Sleep -Seconds 1 }
    return $stopped
}

function Start-LocalValidationSupervisorHost {
    param(
        [Parameter(Mandatory)][string]$RepoRoot,
        [int]$Port = 0,
        [int]$WaitSeconds = 90,
        [switch]$ForceRestart
    )

    if ($Port -le 0) { $Port = [int]$LocalValidationStack.DefaultSupervisorPort }
    $healthUrl = "http://127.0.0.1:$Port/health"
    $servicesUrl = "http://127.0.0.1:$Port/health/services"

    if ($ForceRestart) {
        $null = Stop-LocalValidationSupervisorHost -RepoRoot $RepoRoot -Port $Port
    }
    elseif (Test-LocalValidationHttpReady -Uri $healthUrl -TimeoutSec 2) {
        if (Test-LocalValidationHttpReady -Uri $servicesUrl -TimeoutSec 5) {
            Write-Host ("[local-validation] Local Validation Supervisor :{0} UP (already running)" -f $Port) -ForegroundColor Green
            return $null
        }
        Write-Host "[local-validation] Supervisor /health OK but /health/services failed - restarting..." -ForegroundColor Yellow
        $null = Stop-LocalValidationSupervisorHost -RepoRoot $RepoRoot -Port $Port
    }
    else {
        # Clear stale listeners/processes before start.
        $null = Stop-LocalValidationSupervisorHost -RepoRoot $RepoRoot -Port $Port
    }

    $project = Join-Path $RepoRoot 'tools\ExItS.LocalValidation.Supervisor\ExItS.LocalValidation.Supervisor.csproj'
    if (-not (Test-Path -LiteralPath $project)) {
        throw "Missing supervisor project: $project"
    }

    $null = Invoke-LocalValidationDotnetBuild -Label 'Local Validation Supervisor' -ProjectPath $project
    $envMap = @{
        ASPNETCORE_ENVIRONMENT = 'Development'
        DOTNET_ENVIRONMENT = 'Development'
        ASPNETCORE_URLS = "http://127.0.0.1:$Port"
        LocalValidation__Enabled = 'true'
        LocalValidation__Supervisor__Enabled = 'true'
        LocalValidation__Supervisor__RepoRoot = $RepoRoot
        LocalValidation__Supervisor__BindAddress = '127.0.0.1'
        LocalValidation__Supervisor__Port = "$Port"
    }
    $launch = Start-LocalValidationAppWindow `
        -Title 'ExItS LocalValidation - Supervisor' `
        -RepoRoot $RepoRoot `
        -Project $project `
        -EnvMap $envMap `
        -Mode 'Run' `
        -ServiceKey 'local-validation-supervisor'
    try {
        $ready = Wait-LocalServiceReady `
            -ServiceName 'Local Validation Supervisor' `
            -HealthUri $healthUrl `
            -TimeoutSeconds $WaitSeconds `
            -WindowProcessId $launch.WindowProcessId `
            -ExitMarkerPath $launch.ExitMarkerPath
        $deadline = (Get-Date).AddSeconds([Math]::Max(15, [Math]::Min(60, $WaitSeconds)))
        $servicesReady = $false
        while ((Get-Date) -lt $deadline) {
            if (Test-LocalValidationHttpReady -Uri $servicesUrl -TimeoutSec 3) {
                $servicesReady = $true
                break
            }
            Start-Sleep -Milliseconds 500
        }
        if (-not $servicesReady) {
            throw "Local Validation Supervisor :$Port /health OK but /health/services did not become ready."
        }
        Write-Host ("[local-validation] Local Validation Supervisor :{0} UP ({1}s)" -f $Port, $ready.ReadyInSeconds) -ForegroundColor Green
        return $launch
    }
    catch {
        Write-Host "[local-validation] FAIL Local Validation Supervisor did not become ready on 127.0.0.1:$Port" -ForegroundColor Red
        Write-Host "[local-validation] FAIL Check the 'ExItS LocalValidation - Supervisor' window and exit marker $($launch.ExitMarkerPath)" -ForegroundColor Red
        throw
    }
}
