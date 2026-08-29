using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;

namespace ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;

public sealed record CreateBuyerProductAndLinkRequest(
    Guid ExposureId,
    string Name,
    string UnitOfMeasure,
    decimal SellingPrice,
    string? Sku = null,
    string? Description = null,
    Guid? CategoryId = null,
    Guid? BrandId = null,
    string? Barcode = null,
    bool? TracksExpiration = null,
    Guid? ClientProductId = null,
    string? UsagePreset = null,
    bool? CanBePurchased = null,
    bool? CanBeSold = null,
    bool? CanBeUsedAsIngredient = null,
    bool? IsProduced = null,
    Guid? PurchaseOrderId = null);

public sealed record CreateBuyerProductAndLinkResultDto(
    BuyerSupplierProductLinkDto Link,
    Guid BuyerProductId,
    string BuyerProductName,
    string? BuyerSku,
    decimal BuyerSellingPrice,
    bool CreatedNewProduct,
    bool AlreadyLinked);

public sealed record BuyerProductMatchCandidateDto(
    Guid ProductId,
    string Name,
    string? Sku,
    string UnitOfMeasure,
    decimal SellingPrice,
    string MatchKind);

public sealed record SuggestBuyerProductMatchesResultDto(
    Guid ExposureId,
    string SupplierName,
    string? SupplierSku,
    string UnitOfMeasureCode,
    decimal PoPrice,
    IReadOnlyList<BuyerProductMatchCandidateDto> Candidates);

/// <summary>
/// Suggestion-only matching of shared supplier products to buyer catalog products.
/// Never auto-links; callers must confirm LinkProduct or CreateBuyerProductAndLink.
/// </summary>
public static class BuyerCatalogMatchSuggestions
{
    public const string ExactSku = "ExactSku";
    public const string ExactNameCompatibleUom = "ExactNameCompatibleUom";
    public const string ExactName = "ExactName";

    public static IReadOnlyList<BuyerProductMatchCandidateDto> Rank(
        string supplierName,
        string? supplierSku,
        string supplierUnitOfMeasureCode,
        IEnumerable<CatalogProduct> activeBuyerProducts)
    {
        var name = NormalizeName(supplierName);
        var sku = NormalizeSku(supplierSku);
        UnitOfMeasures.TryParse(supplierUnitOfMeasureCode, out var supplierUom);

        var ranked = new List<(int Rank, BuyerProductMatchCandidateDto Dto)>();
        foreach (var product in activeBuyerProducts)
        {
            if (product.Status != CatalogProductStatus.Active)
            {
                continue;
            }

            var productSku = product.NormalizedSku;
            var nameMatch = string.Equals(NormalizeName(product.Name), name, StringComparison.Ordinal);
            var skuMatch = sku is not null
                && productSku is not null
                && string.Equals(productSku, sku, StringComparison.Ordinal);
            var uomMatch = product.UnitOfMeasure == supplierUom;

            string? kind = null;
            var rank = 99;
            if (skuMatch)
            {
                kind = ExactSku;
                rank = 0;
            }
            else if (nameMatch && uomMatch)
            {
                kind = ExactNameCompatibleUom;
                rank = 1;
            }
            else if (nameMatch)
            {
                kind = ExactName;
                rank = 2;
            }

            if (kind is null)
            {
                continue;
            }

            ranked.Add((rank, new BuyerProductMatchCandidateDto(
                product.Id.Value,
                product.Name,
                product.Sku,
                UnitOfMeasures.ToCode(product.UnitOfMeasure),
                product.SellingPrice,
                kind)));
        }

        return ranked
            .OrderBy(x => x.Rank)
            .ThenBy(x => x.Dto.Name, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.Dto)
            .Take(25)
            .ToList();
    }

    private static string NormalizeName(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToUpperInvariant();

    private static string? NormalizeSku(string? value)
    {
        var (_, normalized) = CatalogProduct.NormalizeOptionalSku(value);
        return normalized;
    }
}

public sealed class SuggestBuyerProductMatches
{
    private readonly IConnectedSupplierRelationshipRepository _relationships;
    private readonly ISupplierProductExposureRepository _exposures;
    private readonly IConnectedBuyerProductShareRepository _shares;
    private readonly ICatalogProductRepository _products;
    private readonly IPosCommercialAccessAccessor _access;

    public SuggestBuyerProductMatches(
        IConnectedSupplierRelationshipRepository relationships,
        ISupplierProductExposureRepository exposures,
        IConnectedBuyerProductShareRepository shares,
        ICatalogProductRepository products,
        IPosCommercialAccessAccessor access)
    {
        _relationships = relationships;
        _exposures = exposures;
        _shares = shares;
        _products = products;
        _access = access;
    }

    public async Task<ApplicationResult<SuggestBuyerProductMatchesResultDto>> ExecuteAsync(
        Guid orgId,
        Guid relationshipId,
        Guid exposureId,
        CancellationToken ct = default)
    {
        var gate = ConnectedSupplierUseCaseGuard.Access(_access, UtangCapability.ViewPurchasing);
        if (!gate.IsSuccess)
        {
            return ConnectedSupplierUseCaseGuard.Failure<SuggestBuyerProductMatchesResultDto>(
                gate.ErrorCode!, gate.ErrorMessage!);
        }

        var buyer = PosOrganizationId.From(orgId);
        var relationship = await _relationships.GetAsync(ConnectedSupplierRelationshipId.From(relationshipId), ct)
            .ConfigureAwait(false);
        if (relationship is null || relationship.BuyerOrganizationId != buyer)
        {
            return ConnectedSupplierUseCaseGuard.Failure<SuggestBuyerProductMatchesResultDto>(
                ConnectedSupplierErrorCodes.NotFound, "Relationship was not found.");
        }

        if (relationship.Status != ConnectedSupplierRelationshipStatus.Active)
        {
            return ConnectedSupplierUseCaseGuard.Failure<SuggestBuyerProductMatchesResultDto>(
                ConnectedSupplierErrorCodes.RelationshipInactive, "Relationship is not active.");
        }

        var exposure = await _exposures.GetAsync(SupplierProductExposureId.From(exposureId), ct).ConfigureAwait(false);
        if (exposure is null
            || exposure.SupplierOrganizationId != relationship.SupplierOrganizationId
            || !exposure.IsExposed)
        {
            return ConnectedSupplierUseCaseGuard.Failure<SuggestBuyerProductMatchesResultDto>(
                ConnectedSupplierErrorCodes.ExposureNotFound, "Exposure was not found.");
        }

        var share = await _shares.FindAsync(relationship.Id, exposure.ProductId, ct).ConfigureAwait(false);
        if (!ConnectedPoPricing.TryResolveEffectivePrice(
                exposure,
                share,
                relationship.CatalogSharingMode,
                relationship.CustomerDiscountPercent,
                sellingPrice: null,
                out var poPrice,
                out _))
        {
            return ConnectedSupplierUseCaseGuard.Failure<SuggestBuyerProductMatchesResultDto>(
                ConnectedSupplierErrorCodes.ExposureNotFound, "This product is not shared with your business.");
        }

        var candidates = new List<CatalogProduct>();
        var skuNorm = CatalogProduct.NormalizeOptionalSku(exposure.SkuSnapshot).Normalized;
        if (skuNorm is not null)
        {
            var bySku = await _products.FindByNormalizedSkuAsync(buyer, skuNorm, ct).ConfigureAwait(false);
            if (bySku is not null && bySku.Status == CatalogProductStatus.Active)
            {
                candidates.Add(bySku);
            }
        }

        var (byName, _) = await _products.ListAsync(
            buyer,
            new CatalogProductFilter(Status: CatalogProductStatus.Active, Search: exposure.NameSnapshot),
            0,
            50,
            ct).ConfigureAwait(false);
        foreach (var item in byName)
        {
            if (candidates.All(x => x.Id != item.Id))
            {
                candidates.Add(item);
            }
        }

        return ApplicationResult<SuggestBuyerProductMatchesResultDto>.Success(new(
            exposure.Id.Value,
            exposure.NameSnapshot,
            exposure.SkuSnapshot,
            exposure.UnitOfMeasureCode,
            poPrice,
            BuyerCatalogMatchSuggestions.Rank(
                exposure.NameSnapshot,
                exposure.SkuSnapshot,
                exposure.UnitOfMeasureCode,
                candidates)));
    }
}

public sealed class CreateBuyerProductAndLink
{
    private readonly IConnectedSupplierRelationshipRepository _relationships;
    private readonly ISupplierProductExposureRepository _exposures;
    private readonly IConnectedBuyerProductShareRepository _shares;
    private readonly IBuyerSupplierProductLinkRepository _links;
    private readonly ICatalogProductRepository _products;
    private readonly ICatalogProductUnitRepository _units;
    private readonly IProductCategoryRepository _categories;
    private readonly IProductBrandRepository _brands;
    private readonly IPurchaseOrderRepository? _purchaseOrders;
    private readonly IPosUnitOfWork _uow;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly IClock _clock;
    private readonly TimeProvider _time;

    public CreateBuyerProductAndLink(
        IConnectedSupplierRelationshipRepository relationships,
        ISupplierProductExposureRepository exposures,
        IConnectedBuyerProductShareRepository shares,
        IBuyerSupplierProductLinkRepository links,
        ICatalogProductRepository products,
        ICatalogProductUnitRepository units,
        IProductCategoryRepository categories,
        IProductBrandRepository brands,
        IPosUnitOfWork uow,
        IPosCommercialAccessAccessor access,
        IClock clock,
        TimeProvider? time = null,
        IPurchaseOrderRepository? purchaseOrders = null)
    {
        _relationships = relationships;
        _exposures = exposures;
        _shares = shares;
        _links = links;
        _products = products;
        _units = units;
        _categories = categories;
        _brands = brands;
        _purchaseOrders = purchaseOrders;
        _uow = uow;
        _access = access;
        _clock = clock;
        _time = time ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<CreateBuyerProductAndLinkResultDto>> ExecuteAsync(
        Guid orgId,
        Guid relationshipId,
        CreateBuyerProductAndLinkRequest request,
        CancellationToken ct = default)
    {
        var catalogGate = ConnectedSupplierUseCaseGuard.Access(_access, UtangCapability.ManageCatalog);
        if (!catalogGate.IsSuccess)
        {
            return ConnectedSupplierUseCaseGuard.Failure<CreateBuyerProductAndLinkResultDto>(
                catalogGate.ErrorCode!, catalogGate.ErrorMessage!);
        }

        var purchasingGate = ConnectedSupplierUseCaseGuard.Access(_access, UtangCapability.ManagePurchasing);
        if (!purchasingGate.IsSuccess)
        {
            return ConnectedSupplierUseCaseGuard.Failure<CreateBuyerProductAndLinkResultDto>(
                purchasingGate.ErrorCode!, purchasingGate.ErrorMessage!);
        }

        try
        {
            return await _uow.ExecuteInSerializableTransactionAsync(
                ctInner => ExecuteCoreAsync(orgId, relationshipId, request, ctInner),
                ct).ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return ConnectedSupplierUseCaseGuard.Failure<CreateBuyerProductAndLinkResultDto>(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            var recovered = await TryRecoverExistingLinkAsync(orgId, relationshipId, request.ExposureId, ct)
                .ConfigureAwait(false);
            if (recovered is not null)
            {
                return ApplicationResult<CreateBuyerProductAndLinkResultDto>.Success(recovered);
            }

            return ConnectedSupplierUseCaseGuard.Failure<CreateBuyerProductAndLinkResultDto>(ex.ErrorCode, ex.Message);
        }
    }

    private async Task<ApplicationResult<CreateBuyerProductAndLinkResultDto>> ExecuteCoreAsync(
        Guid orgId,
        Guid relationshipId,
        CreateBuyerProductAndLinkRequest request,
        CancellationToken ct)
    {
        var buyer = PosOrganizationId.From(orgId);
        var relationship = await _relationships.GetAsync(ConnectedSupplierRelationshipId.From(relationshipId), ct)
            .ConfigureAwait(false);
        if (relationship is null || relationship.BuyerOrganizationId != buyer
            || relationship.Status != ConnectedSupplierRelationshipStatus.Active)
        {
            return ConnectedSupplierUseCaseGuard.Failure<CreateBuyerProductAndLinkResultDto>(
                ConnectedSupplierErrorCodes.NotFound, "Active relationship was not found.");
        }

        var exposure = await _exposures.GetAsync(SupplierProductExposureId.From(request.ExposureId), ct)
            .ConfigureAwait(false);
        if (exposure is null
            || exposure.SupplierOrganizationId != relationship.SupplierOrganizationId
            || !exposure.IsExposed
            || !exposure.IsOrderable)
        {
            return ConnectedSupplierUseCaseGuard.Failure<CreateBuyerProductAndLinkResultDto>(
                ConnectedSupplierErrorCodes.ExposureNotFound,
                "Shared supplier product is not available for linking.");
        }

        var share = await _shares.FindAsync(relationship.Id, exposure.ProductId, ct).ConfigureAwait(false);
        if (!ConnectedPoPricing.TryResolveEffectivePrice(
                exposure,
                share,
                relationship.CatalogSharingMode,
                relationship.CustomerDiscountPercent,
                sellingPrice: null,
                out var effectivePrice,
                out _))
        {
            return ConnectedSupplierUseCaseGuard.Failure<CreateBuyerProductAndLinkResultDto>(
                ConnectedSupplierErrorCodes.ExposureNotFound, "This product is not shared with your business.");
        }

        var existingBySupplier = await _links.FindBySupplierProductAsync(relationship.Id, exposure.ProductId, ct)
            .ConfigureAwait(false);
        if (existingBySupplier is not null)
        {
            var existingProduct = await _products.GetByIdAsync(buyer, existingBySupplier.BuyerProductId, ct)
                .ConfigureAwait(false);
            return ApplicationResult<CreateBuyerProductAndLinkResultDto>.Success(MapResult(
                existingBySupplier,
                existingProduct,
                createdNewProduct: false,
                alreadyLinked: true));
        }

        if (!UnitOfMeasures.TryParse(exposure.UnitOfMeasureCode, out var supplierUom)
            || !UnitOfMeasures.TryParse(request.UnitOfMeasure, out var buyerUom)
            || supplierUom != buyerUom)
        {
            return ConnectedSupplierUseCaseGuard.Failure<CreateBuyerProductAndLinkResultDto>(
                ApplicationErrorCodes.CatalogBulkValidation,
                "Quick create requires the same unit of measure as the supplier product. Use advanced product setup for conversions.");
        }

        // Buyer SellingPrice is independent of supplier PO price. Never equate them here.
        var staged = await CatalogProductCreateCore.StageAsync(
            _products,
            _units,
            _categories,
            _brands,
            _clock,
            exposures: null,
            orgId,
            request.Name,
            request.UnitOfMeasure,
            request.SellingPrice,
            request.Description,
            request.Sku,
            request.Barcode,
            request.CategoryId,
            request.BrandId,
            request.ClientProductId,
            sellingMode: null,
            tracksExpiration: request.TracksExpiration ?? false,
            expirationWarningDays: null,
            request.CanBePurchased,
            request.CanBeSold,
            request.CanBeUsedAsIngredient,
            request.IsProduced,
            request.UsagePreset,
            unitInputs: null,
            canExposeToConnectedBuyers: false,
            defaultConnectedPoPrice: null,
            ct).ConfigureAwait(false);
        if (!staged.IsSuccess)
        {
            return ConnectedSupplierUseCaseGuard.Failure<CreateBuyerProductAndLinkResultDto>(
                staged.ErrorCode!, staged.ErrorMessage!);
        }

        var product = staged.Value!;
        if (product.CanExposeToConnectedBuyers)
        {
            return ConnectedSupplierUseCaseGuard.Failure<CreateBuyerProductAndLinkResultDto>(
                ApplicationErrorCodes.CatalogBulkValidation,
                "Connected supplier imports must not auto-enable connected buyer sharing.");
        }

        var existingByBuyer = await _links.FindAsync(relationship.Id, product.Id, ct).ConfigureAwait(false);
        if (existingByBuyer is not null)
        {
            await _uow.SaveChangesAsync(ct).ConfigureAwait(false);
            return ApplicationResult<CreateBuyerProductAndLinkResultDto>.Success(MapResult(
                existingByBuyer,
                product,
                createdNewProduct: false,
                alreadyLinked: true));
        }

        var link = BuyerSupplierProductLink.Create(
            relationship.Id,
            buyer,
            relationship.SupplierOrganizationId,
            product.Id,
            exposure,
            _time.GetUtcNow(),
            effectiveOrderPrice: effectivePrice);
        await _links.AddAsync(link, ct).ConfigureAwait(false);
        if (request.PurchaseOrderId is Guid poId && poId != Guid.Empty && _purchaseOrders is not null)
        {
            var po = await _purchaseOrders.GetByIdAsync(buyer, PurchaseOrderId.From(poId), ct)
                .ConfigureAwait(false);
            if (po is not null && po.OrganizationId == buyer)
            {
                po.BindBuyerProductForSupplierProduct(exposure.ProductId, product.Id, _time.GetUtcNow());
                await _purchaseOrders.UpdateAsync(po, ct).ConfigureAwait(false);
            }
        }

        await _uow.SaveChangesAsync(ct).ConfigureAwait(false);

        return ApplicationResult<CreateBuyerProductAndLinkResultDto>.Success(MapResult(
            link,
            product,
            createdNewProduct: true,
            alreadyLinked: false));
    }

    private async Task<CreateBuyerProductAndLinkResultDto?> TryRecoverExistingLinkAsync(
        Guid orgId,
        Guid relationshipId,
        Guid exposureId,
        CancellationToken ct)
    {
        var buyer = PosOrganizationId.From(orgId);
        var relationship = await _relationships.GetAsync(ConnectedSupplierRelationshipId.From(relationshipId), ct)
            .ConfigureAwait(false);
        if (relationship is null || relationship.BuyerOrganizationId != buyer)
        {
            return null;
        }

        var exposure = await _exposures.GetAsync(SupplierProductExposureId.From(exposureId), ct).ConfigureAwait(false);
        if (exposure is null)
        {
            return null;
        }

        var existing = await _links.FindBySupplierProductAsync(relationship.Id, exposure.ProductId, ct)
            .ConfigureAwait(false);
        if (existing is null)
        {
            return null;
        }

        var product = await _products.GetByIdAsync(buyer, existing.BuyerProductId, ct).ConfigureAwait(false);
        return MapResult(existing, product, createdNewProduct: false, alreadyLinked: true);
    }

    private static CreateBuyerProductAndLinkResultDto MapResult(
        BuyerSupplierProductLink link,
        CatalogProduct? product,
        bool createdNewProduct,
        bool alreadyLinked) =>
        new(
            ConnectedSupplierMapper.Map(link),
            link.BuyerProductId.Value,
            product?.Name ?? link.SupplierNameSnapshot,
            product?.Sku,
            product?.SellingPrice ?? 0m,
            createdNewProduct,
            alreadyLinked);
}
