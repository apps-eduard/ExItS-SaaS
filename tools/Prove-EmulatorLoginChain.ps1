#Requires -Version 5.1
<#
.SYNOPSIS
  Proves desktop (127.0.0.1:5177) and emulator (10.0.2.2:5177) login HTTP chains.
  Never prints password values or cookie values.
#>
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Probe([string]$Label, [string]$Message) {
    Write-Host ("[{0}] {1}" -f $Label, $Message)
}

function Parse-SetCookieMetadata([string[]]$Headers) {
    foreach ($raw in $Headers) {
        if ([string]::IsNullOrWhiteSpace($raw)) { continue }
        $name = ($raw -split '=', 2)[0]
        $secure = if ($raw -match '(?i);\s*secure') { 'YES' } else { 'NO' }
        $sameSite = if ($raw -match '(?i)samesite=([^;]+)') { $matches[1] } else { 'unknown' }
        $path = if ($raw -match '(?i)path=([^;]+)') { $matches[1] } else { '/' }
        [pscustomobject]@{
            Name     = $name
            Secure   = $secure
            SameSite = $sameSite
            Path     = $path
        }
    }
}

function Invoke-LoginChain {
    param(
        [string]$Label,
        [string]$BrowserHost,
        [string]$Username,
        [string]$Password
    )

    $session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    $loginBody = @{ usernameOrEmail = $Username; password = $Password } | ConvertTo-Json
    $loginUrl = 'http://127.0.0.1:5177/platform-api/api/v1/platform/auth/login'

    Write-Probe $Label ("POST /api/v1/platform/auth/login username={0} passwordLength={1}" -f $Username, $Password.Length)
    $login = Invoke-WebRequest -Uri $loginUrl -Method POST -ContentType 'application/json' -Body $loginBody `
        -Headers @{ Host = $BrowserHost } -WebSession $session -UseBasicParsing
    Write-Probe $Label ("loginStatus={0}" -f [int]$login.StatusCode)

    $cookieHeaders = @($login.Headers['Set-Cookie'])
    if ($cookieHeaders.Count -eq 0) {
        throw "$Label login returned no Set-Cookie headers."
    }
    foreach ($meta in Parse-SetCookieMetadata $cookieHeaders) {
        Write-Probe $Label ("Set-Cookie name={0} Secure={1} SameSite={2} Path={3}" -f $meta.Name, $meta.Secure, $meta.SameSite, $meta.Path)
    }

    $stored = @($session.Cookies.GetCookies('http://127.0.0.1/'))
    Write-Probe $Label ("cookieJarCount={0}" -f $stored.Count)
    foreach ($c in $stored) {
        Write-Probe $Label ("cookieStore name={0} secure={1} path={2}" -f $c.Name, $c.Secure, $c.Path)
    }

    $meUrl = 'http://127.0.0.1:5177/platform-api/api/v1/platform/auth/me'
    $me = Invoke-WebRequest -Uri $meUrl -Method GET -Headers @{ Host = $BrowserHost } -WebSession $session -UseBasicParsing
    Write-Probe $Label ("GET /api/v1/platform/auth/me status={0}" -f [int]$me.StatusCode)

    $orgsUrl = 'http://127.0.0.1:5177/platform-api/api/v1/platform/auth/organizations'
    try {
        $orgs = Invoke-WebRequest -Uri $orgsUrl -Method GET -Headers @{ Host = $BrowserHost } -WebSession $session -UseBasicParsing
        Write-Probe $Label ("GET /api/v1/platform/auth/organizations status={0}" -f [int]$orgs.StatusCode)
    }
    catch {
        $code = $_.Exception.Response.StatusCode.value__
        Write-Probe $Label ("GET /api/v1/platform/auth/organizations status={0}" -f $code)
    }

    [pscustomobject]@{
        Label           = $Label
        LoginStatus     = [int]$login.StatusCode
        MeStatus        = [int]$me.StatusCode
        CookieJarCount  = $stored.Count
        SessionSecure   = ($cookieHeaders -join '; ' -match '(?i);\s*secure')
    }
}

$username = 'kizy@gmail.com'
$password = '1'

Write-Host '======== EMULATOR-LOGIN-FIX01 HTTP CHAIN ========' -ForegroundColor Cyan
$direct = Invoke-WebRequest -Uri 'http://127.0.0.1:8091/api/v1/platform/auth/login' -Method POST `
    -ContentType 'application/json' -Body (@{ usernameOrEmail = $username; password = $password } | ConvertTo-Json) `
    -UseBasicParsing
Write-Probe 'direct8091' ("POST login status={0}" -f [int]$direct.StatusCode)
foreach ($meta in Parse-SetCookieMetadata @($direct.Headers['Set-Cookie'])) {
    Write-Probe 'direct8091' ("Set-Cookie name={0} Secure={1} SameSite={2} Path={3}" -f $meta.Name, $meta.Secure, $meta.SameSite, $meta.Path)
}

$desktop = Invoke-LoginChain -Label 'desktop5177' -BrowserHost '127.0.0.1:5177' -Username $username -Password $password
$emulator = Invoke-LoginChain -Label 'emulator5177' -BrowserHost '10.0.2.2:5177' -Username $username -Password $password

Write-Host ''
Write-Host 'SUMMARY' -ForegroundColor Green
Write-Host ("DESKTOP_LOGIN_POST_STATUS={0}" -f $desktop.LoginStatus)
Write-Host ("EMULATOR_LOGIN_POST_STATUS={0}" -f $emulator.LoginStatus)
Write-Host ("DESKTOP_AUTH_ME={0}" -f $desktop.MeStatus)
Write-Host ("EMULATOR_AUTH_ME={0}" -f $emulator.MeStatus)
Write-Host ("EMULATOR_SESSION_COOKIE_STORED={0}" -f $(if ($emulator.CookieJarCount -gt 0) { 'YES' } else { 'NO' }))
Write-Host ("DESKTOP_PASSWORD_1_LOGIN={0}" -f $(if ($desktop.LoginStatus -eq 200 -and $desktop.MeStatus -eq 200) { 'PASS' } else { 'FAIL' }))
Write-Host ("EMULATOR_PASSWORD_1_LOGIN={0}" -f $(if ($emulator.LoginStatus -eq 200 -and $emulator.MeStatus -eq 200) { 'PASS' } else { 'FAIL' }))
