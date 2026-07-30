using ExItS.PinoyBusinessPOS.Application.Common;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence;

internal static class PersistenceExceptionMapper
{
    public static bool TryMapUniqueViolation(DbUpdateException exception, out string errorCode, out string message)
    {
        errorCode = ApplicationErrorCodes.DomainViolation;
        message = "A persistence constraint was violated.";

        if (exception.InnerException is not PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg)
        {
            return false;
        }

        var constraint = pg.ConstraintName ?? string.Empty;
        if (constraint.Contains("ux_customers_org_active_mobile", StringComparison.OrdinalIgnoreCase)
            || constraint.Contains("mobile", StringComparison.OrdinalIgnoreCase))
        {
            errorCode = ApplicationErrorCodes.MobileConflict;
            message = "An active customer with this mobile number already exists in this organization.";
            return true;
        }

        return true;
    }
}
