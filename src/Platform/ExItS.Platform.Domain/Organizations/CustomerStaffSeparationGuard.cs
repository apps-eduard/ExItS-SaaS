using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Organizations;

/// <summary>
/// Hard boundaries between Organization Staff and Business Customers / Customer Links.
/// </summary>
public static class CustomerStaffSeparationGuard
{
    /// <summary>
    /// Rejects any attempt to convert a Business Customer into Organization Staff via the customer record.
    /// Staff membership requires a separate Organization Staff Invitation (or direct membership grant).
    /// </summary>
    public static void RejectCustomerToStaffConversion()
    {
        throw new DomainException(
            DomainErrorCodes.CustomerToStaffConversionDenied,
            "A Business Customer cannot be converted into Organization Staff. Send an Organization Staff Invitation instead.");
    }

    public static void EnsureCustomerLinkDoesNotGrantStaff(bool createdOrganizationMembership, bool grantedStaffRole)
    {
        if (createdOrganizationMembership || grantedStaffRole)
        {
            throw new DomainException(
                DomainErrorCodes.CustomerLinkMustNotCreateStaff,
                "Customer Link acceptance must not create Organization Staff membership or staff roles.");
        }
    }

    public static void EnsureNotTreatedAsStaff(BusinessCustomer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);
        if (customer.IsOrganizationStaff)
        {
            throw new DomainException(
                DomainErrorCodes.CustomerToStaffConversionDenied,
                "Business Customer must never be treated as Organization Staff.");
        }
    }
}
