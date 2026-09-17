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
            record.ReversedBy,
            Enum.Parse<UtangPaymentMethod>(record.PaymentMethod, ignoreCase: false),
            record.CheckNumber,
            record.BankName,
            record.CheckDate,
            record.AccountName,
            record.Reference,
            Enum.Parse<UtangCheckClearingStatus>(record.CheckClearingStatus, ignoreCase: false),
            record.ClearedAtUtc,
            record.ClearedBy,
            record.BouncedAtUtc,
            record.BouncedBy,
            record.BounceReason,
            record.CancelledAtUtc,
            record.CancelledBy,
            record.CancelReason);

    public static RepaymentRecord ToRecord(Repayment repayment) =>
        new()
        {
            Id = repayment.Id.Value,
            OrganizationId = repayment.OrganizationId.Value,
            CustomerId = repayment.CustomerId.Value,
            Amount = repayment.Amount,
            Remarks = repayment.Remarks,
            PaymentMethod = repayment.PaymentMethod.ToString(),
            CheckNumber = repayment.CheckNumber,
            BankName = repayment.BankName,
            CheckDate = repayment.CheckDate,
            AccountName = repayment.AccountName,
            Reference = repayment.Reference,
            CheckClearingStatus = repayment.CheckClearingStatus.ToString(),
            Status = repayment.Status.ToString(),
            RecordedAtUtc = repayment.RecordedAtUtc,
            RecordedBy = repayment.RecordedBy,
            ReversedAtUtc = repayment.ReversedAtUtc,
            ReversalReason = repayment.ReversalReason,
            ReversedBy = repayment.ReversedBy,
            ClearedAtUtc = repayment.ClearedAtUtc,
            ClearedBy = repayment.ClearedBy,
            BouncedAtUtc = repayment.BouncedAtUtc,
            BouncedBy = repayment.BouncedBy,
            BounceReason = repayment.BounceReason,
            CancelledAtUtc = repayment.CancelledAtUtc,
            CancelledBy = repayment.CancelledBy,
            CancelReason = repayment.CancelReason
        };

    public static void ApplyToRecord(Repayment repayment, RepaymentRecord record)
    {
        record.Status = repayment.Status.ToString();
        record.CheckClearingStatus = repayment.CheckClearingStatus.ToString();
        record.ReversedAtUtc = repayment.ReversedAtUtc;
        record.ReversalReason = repayment.ReversalReason;
        record.ReversedBy = repayment.ReversedBy;
        record.ClearedAtUtc = repayment.ClearedAtUtc;
        record.ClearedBy = repayment.ClearedBy;
        record.BouncedAtUtc = repayment.BouncedAtUtc;
        record.BouncedBy = repayment.BouncedBy;
        record.BounceReason = repayment.BounceReason;
        record.CancelledAtUtc = repayment.CancelledAtUtc;
        record.CancelledBy = repayment.CancelledBy;
        record.CancelReason = repayment.CancelReason;
    }
}
