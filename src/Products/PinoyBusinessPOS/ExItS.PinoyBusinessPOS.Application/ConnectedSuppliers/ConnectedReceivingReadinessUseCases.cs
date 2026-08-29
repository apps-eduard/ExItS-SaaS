using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;

namespace ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;

public sealed record ConnectedReceivingReadinessItemDto(
    Guid SupplierProductId,
    string SupplierName,
    string? SupplierSku,
    string? SupplierBarcode,
    string UnitOfMeasureCode,
    decimal PurchaseUnitPrice,
    decimal FulfillmentQty,
    string Status,
    bool CanAutoLink,
    Guid? CandidateBuyerProductId,
    string? CandidateBuyerProductName,
    Guid? LinkedBuyerProductId,
    string? LinkedBuyerProductName,
    bool NameMatched,
    bool SkuMatched,
    bool BarcodeMatched,
    bool UnitCompatible,
    string MatchDetails,
    bool NeedsSetup);

public sealed record ConnectedReceivingReadinessResultDto(
    Guid PurchaseOrderId,
    Guid? ConnectedPurchaseOrderId,
    Guid? RelationshipId,
    bool CanReceive,
    int ReadyCount,
    int NeedsSetupCount,
    IReadOnlyList<ConnectedReceivingReadinessItemDto> Items);

/// <summary>
/// Batch readiness projection for connected PO receiving. Derived from links + classifier; never auto-links.
/// </summary>
public sealed class ClassifyConnectedPurchaseReceivingReadiness
{
    private readonly IPurchaseOrderRepository _orders;
    private readonly IConnectedPurchaseOrderRepository _connectedOrders;
    private readonly IBuyerSupplierProductLinkRepository _links;
    private readonly ICatalogProductRepository _products;
    private readonly IPosCommercialAccessAccessor _access;

    public ClassifyConnectedPurchaseReceivingReadiness(
        IPurchaseOrderRepository orders,
        IConnectedPurchaseOrderRepository connectedOrders,
        IBuyerSupplierProductLinkRepository links,
        ICatalogProductRepository products,
        IPosCommercialAccessAccessor access)
    {
        _orders = orders;
        _connectedOrders = connectedOrders;
        _links = links;
        _products = products;
        _access = access;
    }

    public async Task<ApplicationResult<ConnectedReceivingReadinessResultDto>> ExecuteAsync(
        Guid orgId,
        Guid purchaseOrderId,
        CancellationToken ct = default)
    {
        var gate = ConnectedSupplierUseCaseGuard.Access(_access, UtangCapability.ViewPurchasing);
        if (!gate.IsSuccess)
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectedReceivingReadinessResultDto>(
                gate.ErrorCode!, gate.ErrorMessage!);
        }

        var org = PosOrganizationId.From(orgId);
        var po = await _orders.GetByIdAsync(org, PurchaseOrderId.From(purchaseOrderId), ct).ConfigureAwait(false);
        if (po is null)
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectedReceivingReadinessResultDto>(
                ApplicationErrorCodes.PurchaseOrderNotFound,
                "Purchase order was not found in this organization.");
        }

        var connected = await _connectedOrders
            .GetByBuyerPurchaseOrderAsync(po.Id, ct)
            .ConfigureAwait(false);
        if (connected is null)
        {
            return ApplicationResult<ConnectedReceivingReadinessResultDto>.Success(new(
                po.Id.Value,
                null,
                null,
                CanReceive: true,
                ReadyCount: po.Lines.Count,
                NeedsSetupCount: 0,
                Items: []));
        }

        if (connected.BuyerOrganizationId != org)
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectedReceivingReadinessResultDto>(
                ApplicationErrorCodes.PurchaseOrderNotFound,
                "Purchase order was not found in this organization.");
        }

        var linkList = await _links
            .ListAsync(connected.RelationshipId, org, ct)
            .ConfigureAwait(false);
        var linksBySupplier = linkList
            .Where(x => x.IsActive)
            .GroupBy(x => x.SupplierProductId.Value)
            .ToDictionary(g => g.Key, g => g.First());

        var buyerProducts = await LoadAllActiveBuyerProductsAsync(_products, org, ct)
            .ConfigureAwait(false);
        var buyerById = buyerProducts.ToDictionary(p => p.Id.Value);

        var receivable = connected.Lines
            .Where(l => l.FulfillmentQty > 0m)
            .ToList();
        var supplierIds = receivable.Select(l => l.ProductId).Distinct().ToList();
        var supplierProducts = supplierIds.Count == 0
            ? Array.Empty<CatalogProduct>()
            : await _products.ListByIdsAsync(connected.SupplierOrganizationId, supplierIds, ct)
                .ConfigureAwait(false);
        var supplierBarcodeByProductId = supplierProducts
            .Where(x => !string.IsNullOrWhiteSpace(x.Barcode))
            .ToDictionary(x => x.Id.Value, x => x.Barcode!);

        var items = new List<ConnectedReceivingReadinessItemDto>(receivable.Count);
        foreach (var line in receivable)
        {
            supplierBarcodeByProductId.TryGetValue(line.ProductId.Value, out var supplierBarcode);
            linksBySupplier.TryGetValue(line.ProductId.Value, out var link);
            CatalogProduct? linkedProduct = null;
            if (link is not null)
            {
                buyerById.TryGetValue(link.BuyerProductId.Value, out linkedProduct);
            }

            var poLine = po.Lines.FirstOrDefault(l =>
                l.SupplierProductId == line.ProductId
                || (link is not null && l.ProductId == link.BuyerProductId));

            BuyerSupplierProductMatchClassification classification;
            if (link is not null && linkedProduct is not null && linkedProduct.Status == CatalogProductStatus.Active
                && poLine?.ProductId is not null)
            {
                classification = BuyerSupplierProductMatchClassifier.Classify(
                    line.NameSnapshot,
                    line.SkuSnapshot,
                    supplierBarcode,
                    line.UnitOfMeasureCode,
                    buyerProducts,
                    link.BuyerProductId.Value);
            }
            else if (link is not null && (linkedProduct is null || linkedProduct.Status != CatalogProductStatus.Active))
            {
                classification = new BuyerSupplierProductMatchClassification(
                    BuyerSupplierProductMatchStatus.Conflict,
                    CanAutoLink: false,
                    link.BuyerProductId.Value,
                    new BuyerSupplierProductMatchEvidence(false, false, false, false),
                    "Previously linked product is unavailable.");
            }
            else
            {
                classification = BuyerSupplierProductMatchClassifier.Classify(
                    line.NameSnapshot,
                    line.SkuSnapshot,
                    supplierBarcode,
                    line.UnitOfMeasureCode,
                    buyerProducts,
                    existingLinkedBuyerProductId: null);
            }

            var status = classification.Status switch
            {
                BuyerSupplierProductMatchStatus.AlreadyLinked => "Ready",
                BuyerSupplierProductMatchStatus.Ready => "Ready",
                BuyerSupplierProductMatchStatus.New => "New",
                BuyerSupplierProductMatchStatus.Review => "Review",
                _ => "Conflict"
            };

            var needsSetup = status != "Ready"
                || poLine is null
                || poLine.ProductId is null
                || linkedProduct is null
                || linkedProduct.Status != CatalogProductStatus.Active;

            // Linked + active + PO bound counts as Ready even when classifier says AlreadyLinked.
            if (link is not null
                && linkedProduct is { Status: CatalogProductStatus.Active }
                && poLine?.ProductId is not null)
            {
                status = "Ready";
                needsSetup = false;
            }

            Guid? candidateId = classification.CandidateBuyerProductId;
            string? candidateName = null;
            if (candidateId is Guid cid && buyerById.TryGetValue(cid, out var candidate))
            {
                candidateName = candidate.Name;
            }

            items.Add(new ConnectedReceivingReadinessItemDto(
                line.ProductId.Value,
                line.NameSnapshot,
                line.SkuSnapshot,
                supplierBarcode,
                line.UnitOfMeasureCode,
                line.UnitPriceSnapshot,
                line.FulfillmentQty,
                status,
                classification.CanAutoLink,
                candidateId,
                candidateName,
                link?.BuyerProductId.Value,
                linkedProduct?.Name,
                classification.Evidence.NameMatched,
                classification.Evidence.SkuMatched,
                classification.Evidence.BarcodeMatched,
                classification.Evidence.UnitCompatible,
                classification.MatchDetails,
                needsSetup));
        }

        var needsSetupCount = items.Count(i => i.NeedsSetup);
        var readyCount = items.Count - needsSetupCount;
        var canReceive = connected.CanBuyerReceive && needsSetupCount == 0 && items.Count > 0;

        return ApplicationResult<ConnectedReceivingReadinessResultDto>.Success(new(
            po.Id.Value,
            connected.Id.Value,
            connected.RelationshipId.Value,
            canReceive,
            readyCount,
            needsSetupCount,
            items));
    }

    private static async Task<IReadOnlyList<CatalogProduct>> LoadAllActiveBuyerProductsAsync(
        ICatalogProductRepository products,
        PosOrganizationId buyer,
        CancellationToken ct)
    {
        const int pageSize = 200;
        var all = new List<CatalogProduct>();
        var skip = 0;
        while (true)
        {
            var (page, total) = await products.ListAsync(
                    buyer,
                    new CatalogProductFilter(Status: CatalogProductStatus.Active),
                    skip,
                    pageSize,
                    ct)
                .ConfigureAwait(false);
            all.AddRange(page);
            skip += page.Count;
            if (skip >= total || page.Count == 0)
            {
                break;
            }
        }

        return all;
    }
}
