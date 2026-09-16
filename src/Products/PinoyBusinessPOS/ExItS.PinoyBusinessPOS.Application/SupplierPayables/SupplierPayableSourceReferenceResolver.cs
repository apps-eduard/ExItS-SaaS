using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.SupplierPayables;

namespace ExItS.PinoyBusinessPOS.Application.SupplierPayables;

/// <summary>
/// Resolves human-readable payable source labels: "PO-xxxx" or "Direct purchase …".
/// </summary>
public sealed class SupplierPayableSourceReferenceResolver
{
    private readonly IPurchaseOrderRepository _orders;
    private readonly IDirectPurchaseReceiptRepository _directReceipts;

    public SupplierPayableSourceReferenceResolver(
        IPurchaseOrderRepository orders,
        IDirectPurchaseReceiptRepository directReceipts)
    {
        _orders = orders;
        _directReceipts = directReceipts;
    }

    public async Task<IReadOnlyDictionary<Guid, string>> ResolveAsync(
        PosOrganizationId organizationId,
        IEnumerable<SupplierPayable> payables,
        CancellationToken cancellationToken = default)
    {
        var map = new Dictionary<Guid, string>();
        foreach (var payable in payables)
        {
            map[payable.Id.Value] = await ResolveOneAsync(organizationId, payable, cancellationToken)
                .ConfigureAwait(false);
        }

        return map;
    }

    public async Task<string> ResolveOneAsync(
        PosOrganizationId organizationId,
        SupplierPayable payable,
        CancellationToken cancellationToken = default)
    {
        if (payable.SourceType == SupplierPayableSourceType.DirectPurchaseReceipt)
        {
            var receipt = await _directReceipts
                .GetByIdAsync(
                    organizationId,
                    DirectPurchaseReceiptId.From(payable.SourceId),
                    cancellationToken)
                .ConfigureAwait(false);
            if (receipt is not null && !string.IsNullOrWhiteSpace(receipt.ReceiptNumber))
            {
                return $"Direct purchase {receipt.ReceiptNumber.Trim()}";
            }

            return "Direct purchase";
        }

        var goodsReceipt = await _orders
            .GetGoodsReceiptByIdAsync(
                organizationId,
                GoodsReceiptId.From(payable.SourceId),
                cancellationToken)
            .ConfigureAwait(false);
        if (goodsReceipt is null)
        {
            return "PO";
        }

        var po = await _orders
            .GetByIdAsync(organizationId, goodsReceipt.PurchaseOrderId, cancellationToken)
            .ConfigureAwait(false);
        if (po is not null && !string.IsNullOrWhiteSpace(po.PoNumber))
        {
            return po.PoNumber.Trim();
        }

        return "PO";
    }
}
