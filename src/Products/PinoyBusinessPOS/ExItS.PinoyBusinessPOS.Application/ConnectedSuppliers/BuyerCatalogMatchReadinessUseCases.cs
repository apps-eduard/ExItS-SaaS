using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;

public sealed record CatalogProductReadinessItemDto(
    Guid ExposureId,
    Guid SupplierProductId,
    string SupplierName,
    string? SupplierSku,
    string? SupplierBarcode,
    string UnitOfMeasureCode,
    decimal PoPrice,
    string Status,
    bool CanAutoLink,
    Guid? CandidateBuyerProductId,
    string? CandidateBuyerProductName,
    bool NameMatched,
    bool SkuMatched,
    bool BarcodeMatched,
    bool UnitCompatible,
    string MatchDetails,
    Guid? LinkedBuyerProductId);

public sealed record CatalogReadinessResultDto(
    Guid RelationshipId,
    int Ready,
    int New,
    int Review,
    int Conflict,
    IReadOnlyList<CatalogProductReadinessItemDto> Items);

public sealed record AutoLinkExactMatchesResultDto(
    Guid RelationshipId,
    int LinkedNow,
    int AlreadyReady,
    int Review,
    int New,
    int Conflict,
    IReadOnlyList<Guid> LinkedExposureIds);

/// <summary>
/// Read-only classification of shared catalog exposures from the buyer perspective.
/// Never auto-links; use <see cref="AutoLinkExactMatches"/> for linking.
/// </summary>
public sealed class ClassifyCatalogReadiness
{
    private readonly IConnectedSupplierRelationshipRepository _relationships;
    private readonly IConnectedBuyerProductShareRepository _shares;
    private readonly IBuyerSupplierProductLinkRepository _links;
    private readonly ICatalogProductRepository _products;
    private readonly IPosCommercialAccessAccessor _access;

    public ClassifyCatalogReadiness(
        IConnectedSupplierRelationshipRepository relationships,
        IConnectedBuyerProductShareRepository shares,
        IBuyerSupplierProductLinkRepository links,
        ICatalogProductRepository products,
        IPosCommercialAccessAccessor access)
    {
        _relationships = relationships;
        _shares = shares;
        _links = links;
        _products = products;
        _access = access;
    }

    public async Task<ApplicationResult<CatalogReadinessResultDto>> ExecuteAsync(
        Guid orgId,
        Guid relationshipId,
        CancellationToken ct = default)
    {
        var gate = ConnectedSupplierUseCaseGuard.Access(_access, UtangCapability.ViewPurchasing);
        if (!gate.IsSuccess)
        {
            return ConnectedSupplierUseCaseGuard.Failure<CatalogReadinessResultDto>(
                gate.ErrorCode!, gate.ErrorMessage!);
        }

        var context = await BuyerCatalogMatchContext.LoadAsync(
            _relationships,
            _shares,
            _links,
            _products,
            orgId,
            relationshipId,
            ct).ConfigureAwait(false);
        if (!context.IsSuccess)
        {
            return ConnectedSupplierUseCaseGuard.Failure<CatalogReadinessResultDto>(
                context.ErrorCode!, context.ErrorMessage!);
        }

        var items = context.Value!.ClassifyAll();
        return ApplicationResult<CatalogReadinessResultDto>.Success(new(
            relationshipId,
            items.Count(x => x.Status is "Ready" or "AlreadyLinked"),
            items.Count(x => x.Status == "New"),
            items.Count(x => x.Status == "Review"),
            items.Count(x => x.Status == "Conflict"),
            items));
    }
}

/// <summary>
/// Idempotent bulk command: auto-links only safe exact Name+SKU+Barcode+UOM unique matches.
/// Revalidates classification server-side before each link. Queries remain read-only elsewhere.
/// </summary>
public sealed class AutoLinkExactMatches
{
    private readonly IConnectedSupplierRelationshipRepository _relationships;
    private readonly ISupplierProductExposureRepository _exposures;
    private readonly IConnectedBuyerProductShareRepository _shares;
    private readonly IBuyerSupplierProductLinkRepository _links;
    private readonly ICatalogProductRepository _products;
    private readonly ICatalogProductUnitRepository _units;
    private readonly IPosUnitOfWork _uow;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly TimeProvider _clock;

    public AutoLinkExactMatches(
        IConnectedSupplierRelationshipRepository relationships,
        ISupplierProductExposureRepository exposures,
        IConnectedBuyerProductShareRepository shares,
        IBuyerSupplierProductLinkRepository links,
        ICatalogProductRepository products,
        ICatalogProductUnitRepository units,
        IPosUnitOfWork uow,
        IPosCommercialAccessAccessor access,
        TimeProvider? clock = null)
    {
        _relationships = relationships;
        _exposures = exposures;
        _shares = shares;
        _links = links;
        _products = products;
        _units = units;
        _uow = uow;
        _access = access;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<AutoLinkExactMatchesResultDto>> ExecuteAsync(
        Guid orgId,
        Guid relationshipId,
        CancellationToken ct = default)
    {
        var gate = ConnectedSupplierUseCaseGuard.Access(_access, UtangCapability.ManagePurchasing);
        if (!gate.IsSuccess)
        {
            return ConnectedSupplierUseCaseGuard.Failure<AutoLinkExactMatchesResultDto>(
                gate.ErrorCode!, gate.ErrorMessage!);
        }

        return await _uow.ExecuteInSerializableTransactionAsync(
            ctInner => ExecuteCoreAsync(orgId, relationshipId, ctInner),
            ct).ConfigureAwait(false);
    }

    private async Task<ApplicationResult<AutoLinkExactMatchesResultDto>> ExecuteCoreAsync(
        Guid orgId,
        Guid relationshipId,
        CancellationToken ct)
    {
        var context = await BuyerCatalogMatchContext.LoadAsync(
            _relationships,
            _shares,
            _links,
            _products,
            orgId,
            relationshipId,
            ct).ConfigureAwait(false);
        if (!context.IsSuccess)
        {
            return ConnectedSupplierUseCaseGuard.Failure<AutoLinkExactMatchesResultDto>(
                context.ErrorCode!, context.ErrorMessage!);
        }

        var loaded = context.Value!;
        var linkProduct = new LinkProduct(
            _relationships,
            _exposures,
            _links,
            _products,
            _units,
            _uow,
            _access,
            _shares,
            _clock);

        var linkedNow = 0;
        var alreadyReady = 0;
        var review = 0;
        var @new = 0;
        var conflict = 0;
        var linkedExposureIds = new List<Guid>();

        foreach (var row in loaded.Rows)
        {
            var classification = loaded.ClassifyRow(row);

            // Revalidate: only auto-link when still safe exact unique.
            if (classification.Status == BuyerSupplierProductMatchStatus.AlreadyLinked
                || (classification.Status == BuyerSupplierProductMatchStatus.Ready
                    && !classification.CanAutoLink))
            {
                alreadyReady++;
                continue;
            }

            if (classification.CanAutoLink
                && classification.Status == BuyerSupplierProductMatchStatus.Ready
                && classification.CandidateBuyerProductId is Guid buyerProductId)
            {
                var linkResult = await linkProduct.ExecuteAsync(
                    orgId,
                    relationshipId,
                    new LinkProductRequest(buyerProductId, row.Exposure.Id.Value),
                    ct).ConfigureAwait(false);

                if (!linkResult.IsSuccess)
                {
                    // Race / inconsistent link: re-classify after failed attempt.
                    var existing = await _links.FindBySupplierProductAsync(
                        loaded.Relationship.Id,
                        row.Exposure.ProductId,
                        ct).ConfigureAwait(false);
                    if (existing is not null)
                    {
                        alreadyReady++;
                        continue;
                    }

                    conflict++;
                    continue;
                }

                // Idempotent success: distinguish newly created vs already present via link list growth.
                // LinkProduct returns success for both new and existing same-product links.
                var wasAlready = loaded.LinksBySupplierProductId.ContainsKey(row.Exposure.ProductId.Value);
                if (wasAlready)
                {
                    alreadyReady++;
                }
                else
                {
                    linkedNow++;
                    linkedExposureIds.Add(row.Exposure.Id.Value);
                    loaded.LinksBySupplierProductId[row.Exposure.ProductId.Value] = buyerProductId;
                }

                continue;
            }

            switch (BuyerSupplierProductMatchClassifier.ToReadinessBucket(classification.Status))
            {
                case BuyerSupplierProductMatchStatus.Review:
                    review++;
                    break;
                case BuyerSupplierProductMatchStatus.Conflict:
                    conflict++;
                    break;
                case BuyerSupplierProductMatchStatus.New:
                    @new++;
                    break;
                case BuyerSupplierProductMatchStatus.Ready:
                    alreadyReady++;
                    break;
            }
        }

        return ApplicationResult<AutoLinkExactMatchesResultDto>.Success(new(
            relationshipId,
            linkedNow,
            alreadyReady,
            review,
            @new,
            conflict,
            linkedExposureIds));
    }
}

internal sealed class BuyerCatalogMatchContext
{
    private const int PageSize = 200;

    private BuyerCatalogMatchContext(
        ConnectedSupplierRelationship relationship,
        IReadOnlyList<SharedCatalogRow> rows,
        IReadOnlyList<CatalogProduct> activeBuyerProducts,
        Dictionary<Guid, Guid> linksBySupplierProductId,
        IReadOnlyDictionary<Guid, CatalogProduct> buyerProductsById)
    {
        Relationship = relationship;
        Rows = rows;
        ActiveBuyerProducts = activeBuyerProducts;
        LinksBySupplierProductId = linksBySupplierProductId;
        BuyerProductsById = buyerProductsById;
    }

    public ConnectedSupplierRelationship Relationship { get; }
    public IReadOnlyList<SharedCatalogRow> Rows { get; }
    public IReadOnlyList<CatalogProduct> ActiveBuyerProducts { get; }
    public Dictionary<Guid, Guid> LinksBySupplierProductId { get; }
    public IReadOnlyDictionary<Guid, CatalogProduct> BuyerProductsById { get; }

    public static async Task<ApplicationResult<BuyerCatalogMatchContext>> LoadAsync(
        IConnectedSupplierRelationshipRepository relationships,
        IConnectedBuyerProductShareRepository shares,
        IBuyerSupplierProductLinkRepository links,
        ICatalogProductRepository products,
        Guid orgId,
        Guid relationshipId,
        CancellationToken ct)
    {
        var buyer = PosOrganizationId.From(orgId);
        var relationship = await relationships.GetAsync(ConnectedSupplierRelationshipId.From(relationshipId), ct)
            .ConfigureAwait(false);
        if (relationship is null || relationship.BuyerOrganizationId != buyer)
        {
            return ConnectedSupplierUseCaseGuard.Failure<BuyerCatalogMatchContext>(
                ConnectedSupplierErrorCodes.NotFound, "Relationship was not found.");
        }

        if (relationship.Status != ConnectedSupplierRelationshipStatus.Active)
        {
            return ConnectedSupplierUseCaseGuard.Failure<BuyerCatalogMatchContext>(
                ConnectedSupplierErrorCodes.RelationshipInactive, "Relationship is not active.");
        }

        var exposures = new List<SupplierProductExposure>();
        var shareList = new List<ConnectedBuyerProductShare>();
        var skip = 0;
        while (true)
        {
            var (pageExposures, pageShares, total) = await shares.SearchSharedCatalogAsync(
                relationship.Id,
                relationship.SupplierOrganizationId,
                query: null,
                category: null,
                skip,
                PageSize,
                ct).ConfigureAwait(false);
            exposures.AddRange(pageExposures);
            shareList.AddRange(pageShares);
            skip += pageExposures.Count;
            if (skip >= total || pageExposures.Count == 0)
            {
                break;
            }
        }

        var activeBuyerProducts = await LoadAllActiveBuyerProductsAsync(products, buyer, ct)
            .ConfigureAwait(false);
        var buyerById = activeBuyerProducts.ToDictionary(x => x.Id.Value);

        var existingLinks = await links.ListAsync(relationship.Id, buyer, ct).ConfigureAwait(false);
        var linksBySupplier = existingLinks
            .Where(x => x.IsActive)
            .GroupBy(x => x.SupplierProductId.Value)
            .ToDictionary(g => g.Key, g => g.First().BuyerProductId.Value);

        var supplierProductIds = exposures.Select(x => x.ProductId).Distinct().ToList();
        var supplierProducts = supplierProductIds.Count == 0
            ? []
            : await products.ListByIdsAsync(relationship.SupplierOrganizationId, supplierProductIds, ct)
                .ConfigureAwait(false);
        var supplierBarcodeByProductId = supplierProducts
            .Where(x => !string.IsNullOrWhiteSpace(x.Barcode))
            .ToDictionary(x => x.Id.Value, x => x.Barcode!);

        var sharesByProduct = shareList
            .GroupBy(x => x.SupplierProductId.Value)
            .ToDictionary(g => g.Key, g => g.First());

        var rows = new List<SharedCatalogRow>();
        foreach (var exposure in exposures)
        {
            sharesByProduct.TryGetValue(exposure.ProductId.Value, out var share);
            if (!ConnectedPoPricing.TryResolveEffectivePrice(
                    exposure,
                    share,
                    relationship.CatalogSharingMode,
                    relationship.CustomerDiscountPercent,
                    sellingPrice: null,
                    out var poPrice,
                    out _))
            {
                continue;
            }

            supplierBarcodeByProductId.TryGetValue(exposure.ProductId.Value, out var barcode);
            rows.Add(new SharedCatalogRow(exposure, poPrice, barcode));
        }

        return ApplicationResult<BuyerCatalogMatchContext>.Success(new(
            relationship,
            rows,
            activeBuyerProducts,
            linksBySupplier,
            buyerById));
    }

    public IReadOnlyList<CatalogProductReadinessItemDto> ClassifyAll() =>
        Rows.Select(ClassifyRowToDto).ToList();

    public BuyerSupplierProductMatchClassification ClassifyRow(SharedCatalogRow row)
    {
        LinksBySupplierProductId.TryGetValue(row.Exposure.ProductId.Value, out var linkedBuyerId);
        return BuyerSupplierProductMatchClassifier.Classify(
            row.Exposure.NameSnapshot,
            row.Exposure.SkuSnapshot,
            row.SupplierBarcode,
            row.Exposure.UnitOfMeasureCode,
            ActiveBuyerProducts,
            linkedBuyerId == Guid.Empty ? null : linkedBuyerId);
    }

    private CatalogProductReadinessItemDto ClassifyRowToDto(SharedCatalogRow row)
    {
        var classification = ClassifyRow(row);
        string? candidateName = null;
        if (classification.CandidateBuyerProductId is Guid candidateId
            && BuyerProductsById.TryGetValue(candidateId, out var candidate))
        {
            candidateName = candidate.Name;
        }

        LinksBySupplierProductId.TryGetValue(row.Exposure.ProductId.Value, out var linkedId);
        var status = classification.Status == BuyerSupplierProductMatchStatus.AlreadyLinked
            ? nameof(BuyerSupplierProductMatchStatus.AlreadyLinked)
            : classification.Status.ToString();

        return new CatalogProductReadinessItemDto(
            row.Exposure.Id.Value,
            row.Exposure.ProductId.Value,
            row.Exposure.NameSnapshot,
            row.Exposure.SkuSnapshot,
            row.SupplierBarcode,
            row.Exposure.UnitOfMeasureCode,
            row.PoPrice,
            status,
            classification.CanAutoLink,
            classification.CandidateBuyerProductId,
            candidateName,
            classification.Evidence.NameMatched,
            classification.Evidence.SkuMatched,
            classification.Evidence.BarcodeMatched,
            classification.Evidence.UnitCompatible,
            classification.MatchDetails,
            linkedId == Guid.Empty ? null : linkedId);
    }

    private static async Task<IReadOnlyList<CatalogProduct>> LoadAllActiveBuyerProductsAsync(
        ICatalogProductRepository products,
        PosOrganizationId buyer,
        CancellationToken ct)
    {
        var filter = new CatalogProductFilter(Status: CatalogProductStatus.Active);
        var all = new List<CatalogProduct>();
        var skip = 0;
        while (true)
        {
            var (page, total) = await products.ListAsync(buyer, filter, skip, PageSize, ct)
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

    internal sealed record SharedCatalogRow(
        SupplierProductExposure Exposure,
        decimal PoPrice,
        string? SupplierBarcode);
}
