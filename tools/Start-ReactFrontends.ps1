#Requires -Version 5.1
<#
.SYNOPSIS
  Start React POS (:5177) and React Admin (:8095) for localhost + Tailscale/LAN.

.DESCRIPTION
  Local Validation helper (not Production).
  - Frees ports 5177 and 8095 if something is already listening
  - Resolves Tailscale / PublicHost automatically when not passed
  - Starts both Vite apps in separate PowerShell windows, bound for LAN/Tailscale

.EXAMPLE
  .\tools\Start-ReactFrontends.ps1

.EXAMPLE
  .\tools\Start-ReactFrontends.ps1 -PublicHost 100.120.79.81
#>
[CmdletBinding()]
param(
    [string]$PublicHost = '',
    [string]$PosClientPath = '',
    [string]$AdminWebPath = '',
    [int]$PosPort = 5177,
    [int]$AdminPort = 8095
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Step([string]$Message) { Write-Host ("[react-frontends] {0}" -f $Message) -ForegroundColor Cyan }
function Write-Ok([string]$Message) { Write-Host ("[react-frontends] OK  {0}" -f $Message) -ForegroundColor Green }
function Write-Note([string]$Message) { Write-Host ("[react-frontends] NOTE {0}" -f $Message) -ForegroundColor Yellow }

function Get-ListeningOwners([int]$Port) {
    $conns = @(Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue)
    $owners = @()
    foreach ($c in $conns) {
        $procId = [int]$c.OwningProcess
        if ($procId -le 0) { continue }
        $proc = Get-Process -Id $procId -ErrorAction SilentlyContinue
        $name = '?'
        if ($null -ne $proc) { $name = $proc.ProcessName }
        $owners += [pscustomobject]@{
            Port        = $Port
            ProcessId   = $procId
            ProcessName = $name
        }
    }
    return @($owners | Sort-Object ProcessId -Unique)
}

function Stop-PortListeners([int]$Port) {
    $owners = @(Get-ListeningOwners -Port $Port)
    if ($owners.Count -eq 0) {
        Write-Ok ("Port {0} is free" -f $Port)
        return
    }

    foreach ($owner in $owners) {
        Write-Note ("Stopping PID {0} ({1}) on port {2}" -f $owner.ProcessId, $owner.ProcessName, $Port)
        Stop-Process -Id $owner.ProcessId -Force -ErrorAction SilentlyContinue
    }

    $deadline = (Get-Date).AddSeconds(8)
    while ((Get-Date) -lt $deadline) {
        if (@(Get-ListeningOwners -Port $Port).Count -eq 0) {
            Write-Ok ("Port {0} freed" -f $Port)
            return
        }
        Start-Sleep -Milliseconds 400
    }

    throw ("Port {0} is still in use after stop attempts." -f $Port)
}

function Get-TailscalePublicHost {
    try {
        $raw = & tailscale ip -4 2>$null
        if ($LASTEXITCODE -ne 0) { return '' }
        if ([string]::IsNullOrWhiteSpace([string]$raw)) { return '' }
        $addr = ([string]($raw | Select-Object -First 1)).Trim()
        if ($addr -match '^100\.\d{1,3}\.\d{1,3}\.\d{1,3}$') {
            return $addr
        }
    } catch {
        return ''
    }
    return ''
}

function Resolve-PublicHost([string]$Value) {
    $v = ''
    if ($null -ne $Value) { $v = $Value.Trim() }
    if ([string]::IsNullOrWhiteSpace($v)) { return '' }
    if ($v -match '://') {
        throw 'PublicHost must be a host or IP only (no scheme). Example: -PublicHost 100.120.79.81'
    }
    if ($v -match '[:/\\\s]') {
        throw 'PublicHost must be a host or IP only (no port/path). Example: -PublicHost 100.120.79.81'
    }
    return $v
}

function Resolve-DefaultPosClientPath {
    $here = Split-Path -Parent $PSCommandPath
    $candidate = Join-Path $here '..\src\Products\PinoyBusinessPOS\ExItS.PinoyBusinessPOS.Client'
    return [System.IO.Path]::GetFullPath($candidate)
}

function Resolve-DefaultAdminWebPath {
    $desktop = [Environment]::GetFolderPath('Desktop')
    $candidates = @(
        (Join-Path $desktop 'ExItS-SaaS-PlatformWeb-local-access\src\Platform\ExItS.Platform.Admin.Web'),
        (Join-Path $desktop 'ExItS-SaaS-PlatformWeb\src\Platform\ExItS.Platform.Admin.Web')
    )
    foreach ($c in $candidates) {
        if (Test-Path -LiteralPath (Join-Path $c 'package.json')) {
            return [System.IO.Path]::GetFullPath($c)
        }
    }
    return [System.IO.Path]::GetFullPath($candidates[0])
}

function Assert-NpmProject([string]$Path, [string]$Label) {
    if (-not (Test-Path -LiteralPath (Join-Path $Path 'package.json'))) {
        throw ("{0} package.json not found: {1}" -f $Label, $Path)
    }
    if (-not (Test-Path -LiteralPath (Join-Path $Path 'node_modules'))) {
        Write-Note ("{0} node_modules missing - run npm install in that folder first." -f $Label)
    }
}

function Start-ViteWindow {
    param(
        [string]$Title,
        [string]$ScriptPath
    )

    Start-Process -FilePath 'powershell.exe' -ArgumentList @(
        '-NoExit',
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        '-File', $ScriptPath
    ) | Out-Null
    Write-Ok ("Started window: {0}" -f $Title)
}

function New-PosLauncherScript {
    param(
        [string]$OutPath,
        [string]$WorkingDirectory,
        [string]$PublicHostValue,
        [int]$Port
    )

    $lines = @(
        ('$Host.UI.RawUI.WindowTitle = ''ExItS React POS :{0}''' -f $Port),
        ('Set-Location -LiteralPath ''{0}''' -f $WorkingDirectory),
        '$env:POS_DEV_HOST = ''0.0.0.0'''
    )

    if (-not [string]::IsNullOrWhiteSpace($PublicHostValue)) {
        $lines += ('$env:POS_DEV_PUBLIC_HOST = ''{0}''' -f $PublicHostValue)
        $lines += ('Write-Host ''[react-pos] http://127.0.0.1:{0}/''' -f $Port)
        $lines += ('Write-Host ''[react-pos] Tailscale/LAN http://{0}:{1}/''' -f $PublicHostValue, $Port)
    } else {
        $lines += 'Remove-Item Env:POS_DEV_PUBLIC_HOST -ErrorAction SilentlyContinue'
        $lines += ('Write-Host ''[react-pos] http://127.0.0.1:{0}/''' -f $Port)
    }

    $lines += 'npm run dev'
    Set-Content -LiteralPath $OutPath -Value ($lines -join [Environment]::NewLine) -Encoding UTF8
}

function New-AdminLauncherScript {
    param(
        [string]$OutPath,
        [string]$WorkingDirectory,
        [string]$PublicHostValue,
        [int]$Port
    )

    $lines = @(
        ('$Host.UI.RawUI.WindowTitle = ''ExItS React Admin :{0}''' -f $Port),
        ('Set-Location -LiteralPath ''{0}''' -f $WorkingDirectory),
        '$env:VITE_LOCAL_VALIDATION_TOOLS = ''true''',
        # Same-origin /api via Vite proxy so Tailscale :8095 reaches Platform API.
        'Remove-Item Env:VITE_PLATFORM_API_BASE_URL -ErrorAction SilentlyContinue',
        ('Write-Host ''[react-admin] http://127.0.0.1:{0}/''' -f $Port)
    )

    if (-not [string]::IsNullOrWhiteSpace($PublicHostValue)) {
        $lines += ('Write-Host ''[react-admin] Tailscale/LAN http://{0}:{1}/''' -f $PublicHostValue, $Port)
    }

    $lines += 'npm run dev'
    Set-Content -LiteralPath $OutPath -Value ($lines -join [Environment]::NewLine) -Encoding UTF8
}

# --- main ---
$resolvedPublicHost = Resolve-PublicHost -Value $PublicHost
if ([string]::IsNullOrWhiteSpace($resolvedPublicHost)) {
    $resolvedPublicHost = Get-TailscalePublicHost
    if (-not [string]::IsNullOrWhiteSpace($resolvedPublicHost)) {
        Write-Ok ("PublicHost auto-detected Tailscale: {0}" -f $resolvedPublicHost)
    } else {
        Write-Note 'No Tailscale PublicHost detected; Vite still binds for LAN. Localhost works.'
    }
} else {
    Write-Ok ("PublicHost from -PublicHost: {0}" -f $resolvedPublicHost)
}

if ([string]::IsNullOrWhiteSpace($PosClientPath)) {
    $PosClientPath = Resolve-DefaultPosClientPath
}
if ([string]::IsNullOrWhiteSpace($AdminWebPath)) {
    $AdminWebPath = Resolve-DefaultAdminWebPath
}

$PosClientPath = [System.IO.Path]::GetFullPath($PosClientPath)
$AdminWebPath = [System.IO.Path]::GetFullPath($AdminWebPath)

Assert-NpmProject -Path $PosClientPath -Label 'React POS'
Assert-NpmProject -Path $AdminWebPath -Label 'React Admin'

Write-Step ("React POS:    {0}  (port {1})" -f $PosClientPath, $PosPort)
Write-Step ("React Admin:  {0}  (port {1})" -f $AdminWebPath, $AdminPort)

Write-Step ("Freeing ports {0} and {1} if occupied..." -f $PosPort, $AdminPort)
Stop-PortListeners -Port $PosPort
Stop-PortListeners -Port $AdminPort

$tempDir = Join-Path $env:TEMP 'exits-react-frontends'
New-Item -ItemType Directory -Path $tempDir -Force | Out-Null
$posLauncher = Join-Path $tempDir 'start-pos.ps1'
$adminLauncher = Join-Path $tempDir 'start-admin.ps1'

New-PosLauncherScript -OutPath $posLauncher -WorkingDirectory $PosClientPath -PublicHostValue $resolvedPublicHost -Port $PosPort
New-AdminLauncherScript -OutPath $adminLauncher -WorkingDirectory $AdminWebPath -PublicHostValue $resolvedPublicHost -Port $AdminPort

Start-ViteWindow -Title ("ExItS React POS :{0}" -f $PosPort) -ScriptPath $posLauncher
Start-ViteWindow -Title ("ExItS React Admin :{0}" -f $AdminPort) -ScriptPath $adminLauncher

Write-Ok 'Both Vite frontends launching in separate windows.'
Write-Host ("Local POS:     http://127.0.0.1:{0}/" -f $PosPort)
Write-Host ("Local Admin:   http://127.0.0.1:{0}/" -f $AdminPort)
if (-not [string]::IsNullOrWhiteSpace($resolvedPublicHost)) {
    Write-Host ("Tailscale POS:   http://{0}:{1}/" -f $resolvedPublicHost, $PosPort)
    Write-Host ("Tailscale Admin: http://{0}:{1}/" -f $resolvedPublicHost, $AdminPort)
}
Write-Note 'Leave those two windows open while developing.'
