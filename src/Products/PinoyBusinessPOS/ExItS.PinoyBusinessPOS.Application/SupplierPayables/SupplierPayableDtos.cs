using ExItS.PinoyBusinessPOS.Domain.SupplierPayables;

namespace ExItS.PinoyBusinessPOS.Application.SupplierPayables;

public sealed record PosSupplierPayableDto(
    Guid PayableId,
    Guid OrganizationId,
    Guid SupplierId,
    string? SupplierName,
    string SourceType,
    Guid SourceId,
    decimal OriginalAmount,
    decimal PaidAtReceiptAmount,
    decimal PaidAmount,
    decimal Balance,
    string Status,
    DateOnly? DueDate,
    string? PaymentMethodAtReceipt,
    DateTimeOffset CreatedAtUtc,
    Guid CreatedBy,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? VoidedAtUtc,
    Guid? VoidedBy,
    string? VoidReason,
    bool HasPostedPayments,
    bool IsOverdue);

public sealed record PosSupplierPayablePaymentDto(
    Guid PaymentId,
    Guid PayableId,
    decimal Amount,
    string PaymentMethod,
    string? Reference,
    string? Notes,
    DateTimeOffset PaidAtUtc,
    Guid RecordedBy,
    DateTimeOffset RecordedAtUtc);

public sealed record PosSupplierPayableSummaryDto(
    Guid SupplierId,
    decimal OutstandingTotal,
    decimal OverdueTotal,
    int OpenCount);

public sealed record PosSupplierPayableReportSummaryDto(
    decimal OutstandingTotal,
    decimal OverdueTotal,
    int OpenCount,
    int PartiallyPaidCount,
    int PaidCount,
    int VoidedCount);

public sealed record PosSupplierPayableSupplierBalanceDto(
    Guid SupplierId,
    string? SupplierName,
    decimal OutstandingBalance,
    decimal OverdueBalance,
    int OpenPayables,
    DateOnly? OldestDueDate);

public sealed record PosSupplierPayableReportRowDto(
    Guid PayableId,
    Guid SupplierId,
    string? SupplierName,
    string SourceType,
    Guid SourceId,
    decimal OriginalAmount,
    decimal PaidAtReceiptAmount,
    decimal PaidAmount,
    decimal Balance,
    string Status,
    DateOnly? DueDate,
    bool IsOverdue,
    DateTimeOffset CreatedAtUtc);

public sealed record PosSupplierPayableReportDto(
    DateOnly AsOfDate,
    PosSupplierPayableReportSummaryDto Summary,
    IReadOnlyList<PosSupplierPayableSupplierBalanceDto> Suppliers,
    IReadOnlyList<PosSupplierPayableReportRowDto> Payables);

public sealed record RecordSupplierPayablePaymentRequest(
    decimal Amount,
    string PaymentMethod,
    string? Reference = null,
    string? Notes = null,
    DateTimeOffset? PaidAtUtc = null);

public static class SupplierPayableMapper
{
    public static PosSupplierPayableDto Map(
        SupplierPayable payable,
        string? supplierName = null,
        DateOnly? asOfDate = null)
    {
        var asOf = asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        return new(
            payable.Id.Value,
            payable.OrganizationId.Value,
            payable.SupplierId.Value,
            supplierName,
            SupplierPayableSourceTypes.ToCode(payable.SourceType),
            payable.SourceId,
            payable.OriginalAmount,
            payable.PaidAtReceiptAmount,
            payable.PaidAmount,
            payable.Balance,
            payable.Status.ToString(),
            payable.DueDate,
            payable.PaymentMethodAtReceipt is null
                ? null
                : SupplierPayablePaymentMethods.ToCode(payable.PaymentMethodAtReceipt.Value),
            payable.CreatedAtUtc,
            payable.CreatedBy,
            payable.UpdatedAtUtc,
            payable.VoidedAtUtc,
            payable.VoidedBy,
            payable.VoidReason,
            payable.HasPostedPayments,
            IsOverdue(payable, asOf));
    }

    public static PosSupplierPayablePaymentDto MapPayment(SupplierPayablePayment payment) =>
        new(
            payment.Id.Value,
            payment.PayableId.Value,
            payment.Amount,
            SupplierPayablePaymentMethods.ToCode(payment.PaymentMethod),
            payment.Reference,
            payment.Notes,
            payment.PaidAtUtc,
            payment.RecordedBy,
            payment.RecordedAtUtc);

    public static PosSupplierPayableReportRowDto MapReportRow(
        SupplierPayable payable,
        string? supplierName,
        DateOnly asOfDate) =>
        new(
            payable.Id.Value,
            payable.SupplierId.Value,
            supplierName,
            SupplierPayableSourceTypes.ToCode(payable.SourceType),
            payable.SourceId,
            payable.OriginalAmount,
            payable.PaidAtReceiptAmount,
            payable.PaidAmount,
            payable.Balance,
            payable.Status.ToString(),
            payable.DueDate,
            IsOverdue(payable, asOfDate),
            payable.CreatedAtUtc);

    public static bool IsOverdue(SupplierPayable payable, DateOnly asOfDate) =>
        payable.Status is SupplierPayableStatus.Open or SupplierPayableStatus.PartiallyPaid
        && payable.Balance > 0m
        && payable.DueDate is DateOnly due
        && due < asOfDate;
}
