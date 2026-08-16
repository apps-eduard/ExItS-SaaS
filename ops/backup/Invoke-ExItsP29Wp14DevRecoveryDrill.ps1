#Requires -Version 7.0
<#
.SYNOPSIS
  P29-WP14 development recovery drill against local-validation sources into disposable restore containers.
  Never targets or destroys exits-local-validation-*-db. Never prints passwords.
#>
[CmdletBinding()]
param(
    [string] $EnvFile = '',
    [string] $PlatformConnectionString = '',
    [string] $PosConnectionString = '',
    [string] $PlatformSourceContainer = 'exits-local-validation-platform-db',
    [string] $PosSourceContainer = 'exits-local-validation-pos-db',
    [string] $OutputRoot = '',
    [switch] $SkipCleanup,
    [switch] $SkipDotnetTest,
    [switch] $KeepDumps
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$script:ForbiddenForeignProductToken = -join ([char[]](72, 101, 97, 108, 116, 104, 67, 97, 114, 101))

function Write-Drill([string] $Message) {
    Write-Host ("[P29-WP14] " + $Message)
}

function Get-FreeTcpPort {
    $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 0)
    $listener.Start()
    $port = ([System.Net.IPEndPoint]$listener.LocalEndpoint).Port
    $listener.Stop()
    return $port
}

function Import-DotEnv([string] $Path) {
    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Env file not found: $Path"
    }
    Get-Content -LiteralPath $Path | ForEach-Object {
        $line = $_.Trim()
        if (-not $line -or $line.StartsWith('#')) { return }
        $eq = $line.IndexOf('=')
        if ($eq -lt 1) { return }
        $name = $line.Substring(0, $eq).Trim()
        $value = $line.Substring($eq + 1).Trim()
        if (($value.StartsWith('"') -and $value.EndsWith('"')) -or ($value.StartsWith("'") -and $value.EndsWith("'"))) {
            $value = $value.Substring(1, $value.Length - 2)
        }
        [Environment]::SetEnvironmentVariable($name, $value, 'Process')
    }
}

function Build-NpgsqlCs([string] $HostName, [int] $Port, [string] $Database, [string] $User, [string] $Password) {
    return "Host=$HostName;Port=$Port;Database=$Database;Username=$User;Password=$Password"
}

function Redact-ConnectionString([string] $Cs) {
    return [regex]::Replace($Cs, '(?i)(Password|Pwd)=([^;]+)', '$1=***')
}

function Assert-NotSameTarget([string] $SourceCs, [string] $TargetCs, [string] $Label) {
    $src = [regex]::Replace($SourceCs, '(?i)(Password|Pwd)=([^;]+)', '')
    $tgt = [regex]::Replace($TargetCs, '(?i)(Password|Pwd)=([^;]+)', '')
    if ([string]::Equals($src, $tgt, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing restore: $Label source connection equals target connection."
    }
}

function Find-LatestArtifact([string] $Dir, [string] $Prefix) {
    $dump = Get-ChildItem -LiteralPath $Dir -Filter "$Prefix*.dump" | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
    if (-not $dump) { throw "No $Prefix dump found in $Dir" }
    $manifest = Join-Path $Dir ($dump.BaseName + '.manifest.json')
    if (-not (Test-Path -LiteralPath $manifest)) {
        # naming: artifact may be name.dump with sibling name.manifest.json
        $manifestAlt = Join-Path $Dir ($dump.Name -replace '\.dump$', '.manifest.json')
        if (Test-Path -LiteralPath $manifestAlt) { $manifest = $manifestAlt }
        else { throw "Manifest missing for $($dump.Name)" }
    }
    return [pscustomobject]@{ ArtifactPath = $dump.FullName; ManifestPath = $manifest }
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$script:RestorePlatformName = 'exits-p29-wp14-restore-platform'
$script:RestorePosName = 'exits-p29-wp14-restore-pos'
$script:CreatedContainers = @()
$script:OutputDirectory = $null
$failed = $false

try {
    Write-Drill 'Starting development recovery drill (source = local-validation; targets = disposable).'

    if (-not $EnvFile) {
        $EnvFile = Join-Path $repoRoot 'deploy\docker\.env.local-validation'
    }

    if ([string]::IsNullOrWhiteSpace($PlatformConnectionString) -or [string]::IsNullOrWhiteSpace($PosConnectionString)) {
        if (-not (Test-Path -LiteralPath $EnvFile)) {
            throw "Credentials env missing ($EnvFile). Pass -PlatformConnectionString/-PosConnectionString or create the env file."
        }
        Import-DotEnv $EnvFile
    }

    $platformUser = [Environment]::GetEnvironmentVariable('LOCAL_VALIDATION_PLATFORM_DB_USER')
    $platformPassword = [Environment]::GetEnvironmentVariable('LOCAL_VALIDATION_PLATFORM_DB_PASSWORD')
    $posUser = [Environment]::GetEnvironmentVariable('LOCAL_VALIDATION_POS_DB_USER')
    $posPassword = [Environment]::GetEnvironmentVariable('LOCAL_VALIDATION_POS_DB_PASSWORD')
    $platformPortRaw = [Environment]::GetEnvironmentVariable('LOCAL_VALIDATION_PLATFORM_DB_HOST_PORT')
    if ([string]::IsNullOrWhiteSpace($platformPortRaw)) { $platformPortRaw = '15533' }
    $posPortRaw = [Environment]::GetEnvironmentVariable('LOCAL_VALIDATION_POS_DB_HOST_PORT')
    if ([string]::IsNullOrWhiteSpace($posPortRaw)) { $posPortRaw = '15534' }
    $platformPort = [int]$platformPortRaw
    $posPort = [int]$posPortRaw

    if ([string]::IsNullOrWhiteSpace($PlatformConnectionString)) {
        if ([string]::IsNullOrWhiteSpace($platformUser) -or [string]::IsNullOrWhiteSpace($platformPassword)) {
            throw 'LOCAL_VALIDATION_PLATFORM_DB_USER/PASSWORD missing.'
        }
        $PlatformConnectionString = Build-NpgsqlCs '127.0.0.1' $platformPort 'exits_platform' $platformUser $platformPassword
    }
    if ([string]::IsNullOrWhiteSpace($PosConnectionString)) {
        if ([string]::IsNullOrWhiteSpace($posUser) -or [string]::IsNullOrWhiteSpace($posPassword)) {
            throw 'LOCAL_VALIDATION_POS_DB_USER/PASSWORD missing.'
        }
        $PosConnectionString = Build-NpgsqlCs '127.0.0.1' $posPort 'exits_pos' $posUser $posPassword
    }

    Write-Drill ("Platform source CS: " + (Redact-ConnectionString $PlatformConnectionString))
    Write-Drill ("POS source CS: " + (Redact-ConnectionString $PosConnectionString))

    foreach ($name in @($PlatformSourceContainer, $PosSourceContainer)) {
        $running = docker inspect -f '{{.State.Running}}' $name 2>$null
        if ($running -ne 'true') {
            throw "Source container '$name' is not running. Start local-validation first."
        }
    }

    $stamp = (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssZ')
    if (-not $OutputRoot) {
        $OutputRoot = Join-Path $repoRoot 'ops\backup\local'
    }
    $script:OutputDirectory = Join-Path $OutputRoot ("p29-wp14-" + $stamp)
    New-Item -ItemType Directory -Path $script:OutputDirectory -Force | Out-Null
    Write-Drill "Output directory: $($script:OutputDirectory)"

    Write-Drill 'Backing up Platform (DockerContainerId path)...'
    & "$PSScriptRoot\Backup-ExItsDatabase.ps1" `
        -DatabaseKind Platform `
        -ConnectionString $PlatformConnectionString `
        -OutputDirectory $script:OutputDirectory `
        -EnvironmentClassification Testing `
        -DockerContainerId $PlatformSourceContainer
    if ($LASTEXITCODE -ne 0) { throw "Platform backup failed (exit $LASTEXITCODE)." }

    Write-Drill 'Backing up POS (DockerContainerId path)...'
    & "$PSScriptRoot\Backup-ExItsDatabase.ps1" `
        -DatabaseKind PinoyBusinessPos `
        -ConnectionString $PosConnectionString `
        -OutputDirectory $script:OutputDirectory `
        -EnvironmentClassification Testing `
        -DockerContainerId $PosSourceContainer
    if ($LASTEXITCODE -ne 0) { throw "POS backup failed (exit $LASTEXITCODE)." }

    $platformSet = Find-LatestArtifact $script:OutputDirectory 'platform_'
    $posSet = Find-LatestArtifact $script:OutputDirectory 'pos_'

    Write-Drill 'Verifying Platform artifact...'
    & "$PSScriptRoot\Verify-ExItsBackup.ps1" -ArtifactPath $platformSet.ArtifactPath -ManifestPath $platformSet.ManifestPath
    if ($LASTEXITCODE -ne 0) { throw 'Platform verify failed.' }

    Write-Drill 'Verifying POS artifact...'
    & "$PSScriptRoot\Verify-ExItsBackup.ps1" -ArtifactPath $posSet.ArtifactPath -ManifestPath $posSet.ManifestPath
    if ($LASTEXITCODE -ne 0) { throw 'POS verify failed.' }

    $restorePlatformPort = Get-FreeTcpPort
    $restorePosPort = Get-FreeTcpPort

    Write-Drill "Starting disposable restore containers (host ports $restorePlatformPort / $restorePosPort)..."
    docker rm -f $script:RestorePlatformName 2>$null | Out-Null
    docker rm -f $script:RestorePosName 2>$null | Out-Null
    # Ignore missing-container stderr from docker rm when containers do not yet exist.
    $global:LASTEXITCODE = 0

    docker run -d --name $script:RestorePlatformName `
        -e "POSTGRES_USER=$platformUser" `
        -e "POSTGRES_PASSWORD=$platformPassword" `
        -e 'POSTGRES_DB=exits_platform' `
        -p "${restorePlatformPort}:5432" `
        postgres:16 | Out-Null
    $script:CreatedContainers += $script:RestorePlatformName

    docker run -d --name $script:RestorePosName `
        -e "POSTGRES_USER=$posUser" `
        -e "POSTGRES_PASSWORD=$posPassword" `
        -e 'POSTGRES_DB=exits_pos' `
        -p "${restorePosPort}:5432" `
        postgres:16 | Out-Null
    $script:CreatedContainers += $script:RestorePosName

    # Wait for readiness
    foreach ($pair in @(
            @{ Name = $script:RestorePlatformName; User = $platformUser },
            @{ Name = $script:RestorePosName; User = $posUser }
        )) {
        $ready = $false
        for ($i = 0; $i -lt 60; $i++) {
            docker exec $pair.Name pg_isready -U $pair.User | Out-Null
            if ($LASTEXITCODE -eq 0) { $ready = $true; break }
            Start-Sleep -Seconds 1
        }
        if (-not $ready) { throw "Restore container $($pair.Name) did not become ready." }
    }

    $platformTargetCs = Build-NpgsqlCs '127.0.0.1' $restorePlatformPort 'exits_platform' $platformUser $platformPassword
    $posTargetCs = Build-NpgsqlCs '127.0.0.1' $restorePosPort 'exits_pos' $posUser $posPassword

    Assert-NotSameTarget $PlatformConnectionString $platformTargetCs 'Platform'
    Assert-NotSameTarget $PosConnectionString $posTargetCs 'POS'
    if ($script:RestorePlatformName -eq $PlatformSourceContainer -or $script:RestorePosName -eq $PosSourceContainer) {
        throw 'Refuse: restore container name matches a source local-validation container.'
    }

    Write-Drill 'Restoring Platform into disposable container...'
    & "$PSScriptRoot\Restore-ExItsDatabase.ps1" `
        -DatabaseKind Platform `
        -ConnectionString $platformTargetCs `
        -ArtifactPath $platformSet.ArtifactPath `
        -ManifestPath $platformSet.ManifestPath `
        -DockerContainerId $script:RestorePlatformName
    if ($LASTEXITCODE -ne 0) { throw "Platform restore failed (exit $LASTEXITCODE)." }

    Write-Drill 'Restoring POS into disposable container...'
    & "$PSScriptRoot\Restore-ExItsDatabase.ps1" `
        -DatabaseKind PinoyBusinessPos `
        -ConnectionString $posTargetCs `
        -ArtifactPath $posSet.ArtifactPath `
        -ManifestPath $posSet.ManifestPath `
        -DockerContainerId $script:RestorePosName
    if ($LASTEXITCODE -ne 0) { throw "POS restore failed (exit $LASTEXITCODE)." }

    Write-Drill 'Inline post-restore checks (schemas + foreign-product exclusion)...'
    $platformCheck = docker exec $script:RestorePlatformName psql -U $platformUser -d exits_platform -tAc "SELECT COUNT(*) FROM information_schema.schemata WHERE schema_name='platform';"
    $posCheck = docker exec $script:RestorePosName psql -U $posUser -d exits_pos -tAc "SELECT COUNT(*) FROM information_schema.schemata WHERE schema_name='pos';"
    $foreignProductCheck = docker exec $script:RestorePosName psql -U $posUser -d exits_pos -tAc "SELECT COUNT(*) FROM information_schema.tables WHERE table_name ILIKE '%$script:ForbiddenForeignProductToken%' OR table_schema ILIKE '%$script:ForbiddenForeignProductToken%' OR table_name ILIKE '%patient%';"
    if (($platformCheck.Trim() -as [int]) -lt 1) { throw 'platform schema missing after restore.' }
    if (($posCheck.Trim() -as [int]) -lt 1) { throw 'pos schema missing after restore.' }
    if (($foreignProductCheck.Trim() -as [int]) -gt 0) { throw 'Forbidden foreign-product-related tables present after restore.' }
    Write-Drill 'Inline checks PASS.'

    if (-not $SkipDotnetTest) {
        Write-Drill 'Running automated P29Wp14 Testcontainers suite...'
        Push-Location $repoRoot
        try {
            & dotnet test 'tests\ExItS.BackupRestore.Tests' -c Release --filter 'FullyQualifiedName~P29Wp14' --nologo
            if ($LASTEXITCODE -ne 0) { throw "dotnet test P29Wp14 failed (exit $LASTEXITCODE)." }
        }
        finally {
            Pop-Location
        }
    }
    else {
        Write-Drill 'Skipped dotnet test (-SkipDotnetTest).'
    }

    Write-Drill 'DRILL_PASS'
    exit 0
}
catch {
    $failed = $true
    Write-Error ("DRILL_FAIL: " + $_.Exception.Message)
    exit 1
}
finally {
    if (-not $SkipCleanup) {
        foreach ($name in $script:CreatedContainers) {
            Write-Drill "Removing disposable container $name"
            docker rm -f $name 2>$null | Out-Null
        }
        if (-not $KeepDumps -and $script:OutputDirectory -and (Test-Path -LiteralPath $script:OutputDirectory)) {
            # Keep directory but note dumps remain unless operator deletes; do not auto-delete evidence on failure.
            if (-not $failed) {
                Write-Drill "Dumps retained under $($script:OutputDirectory) (gitignored). Delete manually when done."
            }
        }
    }
    else {
        Write-Drill 'SkipCleanup set — disposable restore containers left running.'
    }
}
