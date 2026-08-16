using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.Catalog;

public sealed record ConnectedBuyerAvailabilityCategoryFacetDto(
    Guid? CategoryId,
    string? CategoryName,
    int Count);

public sealed record ConnectedBuyerAvailabilityItemDto(
    Guid ProductId,
    string Name,
    string? Sku,
    Guid? CategoryId,
    string? CategoryName,
    decimal SellingPrice,
    decimal? DefaultConnectedPoPrice,
    bool CanExposeToConnectedBuyers,
    string Status);

public sealed record ConnectedBuyerAvailabilityQueryResultDto(
    IReadOnlyList<ConnectedBuyerAvailabilityItemDto> Items,
    int MatchingCount,
    int TotalCount,
    int AvailableCount,
    int NotAvailableCount,
    int Page,
    int PageSize,
    IReadOnlyList<ConnectedBuyerAvailabilityCategoryFacetDto> Categories);

public sealed record BulkConnectedBuyerAvailabilityMutationRequest(
    string Operation,
    IReadOnlyList<Guid>? ProductIds = null,
    bool SelectAllMatching = false,
    string? Query = null,
    Guid? CategoryId = null,
    string? AvailabilityFilter = null,
    bool UncategorizedOnly = false);

public sealed record BulkConnectedBuyerAvailabilityMutationResultDto(int AffectedCount);

public sealed record BulkDefaultConnectedPoPricingRequest(
    string Mode,
    IReadOnlyList<Guid>? ProductIds = null,
    bool SelectAllMatching = false,
    string? Query = null,
    Guid? CategoryId = null,
    string? AvailabilityFilter = null,
    bool UncategorizedOnly = false,
    decimal? Percent = null,
    decimal? Amount = null,
    decimal? FixedPrice = null);

public sealed record DefaultConnectedPoPricePreviewItemDto(
    Guid ProductId,
    string Name,
    decimal SellingPrice,
    decimal? CurrentDefaultPoPrice,
    decimal ProposedDefaultPoPrice);

public sealed record BulkDefaultConnectedPoPricingPreviewDto(
    int AffectedCount,
    bool Truncated,
    IReadOnlyList<DefaultConnectedPoPricePreviewItemDto> Items);

internal static class ConnectedBuyerAvailabilityGuard
{
    public static ApplicationResult Access(IPosCommercialAccessAccessor access, UtangCapability capability) =>
        CommercialAccessGuard.Require(access, capability);

    public static ApplicationResult<T> Failure<T>(string code, string message) =>
        ApplicationResult<T>.Failure(code, message);

    public static CatalogProductFilter BuildFilter(
        string? query,
        Guid? categoryId,
        string? availabilityFilter,
        bool uncategorizedOnly = false)
    {
        bool? canExpose = null;
        if (string.Equals(availabilityFilter, "available", StringComparison.OrdinalIgnoreCase))
        {
            canExpose = true;
        }
        else if (string.Equals(availabilityFilter, "notavailable", StringComparison.OrdinalIgnoreCase)
                 || string.Equals(availabilityFilter, "not_available", StringComparison.OrdinalIgnoreCase))
        {
            canExpose = false;
        }

        ProductCategoryId? category = null;
        if (!uncategorizedOnly && categoryId is Guid id && id != Guid.Empty)
        {
            category = ProductCategoryId.From(id);
        }

        return new CatalogProductFilter(
            Status: CatalogProductStatus.Active,
            CategoryId: category,
            Search: query,
            CanExposeToConnectedBuyers: canExpose,
            UncategorizedOnly: uncategorizedOnly);
    }
}

public sealed class QueryConnectedBuyerAvailability
{
    private readonly ICatalogProductRepository _products;
    private readonly IProductCategoryRepository _categories;
    private readonly IPosCommercialAccessAccessor _access;

    public QueryConnectedBuyerAvailability(
        ICatalogProductRepository products,
        IProductCategoryRepository categories,
        IPosCommercialAccessAccessor access)
    {
        _products = products;
        _categories = categories;
        _access = access;
    }

    public async Task<ApplicationResult<ConnectedBuyerAvailabilityQueryResultDto>> ExecuteAsync(
        Guid organizationId,
        string? query,
        Guid? categoryId,
        string? availabilityFilter,
        int? page,
        int? pageSize,
        bool uncategorizedOnly = false,
        CancellationToken ct = default)
    {
        var gate = ConnectedBuyerAvailabilityGuard.Access(_access, UtangCapability.ViewCatalog);
        if (!gate.IsSuccess)
        {
            return ConnectedBuyerAvailabilityGuard.Failure<ConnectedBuyerAvailabilityQueryResultDto>(
                gate.ErrorCode!, gate.ErrorMessage!);
        }

        var org = PosOrganizationId.From(organizationId);
        var filter = ConnectedBuyerAvailabilityGuard.BuildFilter(
            query, categoryId, availabilityFilter, uncategorizedOnly);
        var (skip, take) = PosPagination.Normalize(page, pageSize);
        var pageNumber = Math.Max(page ?? 1, 1);

        var (items, matching) = await _products.ListAsync(org, filter, skip, take, ct).ConfigureAwait(false);
        var summary = await _products.CountConnectedBuyerAvailabilityAsync(org, ct).ConfigureAwait(false);
        var facets = await _products
            .ListConnectedBuyerAvailabilityCategoryFacetsAsync(org, filter, ct)
            .ConfigureAwait(false);

        var categoryIds = items
            .Select(p => p.CategoryId)
            .Concat(facets.Where(f => f.CategoryId is not null).Select(f => ProductCategoryId.From(f.CategoryId!.Value)))
            .Where(id => id is not null)
            .Select(id => id!)
            .Distinct()
            .ToList();
        var categories = categoryIds.Count == 0
            ? Array.Empty<ProductCategory>()
            : await _categories.ListByIdsAsync(org, categoryIds, ct).ConfigureAwait(false);
        var names = categories.ToDictionary(c => c.Id.Value, c => c.Name);

        var dtos = items.Select(p => new ConnectedBuyerAvailabilityItemDto(
            p.Id.Value,
            p.Name,
            p.Sku,
            p.CategoryId?.Value,
            p.CategoryId is null ? null : names.GetValueOrDefault(p.CategoryId.Value),
            p.SellingPrice,
            p.DefaultConnectedPoPrice,
            p.CanExposeToConnectedBuyers,
            p.Status.ToString())).ToList();

        var facetDtos = facets.Select(f => new ConnectedBuyerAvailabilityCategoryFacetDto(
            f.CategoryId,
            f.CategoryId is null ? null : names.GetValueOrDefault(f.CategoryId.Value),
            f.Count)).ToList();

        return ApplicationResult<ConnectedBuyerAvailabilityQueryResultDto>.Success(new(
            dtos,
            matching,
            summary.TotalCount,
            summary.AvailableCount,
            summary.NotAvailableCount,
            pageNumber,
            take,
            facetDtos));
    }
}

public sealed class BulkMutateConnectedBuyerAvailability
{
    private readonly ICatalogProductRepository _products;
    private readonly ISupplierProductExposureRepository _exposures;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IPosCommercialAccessAccessor _access;

    public BulkMutateConnectedBuyerAvailability(
        ICatalogProductRepository products,
        ISupplierProductExposureRepository exposures,
        IPosUnitOfWork unitOfWork,
        IClock clock,
        IPosCommercialAccessAccessor access)
    {
        _products = products;
        _exposures = exposures;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _access = access;
    }

    public async Task<ApplicationResult<BulkConnectedBuyerAvailabilityMutationResultDto>> ExecuteAsync(
        Guid organizationId,
        BulkConnectedBuyerAvailabilityMutationRequest request,
        CancellationToken ct = default)
    {
        var gate = ConnectedBuyerAvailabilityGuard.Access(_access, UtangCapability.ManageCatalog);
        if (!gate.IsSuccess)
        {
            return ConnectedBuyerAvailabilityGuard.Failure<BulkConnectedBuyerAvailabilityMutationResultDto>(
                gate.ErrorCode!, gate.ErrorMessage!);
        }

        var enable = string.Equals(request.Operation, "enable", StringComparison.OrdinalIgnoreCase);
        var disable = string.Equals(request.Operation, "disable", StringComparison.OrdinalIgnoreCase);
        if (!enable && !disable)
        {
            return ConnectedBuyerAvailabilityGuard.Failure<BulkConnectedBuyerAvailabilityMutationResultDto>(
                ApplicationErrorCodes.CatalogBulkValidation, "Operation must be Enable or Disable.");
        }

        var resolve = await ResolveTargetProductIdsAsync(
                organizationId, request.ProductIds, request.SelectAllMatching,
                request.Query, request.CategoryId, request.AvailabilityFilter, request.UncategorizedOnly, ct)
            .ConfigureAwait(false);
        if (!resolve.IsSuccess)
        {
            return ConnectedBuyerAvailabilityGuard.Failure<BulkConnectedBuyerAvailabilityMutationResultDto>(
                resolve.ErrorCode!, resolve.ErrorMessage!);
        }

        var productIds = resolve.Value!;
        if (productIds.Count == 0)
        {
            return ApplicationResult<BulkConnectedBuyerAvailabilityMutationResultDto>.Success(new(0));
        }

        var org = PosOrganizationId.From(organizationId);
        var catalogIds = productIds.Select(CatalogProductId.From).ToList();
        var products = await _products.ListByIdsAsync(org, catalogIds, ct).ConfigureAwait(false);
        if (products.Count != productIds.Count)
        {
            return ConnectedBuyerAvailabilityGuard.Failure<BulkConnectedBuyerAvailabilityMutationResultDto>(
                ApplicationErrorCodes.ProductNotFound,
                "One or more products were not found in this organization.");
        }

        var now = _clock.UtcNow;
        var affected = 0;
        try
        {
            foreach (var product in products)
            {
                if (product.Status != CatalogProductStatus.Active)
                {
                    return ConnectedBuyerAvailabilityGuard.Failure<BulkConnectedBuyerAvailabilityMutationResultDto>(
                        ApplicationErrorCodes.CatalogBulkValidation,
                        $"{product.Name}: only active products can be managed.");
                }

                if (enable)
                {
                    if (!product.CanExposeToConnectedBuyers)
                    {
                        product.EnableConnectedBuyerAvailability(now);
                        affected++;
                    }
                }
                else if (product.CanExposeToConnectedBuyers)
                {
                    product.DisableConnectedBuyerAvailability(now);
                    affected++;
                }

                await _products.UpdateAsync(product, ct).ConfigureAwait(false);
                await ConnectedProductExposureSync.SyncAsync(product, _exposures, now, ct).ConfigureAwait(false);
            }

            await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return ConnectedBuyerAvailabilityGuard.Failure<BulkConnectedBuyerAvailabilityMutationResultDto>(
                ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ConnectedBuyerAvailabilityGuard.Failure<BulkConnectedBuyerAvailabilityMutationResultDto>(
                ex.ErrorCode, ex.Message);
        }

        return ApplicationResult<BulkConnectedBuyerAvailabilityMutationResultDto>.Success(new(affected));
    }

    private async Task<ApplicationResult<IReadOnlyList<Guid>>> ResolveTargetProductIdsAsync(
        Guid organizationId,
        IReadOnlyList<Guid>? productIds,
        bool selectAllMatching,
        string? query,
        Guid? categoryId,
        string? availabilityFilter,
        bool uncategorizedOnly,
        CancellationToken ct)
    {
        var org = PosOrganizationId.From(organizationId);
        var filter = ConnectedBuyerAvailabilityGuard.BuildFilter(
            query, categoryId, availabilityFilter, uncategorizedOnly);

        if (selectAllMatching)
        {
            var ids = await _products
                .ListIdsAsync(org, filter, 0, ConnectedBuyerAvailabilityBulkPricing.MaxSelectAllMatching + 1, ct)
                .ConfigureAwait(false);
            if (ids.Count > ConnectedBuyerAvailabilityBulkPricing.MaxSelectAllMatching)
            {
                return ConnectedBuyerAvailabilityGuard.Failure<IReadOnlyList<Guid>>(
                    ApplicationErrorCodes.CatalogBulkValidation,
                    $"Too many matching products (max {ConnectedBuyerAvailabilityBulkPricing.MaxSelectAllMatching}). Narrow filters first.");
            }

            return ApplicationResult<IReadOnlyList<Guid>>.Success(ids);
        }

        var idsExplicit = (productIds ?? [])
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList();
        if (idsExplicit.Count == 0)
        {
            return ConnectedBuyerAvailabilityGuard.Failure<IReadOnlyList<Guid>>(
                ApplicationErrorCodes.CatalogBulkValidation, "Select at least one product.");
        }

        if (idsExplicit.Count > ConnectedBuyerAvailabilityBulkPricing.MaxBulkProductIds)
        {
            return ConnectedBuyerAvailabilityGuard.Failure<IReadOnlyList<Guid>>(
                ApplicationErrorCodes.CatalogBulkValidation,
                $"Too many products selected (max {ConnectedBuyerAvailabilityBulkPricing.MaxBulkProductIds}). Use select-all matching.");
        }

        var loaded = await _products
            .ListByIdsAsync(org, idsExplicit.Select(CatalogProductId.From).ToList(), ct)
            .ConfigureAwait(false);
        if (loaded.Count != idsExplicit.Count)
        {
            return ConnectedBuyerAvailabilityGuard.Failure<IReadOnlyList<Guid>>(
                ApplicationErrorCodes.ProductNotFound,
                "One or more products were not found in this organization.");
        }

        return ApplicationResult<IReadOnlyList<Guid>>.Success(idsExplicit);
    }
}

public sealed class PreviewDefaultConnectedPoPricing
{
    private readonly ICatalogProductRepository _products;
    private readonly IPosCommercialAccessAccessor _access;

    public PreviewDefaultConnectedPoPricing(
        ICatalogProductRepository products,
        IPosCommercialAccessAccessor access)
    {
        _products = products;
        _access = access;
    }

    public Task<ApplicationResult<BulkDefaultConnectedPoPricingPreviewDto>> ExecuteAsync(
        Guid organizationId,
        BulkDefaultConnectedPoPricingRequest request,
        CancellationToken ct = default) =>
        BuildAsync(organizationId, request, apply: false, ct);

    internal async Task<ApplicationResult<BulkDefaultConnectedPoPricingPreviewDto>> BuildAsync(
        Guid organizationId,
        BulkDefaultConnectedPoPricingRequest request,
        bool apply,
        CancellationToken ct)
    {
        var gate = ConnectedBuyerAvailabilityGuard.Access(
            _access, apply ? UtangCapability.ManageCatalog : UtangCapability.ViewCatalog);
        if (!gate.IsSuccess)
        {
            return ConnectedBuyerAvailabilityGuard.Failure<BulkDefaultConnectedPoPricingPreviewDto>(
                gate.ErrorCode!, gate.ErrorMessage!);
        }

        if (!Enum.TryParse<ConnectedBuyerAvailabilityPricingMode>(request.Mode, ignoreCase: true, out var mode))
        {
            return ConnectedBuyerAvailabilityGuard.Failure<BulkDefaultConnectedPoPricingPreviewDto>(
                ApplicationErrorCodes.CatalogBulkValidation, "Unsupported pricing mode.");
        }

        var resolve = await ResolveIdsAsync(organizationId, request, ct).ConfigureAwait(false);
        if (!resolve.IsSuccess)
        {
            return ConnectedBuyerAvailabilityGuard.Failure<BulkDefaultConnectedPoPricingPreviewDto>(
                resolve.ErrorCode!, resolve.ErrorMessage!);
        }

        var productIds = resolve.Value!;
        if (productIds.Count == 0)
        {
            return ApplicationResult<BulkDefaultConnectedPoPricingPreviewDto>.Success(new(0, false, []));
        }

        var org = PosOrganizationId.From(organizationId);
        var products = await _products
            .ListByIdsAsync(org, productIds.Select(CatalogProductId.From).ToList(), ct)
            .ConfigureAwait(false);
        if (products.Count != productIds.Count)
        {
            return ConnectedBuyerAvailabilityGuard.Failure<BulkDefaultConnectedPoPricingPreviewDto>(
                ApplicationErrorCodes.ProductNotFound,
                "One or more products were not found in this organization.");
        }

        var byId = products.ToDictionary(p => p.Id.Value);
        var previewItems = new List<DefaultConnectedPoPricePreviewItemDto>();
        foreach (var id in productIds)
        {
            var product = byId[id];
            if (product.Status != CatalogProductStatus.Active)
            {
                return ConnectedBuyerAvailabilityGuard.Failure<BulkDefaultConnectedPoPricingPreviewDto>(
                    ApplicationErrorCodes.CatalogBulkValidation,
                    $"{product.Name}: only active products can be priced.");
            }

            if (!product.CanExposeToConnectedBuyers)
            {
                return ConnectedBuyerAvailabilityGuard.Failure<BulkDefaultConnectedPoPricingPreviewDto>(
                    ApplicationErrorCodes.CatalogBulkValidation,
                    $"{product.Name}: enable connected buyer availability before setting Default PO price.");
            }

            if (!ConnectedBuyerAvailabilityBulkPricing.TryComputeDefaultPoPrice(
                    mode, product.SellingPrice, request.Percent, request.Amount, request.FixedPrice,
                    out var proposed, out var error))
            {
                return ConnectedBuyerAvailabilityGuard.Failure<BulkDefaultConnectedPoPricingPreviewDto>(
                    ApplicationErrorCodes.CatalogBulkValidation, $"{product.Name}: {error}");
            }

            if (previewItems.Count < ConnectedBuyerAvailabilityBulkPricing.PreviewItemLimit)
            {
                previewItems.Add(new(
                    product.Id.Value,
                    product.Name,
                    product.SellingPrice,
                    product.DefaultConnectedPoPrice,
                    proposed));
            }
        }

        return ApplicationResult<BulkDefaultConnectedPoPricingPreviewDto>.Success(new(
            productIds.Count,
            productIds.Count > ConnectedBuyerAvailabilityBulkPricing.PreviewItemLimit,
            previewItems));
    }

    private async Task<ApplicationResult<IReadOnlyList<Guid>>> ResolveIdsAsync(
        Guid organizationId,
        BulkDefaultConnectedPoPricingRequest request,
        CancellationToken ct)
    {
        var org = PosOrganizationId.From(organizationId);
        var filter = ConnectedBuyerAvailabilityGuard.BuildFilter(
            request.Query, request.CategoryId, request.AvailabilityFilter, request.UncategorizedOnly);

        if (request.SelectAllMatching)
        {
            var ids = await _products
                .ListIdsAsync(org, filter, 0, ConnectedBuyerAvailabilityBulkPricing.MaxSelectAllMatching + 1, ct)
                .ConfigureAwait(false);
            if (ids.Count > ConnectedBuyerAvailabilityBulkPricing.MaxSelectAllMatching)
            {
                return ConnectedBuyerAvailabilityGuard.Failure<IReadOnlyList<Guid>>(
                    ApplicationErrorCodes.CatalogBulkValidation,
                    $"Too many matching products (max {ConnectedBuyerAvailabilityBulkPricing.MaxSelectAllMatching}). Narrow filters first.");
            }

            return ApplicationResult<IReadOnlyList<Guid>>.Success(ids);
        }

        var idsExplicit = (request.ProductIds ?? [])
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList();
        if (idsExplicit.Count == 0)
        {
            return ConnectedBuyerAvailabilityGuard.Failure<IReadOnlyList<Guid>>(
                ApplicationErrorCodes.CatalogBulkValidation, "Select at least one product.");
        }

        if (idsExplicit.Count > ConnectedBuyerAvailabilityBulkPricing.MaxBulkProductIds)
        {
            return ConnectedBuyerAvailabilityGuard.Failure<IReadOnlyList<Guid>>(
                ApplicationErrorCodes.CatalogBulkValidation,
                $"Too many products selected (max {ConnectedBuyerAvailabilityBulkPricing.MaxBulkProductIds}).");
        }

        var loaded = await _products
            .ListByIdsAsync(org, idsExplicit.Select(CatalogProductId.From).ToList(), ct)
            .ConfigureAwait(false);
        if (loaded.Count != idsExplicit.Count)
        {
            return ConnectedBuyerAvailabilityGuard.Failure<IReadOnlyList<Guid>>(
                ApplicationErrorCodes.ProductNotFound,
                "One or more products were not found in this organization.");
        }

        return ApplicationResult<IReadOnlyList<Guid>>.Success(idsExplicit);
    }
}

public sealed class ApplyDefaultConnectedPoPricing
{
    private readonly PreviewDefaultConnectedPoPricing _preview;
    private readonly ICatalogProductRepository _products;
    private readonly ISupplierProductExposureRepository _exposures;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IPosCommercialAccessAccessor _access;

    public ApplyDefaultConnectedPoPricing(
        PreviewDefaultConnectedPoPricing preview,
        ICatalogProductRepository products,
        ISupplierProductExposureRepository exposures,
        IPosUnitOfWork unitOfWork,
        IClock clock,
        IPosCommercialAccessAccessor access)
    {
        _preview = preview;
        _products = products;
        _exposures = exposures;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _access = access;
    }

    public async Task<ApplicationResult<BulkConnectedBuyerAvailabilityMutationResultDto>> ExecuteAsync(
        Guid organizationId,
        BulkDefaultConnectedPoPricingRequest request,
        CancellationToken ct = default)
    {
        var preview = await _preview.BuildAsync(organizationId, request, apply: true, ct).ConfigureAwait(false);
        if (!preview.IsSuccess)
        {
            return ConnectedBuyerAvailabilityGuard.Failure<BulkConnectedBuyerAvailabilityMutationResultDto>(
                preview.ErrorCode!, preview.ErrorMessage!);
        }

        if (!Enum.TryParse<ConnectedBuyerAvailabilityPricingMode>(request.Mode, ignoreCase: true, out var mode))
        {
            return ConnectedBuyerAvailabilityGuard.Failure<BulkConnectedBuyerAvailabilityMutationResultDto>(
                ApplicationErrorCodes.CatalogBulkValidation, "Unsupported pricing mode.");
        }

        var org = PosOrganizationId.From(organizationId);
        IReadOnlyList<Guid> productIds;
        if (request.SelectAllMatching)
        {
            var filter = ConnectedBuyerAvailabilityGuard.BuildFilter(
                request.Query, request.CategoryId, request.AvailabilityFilter, request.UncategorizedOnly);
            productIds = await _products
                .ListIdsAsync(org, filter, 0, ConnectedBuyerAvailabilityBulkPricing.MaxSelectAllMatching, ct)
                .ConfigureAwait(false);
        }
        else
        {
            productIds = (request.ProductIds ?? []).Where(x => x != Guid.Empty).Distinct().ToList();
        }

        if (productIds.Count == 0)
        {
            return ApplicationResult<BulkConnectedBuyerAvailabilityMutationResultDto>.Success(new(0));
        }

        var products = await _products
            .ListByIdsAsync(org, productIds.Select(CatalogProductId.From).ToList(), ct)
            .ConfigureAwait(false);
        if (products.Count != productIds.Count)
        {
            return ConnectedBuyerAvailabilityGuard.Failure<BulkConnectedBuyerAvailabilityMutationResultDto>(
                ApplicationErrorCodes.ProductNotFound,
                "One or more products were not found in this organization.");
        }

        var now = _clock.UtcNow;
        var byId = products.ToDictionary(p => p.Id.Value);
        try
        {
            foreach (var id in productIds)
            {
                var product = byId[id];
                if (!ConnectedBuyerAvailabilityBulkPricing.TryComputeDefaultPoPrice(
                        mode, product.SellingPrice, request.Percent, request.Amount, request.FixedPrice,
                        out var proposed, out var error))
                {
                    return ConnectedBuyerAvailabilityGuard.Failure<BulkConnectedBuyerAvailabilityMutationResultDto>(
                        ApplicationErrorCodes.CatalogBulkValidation, $"{product.Name}: {error}");
                }

                product.SetDefaultConnectedPoPrice(proposed, now);
                await _products.UpdateAsync(product, ct).ConfigureAwait(false);
                await ConnectedProductExposureSync.SyncAsync(product, _exposures, now, ct).ConfigureAwait(false);
            }

            await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return ConnectedBuyerAvailabilityGuard.Failure<BulkConnectedBuyerAvailabilityMutationResultDto>(
                ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ConnectedBuyerAvailabilityGuard.Failure<BulkConnectedBuyerAvailabilityMutationResultDto>(
                ex.ErrorCode, ex.Message);
        }

        return ApplicationResult<BulkConnectedBuyerAvailabilityMutationResultDto>.Success(new(productIds.Count));
    }
}
