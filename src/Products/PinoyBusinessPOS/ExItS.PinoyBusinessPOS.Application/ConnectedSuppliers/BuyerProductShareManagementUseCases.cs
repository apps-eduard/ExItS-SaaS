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
    string? ShareFilter = null);

public sealed record BulkBuyerProductShareMutationResultDto(int AffectedCount);

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
    IReadOnlyList<BuyerPricePreviewItemDto> Items);

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

        var items = new List<ConnectedBuyerProductShareDto>(result.Exposures.Count);
        for (var i = 0; i < result.Exposures.Count; i++)
        {
            var exposure = result.Exposures[i];
            var share = result.Shares[i];
            if (share is null)
            {
                items.Add(new(
                    Guid.Empty,
                    relationship.Id.Value,
                    relationship.BuyerOrganizationId.Value,
                    supplier.Value,
                    exposure.ProductId.Value,
                    false,
                    null,
                    null,
                    0,
                    exposure.CreatedAtUtc,
                    exposure.UpdatedAtUtc,
                    exposure.SkuSnapshot,
                    exposure.NameSnapshot,
                    exposure.UnitOfMeasureCode,
                    null,
                    exposure.CategoryNameSnapshot,
                    exposure.SupplierOrderPrice));
            }
            else
            {
                items.Add(ConnectedSupplierMapper.Map(share, exposure, categoryName: exposure.CategoryNameSnapshot));
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
    private readonly ISupplierProductExposureRepository _exposures;
    private readonly IConnectedBuyerProductShareRepository _shares;
    private readonly SetBuyerProductShares _setShares;
    private readonly IPosCommercialAccessAccessor _access;

    public BulkMutateBuyerProductShares(
        IConnectedSupplierRelationshipRepository relationships,
        ISupplierProductExposureRepository exposures,
        IConnectedBuyerProductShareRepository shares,
        SetBuyerProductShares setShares,
        IPosCommercialAccessAccessor access)
    {
        _relationships = relationships;
        _exposures = exposures;
        _shares = shares;
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
        foreach (var productId in productIds)
        {
            var existing = await _shares.FindAsync(relationship!.Id, CatalogProductId.From(productId), ct)
                .ConfigureAwait(false);
            if (share)
            {
                // Preserve any existing buyer-specific price when (re)sharing.
                items.Add(new(productId, true, existing?.BuyerSpecificPoPrice));
            }
            else
            {
                items.Add(new(productId, false, null));
            }
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

        // Fail closed: every product must belong to this supplier and be exposable.
        foreach (var id in ids)
        {
            var exposure = await _exposures.GetByProductAsync(supplier, CatalogProductId.From(id), ct)
                .ConfigureAwait(false);
            if (exposure is null || !exposure.IsExposed)
            {
                return ConnectedSupplierUseCaseGuard.Failure<IReadOnlyList<Guid>>(
                    ConnectedSupplierErrorCodes.ExposureNotFound,
                    "An eligible exposure was not found.");
            }
        }

        return ApplicationResult<IReadOnlyList<Guid>>.Success(ids);
    }
}

public sealed class PreviewBuyerProductPricing
{
    private readonly IConnectedSupplierRelationshipRepository _relationships;
    private readonly ISupplierProductExposureRepository _exposures;
    private readonly IConnectedBuyerProductShareRepository _shares;
    private readonly IPosCommercialAccessAccessor _access;

    public PreviewBuyerProductPricing(
        IConnectedSupplierRelationshipRepository relationships,
        ISupplierProductExposureRepository exposures,
        IConnectedBuyerProductShareRepository shares,
        IPosCommercialAccessAccessor access)
    {
        _relationships = relationships;
        _exposures = exposures;
        _shares = shares;
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

        var previewItems = new List<BuyerPricePreviewItemDto>();
        var mutations = new List<SetBuyerProductShareItem>();
        foreach (var productId in productIds)
        {
            var exposure = await _exposures.GetByProductAsync(supplier, CatalogProductId.From(productId), ct)
                .ConfigureAwait(false);
            if (exposure is null || !exposure.IsExposed)
            {
                return ConnectedSupplierUseCaseGuard.Failure<BulkBuyerPricingPreviewDto>(
                    ConnectedSupplierErrorCodes.ExposureNotFound,
                    "An eligible exposure was not found.");
            }

            if (!BuyerProductShareBulkPricing.TryComputeBuyerPrice(
                    mode, exposure.SupplierOrderPrice, request.Percent, request.Amount, request.FixedPrice,
                    out var proposed, out var error))
            {
                return ConnectedSupplierUseCaseGuard.Failure<BulkBuyerPricingPreviewDto>(
                    ConnectedSupplierErrorCodes.BulkValidation,
                    $"{exposure.NameSnapshot}: {error}");
            }

            var share = await _shares.FindAsync(relationship.Id, exposure.ProductId, ct).ConfigureAwait(false);
            var current = share?.BuyerSpecificPoPrice;
            var effective = proposed ?? exposure.SupplierOrderPrice;
            if (previewItems.Count < BuyerProductShareBulkPricing.PreviewItemLimit)
            {
                previewItems.Add(new(
                    exposure.ProductId.Value,
                    exposure.NameSnapshot,
                    exposure.SupplierOrderPrice,
                    current,
                    proposed,
                    effective));
            }

            // Pricing apply keeps share state; UseDefault clears override only.
            var isShared = share?.IsShared ?? true;
            if (mode != BulkBuyerPricingMode.UseDefault && share is null)
            {
                isShared = true;
            }

            mutations.Add(new(exposure.ProductId.Value, isShared || mode != BulkBuyerPricingMode.UseDefault, proposed));
        }

        return ApplicationResult<BulkBuyerPricingPreviewDto>.Success(new(
            productIds.Count,
            productIds.Count > BuyerProductShareBulkPricing.PreviewItemLimit,
            previewItems));
    }
}

public sealed class ApplyBuyerProductPricing
{
    private readonly PreviewBuyerProductPricing _preview;
    private readonly SetBuyerProductShares _setShares;
    private readonly IConnectedSupplierRelationshipRepository _relationships;
    private readonly ISupplierProductExposureRepository _exposures;
    private readonly IConnectedBuyerProductShareRepository _shares;
    private readonly IPosCommercialAccessAccessor _access;

    public ApplyBuyerProductPricing(
        PreviewBuyerProductPricing preview,
        SetBuyerProductShares setShares,
        IConnectedSupplierRelationshipRepository relationships,
        ISupplierProductExposureRepository exposures,
        IConnectedBuyerProductShareRepository shares,
        IPosCommercialAccessAccessor access)
    {
        _preview = preview;
        _setShares = setShares;
        _relationships = relationships;
        _exposures = exposures;
        _shares = shares;
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
            var exposure = await _exposures.GetByProductAsync(supplier, CatalogProductId.From(productId), ct)
                .ConfigureAwait(false);
            if (exposure is null || !exposure.IsExposed)
            {
                return ConnectedSupplierUseCaseGuard.Failure<BulkBuyerProductShareMutationResultDto>(
                    ConnectedSupplierErrorCodes.ExposureNotFound, "An eligible exposure was not found.");
            }

            if (!BuyerProductShareBulkPricing.TryComputeBuyerPrice(
                    mode, exposure.SupplierOrderPrice, request.Percent, request.Amount, request.FixedPrice,
                    out var proposed, out var error))
            {
                return ConnectedSupplierUseCaseGuard.Failure<BulkBuyerProductShareMutationResultDto>(
                    ConnectedSupplierErrorCodes.BulkValidation, $"{exposure.NameSnapshot}: {error}");
            }

            var share = await _shares.FindAsync(relationship!.Id, exposure.ProductId, ct).ConfigureAwait(false);
            var isShared = share?.IsShared == true;
            if (!isShared && mode != BulkBuyerPricingMode.UseDefault)
            {
                isShared = true;
            }

            if (mode == BulkBuyerPricingMode.UseDefault)
            {
                // Clearing price does not auto-share; keep current share state.
                items.Add(new(exposure.ProductId.Value, isShared, null));
            }
            else
            {
                items.Add(new(exposure.ProductId.Value, isShared, proposed));
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
