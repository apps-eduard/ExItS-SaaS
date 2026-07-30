using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence;

/// <summary>Maps database unique constraint violations to application error codes.</summary>
public static class PersistenceExceptionMapper
{
    public static bool TryMapUniqueViolation(DbUpdateException exception, out string errorCode, out string message)
    {
        errorCode = ApplicationErrorCodes.DomainViolation;
        message = "A persistence conflict occurred.";

        var inner = exception.InnerException;
        if (inner is null)
        {
            return false;
        }

        var detail = inner.Message;
        if (!detail.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
            && !detail.Contains("unique constraint", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Check payment-, subscription-, and organization-specific constraints first: their detail
        // text can also contain "product_code" / generic substrings that would otherwise be caught
        // by the broader catalog checks below.
        if (detail.Contains("ux_saas_payments_reference", StringComparison.OrdinalIgnoreCase)
            || detail.Contains("saas_payments", StringComparison.OrdinalIgnoreCase))
        {
            errorCode = ApplicationErrorCodes.PaymentReferenceConflict;
            message = "A payment with this reference already exists for this organization and method.";
            return true;
        }

        if (detail.Contains("ux_subscriptions_one_active_like", StringComparison.OrdinalIgnoreCase)
            || detail.Contains("subscriptions", StringComparison.OrdinalIgnoreCase))
        {
            errorCode = ApplicationErrorCodes.ActiveSubscriptionConflict;
            message = "An active-like subscription already exists for this organization and product.";
            return true;
        }

        if (detail.Contains("ux_entitlement_snapshots_org_product_version", StringComparison.OrdinalIgnoreCase)
            || detail.Contains("entitlement_snapshots", StringComparison.OrdinalIgnoreCase))
        {
            errorCode = ApplicationErrorCodes.SnapshotVersionConflict;
            message = "An entitlement snapshot with this version already exists for this organization and product.";
            return true;
        }

        if (detail.Contains("entitlement_snapshot_grants", StringComparison.OrdinalIgnoreCase))
        {
            errorCode = ApplicationErrorCodes.EntitlementSnapshotInvalid;
            message = "Duplicate feature code within an entitlement snapshot.";
            return true;
        }

        if (detail.Contains("organizations", StringComparison.OrdinalIgnoreCase)
            || detail.Contains("(slug)", StringComparison.OrdinalIgnoreCase))
        {
            errorCode = ApplicationErrorCodes.SlugConflict;
            message = "A Platform Organization with this slug already exists.";
            return true;
        }

        if (detail.Contains("products", StringComparison.OrdinalIgnoreCase)
            || detail.Contains("product_code", StringComparison.OrdinalIgnoreCase)
            || detail.Contains("(code)", StringComparison.OrdinalIgnoreCase))
        {
            errorCode = ApplicationErrorCodes.DuplicateProductCode;
            message = "A product with this ProductCode already exists.";
            return true;
        }

        if (detail.Contains("plans", StringComparison.OrdinalIgnoreCase)
            && detail.Contains("product_code", StringComparison.OrdinalIgnoreCase))
        {
            errorCode = ApplicationErrorCodes.DuplicatePlanCode;
            message = "A plan with this code already exists for the product.";
            return true;
        }

        if (detail.Contains("feature_definitions", StringComparison.OrdinalIgnoreCase)
            || detail.Contains("feature_code", StringComparison.OrdinalIgnoreCase))
        {
            errorCode = ApplicationErrorCodes.DuplicateFeatureCode;
            message = "A feature with this code already exists for the product.";
            return true;
        }

        if (detail.Contains("plan_versions", StringComparison.OrdinalIgnoreCase))
        {
            errorCode = DomainErrorCodes.InvalidPlanVersionNumber;
            message = "A plan version with this number already exists.";
            return true;
        }

        if (detail.Contains("ux_platform_role_assignments", StringComparison.OrdinalIgnoreCase)
            || detail.Contains("platform_role_assignments", StringComparison.OrdinalIgnoreCase))
        {
            errorCode = ApplicationErrorCodes.RoleAssignmentConflict;
            message = "An active assignment for this Platform User, role, and organization scope already exists.";
            return true;
        }

        errorCode = ApplicationErrorCodes.DomainViolation;
        message = "A unique constraint was violated.";
        return true;
    }
}
