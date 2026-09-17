using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Payments;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Payments;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence;

internal static class BusinessRepaymentEntityMapper
{
    public static BusinessRepayment ToDomain(BusinessRepaymentRecord record) =>
        BusinessRepayment.Rehydrate(
            BusinessRepaymentId.From(record.Id),
            PosOrganizationId.From(record.SellerOrganizationId),
            PosOrganizationId.From(record.BuyerOrganizationId),
            record.ConnectionId,
            record.Amount,
            record.Remarks,
            Enum.Parse<UtangPaymentMethod>(record.PaymentMethod, ignoreCase: false),
            record.CheckNumber,
            record.BankName,
            record.CheckDate,
            record.AccountName,
            record.Reference,
            Enum.Parse<UtangCheckClearingStatus>(record.CheckClearingStatus, ignoreCase: false),
            Enum.Parse<RepaymentStatus>(record.Status, ignoreCase: false),
            record.RecordedAtUtc,
            record.RecordedBy,
            record.ReversedAtUtc,
            record.ReversalReason,
            record.ReversedBy,
            record.ClearedAtUtc,
            record.ClearedBy,
            record.BouncedAtUtc,
            record.BouncedBy,
            record.BounceReason,
            record.CancelledAtUtc,
            record.CancelledBy,
            record.CancelReason);

    public static BusinessRepaymentRecord ToRecord(BusinessRepayment repayment) =>
        new()
        {
            Id = repayment.Id.Value,
            SellerOrganizationId = repayment.SellerOrganizationId.Value,
            BuyerOrganizationId = repayment.BuyerOrganizationId.Value,
            ConnectionId = repayment.ConnectionId,
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

    public static void ApplyToRecord(BusinessRepayment repayment, BusinessRepaymentRecord record)
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
