using ExItS.PinoyBuyNowPayLater.Domain.Customers;

namespace ExItS.PinoyBuyNowPayLater.Infrastructure.Persistence.Customers;

internal static class BnplCustomerEntityMapper
{
    public static BnplCustomer ToDomain(BnplCustomerRecord record) =>
        BnplCustomer.Reconstitute(
            BnplCustomerId.From(record.Id),
            record.OrganizationId,
            record.DisplayName,
            record.Mobile,
            record.NormalizedMobile,
            record.Email,
            record.NormalizedEmail,
            Enum.Parse<BnplCustomerStatus>(record.Status, ignoreCase: true),
            record.LinkedPersonalPublicUserId,
            record.LinkedCommerceCustomerId,
            record.CreatedAtUtc,
            record.UpdatedAtUtc);

    public static BnplCustomerRecord ToRecord(BnplCustomer customer) =>
        new()
        {
            Id = customer.Id.Value,
            OrganizationId = customer.OrganizationId,
            DisplayName = customer.DisplayName,
            Mobile = customer.Mobile,
            NormalizedMobile = customer.NormalizedMobile,
            Email = customer.Email,
            NormalizedEmail = customer.NormalizedEmail,
            Status = customer.Status.ToString(),
            LinkedPersonalPublicUserId = customer.LinkedPersonalPublicUserId,
            LinkedCommerceCustomerId = customer.LinkedCommerceCustomerId,
            CreatedAtUtc = customer.CreatedAtUtc,
            UpdatedAtUtc = customer.UpdatedAtUtc
        };

    public static void CopyToRecord(BnplCustomer customer, BnplCustomerRecord record)
    {
        record.DisplayName = customer.DisplayName;
        record.Mobile = customer.Mobile;
        record.NormalizedMobile = customer.NormalizedMobile;
        record.Email = customer.Email;
        record.NormalizedEmail = customer.NormalizedEmail;
        record.Status = customer.Status.ToString();
        record.LinkedPersonalPublicUserId = customer.LinkedPersonalPublicUserId;
        record.LinkedCommerceCustomerId = customer.LinkedCommerceCustomerId;
        record.UpdatedAtUtc = customer.UpdatedAtUtc;
    }
}
