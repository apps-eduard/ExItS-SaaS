[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$PlatformConnection = $env:EXITS_PLATFORM_DATABASE_CONNECTION,
    [string]$PosConnection = $env:EXITS_POS_DATABASE_CONNECTION,
    # When host psql is unavailable, run against Local Validation Docker DB containers.
    [switch]$UseLocalValidationDocker
)

$ErrorActionPreference = 'Stop'

function Test-DisposableDevelopmentConnection {
    param(
        [Parameter(Mandatory)]
        [string]$Connection,
        [Parameter(Mandatory)]
        [ValidateSet('Platform', 'Pos')]
        [string]$Target
    )

    $lower = $Connection.ToLowerInvariant()
    if ($lower -match 'production' -or $lower -match '(^|[;\s=])prod([;\s=]|$)') {
        throw "Refusing connection that looks like Production ($Target). This script is Development-only."
    }

    $dbMatch = [regex]::Match($Connection, '(?i)(?:Database|Initial Catalog)\s*=\s*([^;]+)')
    $hostMatch = [regex]::Match($Connection, '(?i)(?:Host|Server)\s*=\s*([^;]+)')
    $database = if ($dbMatch.Success) { $dbMatch.Groups[1].Value.Trim() } else { '' }
    $dbHost = if ($hostMatch.Success) { $hostMatch.Groups[1].Value.Trim() } else { '' }

    $allowedDb =
        $database -match '(?i)^exits_platform$' -or
        $database -match '(?i)^exits_pos$' -or
        $database -eq 'ExItS_Platform' -or
        $database -eq 'ExItS_Pos' -or
        $database -match '(?i)(local.?validation|_test|_dev|dev_only)'

    $loopbackHost = $dbHost -match '^(127\.0\.0\.1|localhost|::1)$'
    $hasDevPasswordMarker = $lower -match 'exits_platform_dev_only|exits_pos_dev_only'

    if (-not $allowedDb -and -not ($loopbackHost -and $hasDevPasswordMarker)) {
        throw @"
Refusing $Target connection - database '$database' on host '$dbHost' is not an approved disposable Development target.
Allowed patterns: Local Validation (exits_platform / exits_pos), Development (ExItS_Platform / ExItS_Pos), or names containing local-validation/test/dev on loopback with the documented development password marker.
Never provide a production connection string to this destructive development-only script.
"@
    }
}

function Invoke-Psql {
    param(
        [string]$Connection,
        [string]$Sql,
        [ValidateSet('Platform', 'Pos')]
        [string]$Target
    )

    if ($UseLocalValidationDocker) {
        $container = if ($Target -eq 'Platform') {
            'exits-local-validation-platform-db'
        }
        else {
            'exits-local-validation-pos-db'
        }
        $database = if ($Target -eq 'Platform') { 'exits_platform' } else { 'exits_pos' }
        $tmpHost = Join-Path ([System.IO.Path]::GetTempPath()) ("exits-reset-" + [guid]::NewGuid().ToString('N') + '.sql')
        try {
            [System.IO.File]::WriteAllText($tmpHost, $Sql)
            docker cp $tmpHost "${container}:/tmp/exits-reset.sql" | Out-Null
            docker exec $container sh -c "psql -U `"`$POSTGRES_USER`" -d $database -v ON_ERROR_STOP=1 -f /tmp/exits-reset.sql"
            if ($LASTEXITCODE -ne 0) {
                throw "docker exec psql failed (exit code $LASTEXITCODE). No partial reset was committed for this statement batch."
            }
        }
        finally {
            Remove-Item -LiteralPath $tmpHost -ErrorAction SilentlyContinue
        }

        return
    }

    if (-not (Get-Command psql -ErrorAction SilentlyContinue)) {
        throw "PostgreSQL psql is required and was not found on PATH. Install PostgreSQL client tools, or re-run with -UseLocalValidationDocker against the Local Validation containers."
    }

    Test-DisposableDevelopmentConnection -Connection $Connection -Target $Target

    & psql --set ON_ERROR_STOP=1 --dbname=$Connection --command $Sql
    if ($LASTEXITCODE -ne 0) {
        throw "psql failed (exit code $LASTEXITCODE). No partial reset was committed."
    }
}

if (-not $UseLocalValidationDocker) {
    if ([string]::IsNullOrWhiteSpace($PlatformConnection) -or [string]::IsNullOrWhiteSpace($PosConnection)) {
        throw @"
Both database connections are required. Set EXITS_PLATFORM_DATABASE_CONNECTION and EXITS_POS_DATABASE_CONNECTION,
or pass -PlatformConnection and -PosConnection, or use -UseLocalValidationDocker for the Local Validation containers.
Development defaults are documented in:
src/Platform/ExItS.Platform.Api/appsettings.Development.json and
src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Api/appsettings.Development.json.
Never provide a production connection string to this destructive development-only script.
"@
    }

    Test-DisposableDevelopmentConnection -Connection $PlatformConnection -Target Platform
    Test-DisposableDevelopmentConnection -Connection $PosConnection -Target Pos
}
else {
    $platformRunning = docker inspect -f '{{.State.Running}}' exits-local-validation-platform-db 2>$null
    $posRunning = docker inspect -f '{{.State.Running}}' exits-local-validation-pos-db 2>$null
    if ($platformRunning -ne 'true' -or $posRunning -ne 'true') {
        throw 'Local Validation DB containers are not running. Start them with tools/Start-LocalValidation.ps1, then retry with -UseLocalValidationDocker.'
    }
}

$adminEmails = "'olivia.mendoza@exits.local','rafael.torres@exits.local'"
# Platform EF maps to schema "platform" (not public). Catalog lives in schema "catalog".
$platformCounts = @"
SELECT 'organizations=' || count(*)::text FROM platform.organizations
UNION ALL SELECT 'platform_users=' || count(*)::text FROM platform.platform_users
UNION ALL SELECT 'pos_devices=' || count(*)::text FROM platform.pos_devices
UNION ALL SELECT 'memberships=' || count(*)::text FROM platform.organization_memberships
UNION ALL SELECT 'branches=' || count(*)::text FROM platform.organization_branches
UNION ALL SELECT 'global_products=' || count(*)::text FROM catalog.global_products
UNION ALL SELECT 'global_categories=' || count(*)::text FROM catalog.global_categories
UNION ALL SELECT 'template_products=' || count(*)::text FROM catalog.catalog_template_products
UNION ALL SELECT 'catalog_templates=' || count(*)::text FROM catalog.catalog_templates
UNION ALL SELECT 'business_types=' || count(*)::text FROM catalog.business_types;
"@
$posCounts = @"
SELECT coalesce(string_agg(relname || '=' || n_live_tup::text, E'\n' ORDER BY relname), 'no pos tables')
FROM pg_stat_user_tables WHERE schemaname = 'pos';
"@

Write-Host 'Platform counts before reset:'
Invoke-Psql -Connection $PlatformConnection -Sql $platformCounts -Target Platform
Write-Host 'POS counts before reset:'
Invoke-Psql -Connection $PosConnection -Sql $posCounts -Target Pos

if (-not $PSCmdlet.ShouldProcess('the supplied Platform and POS databases', 'Delete disposable Development customer/catalog/operational data')) {
    return
}

# Preserve:
# - the two local-validation Platform administrators and their Platform role assignments
# - commercial products/plans/features, Business Type definitions, Catalog Template definitions
# - EF migration history, privacy/system reference configuration
# Clear disposable Global Catalog merchandise (products, categories, mappings, template compositions,
# import jobs) so corrected barcode rules can re-seed cleanly. Template definition rows remain.
$platformReset = @"
BEGIN;

-- Disposable Global Catalog merchandise (keep business_types + catalog_templates definitions).
DELETE FROM catalog.catalog_import_items;
DELETE FROM catalog.catalog_import_jobs;
DELETE FROM catalog.catalog_template_products;
DELETE FROM catalog.global_product_business_types;
DELETE FROM catalog.global_products;
DELETE FROM catalog.global_category_business_types;
DELETE FROM catalog.global_categories;

-- Org / commercial operational data (keep products/plans/features/privacy catalog rows).
DELETE FROM platform.pos_devices;
DELETE FROM platform.organization_branches;
DELETE FROM platform.organization_invitations;
DELETE FROM platform.organization_custom_role_assignments;
DELETE FROM platform.organization_context_preferences;
DELETE FROM platform.product_access_assignments;
DELETE FROM platform.product_local_role_grants;
DELETE FROM platform.organization_memberships;
DELETE FROM platform.feature_overrides;
DELETE FROM platform.entitlement_snapshot_grants;
DELETE FROM platform.entitlement_snapshots;
DELETE FROM platform.saas_payments;
DELETE FROM platform.provider_payments;
DELETE FROM platform.subscriptions;
DELETE FROM platform.business_credit_opening_balances;
DELETE FROM platform.linked_customer_app_users;
DELETE FROM platform.customer_link_requests;
DELETE FROM platform.credit_customers;
DELETE FROM platform.business_customers;
DELETE FROM platform.organizations;

-- Personal / non-admin identity data.
DELETE FROM platform.personal_notification_deliveries;
DELETE FROM platform.personal_in_app_notifications;
DELETE FROM platform.personal_reminders;
DELETE FROM platform.personal_utang_entries;
DELETE FROM platform.personal_utang_invitations;
DELETE FROM platform.personal_utang_migration_items;
DELETE FROM platform.personal_utang_migration_batches;
DELETE FROM platform.personal_debt_relationships;
DELETE FROM platform.personal_contacts;
DELETE FROM platform.personal_account_settings;
DELETE FROM platform.platform_custom_role_assignments;

DELETE FROM platform.platform_auth_sessions
 WHERE user_id NOT IN (SELECT id FROM platform.platform_users WHERE normalized_email IN ($adminEmails));
DELETE FROM platform.platform_access_tokens
 WHERE user_id NOT IN (SELECT id FROM platform.platform_users WHERE normalized_email IN ($adminEmails));
DELETE FROM platform.platform_credential_tokens
 WHERE user_id NOT IN (SELECT id FROM platform.platform_users WHERE normalized_email IN ($adminEmails));
DELETE FROM platform.platform_external_logins
 WHERE user_id NOT IN (SELECT id FROM platform.platform_users WHERE normalized_email IN ($adminEmails));
DELETE FROM platform.account_profiles
 WHERE user_identity_id NOT IN (SELECT id FROM platform.platform_users WHERE normalized_email IN ($adminEmails));
DELETE FROM platform.platform_user_credentials
 WHERE user_id NOT IN (SELECT id FROM platform.platform_users WHERE normalized_email IN ($adminEmails));
DELETE FROM platform.platform_role_assignments
 WHERE platform_user_id NOT IN (SELECT id FROM platform.platform_users WHERE normalized_email IN ($adminEmails));
DELETE FROM platform.audit_records;
DELETE FROM platform.platform_users
 WHERE normalized_email NOT IN ($adminEmails);

COMMIT;
"@

Invoke-Psql -Connection $PlatformConnection -Sql $platformReset -Target Platform

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
Invoke-Psql -Connection $PosConnection -Sql $posReset -Target Pos

Write-Host 'Platform counts after reset:'
Invoke-Psql -Connection $PlatformConnection -Sql $platformCounts -Target Platform
Write-Host 'POS counts after reset:'
Invoke-Psql -Connection $PosConnection -Sql $posCounts -Target Pos
Write-Host 'Disposable Development reset completed. Preserved: Platform admins, Business Types, Catalog Template definitions, commercial plans/features, EF migrations. Cleared: Global Catalog products/categories/mappings/template compositions/import jobs, disposable orgs/users, and all POS operational tables.'
