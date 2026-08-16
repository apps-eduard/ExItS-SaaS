#Requires -Version 5.1
# Shared Local Validation stack identity — dot-source from Start/Reset/Stop scripts.
# Not Production. Do not delete unrelated Docker volumes from these helpers.

$script:LocalValidationStack = [pscustomobject]@{
    ComposeProjectName     = 'exits-local-validation'
    ComposeFileName        = 'compose.local-validation.yaml'
    EnvFileName            = '.env.local-validation'
    PlatformDbContainer    = 'exits-local-validation-platform-db'
    PosDbContainer         = 'exits-local-validation-pos-db'
    MailpitContainer       = 'exits-local-validation-mailpit'
    PlatformApiContainer   = 'exits-local-validation-platform-api'
    PosApiContainer        = 'exits-local-validation-pos-api'
    AdminWebContainer      = 'exits-local-validation-admin-web'
    OrgWebContainer        = 'exits-local-validation-org-web'
    PersonalWebContainer   = 'exits-local-validation-personal-web'
    AppComposeServices     = @('platform-api', 'pos-api', 'admin-web', 'org-web', 'personal-web')
    InfraComposeServices   = @('platform-db', 'pos-db', 'mailpit')
    AppMarkers             = @(
        'ExItS.Platform.Api',
        'ExItS.PinoyBusinessPOS.Api',
        'ExItS.Platform.Admin',
        'ExItS.PinoyBusinessPOS.Web',
        'ExItS.Personal.Web'
    )
    PlatformDbVolume       = 'exits_local_validation_platform_db_data'
    PosDbVolume            = 'exits_local_validation_pos_db_data'
    PlatformDbName         = 'exits_platform'
    PosDbName              = 'exits_pos'
    DefaultPlatformDbPort  = 15533
    DefaultPosDbPort       = 15534
    DefaultAdminPort       = 8090
    DefaultPlatformApiPort = 8091
    DefaultPosApiPort      = 8092
    DefaultOrgWebPort      = 8093
    DefaultPersonalWebPort = 8094
    DefaultSeedScope       = 'PlatformAdministratorsOnly'
}

function Get-LocalValidationRepoRoot {
    param([string]$StartPath = $PSScriptRoot)

    $dir = (Resolve-Path -LiteralPath $StartPath).Path
    $probe = Get-Item -LiteralPath $dir
    while ($null -ne $probe) {
        if (Test-Path -LiteralPath (Join-Path $probe.FullName 'ExItS.slnx')) {
            return $probe.FullName
        }
        $probe = $probe.Parent
    }
    throw "Could not locate ExItS.slnx above $StartPath."
}

function Import-LocalValidationDotEnv {
    param([Parameter(Mandatory)][string]$Path)

    $map = @{}
    Get-Content -LiteralPath $Path | ForEach-Object {
        $line = $_.Trim()
        if ($line.Length -eq 0 -or $line.StartsWith('#')) { return }
        $idx = $line.IndexOf('=')
        if ($idx -lt 1) { return }
        $key = $line.Substring(0, $idx).Trim()
        $value = $line.Substring($idx + 1).Trim()
        if (($value.StartsWith('"') -and $value.EndsWith('"')) -or
            ($value.StartsWith("'") -and $value.EndsWith("'"))) {
            $value = $value.Substring(1, $value.Length - 2)
        }
        $map[$key] = $value
    }
    return $map
}

function Require-LocalValidationEnvKey {
    param(
        [Parameter(Mandatory)]$Map,
        [Parameter(Mandatory)][string]$Key
    )

    if (-not $Map.ContainsKey($Key) -or
        [string]::IsNullOrWhiteSpace([string]$Map[$Key]) -or
        ([string]$Map[$Key]).StartsWith('REPLACE_')) {
        throw "Set a real value for $Key in deploy/docker/.env.local-validation (not REPLACE_*)."
    }
}

function Invoke-LocalValidationDocker {
    param(
        [Parameter(Mandatory)]
        [string[]]$DockerArgs
    )
    # Docker progress/status is written to stderr; under $ErrorActionPreference Stop that can abort scripts.
    # Also: never let docker stdout become the function's return value (PowerShell success-stream capture).
    $prev = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $output = & docker @DockerArgs 2>&1
        $exitCode = [int]$LASTEXITCODE
        foreach ($line in @($output)) {
            if ($null -eq $line) { continue }
            Write-Host ([string]$line)
        }
        return $exitCode
    }
    finally {
        $ErrorActionPreference = $prev
    }
}

function Test-LocalValidationDockerAvailable {
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
        throw 'Docker CLI not found. Install/start Docker Desktop.'
    }
    & docker info 1>$null 2>$null
    if ($LASTEXITCODE -ne 0) {
        throw 'Docker Desktop is not available (docker info failed). Start Docker Desktop and retry.'
    }
}

function Get-LocalValidationListeningOwner {
    param([Parameter(Mandatory)][int]$Port)

    $connections = @(Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue)
    if ($connections.Count -eq 0) { return $null }
    $processId = $connections[0].OwningProcess
    $process = Get-Process -Id $processId -ErrorAction SilentlyContinue
    $commandLine = $null
    try {
        $commandLine = (Get-CimInstance Win32_Process -Filter "ProcessId = $processId" -ErrorAction SilentlyContinue).CommandLine
    }
    catch { }
    return [pscustomobject]@{
        Port        = $Port
        ProcessId   = $processId
        ProcessName = if ($process) { $process.ProcessName } else { '?' }
        CommandLine = $commandLine
    }
}

function Get-LocalValidationRepoScopedAppProcesses {
    param([Parameter(Mandatory)][string]$RepoRoot)

    $rootNorm = $RepoRoot.Replace('/', '\').TrimEnd('\')
    $results = @()
    foreach ($process in Get-CimInstance Win32_Process -Filter "Name = 'dotnet.exe'" -ErrorAction SilentlyContinue) {
        $commandLine = [string]$process.CommandLine
        if ([string]::IsNullOrWhiteSpace($commandLine)) { continue }
        if ($commandLine.IndexOf($rootNorm, [StringComparison]::OrdinalIgnoreCase) -lt 0) { continue }
        foreach ($marker in $LocalValidationStack.AppMarkers) {
            if ($commandLine.IndexOf($marker, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
                $results += $process
                break
            }
        }
    }
    return $results
}

function Stop-LocalValidationRepoScopedHostApps {
    param([Parameter(Mandatory)][string]$RepoRoot)

    $stateFile = Join-Path $env:LOCALAPPDATA 'ExItS\LocalValidation\launcher-state.json'
    if (Test-Path -LiteralPath $stateFile) {
        try {
            $state = Get-Content -LiteralPath $stateFile -Raw | ConvertFrom-Json
            if ([string]$state.Mode -ne 'DockerApps') {
                foreach ($windowPid in @($state.WindowPids)) {
                    if ($windowPid -and (Get-Process -Id $windowPid -ErrorAction SilentlyContinue)) {
                        Write-Host "[local-validation] Stopping host launcher window PID $windowPid" -ForegroundColor Cyan
                        Stop-Process -Id $windowPid -Force -ErrorAction SilentlyContinue
                    }
                }
            }
        }
        catch { }
    }

    $processes = @(Get-LocalValidationRepoScopedAppProcesses -RepoRoot $RepoRoot)
    foreach ($process in $processes) {
        $snippet = [string]$process.CommandLine
        if ($snippet.Length -gt 120) { $snippet = $snippet.Substring(0, 120) }
        Write-Host ("[local-validation] Stopping repo-scoped host PID {0}: {1}" -f $process.ProcessId, $snippet) -ForegroundColor Cyan
        Stop-Process -Id $process.ProcessId -Force -ErrorAction SilentlyContinue
    }
    if ($processes.Count -gt 0) { Start-Sleep -Seconds 1 }
    return $processes
}

function Report-LocalValidationPortConflicts {
    param([Parameter(Mandatory)][int[]]$Ports)

    $blocked = @()
    foreach ($port in $Ports) {
        $owner = Get-LocalValidationListeningOwner -Port $port
        if ($null -ne $owner) {
            Write-Host ("[local-validation] FAIL Port {0} in use by PID {1} ({2})" -f $owner.Port, $owner.ProcessId, $owner.ProcessName) -ForegroundColor Red
            if ($owner.CommandLine) { Write-Host "         $($owner.CommandLine)" }
            $blocked += $owner
        }
    }
    return $blocked
}

function Resolve-LocalValidationPublicHostValue {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) { return '' }
    $hostName = $Value.Trim()
    if ($hostName -match '://' -or
        $hostName.Contains('/') -or
        $hostName.Contains('\') -or
        $hostName.Contains(' ') -or
        $hostName -match ':\d+$') {
        throw 'PublicHost must be a host or IP only (no scheme, port, path, or spaces).'
    }
    return $hostName
}

function Resolve-LocalValidationEffectivePublicHost {
    param(
        [string]$ParamValue,
        [Parameter(Mandatory)]$EnvMap,
        [Parameter(Mandatory)][string]$StateFilePath
    )

    $candidate = Resolve-LocalValidationPublicHostValue -Value $ParamValue
    if ($candidate) { return $candidate }

    $candidate = Resolve-LocalValidationPublicHostValue -Value ([string]$EnvMap['LOCAL_VALIDATION_PUBLIC_HOST'])
    if ($candidate) { return $candidate }

    if (Test-Path -LiteralPath $StateFilePath) {
        try {
            $state = Get-Content -LiteralPath $StateFilePath -Raw | ConvertFrom-Json
            $candidate = Resolve-LocalValidationPublicHostValue -Value ([string]$state.PublicHost)
            if ($candidate) { return $candidate }
        }
        catch { }
    }

    try {
        $candidate = Get-NetIPAddress -AddressFamily IPv4 -ErrorAction SilentlyContinue |
            Where-Object {
                $_.IPAddress -like '100.*' -and
                ($_.InterfaceAlias -match '(?i)tailscale' -or $_.PrefixOrigin -eq 'Manual')
            } |
            Select-Object -First 1 -ExpandProperty IPAddress
        return Resolve-LocalValidationPublicHostValue -Value ([string]$candidate)
    }
    catch {
        return ''
    }
}

function Get-LocalValidationAllowedHostsList {
    param(
        [string]$PublicHostValue,
        [Parameter(Mandatory)]$EnvMap
    )

    $hosts = New-Object 'System.Collections.Generic.List[string]'
    foreach ($hostName in @('localhost', '127.0.0.1', '10.0.2.2', 'platform-api', 'pos-api', 'admin-web', 'org-web', 'personal-web')) {
        if (-not $hosts.Contains($hostName)) { $hosts.Add($hostName) }
    }
    if (-not [string]::IsNullOrWhiteSpace($PublicHostValue) -and -not $hosts.Contains($PublicHostValue)) {
        $hosts.Add($PublicHostValue)
    }
    foreach ($part in ([string]$EnvMap['LOCAL_VALIDATION_ALLOWED_HOSTS'] -split ';')) {
        $trimmed = $part.Trim()
        if ($trimmed.Length -gt 0 -and -not $hosts.Contains($trimmed)) { $hosts.Add($trimmed) }
    }
    return ($hosts -join ';')
}

function Stop-LocalValidationDockerAppServices {
    param(
        [Parameter(Mandatory)][string]$ComposeFile,
        [Parameter(Mandatory)][string]$EnvFile
    )

    $args = @(
        'compose', '-p', $LocalValidationStack.ComposeProjectName,
        '-f', $ComposeFile, '--env-file', $EnvFile,
        'stop'
    ) + $LocalValidationStack.AppComposeServices
    $exitCode = Invoke-LocalValidationDocker -DockerArgs $args
    if ($exitCode -ne 0) {
        Write-Host '[local-validation] Docker app services were not running or could not be stopped; continuing.' -ForegroundColor Yellow
    }
    return $exitCode
}

function Start-LocalValidationInfrastructure {
    param(
        [Parameter(Mandatory)][string]$ComposeFile,
        [Parameter(Mandatory)][string]$EnvFile
    )

    $args = @(
        'compose', '-p', $LocalValidationStack.ComposeProjectName,
        '-f', $ComposeFile, '--env-file', $EnvFile,
        'up', '-d'
    ) + $LocalValidationStack.InfraComposeServices
    $exitCode = Invoke-LocalValidationDocker -DockerArgs $args
    if ($exitCode -ne 0) {
        throw "docker compose up platform-db pos-db mailpit failed ($exitCode)."
    }
}

function Get-LocalValidationConnectionSummary {
    param(
        [Parameter(Mandatory)][string]$ConnectionString,
        [Parameter(Mandatory)][string]$Label
    )
    $hostName = $null; $port = $null; $database = $null
    foreach ($part in ($ConnectionString -split ';')) {
        $kv = $part.Trim()
        if ($kv.Length -eq 0) { continue }
        $idx = $kv.IndexOf('=')
        if ($idx -lt 1) { continue }
        $key = $kv.Substring(0, $idx).Trim()
        $value = $kv.Substring($idx + 1).Trim()
        switch -Regex ($key) {
            '^(Host|Server)$' { $hostName = $value }
            '^Port$' { $port = $value }
            '^(Database|Initial Catalog)$' { $database = $value }
        }
    }
    return [pscustomobject]@{
        Label    = $Label
        Host     = $hostName
        Port     = $port
        Database = $database
    }
}

function Write-LocalValidationStartupDiagnostics {
    param(
        [string]$AspNetCoreEnvironment,
        [string]$SeedScope,
        [object]$PlatformCsSummary,
        [object]$PosCsSummary,
        [string]$ComposeProjectName,
        [string]$PlatformDbContainer,
        [string]$PosDbContainer,
        [string]$PlatformDbVolume,
        [string]$PosDbVolume,
        [int[]]$WindowPids
    )
    Write-Host '[local-validation] --- startup diagnostics (no secrets) ---' -ForegroundColor Cyan
    Write-Host ("  ASPNETCORE_ENVIRONMENT: {0}" -f $AspNetCoreEnvironment)
    Write-Host ("  SeedScope:              {0}" -f $SeedScope)
    Write-Host ("  Compose project:        {0}" -f $ComposeProjectName)
    Write-Host ("  Platform DB:            {0}:{1}/{2}" -f $PlatformCsSummary.Host, $PlatformCsSummary.Port, $PlatformCsSummary.Database)
    Write-Host ("  POS DB:                 {0}:{1}/{2}" -f $PosCsSummary.Host, $PosCsSummary.Port, $PosCsSummary.Database)
    Write-Host ("  Expected containers:    {0}, {1}" -f $PlatformDbContainer, $PosDbContainer)
    Write-Host ("  Expected volumes:       {0}, {1}" -f $PlatformDbVolume, $PosDbVolume)
    if ($WindowPids -and $WindowPids.Count -gt 0) {
        Write-Host ("  Launcher window PIDs:   {0}" -f ($WindowPids -join ', '))
    }
}
