using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;

public sealed record BuyerProductShareCategoryFacetDto(string? CategoryName, int Count);

public sealed record BuyerProductShareQueryResultDto(
    IReadOnlyList<ConnectedBuyerProductShareDto> Items,
    int MatchingCount,
    int EligibleCount,
    int SharedCount,
    int Page,
    int PageSize,
    IReadOnlyList<BuyerProductShareCategoryFacetDto> Categories);

public sealed record BulkBuyerProductShareMutationRequest(
    string Operation,
    IReadOnlyList<Guid>? ProductIds = null,
    bool SelectAllMatching = false,
    string? Query = null,
    string? Category = null,
    string? ShareFilter = null,
    IReadOnlyDictionary<Guid, decimal>? EstablishDefaultPoPrices = null);

public sealed record MissingDefaultPoProductDto(
    Guid ProductId,
    string Name,
    decimal SellingPrice);

public sealed record BulkBuyerProductShareMutationResultDto(
    int AffectedCount,
    IReadOnlyList<MissingDefaultPoProductDto>? NeedsDefaultPo = null);

public sealed record BulkBuyerPricingRequest(
    string Mode,
    IReadOnlyList<Guid>? ProductIds = null,
    bool SelectAllMatching = false,
    string? Query = null,
    string? Category = null,
    string? ShareFilter = null,
    decimal? Percent = null,
    decimal? Amount = null,
    decimal? FixedPrice = null);

public sealed record BuyerPricePreviewItemDto(
    Guid SupplierProductId,
    string Name,
    decimal DefaultPoPrice,
    decimal? CurrentBuyerPrice,
    decimal? ProposedBuyerPrice,
    decimal ProposedEffectivePrice);

public sealed record BulkBuyerPricingPreviewDto(
    int AffectedCount,
    bool Truncated,
    IReadOnlyList<BuyerPricePreviewItemDto> Items,
    IReadOnlyList<MissingDefaultPoProductDto>? NeedsDefaultPo = null);

public sealed class QueryBuyerProductShares
{
    private readonly IConnectedSupplierRelationshipRepository _relationships;
    private readonly IConnectedBuyerProductShareRepository _shares;
    private readonly IPosCommercialAccessAccessor _access;

    public QueryBuyerProductShares(
        IConnectedSupplierRelationshipRepository relationships,
        IConnectedBuyerProductShareRepository shares,
        IPosCommercialAccessAccessor access)
    {
        _relationships = relationships;
        _shares = shares;
        _access = access;
    }

    public async Task<ApplicationResult<BuyerProductShareQueryResultDto>> ExecuteAsync(
        Guid orgId,
        Guid relationshipId,
        string? query,
        string? category,
        string? shareFilter,
        int? page,
        int? pageSize,
        CancellationToken ct = default)
    {
        var gate = ConnectedSupplierUseCaseGuard.Access(_access, UtangCapability.ViewSuppliers);
        if (!gate.IsSuccess)
        {
            return ConnectedSupplierUseCaseGuard.Failure<BuyerProductShareQueryResultDto>(gate.ErrorCode!, gate.ErrorMessage!);
        }

        var relationship = await _relationships.GetAsync(ConnectedSupplierRelationshipId.From(relationshipId), ct)
            .ConfigureAwait(false);
        var supplier = PosOrganizationId.From(orgId);
        if (relationship is null || relationship.SupplierOrganizationId != supplier)
        {
            return ConnectedSupplierUseCaseGuard.Failure<BuyerProductShareQueryResultDto>(
                ConnectedSupplierErrorCodes.NotFound, "Relationship was not found.");
        }

        var (skip, take) = PosPagination.Normalize(page, pageSize);
        var pageNumber = Math.Max(page ?? 1, 1);
        var result = await _shares.SearchForSupplierManagementAsync(
            relationship.Id, supplier, query, category, shareFilter, skip, take, idsOnly: false, ct)
            .ConfigureAwait(false);

        var items = new List<ConnectedBuyerProductShareDto>(result.Rows.Count);
        foreach (var row in result.Rows)
        {
            if (row.Share is null)
            {
                items.Add(ConnectedSupplierMapper.MapUnshared(
                    relationship, supplier, row.Product, row.Exposure, row.CategoryName));
            }
            else
            {
                items.Add(ConnectedSupplierMapper.Map(row.Share, row.Exposure, row.Product, row.CategoryName));
            }
        }

        return ApplicationResult<BuyerProductShareQueryResultDto>.Success(new(
            items,
            result.MatchingCount,
            result.EligibleCount,
            result.SharedCount,
            pageNumber,
            take,
            result.CategoryFacets
                .Select(x => new BuyerProductShareCategoryFacetDto(x.CategoryName, x.Count))
                .ToList()));
    }
}

public sealed class BulkMutateBuyerProductShares
{
    private readonly IConnectedSupplierRelationshipRepository _relationships;
    private readonly IConnectedBuyerProductShareRepository _shares;
    private readonly ICatalogProductRepository _products;
    private readonly SetBuyerProductShares _setShares;
    private readonly IPosCommercialAccessAccessor _access;

    public BulkMutateBuyerProductShares(
        IConnectedSupplierRelationshipRepository relationships,
        IConnectedBuyerProductShareRepository shares,
        ICatalogProductRepository products,
        SetBuyerProductShares setShares,
        IPosCommercialAccessAccessor access)
    {
        _relationships = relationships;
        _shares = shares;
        _products = products;
        _setShares = setShares;
        _access = access;
    }

    public async Task<ApplicationResult<BulkBuyerProductShareMutationResultDto>> ExecuteAsync(
        Guid orgId,
        Guid relationshipId,
        BulkBuyerProductShareMutationRequest request,
        CancellationToken ct = default)
    {
        var gate = ConnectedSupplierUseCaseGuard.Access(_access, UtangCapability.ManageSuppliers);
        if (!gate.IsSuccess)
        {
            return ConnectedSupplierUseCaseGuard.Failure<BulkBuyerProductShareMutationResultDto>(
                gate.ErrorCode!, gate.ErrorMessage!);
        }

        var share = string.Equals(request.Operation, "share", StringComparison.OrdinalIgnoreCase);
        var unshare = string.Equals(request.Operation, "unshare", StringComparison.OrdinalIgnoreCase);
        if (!share && !unshare)
        {
            return ConnectedSupplierUseCaseGuard.Failure<BulkBuyerProductShareMutationResultDto>(
                ConnectedSupplierErrorCodes.BulkValidation, "Operation must be Share or Unshare.");
        }

        var resolve = await ResolveTargetProductIdsAsync(orgId, relationshipId, request.ProductIds,
            request.SelectAllMatching, request.Query, request.Category, request.ShareFilter, ct)
            .ConfigureAwait(false);
        if (!resolve.IsSuccess)
        {
            return ConnectedSupplierUseCaseGuard.Failure<BulkBuyerProductShareMutationResultDto>(
                resolve.ErrorCode!, resolve.ErrorMessage!);
        }

        var productIds = resolve.Value!;
        if (productIds.Count == 0)
        {
            return ApplicationResult<BulkBuyerProductShareMutationResultDto>.Success(new(0));
        }

        var relationship = await _relationships.GetAsync(ConnectedSupplierRelationshipId.From(relationshipId), ct)
            .ConfigureAwait(false);
        var supplier = PosOrganizationId.From(orgId);
        var items = new List<SetBuyerProductShareItem>();
        var needsDefaultPo = new List<MissingDefaultPoProductDto>();

        foreach (var productId in productIds)
        {
            var product = await _products.GetByIdAsync(supplier, CatalogProductId.From(productId), ct)
                .ConfigureAwait(false);
            if (product is null
                || product.OrganizationId != supplier
                || product.Status != CatalogProductStatus.Active)
            {
                return ConnectedSupplierUseCaseGuard.Failure<BulkBuyerProductShareMutationResultDto>(
                    ConnectedSupplierErrorCodes.NotFound, "Product was not found.");
            }

            if (share && product.IsBlockedFromConnectedBuyers)
            {
                continue;
            }

            var existing = await _shares.FindAsync(relationship!.Id, CatalogProductId.From(productId), ct)
                .ConfigureAwait(false);
            if (share)
            {
                decimal? establish = null;
                if (product.DefaultConnectedPoPrice is null)
                {
                    if (request.EstablishDefaultPoPrices is not null
                        && request.EstablishDefaultPoPrices.TryGetValue(productId, out var price))
                    {
                        establish = price;
                    }
                    else
                    {
                        needsDefaultPo.Add(new(productId, product.Name, product.SellingPrice));
                        continue;
                    }
                }

                items.Add(new(productId, true, existing?.BuyerSpecificPoPrice, establish));
            }
            else
            {
                items.Add(new(productId, false, null));
            }
        }

        if (share && needsDefaultPo.Count > 0)
        {
            return ApplicationResult<BulkBuyerProductShareMutationResultDto>.Success(
                new(0, needsDefaultPo));
        }

        if (items.Count == 0)
        {
            return ApplicationResult<BulkBuyerProductShareMutationResultDto>.Success(new(0));
        }

        var result = await _setShares.ExecuteAsync(orgId, relationshipId, items, ct).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return ConnectedSupplierUseCaseGuard.Failure<BulkBuyerProductShareMutationResultDto>(
                result.ErrorCode!, result.ErrorMessage!);
        }

        return ApplicationResult<BulkBuyerProductShareMutationResultDto>.Success(new(result.Value!.Count));
    }

    private async Task<ApplicationResult<IReadOnlyList<Guid>>> ResolveTargetProductIdsAsync(
        Guid orgId,
        Guid relationshipId,
        IReadOnlyList<Guid>? productIds,
        bool selectAllMatching,
        string? query,
        string? category,
        string? shareFilter,
        CancellationToken ct)
    {
        var relationship = await _relationships.GetAsync(ConnectedSupplierRelationshipId.From(relationshipId), ct)
            .ConfigureAwait(false);
        var supplier = PosOrganizationId.From(orgId);
        if (relationship is null
            || relationship.SupplierOrganizationId != supplier
            || relationship.Status != ConnectedSupplierRelationshipStatus.Active)
        {
            return ConnectedSupplierUseCaseGuard.Failure<IReadOnlyList<Guid>>(
                ConnectedSupplierErrorCodes.NotFound, "Active relationship was not found.");
        }

        if (selectAllMatching)
        {
            var page = await _shares.SearchForSupplierManagementAsync(
                relationship.Id, supplier, query, category, shareFilter, 0,
                BuyerProductShareBulkPricing.MaxSelectAllMatching + 1, idsOnly: true, ct)
                .ConfigureAwait(false);
            if (page.MatchingProductIds.Count > BuyerProductShareBulkPricing.MaxSelectAllMatching)
            {
                return ConnectedSupplierUseCaseGuard.Failure<IReadOnlyList<Guid>>(
                    ConnectedSupplierErrorCodes.BulkValidation,
                    $"Too many matching products (max {BuyerProductShareBulkPricing.MaxSelectAllMatching}). Narrow filters first.");
            }

            return ApplicationResult<IReadOnlyList<Guid>>.Success(page.MatchingProductIds);
        }

        var ids = (productIds ?? [])
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList();
        if (ids.Count == 0)
        {
            return ConnectedSupplierUseCaseGuard.Failure<IReadOnlyList<Guid>>(
                ConnectedSupplierErrorCodes.BulkValidation, "Select at least one product.");
        }

        if (ids.Count > BuyerProductShareBulkPricing.MaxBulkProductIds)
        {
            return ConnectedSupplierUseCaseGuard.Failure<IReadOnlyList<Guid>>(
                ConnectedSupplierErrorCodes.BulkValidation,
                $"Too many products selected (max {BuyerProductShareBulkPricing.MaxBulkProductIds}). Use select-all matching.");
        }

        foreach (var id in ids)
        {
            var product = await _products.GetByIdAsync(supplier, CatalogProductId.From(id), ct)
                .ConfigureAwait(false);
            if (product is null
                || product.OrganizationId != supplier
                || product.Status != CatalogProductStatus.Active)
            {
                return ConnectedSupplierUseCaseGuard.Failure<IReadOnlyList<Guid>>(
                    ConnectedSupplierErrorCodes.NotFound, "Product was not found.");
            }
        }

        return ApplicationResult<IReadOnlyList<Guid>>.Success(ids);
    }
}

public sealed class PreviewBuyerProductPricing
{
    private readonly IConnectedSupplierRelationshipRepository _relationships;
    private readonly IConnectedBuyerProductShareRepository _shares;
    private readonly ICatalogProductRepository _products;
    private readonly IPosCommercialAccessAccessor _access;

    public PreviewBuyerProductPricing(
        IConnectedSupplierRelationshipRepository relationships,
        IConnectedBuyerProductShareRepository shares,
        ICatalogProductRepository products,
        IPosCommercialAccessAccessor access)
    {
        _relationships = relationships;
        _shares = shares;
        _products = products;
        _access = access;
    }

    public Task<ApplicationResult<BulkBuyerPricingPreviewDto>> ExecuteAsync(
        Guid orgId,
        Guid relationshipId,
        BulkBuyerPricingRequest request,
        CancellationToken ct = default) =>
        BuildPreviewOrApplyAsync(orgId, relationshipId, request, apply: false, ct);

    internal async Task<ApplicationResult<BulkBuyerPricingPreviewDto>> BuildPreviewOrApplyAsync(
        Guid orgId,
        Guid relationshipId,
        BulkBuyerPricingRequest request,
        bool apply,
        CancellationToken ct)
    {
        var gate = ConnectedSupplierUseCaseGuard.Access(
            _access, apply ? UtangCapability.ManageSuppliers : UtangCapability.ViewSuppliers);
        if (!gate.IsSuccess)
        {
            return ConnectedSupplierUseCaseGuard.Failure<BulkBuyerPricingPreviewDto>(
                gate.ErrorCode!, gate.ErrorMessage!);
        }

        if (!Enum.TryParse<BulkBuyerPricingMode>(request.Mode, ignoreCase: true, out var mode))
        {
            return ConnectedSupplierUseCaseGuard.Failure<BulkBuyerPricingPreviewDto>(
                ConnectedSupplierErrorCodes.BulkValidation, "Unsupported pricing mode.");
        }

        var relationship = await _relationships.GetAsync(ConnectedSupplierRelationshipId.From(relationshipId), ct)
            .ConfigureAwait(false);
        var supplier = PosOrganizationId.From(orgId);
        if (relationship is null
            || relationship.SupplierOrganizationId != supplier
            || (apply && relationship.Status != ConnectedSupplierRelationshipStatus.Active))
        {
            return ConnectedSupplierUseCaseGuard.Failure<BulkBuyerPricingPreviewDto>(
                ConnectedSupplierErrorCodes.NotFound,
                apply ? "Active relationship was not found." : "Relationship was not found.");
        }

        IReadOnlyList<Guid> productIds;
        if (request.SelectAllMatching)
        {
            var page = await _shares.SearchForSupplierManagementAsync(
                relationship.Id, supplier, request.Query, request.Category, request.ShareFilter, 0,
                BuyerProductShareBulkPricing.MaxSelectAllMatching + 1, idsOnly: true, ct)
                .ConfigureAwait(false);
            if (page.MatchingProductIds.Count > BuyerProductShareBulkPricing.MaxSelectAllMatching)
            {
                return ConnectedSupplierUseCaseGuard.Failure<BulkBuyerPricingPreviewDto>(
                    ConnectedSupplierErrorCodes.BulkValidation,
                    $"Too many matching products (max {BuyerProductShareBulkPricing.MaxSelectAllMatching}). Narrow filters first.");
            }

            productIds = page.MatchingProductIds;
        }
        else
        {
            productIds = (request.ProductIds ?? []).Where(x => x != Guid.Empty).Distinct().ToList();
            if (productIds.Count == 0)
            {
                return ConnectedSupplierUseCaseGuard.Failure<BulkBuyerPricingPreviewDto>(
                    ConnectedSupplierErrorCodes.BulkValidation, "Select at least one product.");
            }

            if (productIds.Count > BuyerProductShareBulkPricing.MaxBulkProductIds)
            {
                return ConnectedSupplierUseCaseGuard.Failure<BulkBuyerPricingPreviewDto>(
                    ConnectedSupplierErrorCodes.BulkValidation,
                    $"Too many products selected (max {BuyerProductShareBulkPricing.MaxBulkProductIds}).");
            }
        }

        var products = new List<CatalogProduct>(productIds.Count);
        var needsDefaultPo = new List<MissingDefaultPoProductDto>();
        foreach (var productId in productIds)
        {
            var product = await _products.GetByIdAsync(supplier, CatalogProductId.From(productId), ct)
                .ConfigureAwait(false);
            if (product is null
                || product.OrganizationId != supplier
                || product.Status != CatalogProductStatus.Active)
            {
                return ConnectedSupplierUseCaseGuard.Failure<BulkBuyerPricingPreviewDto>(
                    ConnectedSupplierErrorCodes.BulkValidation,
                    "One or more selected products were not found or are not active.");
            }

            if (product.DefaultConnectedPoPrice is null)
            {
                needsDefaultPo.Add(new(product.Id.Value, product.Name, product.SellingPrice));
                continue;
            }

            products.Add(product);
        }

        if (needsDefaultPo.Count > 0)
        {
            // Preview stays non-mutating: return the missing list so UI can collect Default PO prices.
            if (!apply)
            {
                return ApplicationResult<BulkBuyerPricingPreviewDto>.Success(new(
                    0,
                    false,
                    Array.Empty<BuyerPricePreviewItemDto>(),
                    needsDefaultPo));
            }

            return ConnectedSupplierUseCaseGuard.Failure<BulkBuyerPricingPreviewDto>(
                ConnectedSupplierErrorCodes.MissingDefaultPo,
                "A Default PO price is required before buyer pricing can be set.");
        }

        var previewItems = new List<BuyerPricePreviewItemDto>();
        foreach (var product in products)
        {
            var baseline = product.DefaultConnectedPoPrice!.Value;
            if (!BuyerProductShareBulkPricing.TryComputeBuyerPrice(
                    mode, baseline, request.Percent, request.Amount, request.FixedPrice,
                    out var proposed, out var error))
            {
                return ConnectedSupplierUseCaseGuard.Failure<BulkBuyerPricingPreviewDto>(
                    ConnectedSupplierErrorCodes.BulkValidation,
                    $"{product.Name}: {error}");
            }

            var share = await _shares.FindAsync(relationship.Id, product.Id, ct).ConfigureAwait(false);
            var current = share?.BuyerSpecificPoPrice;
            var effective = proposed ?? baseline;
            if (previewItems.Count < BuyerProductShareBulkPricing.PreviewItemLimit)
            {
                previewItems.Add(new(
                    product.Id.Value,
                    product.Name,
                    baseline,
                    current,
                    proposed,
                    effective));
            }
        }

        return ApplicationResult<BulkBuyerPricingPreviewDto>.Success(new(
            products.Count,
            products.Count > BuyerProductShareBulkPricing.PreviewItemLimit,
            previewItems));
    }
}

public sealed class ApplyBuyerProductPricing
{
    private readonly PreviewBuyerProductPricing _preview;
    private readonly SetBuyerProductShares _setShares;
    private readonly IConnectedSupplierRelationshipRepository _relationships;
    private readonly IConnectedBuyerProductShareRepository _shares;
    private readonly ICatalogProductRepository _products;
    private readonly IPosCommercialAccessAccessor _access;

    public ApplyBuyerProductPricing(
        PreviewBuyerProductPricing preview,
        SetBuyerProductShares setShares,
        IConnectedSupplierRelationshipRepository relationships,
        IConnectedBuyerProductShareRepository shares,
        ICatalogProductRepository products,
        IPosCommercialAccessAccessor access)
    {
        _preview = preview;
        _setShares = setShares;
        _relationships = relationships;
        _shares = shares;
        _products = products;
        _access = access;
    }

    public async Task<ApplicationResult<BulkBuyerProductShareMutationResultDto>> ExecuteAsync(
        Guid orgId,
        Guid relationshipId,
        BulkBuyerPricingRequest request,
        CancellationToken ct = default)
    {
        var preview = await _preview.BuildPreviewOrApplyAsync(orgId, relationshipId, request, apply: true, ct)
            .ConfigureAwait(false);
        if (!preview.IsSuccess)
        {
            return ConnectedSupplierUseCaseGuard.Failure<BulkBuyerProductShareMutationResultDto>(
                preview.ErrorCode!, preview.ErrorMessage!);
        }

        if (!Enum.TryParse<BulkBuyerPricingMode>(request.Mode, ignoreCase: true, out var mode))
        {
            return ConnectedSupplierUseCaseGuard.Failure<BulkBuyerProductShareMutationResultDto>(
                ConnectedSupplierErrorCodes.BulkValidation, "Unsupported pricing mode.");
        }

        var relationship = await _relationships.GetAsync(ConnectedSupplierRelationshipId.From(relationshipId), ct)
            .ConfigureAwait(false);
        var supplier = PosOrganizationId.From(orgId);
        IReadOnlyList<Guid> productIds;
        if (request.SelectAllMatching)
        {
            var page = await _shares.SearchForSupplierManagementAsync(
                relationship!.Id, supplier, request.Query, request.Category, request.ShareFilter, 0,
                BuyerProductShareBulkPricing.MaxSelectAllMatching, idsOnly: true, ct)
                .ConfigureAwait(false);
            productIds = page.MatchingProductIds;
        }
        else
        {
            productIds = (request.ProductIds ?? []).Where(x => x != Guid.Empty).Distinct().ToList();
        }

        var items = new List<SetBuyerProductShareItem>();
        foreach (var productId in productIds)
        {
            var product = await _products.GetByIdAsync(supplier, CatalogProductId.From(productId), ct)
                .ConfigureAwait(false);
            if (product is null
                || product.OrganizationId != supplier
                || product.Status != CatalogProductStatus.Active
                || product.DefaultConnectedPoPrice is null)
            {
                return ConnectedSupplierUseCaseGuard.Failure<BulkBuyerProductShareMutationResultDto>(
                    ConnectedSupplierErrorCodes.MissingDefaultPo,
                    "A Default PO price is required before buyer pricing can be set.");
            }

            if (!BuyerProductShareBulkPricing.TryComputeBuyerPrice(
                    mode, product.DefaultConnectedPoPrice.Value, request.Percent, request.Amount, request.FixedPrice,
                    out var proposed, out var error))
            {
                return ConnectedSupplierUseCaseGuard.Failure<BulkBuyerProductShareMutationResultDto>(
                    ConnectedSupplierErrorCodes.BulkValidation, $"{product.Name}: {error}");
            }

            var share = await _shares.FindAsync(relationship!.Id, product.Id, ct).ConfigureAwait(false);
            var isShared = share?.IsShared == true;
            if (!isShared && mode != BulkBuyerPricingMode.UseDefault)
            {
                isShared = true;
            }

            if (mode == BulkBuyerPricingMode.UseDefault)
            {
                items.Add(new(product.Id.Value, isShared, null));
            }
            else
            {
                items.Add(new(product.Id.Value, isShared, proposed));
            }
        }

        if (items.Count == 0)
        {
            return ApplicationResult<BulkBuyerProductShareMutationResultDto>.Success(new(0));
        }

        var result = await _setShares.ExecuteAsync(orgId, relationshipId, items, ct).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return ConnectedSupplierUseCaseGuard.Failure<BulkBuyerProductShareMutationResultDto>(
                result.ErrorCode!, result.ErrorMessage!);
        }

        return ApplicationResult<BulkBuyerProductShareMutationResultDto>.Success(new(result.Value!.Count));
    }
}
