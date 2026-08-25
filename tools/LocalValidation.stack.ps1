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
    ReactPosContainer      = 'exits-local-validation-react-pos'
    AppComposeServices     = @('platform-api', 'pos-api', 'admin-web', 'org-web', 'personal-web', 'react-pos')
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
    DefaultReactPosPort    = 5177
    # React Platform Admin (Vite) — owns Mailpit activation/reset pages (/admin/activate-account, etc.)
    DefaultReactAdminPort  = 8095
    DefaultSeedScope       = 'PlatformAdministratorsOnly'
}

function Resolve-LocalValidationAuthPublicBaseUrl {
    <#
    .SYNOPSIS
      Public base URL embedded in PlatformEmail activation/reset links.
      Must point at the frontend that hosts /admin/activate-account and /admin/reset-password
      (React Admin Vite on :8095), not Blazor Admin :8090.
    #>
    param(
        [hashtable]$EnvMap,
        [string]$ResolvedPublicHost = '',
        [int]$ReactAdminPort = 0
    )

    if ($ReactAdminPort -le 0) {
        $ReactAdminPort = [int]$LocalValidationStack.DefaultReactAdminPort
    }

    $override = [string]$env:EXITS_ADMIN_PUBLIC_BASE_URL
    if (-not [string]::IsNullOrWhiteSpace($override)) {
        return $override.TrimEnd('/')
    }

    $fromEnv = if ($EnvMap -and $EnvMap['LOCAL_VALIDATION_REACT_ADMIN_ORIGIN']) {
        [string]$EnvMap['LOCAL_VALIDATION_REACT_ADMIN_ORIGIN']
    } else {
        ''
    }
    if (-not [string]::IsNullOrWhiteSpace($fromEnv)) {
        return $fromEnv.TrimEnd('/')
    }

    if (-not [string]::IsNullOrWhiteSpace($ResolvedPublicHost)) {
        return "http://$($ResolvedPublicHost.Trim()):$ReactAdminPort"
    }

    return "http://127.0.0.1:$ReactAdminPort"
}

function Add-LocalValidationReactCorsOrigins {
    <#
    .SYNOPSIS
      Ensures React POS (:5177) and React Admin (:8095) origins are allowed for Platform/POS APIs.
    #>
    param(
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [string[]]$CorsOrigins,
        [hashtable]$EnvMap,
        [string]$ResolvedPublicHost = '',
        [int]$ReactPosPort = 0,
        [int]$ReactAdminPort = 0
    )

    if ($ReactPosPort -le 0) { $ReactPosPort = [int]$LocalValidationStack.DefaultReactPosPort }
    if ($ReactAdminPort -le 0) { $ReactAdminPort = [int]$LocalValidationStack.DefaultReactAdminPort }

    $list = [System.Collections.Generic.List[string]]::new()
    foreach ($o in @($CorsOrigins)) {
        if (-not [string]::IsNullOrWhiteSpace($o) -and -not $list.Contains($o)) {
            [void]$list.Add($o)
        }
    }

    $candidates = @(
        "http://127.0.0.1:$ReactPosPort",
        "http://localhost:$ReactPosPort",
        "http://127.0.0.1:$ReactAdminPort",
        "http://localhost:$ReactAdminPort",
        "http://10.0.2.2:$ReactPosPort"
    )

    if ($EnvMap) {
        foreach ($key in @(
                'LOCAL_VALIDATION_REACT_POS_ORIGIN',
                'LOCAL_VALIDATION_REACT_POS_ORIGIN_LOCALHOST',
                'LOCAL_VALIDATION_REACT_POS_ORIGIN_EMULATOR',
                'LOCAL_VALIDATION_REACT_ADMIN_ORIGIN',
                'LOCAL_VALIDATION_ADMIN_ORIGIN'
            )) {
            if ($EnvMap[$key]) { $candidates += [string]$EnvMap[$key] }
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($ResolvedPublicHost)) {
        $host = $ResolvedPublicHost.Trim()
        $candidates += @(
            "http://${host}:$ReactPosPort",
            "http://${host}:$ReactAdminPort"
        )
    }

    foreach ($c in $candidates) {
        $n = [string]$c
        if ([string]::IsNullOrWhiteSpace($n)) { continue }
        if (-not $list.Contains($n)) { [void]$list.Add($n) }
    }

    return ,$list.ToArray()
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

    return Stop-LocalValidationCrossWorktreeHostApps -RepoRoot $RepoRoot
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
    foreach ($hostName in @('localhost', '127.0.0.1', '10.0.2.2', 'platform-api', 'pos-api', 'admin-web', 'org-web', 'personal-web', 'react-pos')) {
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

function Get-ExItSRepositoryWorktrees {
    param([Parameter(Mandatory)][string]$RepoRoot)

    $entries = @()
    $current = @{}
    $raw = & git -C $RepoRoot worktree list --porcelain 2>$null
    if ($LASTEXITCODE -ne 0 -or -not $raw) {
        return @(
            [pscustomobject]@{
                Path   = (Resolve-Path -LiteralPath $RepoRoot).Path
                Head   = (& git -C $RepoRoot rev-parse HEAD 2>$null)
                Branch = (& git -C $RepoRoot branch --show-current 2>$null)
            }
        )
    }

    foreach ($line in @($raw)) {
        if ($line.StartsWith('worktree ')) {
            if ($current.Count -gt 0) {
                $entries += [pscustomobject]$current
                $current = @{}
            }
            $current.Path = $line.Substring(9).Trim()
        }
        elseif ($line.StartsWith('HEAD ')) {
            $current.Head = $line.Substring(5).Trim()
        }
        elseif ($line.StartsWith('branch ')) {
            $current.Branch = ($line.Substring(7).Trim() -replace '^refs/heads/', '')
        }
        elseif ($line -eq 'detached') {
            $current.Branch = '(detached)'
        }
    }
    if ($current.Count -gt 0) {
        $entries += [pscustomobject]$current
    }
    return $entries
}

function Get-LocalValidationWorktreeGitMetadata {
    param([Parameter(Mandatory)][string]$WorktreePath)

    $head = [string](& git -C $WorktreePath rev-parse HEAD 2>$null)
    $branch = [string](& git -C $WorktreePath branch --show-current 2>$null)
    if ([string]::IsNullOrWhiteSpace($branch)) { $branch = '(detached)' }
    return [pscustomobject]@{
        WorktreePath = (Resolve-Path -LiteralPath $WorktreePath).Path
        Head         = $head
        Branch       = $branch
    }
}

function Resolve-LocalValidationWorktreeFromCommandLine {
    param(
        [string]$CommandLine,
        [Parameter(Mandatory)][object[]]$Worktrees
    )

    if ([string]::IsNullOrWhiteSpace($CommandLine)) { return $null }
    $cmdNorm = $CommandLine.Replace('/', '\')
    $matches = @()
    foreach ($worktree in $Worktrees) {
        $pathNorm = ([string]$worktree.Path).Replace('/', '\').TrimEnd('\')
        if ($cmdNorm.IndexOf($pathNorm, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
            $matches += $worktree
        }
    }
    if ($matches.Count -eq 0) { return $null }
    return ($matches | Sort-Object { ([string]$_.Path).Length } -Descending | Select-Object -First 1)
}

function Get-LocalValidationCrossWorktreeHostProcesses {
    param([Parameter(Mandatory)][string]$RepoRoot)

    $worktrees = @(Get-ExItSRepositoryWorktrees -RepoRoot $RepoRoot)
    $byPid = @{}
    foreach ($worktree in $worktrees) {
        foreach ($process in @(Get-LocalValidationRepoScopedAppProcesses -RepoRoot $worktree.Path)) {
            $byPid[$process.ProcessId] = $process
        }
    }
    return @($byPid.Values)
}

function Get-LocalValidationDockerAppContainers {
    $names = @(
        $LocalValidationStack.PlatformApiContainer,
        $LocalValidationStack.PosApiContainer,
        $LocalValidationStack.AdminWebContainer,
        $LocalValidationStack.OrgWebContainer,
        $LocalValidationStack.PersonalWebContainer,
        $LocalValidationStack.ReactPosContainer
    )
    $results = @()
    foreach ($name in $names) {
        $state = (& docker inspect -f '{{.State.Status}}' $name 2>$null)
        if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($state)) { continue }
        $labelsJson = (& docker inspect -f '{{json .Config.Labels}}' $name 2>$null)
        $labels = $null
        if ($labelsJson) {
            try { $labels = $labelsJson | ConvertFrom-Json } catch { }
        }
        $results += [pscustomobject]@{
            Name              = $name
            Status            = [string]$state
            ComposeWorkingDir = if ($labels) { [string]$labels.'com.docker.compose.project.working_dir' } else { $null }
            ComposeConfigFile = if ($labels) { [string]$labels.'com.docker.compose.project.config_files' } else { $null }
            ComposeService    = if ($labels) { [string]$labels.'com.docker.compose.service' } else { $null }
        }
    }
    return $results
}

function Resolve-LocalValidationDockerContainerForPort {
    param([Parameter(Mandatory)][int]$Port)

    foreach ($container in @(Get-LocalValidationDockerAppContainers)) {
        if ($container.Status -ne 'running') { continue }
        $portLines = @(& docker port $container.Name 2>$null)
        foreach ($line in $portLines) {
            if ($line -match ":$Port`$") {
                return $container
            }
        }
    }
    return $null
}

function Resolve-LocalValidationPortRuntimeProvenance {
    param(
        [Parameter(Mandatory)][int]$Port,
        [Parameter(Mandatory)][string]$ExpectedRepoRoot,
        [string]$AppLabel = '?'
    )

    $expectedNorm = (Resolve-Path -LiteralPath $ExpectedRepoRoot).Path
    $worktrees = @(Get-ExItSRepositoryWorktrees -RepoRoot $ExpectedRepoRoot)
    $dockerContainer = Resolve-LocalValidationDockerContainerForPort -Port $Port
    if ($null -ne $dockerContainer) {
        $composeDir = $dockerContainer.ComposeWorkingDir
        $worktreePath = $null
        if ($composeDir) {
            $probe = (Resolve-Path -LiteralPath $composeDir).Path
            while ($probe) {
                if (Test-Path -LiteralPath (Join-Path $probe 'ExItS.slnx')) {
                    $worktreePath = $probe
                    break
                }
                $parent = Split-Path -Parent $probe
                if ($parent -eq $probe) { break }
                $probe = $parent
            }
        }
        $git = if ($worktreePath) {
            Get-LocalValidationWorktreeGitMetadata -WorktreePath $worktreePath
        } else {
            [pscustomobject]@{ WorktreePath = $composeDir; Head = ''; Branch = '' }
        }
        $expected = ($git.WorktreePath -and ($git.WorktreePath.Replace('/', '\').TrimEnd('\').Equals(
            $expectedNorm.Replace('/', '\').TrimEnd('\'), [StringComparison]::OrdinalIgnoreCase)))
        $composeService = if ($dockerContainer.ComposeService) { $dockerContainer.ComposeService } else { $AppLabel }
        return [pscustomobject]@{
            Port        = $Port
            App         = $composeService
            RuntimeKind = 'docker'
            ProcessId   = $dockerContainer.Name
            ProcessName = 'docker'
            CommandLine = $dockerContainer.ComposeConfigFile
            Worktree    = $git.WorktreePath
            Branch      = $git.Branch
            Head        = $git.Head
            Expected    = [bool]$expected
            StartTime   = $null
        }
    }

    $owner = Get-LocalValidationListeningOwner -Port $Port
    if ($null -eq $owner) {
        return [pscustomobject]@{
            Port        = $Port
            App         = $AppLabel
            RuntimeKind = 'free'
            ProcessId   = $null
            ProcessName = $null
            CommandLine = $null
            Worktree    = $null
            Branch      = $null
            Head        = $null
            Expected    = $true
            StartTime   = $null
        }
    }

    $matchedWorktree = Resolve-LocalValidationWorktreeFromCommandLine -CommandLine $owner.CommandLine -Worktrees $worktrees
    $worktreePath = if ($matchedWorktree) { $matchedWorktree.Path } else { $null }
    $branch = if ($matchedWorktree) { $matchedWorktree.Branch } else { $null }
    $head = if ($matchedWorktree) { $matchedWorktree.Head } else { $null }
    if ($worktreePath -and (-not $head -or -not $branch)) {
        $git = Get-LocalValidationWorktreeGitMetadata -WorktreePath $worktreePath
        $branch = $git.Branch
        $head = $git.Head
    }
    $expected = ($worktreePath -and ($worktreePath.Replace('/', '\').TrimEnd('\').Equals(
        $expectedNorm.Replace('/', '\').TrimEnd('\'), [StringComparison]::OrdinalIgnoreCase)))
    $startTime = $null
    try {
        $startTime = (Get-Process -Id $owner.ProcessId -ErrorAction SilentlyContinue).StartTime
    }
    catch { }

    return [pscustomobject]@{
        Port        = $Port
        App         = $AppLabel
        RuntimeKind = 'host'
        ProcessId   = $owner.ProcessId
        ProcessName = $owner.ProcessName
        CommandLine = $owner.CommandLine
        Worktree    = $worktreePath
        Branch      = $branch
        Head        = $head
        Expected    = [bool]$expected
        StartTime   = $startTime
    }
}

function Write-LocalValidationRuntimeProvenanceTable {
    param(
        [Parameter(Mandatory)][hashtable]$PortLabels,
        [Parameter(Mandatory)][string]$ExpectedRepoRoot
    )

    Write-Host '[local-validation] --- runtime provenance (before startup) ---' -ForegroundColor Cyan
    Write-Host ('{0,-6} {1,-22} {2,-8} {3,-48} {4,-28} {5,-12} {6,-8}' -f `
        'PORT', 'APP', 'KIND', 'WORKTREE', 'BRANCH', 'HEAD', 'EXPECTED?')
    foreach ($entry in ($PortLabels.GetEnumerator() | Sort-Object { [int]$_.Key })) {
        $row = Resolve-LocalValidationPortRuntimeProvenance -Port ([int]$entry.Key) -ExpectedRepoRoot $ExpectedRepoRoot -AppLabel $entry.Value
        $headShort = if ($row.Head) { $row.Head.Substring(0, [Math]::Min(12, $row.Head.Length)) } else { '-' }
        $worktreeShort = if ($row.Worktree) {
            $norm = $row.Worktree.Replace('/', '\')
            if ($norm.Length -gt 46) { '...' + $norm.Substring($norm.Length - 43) } else { $norm }
        } else { '-' }
        $expectedLabel = if ($row.RuntimeKind -eq 'free') { 'free' } elseif ($row.Expected) { 'YES' } else { 'NO' }
        $pidLabel = if ($row.ProcessId) { [string]$row.ProcessId } else { '-' }
        $branchLabel = if ($row.Branch) { $row.Branch } else { '-' }
        $cmdLine = if ($row.CommandLine) { $row.CommandLine } else { '' }
        Write-Host ('{0,-6} {1,-22} {2,-8} {3,-48} {4,-28} {5,-12} {6,-8}' -f `
            $row.Port, $row.App, $row.RuntimeKind, $worktreeShort, $branchLabel, $headShort, $expectedLabel)
        if ($row.RuntimeKind -ne 'free') {
            Write-Host ("         PID={0} {1}" -f $pidLabel, $cmdLine)
        }
    }
}

function Write-LocalValidationRuntimeSummary {
    param(
        [Parameter(Mandatory)][hashtable]$PortLabels,
        [Parameter(Mandatory)][string]$ExpectedRepoRoot,
        [ValidateSet('HostApps', 'DockerApps')]
        [string]$Mode = 'HostApps'
    )

    Write-Host ''
    Write-Host 'LOCAL VALIDATION RUNTIME' -ForegroundColor Green
    Write-Host ("  Mode: {0}" -f $Mode)
    foreach ($entry in ($PortLabels.GetEnumerator() | Sort-Object { [int]$_.Key })) {
        $row = Resolve-LocalValidationPortRuntimeProvenance -Port ([int]$entry.Key) -ExpectedRepoRoot $ExpectedRepoRoot -AppLabel $entry.Value
        $worktreeLabel = if ($row.Worktree) { $row.Worktree } else { '(none / foreign runtime)' }
        $branchLabel = if ($row.Branch) { $row.Branch } else { '-' }
        $headLabel = if ($row.Head) { $row.Head } else { '-' }
        $pidLabelSummary = if ($row.ProcessId) { [string]$row.ProcessId } else { '-' }
        Write-Host ("  {0} :{1}" -f $entry.Value, $entry.Key)
        Write-Host ("    Worktree: {0}" -f $worktreeLabel)
        Write-Host ("    Branch:   {0}" -f $branchLabel)
        Write-Host ("    SHA:      {0}" -f $headLabel)
        Write-Host ("    PID:      {0} ({1})" -f $pidLabelSummary, $row.RuntimeKind)
        if (-not $row.Expected -and $row.RuntimeKind -ne 'free') {
            Write-Host '    WARNING: port owner does not match this launcher worktree/branch.' -ForegroundColor Yellow
        }
    }
}

function Assert-LocalValidationPortsOwnedByExpectedWorktree {
    param(
        [Parameter(Mandatory)][hashtable]$PortLabels,
        [Parameter(Mandatory)][string]$ExpectedRepoRoot
    )

    $blockers = @()
    foreach ($entry in $PortLabels.GetEnumerator()) {
        $row = Resolve-LocalValidationPortRuntimeProvenance -Port ([int]$entry.Key) -ExpectedRepoRoot $ExpectedRepoRoot -AppLabel $entry.Value
        if ($row.RuntimeKind -eq 'free') {
            $blockers += "Port $($entry.Key) ($($entry.Value)) is not listening after startup."
            continue
        }
        if (-not $row.Expected) {
            $worktreeBlocker = if ($row.Worktree) { $row.Worktree } else { 'unknown' }
            $headBlocker = if ($row.Head) { $row.Head } else { '?' }
            $blockers += ("Port {0} ({1}) is owned by {2} @ {3} ({4}) - expected {5}." -f `
                $entry.Key, $entry.Value, $row.RuntimeKind, $worktreeBlocker, $headBlocker, $ExpectedRepoRoot)
        }
    }
    if ($blockers.Count -gt 0) {
        throw ($blockers -join ' ')
    }
}

function Stop-LocalValidationCrossWorktreeHostApps {
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

    $processes = @(Get-LocalValidationCrossWorktreeHostProcesses -RepoRoot $RepoRoot)
    foreach ($process in $processes) {
        $snippet = [string]$process.CommandLine
        if ($snippet.Length -gt 120) { $snippet = $snippet.Substring(0, 120) }
        Write-Host ("[local-validation] Stopping cross-worktree host PID {0}: {1}" -f $process.ProcessId, $snippet) -ForegroundColor Cyan
        Stop-Process -Id $process.ProcessId -Force -ErrorAction SilentlyContinue
    }
    if ($processes.Count -gt 0) { Start-Sleep -Seconds 1 }
    return $processes
}

function Report-LocalValidationPortConflictsWithProvenance {
    param(
        [Parameter(Mandatory)][hashtable]$PortLabels,
        [Parameter(Mandatory)][string]$ExpectedRepoRoot
    )

    $blocked = @()
    foreach ($entry in ($PortLabels.GetEnumerator() | Sort-Object { [int]$_.Key })) {
        $row = Resolve-LocalValidationPortRuntimeProvenance -Port ([int]$entry.Key) -ExpectedRepoRoot $ExpectedRepoRoot -AppLabel $entry.Value
        if ($row.RuntimeKind -eq 'free') { continue }
        $pidConflict = if ($row.ProcessId) { [string]$row.ProcessId } else { '?' }
        Write-Host ("[local-validation] FAIL Port {0} in use ({1}) by {2} PID {3}" -f `
            $row.Port, $row.App, $row.RuntimeKind, $pidConflict) -ForegroundColor Red
        if ($row.Worktree) {
            $branchConflict = if ($row.Branch) { $row.Branch } else { '-' }
            $headConflict = if ($row.Head) { $row.Head } else { '-' }
            Write-Host ("         Worktree: {0}" -f $row.Worktree)
            Write-Host ("         Branch:   {0}  SHA: {1}" -f $branchConflict, $headConflict)
        }
        if ($row.CommandLine) { Write-Host "         $($row.CommandLine)" }
        $blocked += $row
    }
    return $blocked
}
