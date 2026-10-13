#Requires -Version 5.1
# Fast local development ports. These must stay off the Docker/local-validation app ports.
# Dot-source after LocalValidation.stack.ps1. Not Production.

$script:LocalDevStack = [pscustomobject]@{
    # Existing Development launchSettings ports. They do not overlap 8091/8092.
    PlatformApiPort = 5288
    PosApiPort      = 5290
    # 5177 is Docker react-pos and Start-LocalValidation Vite. 5176 is Loan Manager.
    ReactPosPort    = 5178
    # 8095 is Docker admin-web and the validation Admin Vite default.
    AdminPort       = 5195
    LoanPort        = 5176
    StateDirectory  = 'ExItS\LocalDev'
    StateFileName   = 'launcher-state.json'
    DataProtectionDirectory = 'ExItS\LocalDev\DataProtectionKeys'
}

function Get-LocalDevReservedValidationPorts {
    return @(
        [int]$LocalValidationStack.DefaultPlatformApiPort,
        [int]$LocalValidationStack.DefaultPosApiPort,
        [int]$LocalValidationStack.DefaultAdminPort,
        [int]$LocalValidationStack.DefaultReactPosPort,
        [int]$LocalValidationStack.DefaultPlatformDbPort,
        [int]$LocalValidationStack.DefaultPosDbPort
    )
}

function Get-LocalDevAppPorts {
    param([switch]$IncludeLoanManager)

    $ports = @(
        [int]$LocalDevStack.PlatformApiPort,
        [int]$LocalDevStack.PosApiPort,
        [int]$LocalDevStack.ReactPosPort,
        [int]$LocalDevStack.AdminPort
    )
    if ($IncludeLoanManager) {
        $ports += [int]$LocalDevStack.LoanPort
    }
    return $ports
}

function Get-LocalDevServicePort {
    param([Parameter(Mandatory)][string]$ServiceKey)

    switch ($ServiceKey.Trim().ToLowerInvariant()) {
        'platform-api' { return [int]$LocalDevStack.PlatformApiPort }
        'pos-api' { return [int]$LocalDevStack.PosApiPort }
        'react-pos' { return [int]$LocalDevStack.ReactPosPort }
        'platform-admin' { return [int]$LocalDevStack.AdminPort }
        'mailpit' { return 8025 }
        default { throw "Unknown fast-dev service '$ServiceKey'." }
    }
}

function Test-LocalDevPortsDoNotOverlapValidation {
    $reserved = @(Get-LocalDevReservedValidationPorts)
    $devPorts = @(Get-LocalDevAppPorts -IncludeLoanManager)
    foreach ($port in $devPorts) {
        if ($reserved -contains $port) {
            throw "Fast-dev port $port overlaps a local-validation/Docker port."
        }
    }
}
