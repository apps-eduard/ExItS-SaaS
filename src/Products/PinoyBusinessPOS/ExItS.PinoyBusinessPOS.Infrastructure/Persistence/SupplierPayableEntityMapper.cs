using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.SupplierPayables;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.SupplierPayables;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence;

internal static class SupplierPayableEntityMapper
{
    public static SupplierPayable ToDomain(
        SupplierPayableRecord record,
        IReadOnlyList<SupplierPayablePaymentRecord>? payments = null)
    {
        var paymentEntities = (payments ?? Array.Empty<SupplierPayablePaymentRecord>())
            .Select(ToPaymentDomain)
            .ToList();

        return SupplierPayable.Rehydrate(
            SupplierPayableId.From(record.Id),
            PosOrganizationId.From(record.OrganizationId),
            SupplierId.From(record.SupplierId),
            SupplierPayableSourceTypes.Parse(record.SourceType),
            record.SourceId,
            record.OriginalAmount,
            record.PaidAtReceiptAmount,
            record.PaidAmount,
            record.Balance,
            Enum.Parse<SupplierPayableStatus>(record.Status, ignoreCase: true),
            record.DueDate,
            string.IsNullOrWhiteSpace(record.PaymentMethodAtReceipt)
                ? null
                : SupplierPayablePaymentMethods.Parse(record.PaymentMethodAtReceipt),
            record.CreatedAtUtc,
            record.CreatedBy,
            record.UpdatedAtUtc,
            record.VoidedAtUtc,
            record.VoidedBy,
            record.VoidReason,
            paymentEntities);
    }

    public static SupplierPayablePayment ToPaymentDomain(SupplierPayablePaymentRecord record) =>
        SupplierPayablePayment.Rehydrate(
            SupplierPayablePaymentId.From(record.Id),
            SupplierPayableId.From(record.PayableId),
            record.Amount,
            SupplierPayablePaymentMethods.Parse(record.PaymentMethod),
            record.Reference,
            record.Notes,
            record.PaidAtUtc,
            record.RecordedBy,
            record.RecordedAtUtc);

    public static SupplierPayableRecord ToRecord(SupplierPayable payable) =>
        new()
        {
            Id = payable.Id.Value,
            OrganizationId = payable.OrganizationId.Value,
            SupplierId = payable.SupplierId.Value,
            SourceType = SupplierPayableSourceTypes.ToCode(payable.SourceType),
            SourceId = payable.SourceId,
            OriginalAmount = payable.OriginalAmount,
            PaidAtReceiptAmount = payable.PaidAtReceiptAmount,
            PaidAmount = payable.PaidAmount,
            Balance = payable.Balance,
            Status = payable.Status.ToString(),
            DueDate = payable.DueDate,
            PaymentMethodAtReceipt = payable.PaymentMethodAtReceipt is null
                ? null
                : SupplierPayablePaymentMethods.ToCode(payable.PaymentMethodAtReceipt.Value),
            CreatedAtUtc = payable.CreatedAtUtc,
            CreatedBy = payable.CreatedBy,
            UpdatedAtUtc = payable.UpdatedAtUtc,
            VoidedAtUtc = payable.VoidedAtUtc,
            VoidedBy = payable.VoidedBy,
            VoidReason = payable.VoidReason
        };

    public static SupplierPayablePaymentRecord ToPaymentRecord(
        SupplierPayablePayment payment,
        Guid organizationId) =>
        new()
        {
            Id = payment.Id.Value,
            OrganizationId = organizationId,
            PayableId = payment.PayableId.Value,
            Amount = payment.Amount,
            PaymentMethod = SupplierPayablePaymentMethods.ToCode(payment.PaymentMethod),
            Reference = payment.Reference,
            Notes = payment.Notes,
            PaidAtUtc = payment.PaidAtUtc,
            RecordedBy = payment.RecordedBy,
            RecordedAtUtc = payment.RecordedAtUtc
        };

    public static void ApplyToRecord(SupplierPayable payable, SupplierPayableRecord record)
    {
        record.PaidAmount = payable.PaidAmount;
        record.Balance = payable.Balance;
        record.Status = payable.Status.ToString();
        record.UpdatedAtUtc = payable.UpdatedAtUtc;
        record.VoidedAtUtc = payable.VoidedAtUtc;
        record.VoidedBy = payable.VoidedBy;
        record.VoidReason = payable.VoidReason;
    }
}
