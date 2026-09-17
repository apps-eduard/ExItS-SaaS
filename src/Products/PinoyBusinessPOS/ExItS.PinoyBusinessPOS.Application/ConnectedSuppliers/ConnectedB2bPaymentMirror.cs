using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.Payments;
using ExItS.PinoyBusinessPOS.Application.SupplierPayables;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Credit;
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
    private readonly IBusinessCreditEntryRepository _credits;
    private readonly IConnectedSupplierRelationshipRepository _relationships;
    private readonly IClock _clock;

    public ConnectedB2bPaymentMirror(
        ISupplierRepository suppliers,
        ISupplierPayableRepository payables,
        IBusinessRepaymentRepository repayments,
        IBusinessCreditEntryRepository credits,
        IConnectedSupplierRelationshipRepository relationships,
        IClock clock)
    {
        _suppliers = suppliers;
        _payables = payables;
        _repayments = repayments;
        _credits = credits;
        _relationships = relationships;
        _clock = clock;
    }

    public static bool IsSyncMarker(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.StartsWith(SyncReferencePrefix, StringComparison.Ordinal);

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

    public Task MirrorSellerRepaymentAsync(
        BusinessRepayment repayment,
        CancellationToken cancellationToken = default) =>
        MirrorSellerRepaymentAsync(repayment, allocations: null, creditEntries: null, cancellationToken);

    public async Task MirrorSellerRepaymentAsync(
        BusinessRepayment repayment,
        IReadOnlyList<BusinessRepaymentAllocation>? allocations,
        IReadOnlyList<BusinessCreditEntry>? creditEntries,
        CancellationToken cancellationToken = default)
    {
        if (IsSyncMarker(repayment.Remarks) || repayment.Amount <= 0m)
        {
            return;
        }

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

        var resolvedAllocations = allocations
            ?? await _repayments
                .ListAllocationsByRepaymentAsync(
                    repayment.SellerOrganizationId,
                    repayment.Id,
                    cancellationToken)
                .ConfigureAwait(false);

        IReadOnlyDictionary<Guid, BusinessCreditEntry> creditById;
        if (creditEntries is not null)
        {
            creditById = creditEntries.ToDictionary(c => c.Id.Value);
        }
        else if (resolvedAllocations.Count > 0)
        {
            var loaded = await _credits
                .ListChronologicalForBuyerAsync(
                    repayment.SellerOrganizationId,
                    repayment.BuyerOrganizationId,
                    cancellationToken)
                .ConfigureAwait(false);
            creditById = loaded.ToDictionary(c => c.Id.Value);
        }
        else
        {
            creditById = new Dictionary<Guid, BusinessCreditEntry>();
        }

        var remaining = SaleMoney.RoundMoney(repayment.Amount);

        if (resolvedAllocations.Count > 0)
        {
            foreach (var allocation in resolvedAllocations)
            {
                if (remaining <= 0m)
                {
                    break;
                }

                if (!creditById.TryGetValue(allocation.CreditEntryId.Value, out var credit))
                {
                    continue;
                }

                var apply = remaining > allocation.Amount ? allocation.Amount : remaining;
                var mirrored = await TryApplyToMatchingPayableAsync(
                        repayment,
                        supplier,
                        credit,
                        apply,
                        cancellationToken)
                    .ConfigureAwait(false);
                if (mirrored)
                {
                    remaining = SaleMoney.RoundMoney(remaining - apply);
                }
            }
        }

        if (remaining <= 0m)
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

        foreach (var payable in openPayables
                     .Where(p => p.Balance > 0m)
                     .OrderBy(p => p.DueDate is null)
                     .ThenBy(p => p.DueDate ?? DateOnly.MaxValue)
                     .ThenBy(p => p.CreatedAtUtc)
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

    private async Task<bool> TryApplyToMatchingPayableAsync(
        BusinessRepayment repayment,
        Supplier supplier,
        BusinessCreditEntry credit,
        decimal amount,
        CancellationToken cancellationToken)
    {
        var creditRemarks = credit.Remarks;
        SupplierPayable? payable = null;
        if (ConnectedPoUtangObligationProjection.TryParseGoodsReceiptId(creditRemarks, out var grnId))
        {
            payable = await _payables
                .FindBySourceAsync(
                    repayment.BuyerOrganizationId,
                    SupplierPayableSourceType.GoodsReceipt,
                    grnId,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        else if (ConnectedPoUtangObligationProjection.TryParseDirectPurchaseReceiptId(creditRemarks, out var dprId))
        {
            payable = await _payables
                .FindBySourceAsync(
                    repayment.BuyerOrganizationId,
                    SupplierPayableSourceType.DirectPurchaseReceipt,
                    dprId,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        else if (ConnectedPoUtangObligationProjection.TryParseSaleId(creditRemarks, out var saleIdFromRemark))
        {
            payable = await _payables
                .FindBySourceAsync(
                    repayment.BuyerOrganizationId,
                    SupplierPayableSourceType.Sale,
                    saleIdFromRemark,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        else if (credit.SourceSaleId is not null)
        {
            // Historical Sell Utang credits used free-text remarks without sale: prefix.
            payable = await _payables
                .FindBySourceAsync(
                    repayment.BuyerOrganizationId,
                    SupplierPayableSourceType.Sale,
                    credit.SourceSaleId.Value,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        if (payable is null || payable.SupplierId != supplier.Id || payable.Balance <= 0m)
        {
            return false;
        }

        var apply = amount > payable.Balance ? payable.Balance : amount;
        if (apply <= 0m)
        {
            return false;
        }

        payable.ApplyPayment(
            apply,
            SupplierPayablePaymentMethod.Other,
            repayment.RecordedBy,
            _clock.UtcNow,
            reference: $"{SyncReferencePrefix}{repayment.Id.Value:D}",
            notes: "Mirrored from seller business repayment");
        await _payables.UpdateAsync(payable, cancellationToken).ConfigureAwait(false);
        return true;
    }
}
