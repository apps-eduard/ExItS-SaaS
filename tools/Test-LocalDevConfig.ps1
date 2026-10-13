#Requires -Version 5.1
# Asserts fast-dev ports, shared database identity, and that the launcher does not rebuild images.
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LocalValidation.stack.ps1')
. (Join-Path $PSScriptRoot 'LocalDev.stack.ps1')

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
    Write-Host "PASS $Message"
}

Test-LocalDevPortsDoNotOverlapValidation
Assert-True ($true) 'Fast-dev app ports do not overlap validation app or database ports'

$start = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'Start-LocalDev.ps1') -Raw
$stop = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'Stop-LocalDev.ps1') -Raw
Assert-True ($start -match '--no-build') 'Start-LocalDev does not build images'
Assert-True ($start -notmatch 'down -v') 'Start-LocalDev does not delete volumes'
Assert-True ($start -match 'dotnet watch') 'Start-LocalDev uses dotnet watch'
Assert-True ($start -match 'ASPNETCORE_ENVIRONMENT = ''Development''') 'Start-LocalDev uses Development'
Assert-True ($start -match 'Host=127\.0\.0\.1') 'Start-LocalDev connects through the published database port'
Assert-True ($start -notmatch 'AllowedOrigins__\d+''\] = ''\*''') 'Start-LocalDev does not set wildcard CORS'
Assert-True ($start -notmatch 'IgnoreAntiforgery|DisableAntiforgery|Antiforgery__Enabled.*=.*false') 'Start-LocalDev does not disable CSRF'
Assert-True ($start -match 'PlatformDbVolume') 'Start-LocalDev uses the existing platform volume identity'
Assert-True ($start -match 'PosDbVolume') 'Start-LocalDev uses the existing POS volume identity'
Assert-True ($stop -notmatch 'down -v') 'Stop-LocalDev does not delete volumes'
Assert-True ($stop -match 'left running') 'Stop-LocalDev leaves PostgreSQL running'
Assert-True ($start -match 'OnlyServices') 'Start-LocalDev can restart one local service'
Assert-True ($stop -match 'OnlyServices') 'Stop-LocalDev can stop one local service'
$control = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'Invoke-LocalValidationControl.ps1') -Raw
Assert-True ($control -match 'local-dev') 'Login restart can target the local profile'
Assert-True ($control -match 'Start-LocalDev.ps1') 'Local profile restart uses Start-LocalDev'

$compose = Get-Content -LiteralPath (Join-Path $PSScriptRoot '..\deploy\docker\compose.local-validation.yaml') -Raw
Assert-True ($compose -match 'exits_local_validation_platform_db_data') 'Compose keeps the platform database volume'
Assert-True ($compose -match 'exits_local_validation_pos_db_data') 'Compose keeps the POS database volume'
Assert-True ($compose -match '15533') 'Compose still publishes platform DB on 15533'
Assert-True ($compose -match '15534') 'Compose still publishes POS DB on 15534'

Write-Host 'Local dev config checks passed.'
