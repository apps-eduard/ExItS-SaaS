using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Credit;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence;

internal static class CreditDueDateChangeEntityMapper
{
    public static CreditDueDateChange ToDomain(CreditDueDateChangeRecord record) =>
        CreditDueDateChange.Rehydrate(
            CreditDueDateChangeId.From(record.Id),
            PosOrganizationId.From(record.OrganizationId),
            CreditEntryId.From(record.CreditEntryId),
            POSCustomerId.From(record.CustomerId),
            record.PreviousDueDate,
            record.NewDueDate,
            record.Reason,
            record.ChangedBy,
            record.ChangedAtUtc);

    public static CreditDueDateChangeRecord ToRecord(CreditDueDateChange change) =>
        new()
        {
            Id = change.Id.Value,
            OrganizationId = change.OrganizationId.Value,
            CreditEntryId = change.CreditEntryId.Value,
            CustomerId = change.CustomerId.Value,
            PreviousDueDate = change.PreviousDueDate,
            NewDueDate = change.NewDueDate,
            Reason = change.Reason,
            ChangedBy = change.ChangedBy,
            ChangedAtUtc = change.ChangedAtUtc
        };
}
