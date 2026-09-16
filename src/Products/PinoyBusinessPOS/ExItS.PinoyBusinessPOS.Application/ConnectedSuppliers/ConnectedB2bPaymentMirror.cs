using ExItS.PinoyBusinessPOS.Application.Payments;
using ExItS.PinoyBusinessPOS.Application.SupplierPayables;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Payments;
using ExItS.PinoyBusinessPOS.Domain.Sales;
using ExItS.PinoyBusinessPOS.Domain.SupplierPayables;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;

namespace ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;

/// <summary>
/// Keeps seller BusinessRepayment and buyer SupplierPayable payment projections aligned
/// for the same connected B2B relationship (one economic settlement).
/// </summary>
public sealed class ConnectedB2bPaymentMirror
{
    public const string SyncReferencePrefix = "b2b-sync:";

    private readonly ISupplierRepository _suppliers;
    private readonly ISupplierPayableRepository _payables;
    private readonly IBusinessRepaymentRepository _repayments;
    private readonly IConnectedSupplierRelationshipRepository _relationships;
    private readonly IClock _clock;

    public ConnectedB2bPaymentMirror(
        ISupplierRepository suppliers,
        ISupplierPayableRepository payables,
        IBusinessRepaymentRepository repayments,
        IConnectedSupplierRelationshipRepository relationships,
        IClock clock)
    {
        _suppliers = suppliers;
        _payables = payables;
        _repayments = repayments;
        _relationships = relationships;
        _clock = clock;
    }

    public static bool IsSyncMarker(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.StartsWith(SyncReferencePrefix, StringComparison.Ordinal);

    /// <summary>
    /// Buyer recorded a payable payment → mirror as seller BusinessRepayment when supplier is connected.
    /// </summary>
    public async Task MirrorBuyerPayablePaymentAsync(
        PosOrganizationId buyerOrganizationId,
        SupplierId supplierId,
        decimal amount,
        Guid actorId,
        string? sourcePaymentReference,
        CancellationToken cancellationToken = default)
    {
        if (IsSyncMarker(sourcePaymentReference) || amount <= 0m)
        {
            return;
        }

        var supplier = await _suppliers
            .GetByIdAsync(buyerOrganizationId, supplierId, cancellationToken)
            .ConfigureAwait(false);
        if (supplier?.ConnectionType != SupplierConnectionType.ConnectedOrganization
            || supplier.ConnectedRelationshipId is null)
        {
            return;
        }

        var relationship = await _relationships
            .GetAsync(supplier.ConnectedRelationshipId, cancellationToken)
            .ConfigureAwait(false);
        if (relationship is null
            || relationship.BuyerOrganizationId != buyerOrganizationId
            || relationship.Status != ConnectedSupplierRelationshipStatus.Active)
        {
            return;
        }

        var normalized = SaleMoney.RoundMoney(amount);
        var repayment = BusinessRepayment.Create(
            relationship.SupplierOrganizationId,
            relationship.BuyerOrganizationId,
            relationship.Id.Value,
            normalized,
            remarks: $"{SyncReferencePrefix}payable",
            recordedBy: actorId,
            utcNow: _clock.UtcNow,
            paymentMethod: UtangPaymentMethod.Cash);
        await _repayments.AddAsync(repayment, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Seller recorded a BusinessRepayment → apply FIFO payments on buyer open SupplierPayables.
    /// </summary>
    public async Task MirrorSellerRepaymentAsync(
        BusinessRepayment repayment,
        CancellationToken cancellationToken = default)
    {
        if (IsSyncMarker(repayment.Remarks) || repayment.Amount <= 0m)
        {
            return;
        }

        // Only settle immediately for cash-like methods; checks wait until cleared.
        if (repayment.PaymentMethod == UtangPaymentMethod.Check
            && repayment.CheckClearingStatus != UtangCheckClearingStatus.Cleared)
        {
            return;
        }

        var supplier = await _suppliers
            .FindByConnectedRelationshipIdAsync(
                repayment.BuyerOrganizationId,
                ConnectedSupplierRelationshipId.From(repayment.ConnectionId),
                cancellationToken)
            .ConfigureAwait(false);
        if (supplier is null)
        {
            return;
        }

        var (openPayables, _) = await _payables
            .ListAsync(
                repayment.BuyerOrganizationId,
                new SupplierPayableFilter(
                    supplier.Id,
                    OutstandingOnly: true),
                skip: 0,
                take: 200,
                cancellationToken)
            .ConfigureAwait(false);

        var remaining = SaleMoney.RoundMoney(repayment.Amount);
        foreach (var payable in openPayables
                     .Where(p => p.Balance > 0m)
                     .OrderBy(p => p.CreatedAtUtc)
                     .ThenBy(p => p.Id.Value))
        {
            if (remaining <= 0m)
            {
                break;
            }

            var apply = remaining > payable.Balance ? payable.Balance : remaining;
            payable.ApplyPayment(
                apply,
                SupplierPayablePaymentMethod.Other,
                repayment.RecordedBy,
                _clock.UtcNow,
                reference: $"{SyncReferencePrefix}{repayment.Id.Value:D}",
                notes: "Mirrored from seller business repayment");
            await _payables.UpdateAsync(payable, cancellationToken).ConfigureAwait(false);
            remaining = SaleMoney.RoundMoney(remaining - apply);
        }
    }
}
