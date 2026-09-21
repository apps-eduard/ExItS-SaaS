using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;

namespace ExItS.PinoyBusinessPOS.Application.Returns;

/// <summary>
/// Creates receipt-line eligibility buckets at connected-PO goods receipt with snapshotted policy.
/// </summary>
public sealed class ConnectedPoReturnEligibilityService
{
    private readonly IConnectedPoReturnEligibilityBucketRepository _buckets;
    private readonly IOrganizationConnectedCommerceSettingsRepository _settings;
    private readonly ICatalogProductRepository _products;

    public ConnectedPoReturnEligibilityService(
        IConnectedPoReturnEligibilityBucketRepository buckets,
        IOrganizationConnectedCommerceSettingsRepository settings,
        ICatalogProductRepository products)
    {
        _buckets = buckets;
        _settings = settings;
        _products = products;
    }

    public async Task CreateBucketsForConnectedReceiptAsync(
        PosOrganizationId buyerOrganizationId,
        PosOrganizationId sellerOrganizationId,
        PurchaseOrder purchaseOrder,
        GoodsReceipt receipt,
        CancellationToken cancellationToken = default)
    {
        var settings = await _settings.GetAsync(sellerOrganizationId, cancellationToken).ConfigureAwait(false)
            ?? OrganizationConnectedCommerceSettings.CreateDefault(sellerOrganizationId, receipt.ReceivedAtUtc);

        var poLineById = purchaseOrder.Lines.ToDictionary(l => l.Id.Value);
        var supplierIds = purchaseOrder.Lines
            .Where(l => l.SupplierProductId is not null)
            .Select(l => l.SupplierProductId!)
            .Distinct()
            .ToList();

        var supplierProducts = supplierIds.Count == 0
            ? new Dictionary<Guid, CatalogProduct>()
            : (await _products
                    .ListByIdsAsync(sellerOrganizationId, supplierIds, cancellationToken)
                    .ConfigureAwait(false))
                .ToDictionary(p => p.Id.Value);

        var created = new List<ConnectedPoReturnEligibilityBucket>();
        foreach (var line in receipt.Lines.Where(l => l.QuantityReceived > 0m))
        {
            poLineById.TryGetValue(line.PurchaseOrderLineId.Value, out var poLine);
            CatalogProduct? sellerProduct = null;
            if (poLine?.SupplierProductId is CatalogProductId sid
                && supplierProducts.TryGetValue(sid.Value, out var loaded))
            {
                sellerProduct = loaded;
            }

            var policy = sellerProduct is null
                ? ConnectedPoReturnPolicyResolver.Resolve(
                    settings.ReturnsAllowed,
                    settings.ReturnWindowDays,
                    settings.ReceivingIssueWindowDays,
                    settings.RequireReturnApproval,
                    categoryRule: null,
                    ConnectedPoReturnPolicyMode.UseDefault,
                    null,
                    null)
                : ConnectedPoReturnPolicyResolver.ResolveFromSettings(
                    settings,
                    sellerProduct.CategoryId?.Value,
                    sellerProduct.ReturnPolicyMode,
                    sellerProduct.ReturnPolicyReturnsAllowed,
                    sellerProduct.ReturnPolicyWindowDays);

            created.Add(ConnectedPoReturnEligibilityBucket.CreateFromReceiptLine(
                buyerOrganizationId,
                sellerOrganizationId,
                purchaseOrder.Id,
                receipt,
                line,
                buyerProductId: poLine?.ProductId ?? line.ProductId,
                supplierProductId: poLine?.SupplierProductId ?? sellerProduct?.Id,
                policy,
                receipt.ReceivedAtUtc));
        }

        if (created.Count > 0)
        {
            await _buckets.AddRangeAsync(created, cancellationToken).ConfigureAwait(false);
        }
    }
}
