#Requires -Version 5.1
# Shared identity for LEGACY MAUI/Blazor Local Validation (LEGACY-MAUI-ISO-01).
# Dot-source from Start/Stop-MauiLegacyLocalValidation.ps1 only.
# Never confuse with tools/LocalValidation.stack.ps1 (React / classic 809x stack).

$script:MauiLocalValidationStack = [pscustomobject]@{
    ComposeProjectName   = 'exits-maui-local-validation'
    ComposeFileName      = 'compose.maui-local-validation.yaml'
    EnvFileName          = '.env.maui-local-validation'
    EnvExampleFileName   = '.env.maui-local-validation.example'
    PlatformDbContainer  = 'exits-maui-local-validation-platform-db'
    PosDbContainer       = 'exits-maui-local-validation-pos-db'
    MailpitContainer     = 'exits-maui-local-validation-mailpit'
    PlatformApiContainer = 'exits-maui-local-validation-platform-api'
    PosApiContainer      = 'exits-maui-local-validation-pos-api'
    AdminWebContainer    = 'exits-maui-local-validation-admin-web'
    OrgWebContainer      = 'exits-maui-local-validation-org-web'
    PersonalWebContainer = 'exits-maui-local-validation-personal-web'
    PlatformDbVolume     = 'exits_maui_local_validation_platform_db_data'
    PosDbVolume          = 'exits_maui_local_validation_pos_db_data'
    PlatformDbName       = 'exits_maui_platform'
    PosDbName            = 'exits_maui_pos'
    NetworkName          = 'exits-maui-local-validation'
    # Host ports — disjoint from React (8091/8092/8095/5177/15533/15534)
    DefaultAdminPort         = 8190
    DefaultPlatformApiPort   = 8191
    DefaultPosApiPort        = 8192
    DefaultOrgWebPort        = 8193
    DefaultPersonalWebPort   = 8194
    DefaultPlatformDbPort    = 16533
    DefaultPosDbPort         = 16534
    DefaultMailpitUiPort     = 8125
    DefaultMailpitSmtpPort   = 1125
    # React ports that this stack must never stop or bind
    ForbiddenReactPorts      = @(8091, 8092, 8095, 5177, 15533, 15534)
}

function Get-MauiLocalValidationRepoRoot {
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

function Import-MauiLocalValidationDotEnv {
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

function Require-MauiLocalValidationEnvKey {
    param(
        [Parameter(Mandatory)]$Map,
        [Parameter(Mandatory)][string]$Key
    )

    if (-not $Map.ContainsKey($Key) -or
        [string]::IsNullOrWhiteSpace([string]$Map[$Key]) -or
        ([string]$Map[$Key]).StartsWith('REPLACE_')) {
        throw "Set a real value for $Key in deploy/docker/.env.maui-local-validation (not REPLACE_*)."
    }
}

function Test-MauiLocalValidationDockerAvailable {
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
        throw 'Docker CLI not found. Install/start Docker Desktop.'
    }
    & docker info 1>$null 2>$null
    if ($LASTEXITCODE -ne 0) {
        throw 'Docker Desktop is not available (docker info failed). Start Docker Desktop and retry.'
    }
}

function Invoke-MauiLocalValidationDocker {
    param(
        [Parameter(Mandatory)]
        [string[]]$DockerArgs
    )
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

function Get-MauiLocalValidationComposeArgs {
    param(
        [Parameter(Mandatory)][string]$RepoRoot,
        [Parameter(Mandatory)][string]$EnvFile
    )

    $composeFile = Join-Path $RepoRoot "deploy\docker\$($MauiLocalValidationStack.ComposeFileName)"
    if (-not (Test-Path -LiteralPath $composeFile)) {
        throw "Missing compose file: $composeFile"
    }
    return @(
        'compose',
        '-p', $MauiLocalValidationStack.ComposeProjectName,
        '-f', $composeFile,
        '--env-file', $EnvFile
    )
}
