using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Payments;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Payments;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence;

internal static class WriteOffEntityMapper
{
    public static WriteOff ToDomain(WriteOffRecord record) =>
        WriteOff.Rehydrate(
            WriteOffId.From(record.Id),
            PosOrganizationId.From(record.OrganizationId),
            POSCustomerId.From(record.CustomerId),
            record.Amount,
            record.Reason,
            Enum.Parse<WriteOffStatus>(record.Status, ignoreCase: false),
            record.RecordedAtUtc,
            record.RecordedBy,
            record.ReversedAtUtc,
            record.ReversalReason,
            record.ReversedBy);

    public static WriteOffRecord ToRecord(WriteOff writeOff) =>
        new()
        {
            Id = writeOff.Id.Value,
            OrganizationId = writeOff.OrganizationId.Value,
            CustomerId = writeOff.CustomerId.Value,
            Amount = writeOff.Amount,
            Reason = writeOff.Reason,
            Status = writeOff.Status.ToString(),
            RecordedAtUtc = writeOff.RecordedAtUtc,
            RecordedBy = writeOff.RecordedBy,
            ReversedAtUtc = writeOff.ReversedAtUtc,
            ReversalReason = writeOff.ReversalReason,
            ReversedBy = writeOff.ReversedBy
        };

    public static void ApplyToRecord(WriteOff writeOff, WriteOffRecord record)
    {
        record.Status = writeOff.Status.ToString();
        record.ReversedAtUtc = writeOff.ReversedAtUtc;
        record.ReversalReason = writeOff.ReversalReason;
        record.ReversedBy = writeOff.ReversedBy;
    }
}
