using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.SupplierPayables;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;

namespace ExItS.PinoyBusinessPOS.Application.SupplierPayables;

/// <summary>
/// Internal receipt-hook service. Creates organization-scoped payables after posted receipts
/// (idempotent by organization + source). Does not insert payment rows for PaidNow —
/// PaidAtReceiptAmount is the settlement snapshot (PAID_AT_RECEIPT vs POSTED_PAYMENTS).
/// </summary>
public sealed class CreateSupplierPayableFromReceipt
{
    private readonly ISupplierPayableRepository _payables;

    public CreateSupplierPayableFromReceipt(ISupplierPayableRepository payables) =>
        _payables = payables;

    public Task CreateFromGoodsReceiptAsync(
        GoodsReceipt receipt,
        decimal? paidNow,
        DateOnly? dueDate,
        string? paymentMethodAtReceipt,
        Guid actorId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default)
    {
        var original = receipt.Lines.Sum(l => l.LineTotalSnapshot);
        return CreateCoreAsync(
            receipt.OrganizationId,
            receipt.SupplierId,
            SupplierPayableSourceType.GoodsReceipt,
            receipt.Id.Value,
            original,
            paidNow,
            dueDate,
            paymentMethodAtReceipt,
            actorId,
            utcNow,
            cancellationToken);
    }

    /// <summary>
    /// Direct purchase: skip when no supplier and fully paid (NO_SUPPLIER_SKIP).
    /// Credit without supplier fails (DIRECT_PURCHASE_REQUIRES_SUPPLIER_FOR_PAYABLE).
    /// </summary>
    public async Task<ApplicationResult<bool>> TryCreateFromDirectPurchaseAsync(
        DirectPurchaseReceipt receipt,
        decimal? paidNow,
        DateOnly? dueDate,
        string? paymentMethodAtReceipt,
        Guid actorId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default)
    {
        var original = receipt.TotalCost;
        var effectivePaid = paidNow ?? original;

        if (receipt.SupplierId is null)
        {
            if (effectivePaid < original)
            {
                return ApplicationResult<bool>.Failure(
                    DomainErrorCodes.DirectPurchaseRequiresSupplierForCredit,
                    "A supplier is required when recording a direct purchase on credit (PaidNow less than total).");
            }

            return ApplicationResult<bool>.Success(false);
        }

        await CreateCoreAsync(
                receipt.OrganizationId,
                receipt.SupplierId,
                SupplierPayableSourceType.DirectPurchaseReceipt,
                receipt.Id.Value,
                original,
                paidNow,
                dueDate,
                paymentMethodAtReceipt,
                actorId,
                utcNow,
                cancellationToken)
            .ConfigureAwait(false);

        return ApplicationResult<bool>.Success(true);
    }

    public async Task EnsureVoidOrBlockForReceiptReversalAsync(
        PosOrganizationId organizationId,
        SupplierPayableSourceType sourceType,
        Guid sourceId,
        string voidReason,
        Guid actorId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default)
    {
        var existing = await _payables
            .FindBySourceAsync(organizationId, sourceType, sourceId, cancellationToken)
            .ConfigureAwait(false);
        if (existing is null || existing.Status == SupplierPayableStatus.Voided)
        {
            return;
        }

        if (existing.HasPostedPayments)
        {
            throw new DomainException(
                DomainErrorCodes.SupplierPayableReceiptReversalBlocked,
                "Cannot reverse this receipt because the supplier payable already has recorded payments.");
        }

        existing.Void(voidReason, actorId, utcNow);
        await _payables.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
    }

    private async Task CreateCoreAsync(
        PosOrganizationId organizationId,
        SupplierId supplierId,
        SupplierPayableSourceType sourceType,
        Guid sourceId,
        decimal originalAmount,
        decimal? paidNow,
        DateOnly? dueDate,
        string? paymentMethodAtReceipt,
        Guid actorId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var existing = await _payables
            .FindBySourceAsync(organizationId, sourceType, sourceId, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            return;
        }

        SupplierPayablePaymentMethod? method = null;
        if (!string.IsNullOrWhiteSpace(paymentMethodAtReceipt))
        {
            method = SupplierPayablePaymentMethods.Parse(paymentMethodAtReceipt);
        }

        var payable = SupplierPayable.Create(
            organizationId,
            supplierId,
            sourceType,
            sourceId,
            originalAmount,
            actorId,
            utcNow,
            paidNow,
            dueDate,
            method);

        await _payables.AddAsync(payable, cancellationToken).ConfigureAwait(false);
    }
}
