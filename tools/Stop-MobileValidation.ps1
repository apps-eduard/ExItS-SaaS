#Requires -Version 5.1
<#
.SYNOPSIS
  Stop only the dedicated ExItS dual-emulator validation AVDs (ExItS-M01 / ExItS-M02).

.DESCRIPTION
  Maps adb serials to AVD names at runtime and kills only ExItS-M01 and ExItS-M02.
  Leaves physical devices and unrelated emulators running.

.EXAMPLE
  .\tools\Stop-MobileValidation.ps1
#>
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:AvdNames = @('ExItS-M01', 'ExItS-M02')

function Write-Step([string]$Message) { Write-Host "[mobile-validation] $Message" -ForegroundColor Cyan }
function Write-Ok([string]$Message) { Write-Host "[mobile-validation] OK  $Message" -ForegroundColor Green }
function Write-Note([string]$Message) { Write-Host "[mobile-validation] NOTE $Message" -ForegroundColor Yellow }

function Resolve-AndroidSdk {
    $candidates = @(
        $env:ANDROID_HOME,
        $env:ANDROID_SDK_ROOT,
        (Join-Path $env:LOCALAPPDATA 'Android\Sdk')
    ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

    foreach ($sdk in $candidates) {
        $adb = Join-Path $sdk 'platform-tools\adb.exe'
        if (Test-Path -LiteralPath $adb) {
            return [pscustomobject]@{ SdkRoot = $sdk; Adb = $adb }
        }
    }

    throw 'Android SDK not found. Set ANDROID_HOME or install under %LOCALAPPDATA%\Android\Sdk.'
}

function Get-AdbSerials([string]$Adb) {
    & $Adb devices |
        Select-Object -Skip 1 |
        ForEach-Object {
            if ($_ -match '^(emulator-\d+)\s+(device|offline)\b') {
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
        if ($trim -match 'error|exception|not\s+found') { continue }
        return $trim
    }

    $prop = (& $Adb -s $Serial shell getprop ro.boot.qemu.avd_name 2>$null | Out-String).Trim()
    if (-not [string]::IsNullOrWhiteSpace($prop)) {
        return $prop
    }

    return $null
}

$sdk = Resolve-AndroidSdk
$env:ANDROID_HOME = $sdk.SdkRoot
$env:PATH = "$($sdk.SdkRoot)\platform-tools;$env:PATH"

$stopped = @()
foreach ($serial in (Get-AdbSerials -Adb $sdk.Adb)) {
    $name = Get-AvdNameForSerial -Adb $sdk.Adb -Serial $serial
    if ($script:AvdNames -contains $name) {
        Write-Step "Stopping $name ($serial)..."
        & $sdk.Adb -s $serial emu kill 2>$null | Out-Null
        $stopped += "$name ($serial)"
    }
    else {
        Write-Note "Leaving $serial alone (AVD='$name')"
    }
}

if ($stopped.Count -eq 0) {
    Write-Note 'No ExItS-M01 / ExItS-M02 emulators were running.'
}
else {
    Write-Ok ("Stopped: " + ($stopped -join ', '))
}

Start-Sleep -Seconds 2
Write-Host ''
Write-Host 'Remaining adb devices:' -ForegroundColor White
& $sdk.Adb devices -l
