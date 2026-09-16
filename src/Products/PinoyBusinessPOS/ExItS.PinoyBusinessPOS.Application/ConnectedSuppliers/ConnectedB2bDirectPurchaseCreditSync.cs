using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.Suppliers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Sales;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;

namespace ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;

/// <summary>
/// Posts/reverses seller BusinessCreditEntry for connected Direct Purchase Utang
/// so buyer payable and seller receivable stay on one canonical ledger.
/// Direct purchase never creates PO reservations.
/// </summary>
public sealed class ConnectedB2bDirectPurchaseCreditSync
{
    private readonly ISupplierRepository _suppliers;
    private readonly IConnectedSupplierRelationshipRepository _relationships;
    private readonly IBusinessCreditEntryRepository _businessCredits;

    public ConnectedB2bDirectPurchaseCreditSync(
        ISupplierRepository suppliers,
        IConnectedSupplierRelationshipRepository relationships,
        IBusinessCreditEntryRepository businessCredits)
    {
        _suppliers = suppliers;
        _relationships = relationships;
        _businessCredits = businessCredits;
    }

    public async Task PostFromReceiptAsync(
        DirectPurchaseReceipt receipt,
        decimal? paidNow,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default)
    {
        if (receipt.SupplierId is null)
        {
            return;
        }

        var total = SaleMoney.RoundMoney(receipt.TotalCost);
        if (total <= 0m)
        {
            return;
        }

        var effectivePaid = paidNow is null
            ? total
            : SaleMoney.RoundMoney(paidNow.Value);
        if (effectivePaid < 0m)
        {
            effectivePaid = 0m;
        }

        if (effectivePaid > total)
        {
            effectivePaid = total;
        }

        var obligation = ConnectedPoUtangObligationProjection.ObligationAmount(total, effectivePaid);
        if (obligation <= 0m)
        {
            return;
        }

        var relationship = await ResolveActiveConnectedRelationshipAsync(
                receipt.OrganizationId,
                receipt.SupplierId,
                cancellationToken)
            .ConfigureAwait(false);
        if (relationship is null)
        {
            return;
        }

        var entry = BusinessCreditEntry.Create(
            relationship.SupplierOrganizationId,
            relationship.BuyerOrganizationId,
            obligation,
            ConnectedPoUtangObligationProjection.BuildDirectPurchaseRemark(
                receipt.Id.Value,
                receipt.ReceiptNumber),
            utcNow,
            relationship.Id.Value);
        await _businessCredits.AddAsync(entry, cancellationToken).ConfigureAwait(false);
    }

    public async Task ReverseForReceiptAsync(
        DirectPurchaseReceipt receipt,
        string voidReason,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default)
    {
        if (receipt.SupplierId is null)
        {
            return;
        }

        var relationship = await ResolveActiveConnectedRelationshipAsync(
                receipt.OrganizationId,
                receipt.SupplierId,
                cancellationToken,
                requireActive: false)
            .ConfigureAwait(false);
        if (relationship is null)
        {
            return;
        }

        var entries = await _businessCredits
            .ListChronologicalForBuyerAsync(
                relationship.SupplierOrganizationId,
                relationship.BuyerOrganizationId,
                cancellationToken)
            .ConfigureAwait(false);

        foreach (var entry in entries)
        {
            if (entry.Status != CreditEntryStatus.Active)
            {
                continue;
            }

            if (!ConnectedPoUtangObligationProjection.TryParseDirectPurchaseReceiptId(
                    entry.Remarks,
                    out var receiptId)
                || receiptId != receipt.Id.Value)
            {
                continue;
            }

            entry.Reverse(voidReason, utcNow);
            await _businessCredits.UpdateAsync(entry, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<ConnectedSupplierRelationship?> ResolveActiveConnectedRelationshipAsync(
        PosOrganizationId buyerOrganizationId,
        SupplierId supplierId,
        CancellationToken cancellationToken,
        bool requireActive = true)
    {
        var supplier = await _suppliers
            .GetByIdAsync(buyerOrganizationId, supplierId, cancellationToken)
            .ConfigureAwait(false);
        if (supplier?.ConnectionType != SupplierConnectionType.ConnectedOrganization
            || supplier.ConnectedRelationshipId is null)
        {
            return null;
        }

        var relationship = await _relationships
            .GetAsync(supplier.ConnectedRelationshipId, cancellationToken)
            .ConfigureAwait(false);
        if (relationship is null || relationship.BuyerOrganizationId != buyerOrganizationId)
        {
            return null;
        }

        if (requireActive && relationship.Status != ConnectedSupplierRelationshipStatus.Active)
        {
            return null;
        }

        return relationship;
    }
}
