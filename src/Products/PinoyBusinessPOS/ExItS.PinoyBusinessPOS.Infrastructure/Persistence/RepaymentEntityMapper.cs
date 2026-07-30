using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Payments;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Payments;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence;

internal static class RepaymentEntityMapper
{
    public static Repayment ToDomain(RepaymentRecord record) =>
        Repayment.Rehydrate(
            RepaymentId.From(record.Id),
            PosOrganizationId.From(record.OrganizationId),
            POSCustomerId.From(record.CustomerId),
            record.Amount,
            record.Remarks,
            Enum.Parse<RepaymentStatus>(record.Status, ignoreCase: false),
            record.RecordedAtUtc,
            record.RecordedBy,
            record.ReversedAtUtc,
            record.ReversalReason,
            record.ReversedBy);

    public static RepaymentRecord ToRecord(Repayment repayment) =>
        new()
        {
            Id = repayment.Id.Value,
            OrganizationId = repayment.OrganizationId.Value,
            CustomerId = repayment.CustomerId.Value,
            Amount = repayment.Amount,
            Remarks = repayment.Remarks,
            Status = repayment.Status.ToString(),
            RecordedAtUtc = repayment.RecordedAtUtc,
            RecordedBy = repayment.RecordedBy,
            ReversedAtUtc = repayment.ReversedAtUtc,
            ReversalReason = repayment.ReversalReason,
            ReversedBy = repayment.ReversedBy
        };

    public static void ApplyToRecord(Repayment repayment, RepaymentRecord record)
    {
        record.Status = repayment.Status.ToString();
        record.ReversedAtUtc = repayment.ReversedAtUtc;
        record.ReversalReason = repayment.ReversalReason;
        record.ReversedBy = repayment.ReversedBy;
    }
}
