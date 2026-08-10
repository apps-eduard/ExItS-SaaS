[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$PlatformConnection = $env:EXITS_PLATFORM_DATABASE_CONNECTION,
    [string]$PosConnection = $env:EXITS_POS_DATABASE_CONNECTION
)

$ErrorActionPreference = 'Stop'

function Invoke-Psql {
    param([string]$Connection, [string]$Sql)
    & psql --set ON_ERROR_STOP=1 --dbname=$Connection --command $Sql
    if ($LASTEXITCODE -ne 0) {
        throw "psql failed (exit code $LASTEXITCODE). No partial reset was committed."
    }
}

if (-not (Get-Command psql -ErrorAction SilentlyContinue)) {
    throw "PostgreSQL psql is required and was not found on PATH. Install PostgreSQL client tools, then retry."
}

if ([string]::IsNullOrWhiteSpace($PlatformConnection) -or [string]::IsNullOrWhiteSpace($PosConnection)) {
    throw @"
Both database connections are required. Set EXITS_PLATFORM_DATABASE_CONNECTION and EXITS_POS_DATABASE_CONNECTION,
or pass -PlatformConnection and -PosConnection. Development defaults are documented in:
src/Platform/ExItS.Platform.Api/appsettings.Development.json and
src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Api/appsettings.Development.json.
Never provide a production connection string to this destructive development-only script.
"@
}

$adminEmails = "'olivia.mendoza@exits.local','rafael.torres@exits.local'"
$platformCounts = @"
SELECT 'organizations=' || count(*) FROM public.organizations
UNION ALL SELECT 'platform_users=' || count(*) FROM public.platform_users
UNION ALL SELECT 'pos_devices=' || count(*) FROM public.pos_devices
UNION ALL SELECT 'memberships=' || count(*) FROM public.organization_memberships;
"@
$posCounts = @"
SELECT coalesce(string_agg(table_name || '=' || n_live_tup, E'\n' ORDER BY table_name), 'no pos tables')
FROM pg_stat_user_tables WHERE schemaname = 'pos';
"@

Write-Host 'Platform counts before reset:'
Invoke-Psql $PlatformConnection $platformCounts
Write-Host 'POS counts before reset:'
Invoke-Psql $PosConnection $posCounts

if (-not $PSCmdlet.ShouldProcess('the supplied Platform and POS databases', 'Delete disposable customer/organization operational data')) {
    return
}

# Preserve the two local-validation Platform administrators and their Platform role assignments.
# Catalog, commercial plan/feature, business-type, template, composition, and EF migration tables are not targeted.
$platformReset = @"
BEGIN;
DELETE FROM public.organizations;
DELETE FROM public.organization_invitations;
DELETE FROM public.platform_auth_sessions
 WHERE user_id NOT IN (SELECT id FROM public.platform_users WHERE normalized_email IN ($adminEmails));
DELETE FROM public.platform_access_tokens
 WHERE user_id NOT IN (SELECT id FROM public.platform_users WHERE normalized_email IN ($adminEmails));
DELETE FROM public.platform_credential_tokens
 WHERE user_id NOT IN (SELECT id FROM public.platform_users WHERE normalized_email IN ($adminEmails));
DELETE FROM public.platform_external_logins
 WHERE user_id NOT IN (SELECT id FROM public.platform_users WHERE normalized_email IN ($adminEmails));
DELETE FROM public.account_profiles
 WHERE user_identity_id NOT IN (SELECT id FROM public.platform_users WHERE normalized_email IN ($adminEmails));
DELETE FROM public.personal_account_settings
 WHERE user_identity_id NOT IN (SELECT id FROM public.platform_users WHERE normalized_email IN ($adminEmails));
DELETE FROM public.platform_users
 WHERE normalized_email NOT IN ($adminEmails);
COMMIT;
"@

Invoke-Psql $PlatformConnection $platformReset

# The POS product database is independent of Platform. Truncate every operational pos-schema table
# with CASCADE/restarted identities, while deliberately leaving public.__EFMigrationsHistory untouched.
$posReset = @"
DO `$`$
DECLARE tables text;
BEGIN
  SELECT string_agg(format('%I.%I', schemaname, tablename), ', ' ORDER BY tablename)
    INTO tables
    FROM pg_tables
   WHERE schemaname = 'pos';
  IF tables IS NOT NULL THEN
    EXECUTE 'TRUNCATE TABLE ' || tables || ' RESTART IDENTITY CASCADE';
  END IF;
END `$`$;
"@
Invoke-Psql $PosConnection $posReset

Write-Host 'Platform counts after reset:'
Invoke-Psql $PlatformConnection $platformCounts
Write-Host 'POS counts after reset:'
Invoke-Psql $PosConnection $posCounts
Write-Host 'Disposable customer data reset completed. Catalog/commercial data, preserved administrators, roles, and EF migration history remain.'
