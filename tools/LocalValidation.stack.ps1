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
    PlatformDbVolume       = 'exits_local_validation_platform_db_data'
    PosDbVolume            = 'exits_local_validation_pos_db_data'
    PlatformDbName         = 'exits_platform'
    PosDbName              = 'exits_pos'
    DefaultPlatformDbPort  = 15533
    DefaultPosDbPort       = 15534
    DefaultAdminPort       = 8090
    DefaultPlatformApiPort = 8091
    DefaultPosApiPort      = 8092
    DefaultSeedScope       = 'PlatformAdministratorsOnly'
}

function Invoke-LocalValidationDocker {
    param(
        [Parameter(Mandatory)]
        [string[]]$DockerArgs
    )
    # Docker progress/status is written to stderr; under $ErrorActionPreference Stop that can abort scripts.
    $prev = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        & docker @DockerArgs
        return [int]$LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $prev
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
