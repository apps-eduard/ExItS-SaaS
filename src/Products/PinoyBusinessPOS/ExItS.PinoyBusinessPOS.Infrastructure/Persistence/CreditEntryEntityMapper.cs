using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Credit;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence;

internal static class CreditEntryEntityMapper
{
    public static CreditEntry ToDomain(CreditEntryRecord record) =>
        CreditEntry.Rehydrate(
            CreditEntryId.From(record.Id),
            PosOrganizationId.From(record.OrganizationId),
            POSCustomerId.From(record.CustomerId),
            record.Amount,
            record.Remarks,
            Enum.Parse<CreditEntryStatus>(record.Status, ignoreCase: false),
            record.CreatedAtUtc,
            record.ReversedAtUtc,
            record.ReversalReason);

    public static CreditEntryRecord ToRecord(CreditEntry entry) =>
        new()
        {
            Id = entry.Id.Value,
            OrganizationId = entry.OrganizationId.Value,
            CustomerId = entry.CustomerId.Value,
            Amount = entry.Amount,
            Remarks = entry.Remarks,
            Status = entry.Status.ToString(),
            CreatedAtUtc = entry.CreatedAtUtc,
            ReversedAtUtc = entry.ReversedAtUtc,
            ReversalReason = entry.ReversalReason
        };

    public static void ApplyToRecord(CreditEntry entry, CreditEntryRecord record)
    {
        record.Status = entry.Status.ToString();
        record.ReversedAtUtc = entry.ReversedAtUtc;
        record.ReversalReason = entry.ReversalReason;
    }
}
