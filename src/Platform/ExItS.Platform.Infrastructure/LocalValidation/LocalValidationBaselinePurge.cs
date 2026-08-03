using ExItS.Platform.Application.LocalValidation;
using ExItS.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExItS.Platform.Infrastructure.LocalValidation;

/// <summary>
/// FK-safe truncate of Platform transactional tables for the Local Validation onboarding baseline.
/// Retains migration history, product/plan/feature/trial catalog, and platform_role_definitions.
/// </summary>
public sealed class LocalValidationBaselinePurge(
    PlatformDbContext db,
    ILogger<LocalValidationBaselinePurge> logger) : ILocalValidationBaselinePurge
{
    public async Task PurgeTransactionalDataAsync(CancellationToken cancellationToken = default)
    {
        logger.LogWarning(
            "Purging Local Validation transactional data for PlatformAdministratorsOnly onboarding baseline.");

        await using var tx = await db.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        // TRUNCATE ... CASCADE removes dependents that FK into these tables.
        // Catalog tables (products, plans, features, trials, platform_role_definitions) are omitted.
        await db.Database.ExecuteSqlRawAsync(
                """
                TRUNCATE TABLE
                  platform.personal_notification_deliveries,
                  platform.personal_in_app_notifications,
                  platform.personal_reminders,
                  platform.personal_utang_entries,
                  platform.personal_utang_invitations,
                  platform.personal_utang_migration_items,
                  platform.personal_utang_migration_batches,
                  platform.personal_debt_relationships,
                  platform.personal_contacts,
                  platform.personal_account_settings,
                  platform.business_credit_opening_balances,
                  platform.linked_customer_app_users,
                  platform.customer_link_requests,
                  platform.credit_customers,
                  platform.business_customers,
                  platform.provider_payments,
                  platform.saas_payments,
                  platform.entitlement_snapshot_grants,
                  platform.entitlement_snapshots,
                  platform.feature_overrides,
                  platform.subscriptions,
                  platform.product_local_role_grants,
                  platform.product_access_assignments,
                  platform.organization_custom_role_assignments,
                  platform.organization_role_definitions,
                  platform.platform_custom_role_assignments,
                  platform.organization_invitations,
                  platform.organization_memberships,
                  platform.organization_context_preferences,
                  platform.platform_auth_sessions,
                  platform.platform_access_tokens,
                  platform.platform_credential_tokens,
                  platform.platform_external_logins,
                  platform.account_profiles,
                  platform.platform_user_credentials,
                  platform.platform_role_assignments,
                  platform.organizations,
                  platform.platform_users,
                  platform.audit_records
                RESTART IDENTITY CASCADE;
                """,
                cancellationToken)
            .ConfigureAwait(false);

        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);

        logger.LogWarning("Local Validation transactional purge completed.");
    }
}
