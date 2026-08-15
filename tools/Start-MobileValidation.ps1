#Requires -Version 5.1
<#
.SYNOPSIS
  Boot ExItS-M01 + ExItS-M02, build the MAUI Debug APK once, install on both emulators.

.DESCRIPTION
  Dedicated dual-emulator mobile validation helper.
  - Does not destroy HealthCare_* or other AVDs.
  - Maps AVD name → adb serial at runtime (ports are not hard-coded).
  - Builds once with android-x64 (emulator ABI) while keeping Local Validation Tailscale PublicHost.
  - Installs/updates the same APK on both devices.

.PARAMETER SkipBuild
  Skip the MAUI build and reuse the newest matching APK under the Maui project bin folder.

.PARAMETER SkipLaunch
  Install only; do not start the app activity.

.PARAMETER AvdBootSeconds
  Max seconds to wait for each AVD to report boot_completed.

.EXAMPLE
  .\tools\Start-MobileValidation.ps1
#>
[CmdletBinding()]
param(
    [switch]$SkipBuild,
    [switch]$SkipLaunch,
    [int]$AvdBootSeconds = 240
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:AvdNames = @('ExItS-M01', 'ExItS-M02')
$script:PackageId = 'com.exits.pinoybusinesspos'
$script:LaunchActivity = 'crc64e53d2f7fc98556d0.MainActivity'

function Write-Step([string]$Message) { Write-Host "[mobile-validation] $Message" -ForegroundColor Cyan }
function Write-Ok([string]$Message) { Write-Host "[mobile-validation] OK  $Message" -ForegroundColor Green }
function Write-Fail([string]$Message) { Write-Host "[mobile-validation] FAIL $Message" -ForegroundColor Red }
function Write-Note([string]$Message) { Write-Host "[mobile-validation] NOTE $Message" -ForegroundColor Yellow }

function Get-RepoRoot {
    $dir = (Resolve-Path -LiteralPath $PSScriptRoot).Path
    $probe = Get-Item -LiteralPath $dir
    while ($null -ne $probe) {
        if (Test-Path -LiteralPath (Join-Path $probe.FullName 'ExItS.slnx')) {
            return $probe.FullName
        }
        $probe = $probe.Parent
    }
    throw "Could not locate ExItS.slnx above $PSScriptRoot."
}

function Resolve-AndroidSdk {
    $candidates = @(
        $env:ANDROID_HOME,
        $env:ANDROID_SDK_ROOT,
        (Join-Path $env:LOCALAPPDATA 'Android\Sdk')
    ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

    foreach ($sdk in $candidates) {
        $adb = Join-Path $sdk 'platform-tools\adb.exe'
        $emu = Join-Path $sdk 'emulator\emulator.exe'
        if ((Test-Path -LiteralPath $adb) -and (Test-Path -LiteralPath $emu)) {
            return [pscustomobject]@{
                SdkRoot   = $sdk
                Adb       = $adb
                Emulator  = $emu
            }
        }
    }

    throw 'Android SDK not found. Set ANDROID_HOME or install under %LOCALAPPDATA%\Android\Sdk.'
}

function Get-AdbSerials([string]$Adb) {
    & $Adb devices |
        Select-Object -Skip 1 |
        ForEach-Object {
            if ($_ -match '^(emulator-\d+)\s+device\b') {
                $Matches[1]
            }
        }
}

function Get-AvdNameForSerial([string]$Adb, [string]$Serial) {
    $raw = & $Adb -s $Serial emu avd name 2>&1 | Out-String
    foreach ($line in ($raw -split "`r?`n")) {
        $trim = $line.Trim()
        if ([string]::IsNullOrWhiteSpace($trim)) { continue }
        if ($trim -eq 'OK' -or $trim -eq 'KO') { continue }
        if ($trim -match 'error|exception|not\s+found' ) { continue }
        return $trim
    }

    $prop = (& $Adb -s $Serial shell getprop ro.boot.qemu.avd_name 2>$null | Out-String).Trim()
    if (-not [string]::IsNullOrWhiteSpace($prop)) {
        return $prop
    }

    return $null
}

function Get-RunningAvdMap([string]$Adb) {
    $map = @{}
    foreach ($serial in (Get-AdbSerials -Adb $Adb)) {
        $name = Get-AvdNameForSerial -Adb $Adb -Serial $serial
        if (-not [string]::IsNullOrWhiteSpace($name)) {
            $map[$name] = $serial
        }
    }
    return $map
}

function Test-AvdExists([string]$Emulator, [string]$AvdName) {
    $list = & $Emulator -list-avds 2>&1
    return @($list | ForEach-Object { $_.ToString().Trim() }) -contains $AvdName
}

function Start-AvdIfNeeded([string]$Emulator, [string]$Adb, [string]$AvdName) {
    $map = Get-RunningAvdMap -Adb $Adb
    if ($map.ContainsKey($AvdName)) {
        Write-Ok "$AvdName already running as $($map[$AvdName])"
        return $map[$AvdName]
    }

    if (-not (Test-AvdExists -Emulator $Emulator -AvdName $AvdName)) {
        throw "AVD '$AvdName' is not installed. Create it before running this script."
    }

    Write-Step "Starting $AvdName..."
    Start-Process -FilePath $Emulator -ArgumentList @(
        '-avd', $AvdName,
        '-netdelay', 'none',
        '-netspeed', 'full'
    ) -WindowStyle Normal | Out-Null
}

function Wait-ForAvdBoot([string]$Adb, [string]$AvdName, [int]$TimeoutSeconds) {
    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    while ([DateTime]::UtcNow -lt $deadline) {
        $map = Get-RunningAvdMap -Adb $Adb
        if ($map.ContainsKey($AvdName)) {
            $serial = $map[$AvdName]
            $boot = (& $Adb -s $serial shell getprop sys.boot_completed 2>$null | Out-String).Trim()
            if ($boot -eq '1') {
                Write-Ok "$AvdName boot_completed on $serial"
                return $serial
            }
            Write-Step "$AvdName attached as $serial; waiting for boot_completed..."
        }
        else {
            Write-Step "Waiting for $AvdName to appear in adb..."
        }
        Start-Sleep -Seconds 5
    }

    throw "Timed out waiting for $AvdName to finish booting (${TimeoutSeconds}s)."
}

function Find-MauiApk([string]$MauiProjectDir) {
    $bin = Join-Path $MauiProjectDir 'bin'
    if (-not (Test-Path -LiteralPath $bin)) {
        return $null
    }

    $candidates = Get-ChildItem -LiteralPath $bin -Recurse -Filter '*.apk' -File -ErrorAction SilentlyContinue |
        Where-Object {
            $_.Name -like '*pinoybusinesspos*' -or
            $_.Name -like '*Signed.apk' -or
            $_.DirectoryName -match 'net10\.0-android'
        } |
        Sort-Object LastWriteTimeUtc -Descending

    # Prefer x64 / android-x64 artifacts for emulator installs.
    $preferred = $candidates | Where-Object { $_.FullName -match 'android-x64|x86_64|x64' } | Select-Object -First 1
    if ($preferred) { return $preferred.FullName }
    if ($candidates) { return $candidates[0].FullName }
    return $null
}

function Resolve-LaunchComponent([string]$Adb, [string]$Serial, [string]$PackageId) {
    $dump = & $Adb -s $Serial shell cmd package resolve-activity --brief $PackageId 2>$null | Out-String
    foreach ($line in ($dump -split "`r?`n")) {
        $trim = $line.Trim()
        if ($trim -match "^$([regex]::Escape($PackageId))/") {
            return $trim
        }
    }

    # Fallback known MAUI Android activity name pattern for this project.
    return "$PackageId/$script:LaunchActivity"
}

$repoRoot = Get-RepoRoot
$sdk = Resolve-AndroidSdk
$env:ANDROID_HOME = $sdk.SdkRoot
$env:PATH = "$($sdk.SdkRoot)\platform-tools;$($sdk.SdkRoot)\emulator;$env:PATH"
if (-not $env:NUGET_PACKAGES) {
    $env:NUGET_PACKAGES = Join-Path $env:USERPROFILE '.nuget\packages'
}

Write-Step "Repo: $repoRoot"
Write-Step "Android SDK: $($sdk.SdkRoot)"

foreach ($avd in $script:AvdNames) {
    [void](Start-AvdIfNeeded -Emulator $sdk.Emulator -Adb $sdk.Adb -AvdName $avd)
}

$serialByAvd = @{}
foreach ($avd in $script:AvdNames) {
    $serialByAvd[$avd] = Wait-ForAvdBoot -Adb $sdk.Adb -AvdName $avd -TimeoutSeconds $AvdBootSeconds
}

$mauiProj = Join-Path $repoRoot 'src\Products\PinoyBusinessPOS\ExItS.PinoyBusinessPOS.Maui\ExItS.PinoyBusinessPOS.Maui.csproj'
$mauiDir = Split-Path -Parent $mauiProj
$apkPath = $null
$buildOk = $false

if ($SkipBuild) {
    $apkPath = Find-MauiApk -MauiProjectDir $mauiDir
    if (-not $apkPath) {
        throw 'SkipBuild specified but no APK was found under the Maui bin folder.'
    }
    Write-Note "Reusing APK: $apkPath"
    $buildOk = $true
}
else {
    Write-Step 'Building MAUI Android Debug (android-x64) once...'
    Write-Note 'PosLocalValidationTarget=PhysicalDevice keeps Tailscale PublicHost (100.120.79.81).'
    $buildArgs = @(
        'build', $mauiProj,
        '-c', 'Debug',
        '-f', 'net10.0-android',
        "-p:AndroidSdkDirectory=$($sdk.SdkRoot)",
        '-p:RuntimeIdentifier=android-x64',
        '-p:PosLocalValidationTarget=PhysicalDevice',
        '-p:EmbedAssembliesIntoApk=true'
    )
    & dotnet @buildArgs
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed with exit code $LASTEXITCODE."
    }
    $apkPath = Find-MauiApk -MauiProjectDir $mauiDir
    if (-not $apkPath) {
        throw 'Build succeeded but no APK was found.'
    }
    $buildOk = $true
    Write-Ok "APK: $apkPath"
}

$installStatus = @{}
$launchStatus = @{}
foreach ($avd in $script:AvdNames) {
    $serial = $serialByAvd[$avd]
    Write-Step "Installing on $avd ($serial)..."
    & $sdk.Adb -s $serial install -r -t --no-incremental $apkPath
    if ($LASTEXITCODE -ne 0) {
        $installStatus[$avd] = 'Failed'
        Write-Fail "Install failed on $avd"
        continue
    }
    $installStatus[$avd] = 'Installed'
    Write-Ok "Installed on $avd"

    if ($SkipLaunch) {
        $launchStatus[$avd] = 'Skipped'
        continue
    }

    $component = Resolve-LaunchComponent -Adb $sdk.Adb -Serial $serial -PackageId $script:PackageId
    Write-Step "Launching $component on $avd..."
    & $sdk.Adb -s $serial shell am start -n $component | Out-Null
    if ($LASTEXITCODE -eq 0) {
        $launchStatus[$avd] = 'Launched'
        Write-Ok "Launched on $avd"
    }
    else {
        # Fallback: monkey launcher by package
        & $sdk.Adb -s $serial shell monkey -p $script:PackageId -c android.intent.category.LAUNCHER 1 | Out-Null
        $launchStatus[$avd] = if ($LASTEXITCODE -eq 0) { 'Launched' } else { 'InstallOnly' }
    }
}

Write-Host ''
Write-Host 'ExItS Mobile Validation' -ForegroundColor White
Write-Host ''
foreach ($avd in $script:AvdNames) {
    Write-Host $avd
    Write-Host "Serial: $($serialByAvd[$avd])"
    Write-Host 'Status: Ready'
    Write-Host "App: $($installStatus[$avd])"
    if ($launchStatus.ContainsKey($avd)) {
        Write-Host "Launch: $($launchStatus[$avd])"
    }
    Write-Host ''
}

Write-Host "Build: $(if ($buildOk) { 'OK' } else { 'Failed' })"
Write-Host "APK: $apkPath"
Write-Host 'Network/API host: http://100.120.79.81:8091 (PosApi) / :8092 (PosBusinessApi) via PhysicalDevice Local Validation profile'
Write-Ok 'Dual-emulator mobile validation ready.'
