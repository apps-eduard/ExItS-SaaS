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

        // Check subscription- and organization-specific constraints first: their detail text can
        // also contain "product_code" / generic substrings that would otherwise be caught by the
        // broader catalog checks below.
        if (detail.Contains("ux_subscriptions_one_active_like", StringComparison.OrdinalIgnoreCase)
            || detail.Contains("subscriptions", StringComparison.OrdinalIgnoreCase))
        {
            errorCode = ApplicationErrorCodes.ActiveSubscriptionConflict;
            message = "An active-like subscription already exists for this organization and product.";
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

        errorCode = ApplicationErrorCodes.DomainViolation;
        message = "A unique constraint was violated.";
        return true;
    }
}
