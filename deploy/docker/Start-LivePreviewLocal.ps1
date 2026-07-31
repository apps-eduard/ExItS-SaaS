#Requires -Version 5.1
<#
.SYNOPSIS
  Starts live-preview Platform API, POS API, and Admin on the host against Docker DBs only.

.DESCRIPTION
  - Ensures exits-live-preview platform-db + pos-db are up (does not start app containers).
  - Loads deploy/docker/.env.live-preview (gitignored).
  - Launches three `dotnet run --launch-profile LivePreview` processes in new windows.
  - Does not delete or reset database volumes.
  - Not Production. Does not start P14-WP03.
#>
[CmdletBinding()]
param(
    [switch]$SkipDbUp,
    [switch]$NoNewWindows
)

$ErrorActionPreference = 'Stop'
$dockerDir = $PSScriptRoot
$repoRoot = (Resolve-Path (Join-Path $dockerDir '..\..')).Path
$envFile = Join-Path $dockerDir '.env.live-preview'
$composeFile = Join-Path $dockerDir 'compose.live-preview.yaml'

if (-not (Test-Path $envFile)) {
    throw "Missing $envFile — copy .env.live-preview.example and fill REPLACE_* values."
}

function Import-DotEnv {
    param([string]$Path)
    $map = @{}
    Get-Content -LiteralPath $Path | ForEach-Object {
        $line = $_.Trim()
        if ($line.Length -eq 0 -or $line.StartsWith('#')) { return }
        $idx = $line.IndexOf('=')
        if ($idx -lt 1) { return }
        $key = $line.Substring(0, $idx).Trim()
        $value = $line.Substring($idx + 1).Trim()
        if (($value.StartsWith('"') -and $value.EndsWith('"')) -or ($value.StartsWith("'") -and $value.EndsWith("'"))) {
            $value = $value.Substring(1, $value.Length - 2)
        }
        $map[$key] = $value
    }
    return $map
}

function Require-EnvKey {
    param($Map, [string]$Key)
    if (-not $Map.ContainsKey($Key) -or [string]::IsNullOrWhiteSpace($Map[$Key]) -or $Map[$Key].StartsWith('REPLACE_')) {
        throw "Set a real value for $Key in .env.live-preview (not REPLACE_*)."
    }
}

$envMap = Import-DotEnv -Path $envFile
Require-EnvKey $envMap 'LIVE_PREVIEW_PLATFORM_DB_USER'
Require-EnvKey $envMap 'LIVE_PREVIEW_PLATFORM_DB_PASSWORD'
Require-EnvKey $envMap 'LIVE_PREVIEW_POS_DB_USER'
Require-EnvKey $envMap 'LIVE_PREVIEW_POS_DB_PASSWORD'
Require-EnvKey $envMap 'LIVE_PREVIEW_SHARED_PASSWORD'

$platformDbPort = if ($envMap['LIVE_PREVIEW_PLATFORM_DB_HOST_PORT']) { $envMap['LIVE_PREVIEW_PLATFORM_DB_HOST_PORT'] } else { '15533' }
$posDbPort = if ($envMap['LIVE_PREVIEW_POS_DB_HOST_PORT']) { $envMap['LIVE_PREVIEW_POS_DB_HOST_PORT'] } else { '15534' }
$adminPort = if ($envMap['LIVE_PREVIEW_ADMIN_HOST_PORT']) { $envMap['LIVE_PREVIEW_ADMIN_HOST_PORT'] } else { '8090' }
$platformApiPort = if ($envMap['LIVE_PREVIEW_PLATFORM_API_HOST_PORT']) { $envMap['LIVE_PREVIEW_PLATFORM_API_HOST_PORT'] } else { '8091' }
$posApiPort = if ($envMap['LIVE_PREVIEW_POS_API_HOST_PORT']) { $envMap['LIVE_PREVIEW_POS_API_HOST_PORT'] } else { '8092' }
$adminOrigin = if ($envMap['LIVE_PREVIEW_ADMIN_ORIGIN']) { $envMap['LIVE_PREVIEW_ADMIN_ORIGIN'] } else { "http://localhost:$adminPort" }

if (-not $SkipDbUp) {
    Write-Host 'Starting live-preview databases only (preserving volumes)...'
    & docker compose -f $composeFile --env-file $envFile up -d
    if ($LASTEXITCODE -ne 0) { throw "docker compose up failed ($LASTEXITCODE)." }
}

$platformCs = "Host=127.0.0.1;Port=$platformDbPort;Database=exits_platform;Username=$($envMap['LIVE_PREVIEW_PLATFORM_DB_USER']);Password=$($envMap['LIVE_PREVIEW_PLATFORM_DB_PASSWORD'])"
$posCs = "Host=127.0.0.1;Port=$posDbPort;Database=exits_pos;Username=$($envMap['LIVE_PREVIEW_POS_DB_USER']);Password=$($envMap['LIVE_PREVIEW_POS_DB_PASSWORD'])"
$platformApiUrl = "http://localhost:$platformApiPort"
$dpKeys = Join-Path $env:TEMP 'exits-admin-dp-keys-live-preview'

$apps = @(
    @{
        Name = 'Platform API'
        Project = Join-Path $repoRoot 'src\Platform\ExItS.Platform.Api\ExItS.Platform.Api.csproj'
        Env = @{
            ASPNETCORE_ENVIRONMENT = 'Staging'
            ASPNETCORE_URLS = $platformApiUrl
            ConnectionStrings__PlatformDatabase = $platformCs
            AllowedHosts = 'localhost;127.0.0.1'
            Cors__AllowedOrigins__0 = $adminOrigin
            Security__EnforceHttps = 'false'
            LivePreview__Enabled = 'true'
            LivePreview__SharedPassword = $envMap['LIVE_PREVIEW_SHARED_PASSWORD']
        }
    },
    @{
        Name = 'POS API'
        Project = Join-Path $repoRoot 'src\Products\PinoyBusinessPOS\ExItS.PinoyBusinessPOS.Api\ExItS.PinoyBusinessPOS.Api.csproj'
        Env = @{
            ASPNETCORE_ENVIRONMENT = 'Staging'
            ASPNETCORE_URLS = "http://localhost:$posApiPort"
            ConnectionStrings__PosDatabase = $posCs
            AllowedHosts = 'localhost;127.0.0.1'
            Cors__AllowedOrigins__0 = $adminOrigin
            Security__EnforceHttps = 'false'
            LivePreview__Enabled = 'true'
            LivePreview__PlatformApiBaseUrl = $platformApiUrl
            PlatformAuth__BaseUrl = $platformApiUrl
        }
    },
    @{
        Name = 'Platform Admin'
        Project = Join-Path $repoRoot 'src\Platform\ExItS.Platform.Admin\ExItS.Platform.Admin.csproj'
        Env = @{
            ASPNETCORE_ENVIRONMENT = 'Staging'
            ASPNETCORE_URLS = "http://localhost:$adminPort"
            AllowedHosts = 'localhost;127.0.0.1'
            PlatformApi__BaseUrl = $platformApiUrl
            PlatformApi__TimeoutSeconds = '30'
            LivePreview__Enabled = 'true'
            DataProtection__KeysPath = $dpKeys
        }
    }
)

function ConvertTo-EnvPrefix {
    param($EnvMap)
    ($EnvMap.GetEnumerator() | ForEach-Object {
        $escaped = $_.Value -replace "'", "''"
        "`$env:$($_.Key) = '$escaped'; "
    }) -join ''
}

foreach ($app in $apps) {
    $prefix = ConvertTo-EnvPrefix -EnvMap $app.Env
    $run = "$prefix Set-Location '$repoRoot'; Write-Host '=== $($app.Name) (live-preview local) ==='; dotnet run --project '$($app.Project)' --launch-profile LivePreview"

    if ($NoNewWindows) {
        Write-Host "Starting $($app.Name) in background job..."
        Start-Job -Name ("exits-lp-" + ($app.Name -replace '\s', '')) -ScriptBlock {
            param($Command)
            Invoke-Expression $Command
        } -ArgumentList $run | Out-Null
    }
    else {
        Start-Process -FilePath 'powershell.exe' -ArgumentList @(
            '-NoExit',
            '-NoProfile',
            '-ExecutionPolicy', 'Bypass',
            '-Command', $run
        ) | Out-Null
        Write-Host "Opened window: $($app.Name)"
    }
}

Write-Host ''
Write-Host 'Live-preview local processes starting.'
Write-Host "  Admin:        http://localhost:$adminPort/"
Write-Host "  Platform API: $platformApiUrl"
Write-Host "  POS API:      http://localhost:$posApiPort"
Write-Host 'Volumes preserved. Stop apps in their windows; stop DBs with Stop-LivePreviewLocal.ps1 (no -v).'
