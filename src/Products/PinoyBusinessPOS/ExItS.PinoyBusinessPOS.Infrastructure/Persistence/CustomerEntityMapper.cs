using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Customers;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence;

internal static class CustomerEntityMapper
{
    public static POSCustomer ToDomain(POSCustomerRecord record) =>
        POSCustomer.Rehydrate(
            POSCustomerId.From(record.Id),
            PosOrganizationId.From(record.OrganizationId),
            record.DisplayName,
            record.MobileNumber,
            record.NormalizedMobile,
            record.Address,
            record.Notes,
            Enum.Parse<CustomerStatus>(record.Status, ignoreCase: true),
            record.PlatformBusinessCustomerId,
            record.CreatedAtUtc,
            record.UpdatedAtUtc,
            record.LinkedPersonalPublicUserId,
            record.LinkedBuyerOrganizationId,
            record.LinkedBuyerPublicOrganizationId);

    public static POSCustomerRecord ToRecord(POSCustomer customer) =>
        new()
        {
            Id = customer.Id.Value,
            OrganizationId = customer.OrganizationId.Value,
            DisplayName = customer.DisplayName,
            MobileNumber = customer.MobileNumber,
            NormalizedMobile = customer.NormalizedMobile,
            Address = customer.Address,
            Notes = customer.Notes,
            Status = customer.Status.ToString(),
            PlatformBusinessCustomerId = customer.PlatformBusinessCustomerId,
            LinkedPersonalPublicUserId = customer.LinkedPersonalPublicUserId,
            LinkedBuyerOrganizationId = customer.LinkedBuyerOrganizationId,
            LinkedBuyerPublicOrganizationId = customer.LinkedBuyerPublicOrganizationId,
            CreatedAtUtc = customer.CreatedAtUtc,
            UpdatedAtUtc = customer.UpdatedAtUtc
        };

    public static void ApplyToRecord(POSCustomer customer, POSCustomerRecord record)
    {
        record.DisplayName = customer.DisplayName;
        record.MobileNumber = customer.MobileNumber;
        record.NormalizedMobile = customer.NormalizedMobile;
        record.Address = customer.Address;
        record.Notes = customer.Notes;
        record.Status = customer.Status.ToString();
        record.PlatformBusinessCustomerId = customer.PlatformBusinessCustomerId;
        record.LinkedPersonalPublicUserId = customer.LinkedPersonalPublicUserId;
        record.LinkedBuyerOrganizationId = customer.LinkedBuyerOrganizationId;
        record.LinkedBuyerPublicOrganizationId = customer.LinkedBuyerPublicOrganizationId;
        record.UpdatedAtUtc = customer.UpdatedAtUtc;
        // OrganizationId is immutable — never rewritten from the aggregate.
    }
}
