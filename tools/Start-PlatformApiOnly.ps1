#Requires -Version 5.1
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$envFile = Join-Path $repoRoot 'deploy\docker\.env.local-validation'
$map = @{}
Get-Content -LiteralPath $envFile | ForEach-Object {
    $line = $_.Trim()
    if ($line.Length -eq 0 -or $line.StartsWith('#')) { return }
    $idx = $line.IndexOf('=')
    if ($idx -lt 1) { return }
    $map[$line.Substring(0, $idx).Trim()] = $line.Substring($idx + 1).Trim()
}

$platformCs = "Host=127.0.0.1;Port=15533;Database=exits_platform;Username=$($map['LOCAL_VALIDATION_PLATFORM_DB_USER']);Password=$($map['LOCAL_VALIDATION_PLATFORM_DB_PASSWORD'])"
$mailpitSmtpPort = if ($map['LOCAL_VALIDATION_MAILPIT_SMTP_HOST_PORT']) { [int]$map['LOCAL_VALIDATION_MAILPIT_SMTP_HOST_PORT'] } else { 1025 }
$mailpitUiPort = if ($map['LOCAL_VALIDATION_MAILPIT_UI_HOST_PORT']) { [int]$map['LOCAL_VALIDATION_MAILPIT_UI_HOST_PORT'] } else { 8025 }
$publicHost = if ($map['LOCAL_VALIDATION_PUBLIC_HOST']) { [string]$map['LOCAL_VALIDATION_PUBLIC_HOST'].Trim() } else { '' }
# React Admin Vite :8095 (never Blazor :8090). Prefer Tailscale PublicHost for Mailpit links off-box.
$adminOrigin = if ($env:EXITS_ADMIN_PUBLIC_BASE_URL) {
    [string]$env:EXITS_ADMIN_PUBLIC_BASE_URL
} elseif ($publicHost) {
    "http://${publicHost}:8095"
} elseif ($map['LOCAL_VALIDATION_REACT_ADMIN_ORIGIN']) {
    [string]$map['LOCAL_VALIDATION_REACT_ADMIN_ORIGIN']
} else {
    'http://127.0.0.1:8095'
}

$env:ASPNETCORE_ENVIRONMENT = 'Staging'
# Bind all interfaces so Tailscale clients can reach Platform API (same as LocalValidation launch profile).
$env:ASPNETCORE_URLS = 'http://0.0.0.0:8091'
$env:ConnectionStrings__PlatformDatabase = $platformCs
$env:LocalValidation__Enabled = 'true'
$env:Security__EnforceHttps = 'false'
$allowedHosts = 'localhost;127.0.0.1;10.0.2.2'
if ($publicHost) { $allowedHosts = "$allowedHosts;$publicHost" }
$env:AllowedHosts = $allowedHosts
$env:LocalValidation__SharedPassword = $map['LOCAL_VALIDATION_SHARED_PASSWORD']
$cors = @(
    'http://127.0.0.1:5177',
    'http://localhost:5177',
    'http://127.0.0.1:8095',
    'http://localhost:8095'
)
if ($publicHost) {
    $cors += "http://${publicHost}:5177"
    $cors += "http://${publicHost}:8095"
}
for ($i = 0; $i -lt $cors.Count; $i++) {
    Set-Item -Path "env:Cors__AllowedOrigins__$i" -Value $cors[$i]
}
# Mailpit catcher (SMTP stays loopback; UI is reachable via Tailscale PublicHost:8025).
$env:PlatformEmail__SmtpHost = '127.0.0.1'
$env:PlatformEmail__SmtpPort = "$mailpitSmtpPort"
$env:PlatformEmail__UseSsl = 'false'
$env:PlatformEmail__FromAddress = 'noreply@exits.local'
$env:PlatformEmail__FromDisplayName = 'ExItS Local Validation'
$env:PlatformEmail__AdminPublicBaseUrl = $adminOrigin
$env:PlatformAuthentication__Password__MinimumLength = '1'
$env:PlatformAuthentication__Password__RequireUppercase = 'false'
$env:PlatformAuthentication__Password__RequireLowercase = 'false'
$env:PlatformAuthentication__Password__RequireDigit = 'false'
$env:PlatformAuthentication__Password__RequireNonAlphanumeric = 'false'

Write-Host "Platform API:     http://127.0.0.1:8091 (bound 0.0.0.0)"
Write-Host "Activation links: $adminOrigin"
if ($publicHost) {
    Write-Host "Mailpit (Tailscale): http://${publicHost}:${mailpitUiPort}/"
} else {
    Write-Host "Mailpit (local):     http://127.0.0.1:${mailpitUiPort}/"
}

Push-Location (Join-Path $repoRoot 'src\Platform\ExItS.Platform.Api')
try {
    dotnet build -c Debug --no-restore 2>$null | Out-Null
    dotnet run --no-build --launch-profile LocalValidation
}
finally {
    Pop-Location
}
