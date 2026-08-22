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

$env:ASPNETCORE_ENVIRONMENT = 'Staging'
$env:ASPNETCORE_URLS = 'http://127.0.0.1:8091'
$env:ConnectionStrings__PlatformDatabase = $platformCs
$env:LocalValidation__Enabled = 'true'
$env:Security__EnforceHttps = 'false'
$env:AllowedHosts = 'localhost;127.0.0.1;10.0.2.2'
$env:LocalValidation__SharedPassword = $map['LOCAL_VALIDATION_SHARED_PASSWORD']
$env:Cors__AllowedOrigins__0 = 'http://127.0.0.1:5177'
$env:Cors__AllowedOrigins__1 = 'http://localhost:5177'

Push-Location (Join-Path $repoRoot 'src\Platform\ExItS.Platform.Api')
try {
    dotnet build -c Debug --no-restore 2>$null | Out-Null
    dotnet run --no-build --launch-profile LocalValidation
}
finally {
    Pop-Location
}
