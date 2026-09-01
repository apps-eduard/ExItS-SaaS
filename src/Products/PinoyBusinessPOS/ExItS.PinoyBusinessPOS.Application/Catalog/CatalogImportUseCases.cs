using System.IO;
using System.Net.Http;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.Catalog;

public sealed class CatalogImportQueryService
{
    private readonly ICatalogImportJobRepository _imports;

    public CatalogImportQueryService(ICatalogImportJobRepository imports) => _imports = imports;

    public async Task<PosCatalogImportJobDto?> GetJobAsync(
        Guid organizationId,
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var job = await _imports
            .GetByIdAsync(
                PosOrganizationId.From(organizationId),
                CatalogImportJobId.From(jobId),
                cancellationToken)
            .ConfigureAwait(false);
        return job is null ? null : Map(job);
    }

    public async Task<PagedResult<PosCatalogImportItemDto>> GetItemsAsync(
        Guid organizationId,
        Guid jobId,
        string? status,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        PosCatalogImportItemStatus? parsed = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse(status, ignoreCase: true, out PosCatalogImportItemStatus value))
            {
                return new PagedResult<PosCatalogImportItemDto>([], 0, Math.Max(page ?? 1, 1), pageSize ?? 20);
            }

            parsed = value;
        }

        var (skip, take) = PosPagination.Normalize(page, pageSize);
        var (items, total) = await _imports
            .ListItemsAsync(
                PosOrganizationId.From(organizationId),
                CatalogImportJobId.From(jobId),
                parsed,
                skip,
                take,
                cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<PosCatalogImportItemDto>(
            items.Select(Map).ToList(),
            total,
            Math.Max(page ?? 1, 1),
            take);
    }

    public static PosCatalogImportJobDto Map(CatalogImportJob job) =>
        new(
            job.Id.Value,
            job.OrganizationId.Value,
            job.JobKind.ToString(),
            job.PlatformTemplateId,
            job.BatchNumber,
            job.CatalogSource.ToString(),
            job.Status.ToString(),
            job.TotalCount,
            job.ProcessedCount,
            job.ImportedCount,
            job.SkippedCount,
            job.FailedCount,
            job.CurrentStage,
            job.ErrorSummary,
            job.CreatedAtUtc,
            job.UpdatedAtUtc,
            job.StartedAtUtc,
            job.CompletedAtUtc);

    public static PosCatalogImportItemDto Map(CatalogImportItemResult item) =>
        new(
            item.Id.Value,
            item.PlatformGlobalProductId,
            item.SortOrder,
            item.Name,
            item.Sku,
            item.Barcode,
            item.UnitOfMeasure,
            item.SellingMode,
            item.SuggestedPrice,
            item.Status.ToString(),
            item.LocalProductId?.Value,
            item.ErrorCode,
            item.ErrorMessage,
            item.ProcessedAtUtc);
}

/// <summary>
/// Computes whether an organization can import batch 1, a subsequent chunk, or has nothing left.
/// </summary>
public sealed class GetTemplateImportStatus(
    ICatalogProductRepository products,
    IPlatformMerchantCatalogClient platform)
{
    public async Task<ApplicationResult<PosTemplateImportStatusDto>> ExecuteAsync(
        Guid organizationId,
        Guid platformTemplateId,
        string? platformSessionToken,
        CancellationToken cancellationToken = default)
    {
        PlatformMerchantCatalogTemplateDto? template;
        try
        {
            template = await platform
                .GetPublishedTemplateAsync(platformTemplateId, platformSessionToken, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ImportTemplateBatch.TryMapPlatformFailure<PosTemplateImportStatusDto>(ex, cancellationToken, out var mapped))
        {
            return mapped!;
        }

        if (template is null || !string.Equals(template.Status, "Published", StringComparison.OrdinalIgnoreCase))
        {
            return ApplicationResult<PosTemplateImportStatusDto>.Failure(
                ApplicationErrorCodes.CatalogImportTemplateNotFound,
                "Published template was not found.");
        }

        var flaggedFirst = template.Products
            .Where(p => p.IsFirstBatch && p.GlobalProductId != Guid.Empty)
            .OrderBy(p => p.SortOrder)
            .Select(p => p.GlobalProductId)
            .Distinct()
            .ToList();
        var orderedIds = template.Products
            .Where(p => p.GlobalProductId != Guid.Empty)
            .OrderBy(p => p.SortOrder)
            .Select(p => p.GlobalProductId)
            .Distinct()
            .ToList();
        var defaultBatchSize = Math.Max(1, template.DefaultBatchSize);

        // Templates linked without IsFirstBatch flags still need a usable first batch window.
        IReadOnlyList<Guid> firstBatch;
        IReadOnlyList<Guid> subsequent;
        if (flaggedFirst.Count > 0)
        {
            firstBatch = flaggedFirst;
            subsequent = orderedIds.Where(id => !flaggedFirst.Contains(id)).ToList();
        }
        else
        {
            firstBatch = orderedIds.Take(defaultBatchSize).ToList();
            subsequent = orderedIds.Skip(defaultBatchSize).ToList();
        }

        var allIds = firstBatch.Concat(subsequent).Distinct().ToList();
        IReadOnlySet<Guid> already = allIds.Count == 0
            ? new HashSet<Guid>()
            : await products
                .ListPlatformGlobalProductIdsAsync(
                    PosOrganizationId.From(organizationId),
                    allIds,
                    cancellationToken)
                .ConfigureAwait(false);

        var firstImported = firstBatch.Count(already.Contains);
        var subsequentImported = subsequent.Count(already.Contains);
        var subsequentRemaining = subsequent.Count - subsequentImported;
        var firstComplete = firstBatch.Count == 0 || firstImported >= firstBatch.Count;
        var hasSubsequent = subsequent.Count > 0;
        var nextBatchEstimate = Math.Min(defaultBatchSize, Math.Max(0, subsequentRemaining));

        return ApplicationResult<PosTemplateImportStatusDto>.Success(new PosTemplateImportStatusDto(
            PlatformTemplateId: platformTemplateId,
            FirstBatchTotal: firstBatch.Count,
            FirstBatchImportedCount: firstImported,
            FirstBatchComplete: firstComplete,
            SubsequentTotal: subsequent.Count,
            SubsequentImportedCount: subsequentImported,
            SubsequentRemainingCount: subsequentRemaining,
            HasSubsequentBatches: hasSubsequent,
            CanImportFirstBatch: firstBatch.Count > 0 && !firstComplete,
            CanImportNextBatch: firstComplete && subsequentRemaining > 0,
            SuggestedNextBatchNumber: firstComplete ? 2 : 1,
            NextBatchSizeEstimate: nextBatchEstimate,
            DefaultBatchSize: defaultBatchSize));
    }
}

public sealed class ImportTemplateBatch
{
    private readonly ICatalogImportJobRepository _imports;
    private readonly ICatalogProductRepository _products;
    private readonly IPlatformMerchantCatalogClient _platform;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly CatalogProductGovernanceAuthority _governance;
    private readonly ICatalogGovernanceActorAccessor _actorAccessor;

    public ImportTemplateBatch(
        ICatalogImportJobRepository imports,
        ICatalogProductRepository products,
        IPlatformMerchantCatalogClient platform,
        IPosUnitOfWork unitOfWork,
        IClock clock,
        CatalogProductGovernanceAuthority governance,
        ICatalogGovernanceActorAccessor actorAccessor)
    {
        _imports = imports;
        _products = products;
        _platform = platform;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _governance = governance;
        _actorAccessor = actorAccessor;
    }

    public Task<ApplicationResult<PosCatalogImportJobDto>> ExecuteAsync(
        Guid organizationId,
        Guid platformTemplateId,
        int batchNumber,
        string requestedBy,
        string? platformSessionToken,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default) =>
        ExecuteCoreAsync(
            organizationId,
            platformTemplateId,
            batchNumber,
            requestedBy,
            platformSessionToken,
            idempotencyKey,
            cancellationToken);

    private async Task<ApplicationResult<PosCatalogImportJobDto>> ExecuteCoreAsync(
        Guid organizationId,
        Guid platformTemplateId,
        int batchNumber,
        string requestedBy,
        string? platformSessionToken,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (!_governance.CanCreateOrganizationStandard(_actorAccessor.GetActor()))
        {
            return ApplicationResult<PosCatalogImportJobDto>.Failure(
                ApplicationErrorCodes.ProductScopeForbidden,
                "Global catalog import creates OrganizationStandard products and requires organization Owner/Administrator.");
        }

        try
        {
            var orgId = PosOrganizationId.From(organizationId);
            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                var existing = await _imports
                    .FindByIdempotencyKeyAsync(orgId, idempotencyKey.Trim(), cancellationToken)
                    .ConfigureAwait(false);
                if (existing is not null)
                {
                    return ApplicationResult<PosCatalogImportJobDto>.Success(CatalogImportQueryService.Map(existing));
                }
            }

            PlatformMerchantCatalogTemplateDto? template;
            try
            {
                template = await _platform
                    .GetPublishedTemplateAsync(platformTemplateId, platformSessionToken, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex) when (TryMapPlatformFailure<PosCatalogImportJobDto>(ex, cancellationToken, out var mapped))
            {
                return mapped!;
            }

            if (template is null || !string.Equals(template.Status, "Published", StringComparison.OrdinalIgnoreCase))
            {
                return ApplicationResult<PosCatalogImportJobDto>.Failure(
                    ApplicationErrorCodes.CatalogImportTemplateNotFound,
                    "Published template was not found.");
            }

            var candidates = SelectTemplateProducts(template, batchNumber);
            if (candidates.Count == 0)
            {
                return ApplicationResult<PosCatalogImportJobDto>.Failure(
                    ApplicationErrorCodes.CatalogImportNoProducts,
                    batchNumber <= 1
                        ? "Template has no first-batch products to import."
                        : "No additional template products remain to import.");
            }

            var already = await _products
                .ListPlatformGlobalProductIdsAsync(
                    orgId,
                    candidates.Select(c => c.GlobalProductId).ToList(),
                    cancellationToken)
                .ConfigureAwait(false);
            var remaining = candidates
                .Where(c => !already.Contains(c.GlobalProductId))
                .ToList();

            if (batchNumber > 1)
            {
                remaining = remaining.Take(Math.Max(1, template.DefaultBatchSize)).ToList();
            }

            if (remaining.Count == 0)
            {
                return ApplicationResult<PosCatalogImportJobDto>.Failure(
                    ApplicationErrorCodes.CatalogImportProductAlreadyImported,
                    "All selected template products are already imported.");
            }

            // Template batch IDs come only from the Platform published-template payload already
            // entitlement-filtered in the same request. Do not re-GET every product (N parallel
            // Platform calls): that path was timing out / failing as "platform unavailable" after
            // preview succeeded. Live product GET is a fallback for sparse/unavailable links only.
            // (Selected-product import still re-checks client-supplied IDs via GetActiveProductsAsync.)
            var orderedRemaining = remaining
                .OrderBy(r => r.SortOrder)
                .ThenBy(r => r.GlobalProductId)
                .ToList();
            var items = new List<CatalogImportItemResult>();
            var needsLiveFetch = new List<PlatformMerchantCatalogTemplateProductDto>();
            var sort = 0;
            foreach (var link in orderedRemaining)
            {
                if (TryCreatePendingFromTemplateLink(link, sort, out var snapshot))
                {
                    items.Add(snapshot!);
                    sort++;
                    continue;
                }

                needsLiveFetch.Add(link);
            }

            if (needsLiveFetch.Count > 0)
            {
                IReadOnlyList<PlatformMerchantGlobalProductDto> entitledProducts;
                try
                {
                    entitledProducts = await _platform
                        .GetActiveProductsAsync(
                            needsLiveFetch.Select(r => r.GlobalProductId).ToList(),
                            platformSessionToken,
                            cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (Exception ex) when (TryMapPlatformFailure<PosCatalogImportJobDto>(ex, cancellationToken, out var mappedProducts))
                {
                    return mappedProducts!;
                }

                var entitledById = entitledProducts.ToDictionary(p => p.Id);
                foreach (var link in needsLiveFetch)
                {
                    if (!entitledById.TryGetValue(link.GlobalProductId, out var product))
                    {
                        continue;
                    }

                    items.Add(CatalogImportItemResult.CreatePending(
                        product.Id,
                        sort++,
                        product.Name,
                        product.Unit,
                        product.SellingPrice ?? 0m,
                        product.Description,
                        product.Sku,
                        product.Barcode,
                        product.GlobalCategoryId ?? link.CategoryId,
                        sourceCategoryName: FirstNonBlank(link.CategoryName),
                        sellingMode: string.IsNullOrWhiteSpace(product.SellingMode) ? "PerItem" : product.SellingMode));
                }
            }

            if (items.Count == 0)
            {
                return ApplicationResult<PosCatalogImportJobDto>.Failure(
                    ApplicationErrorCodes.CatalogImportNoProducts,
                    "No entitled Platform products were available for import.");
            }

            var job = CatalogImportJob.CreateQueued(
                orgId,
                PosCatalogImportJobKind.TemplateBatch,
                CatalogSource.Template,
                requestedBy,
                items,
                _clock.UtcNow,
                platformTemplateId,
                batchNumber,
                idempotencyKey);

            await _imports.AddAsync(job, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<PosCatalogImportJobDto>.Success(CatalogImportQueryService.Map(job));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PosCatalogImportJobDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<PosCatalogImportJobDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    private static List<PlatformMerchantCatalogTemplateProductDto> SelectTemplateProducts(
        PlatformMerchantCatalogTemplateDto template,
        int batchNumber)
    {
        var ordered = template.Products
            .Where(p => p.GlobalProductId != Guid.Empty)
            .OrderBy(p => p.SortOrder)
            .ToList();
        var flaggedFirst = ordered.Where(p => p.IsFirstBatch).ToList();
        var batchSize = Math.Max(1, template.DefaultBatchSize);

        if (batchNumber <= 1)
        {
            if (flaggedFirst.Count > 0)
            {
                return flaggedFirst;
            }

            // No IsFirstBatch flags (common after CSV→template link): import a usable first window.
            return ordered.Take(batchSize).ToList();
        }

        if (flaggedFirst.Count > 0)
        {
            return ordered.Where(p => !p.IsFirstBatch).ToList();
        }

        return ordered.Skip(batchSize).ToList();
    }

    private static bool TryCreatePendingFromTemplateLink(
        PlatformMerchantCatalogTemplateProductDto link,
        int sortOrder,
        out CatalogImportItemResult? item)
    {
        item = null;
        if (link.GlobalProductId == Guid.Empty
            || string.IsNullOrWhiteSpace(link.ProductName)
            || string.Equals(link.ProductName, "Unavailable product", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        item = CatalogImportItemResult.CreatePending(
            link.GlobalProductId,
            sortOrder,
            link.ProductName!,
            string.IsNullOrWhiteSpace(link.Unit) ? "Piece" : link.Unit!,
            link.SellingPrice ?? 0m,
            description: null,
            sku: link.Sku,
            barcode: link.Barcode,
            sourceGlobalCategoryId: link.CategoryId,
            sourceCategoryName: link.CategoryName,
            sellingMode: string.IsNullOrWhiteSpace(link.SellingMode) ? "PerItem" : link.SellingMode);
        return true;
    }

    private static string? FirstNonBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>
    /// HttpClient timeouts raise <see cref="TaskCanceledException"/> (an
    /// <see cref="OperationCanceledException"/>) even when the request token is still open.
    /// Those must map to a controlled failure — not an unhandled 500.
    /// Do <b>not</b> treat 4xx Platform responses as "temporarily unavailable".
    /// </summary>
    public static bool IsTransientPlatformFailure(Exception ex, CancellationToken cancellationToken)
    {
        ex = Unwrap(ex);
        if (ex is PlatformMerchantCatalogTransientException)
        {
            return true;
        }

        if (ex is PlatformMerchantCatalogRequestException)
        {
            return false;
        }

        if (ex is OperationCanceledException)
        {
            // HttpClient timeout cancels with an open caller token → treat as transient.
            return !cancellationToken.IsCancellationRequested;
        }

        return ex is HttpRequestException or IOException or TimeoutException;
    }

    public static bool TryMapPlatformFailure<T>(
        Exception ex,
        CancellationToken cancellationToken,
        out ApplicationResult<T>? mapped)
    {
        ex = Unwrap(ex);
        if (ex is PlatformMerchantCatalogRequestException request)
        {
            if (request.IsUnauthorized)
            {
                mapped = ApplicationResult<T>.Failure(
                    ApplicationErrorCodes.CatalogImportPlatformSessionRequired,
                    "Platform session is required to import catalog templates. Sign in again and retry.");
                return true;
            }

            mapped = ApplicationResult<T>.Failure(
                ApplicationErrorCodes.CatalogImportTemplateNotFound,
                string.IsNullOrWhiteSpace(request.Message)
                    ? "Published template was not found."
                    : request.Message);
            return true;
        }

        if (IsTransientPlatformFailure(ex, cancellationToken))
        {
            mapped = ApplicationResult<T>.Failure(
                ApplicationErrorCodes.CatalogImportPlatformUnavailable,
                "Platform catalog is temporarily unavailable. Existing POS selling is unaffected.");
            return true;
        }

        mapped = null;
        return false;
    }

    private static Exception Unwrap(Exception ex) =>
        ex is AggregateException aggregate && aggregate.InnerExceptions.Count == 1
            ? aggregate.InnerExceptions[0]
            : ex;
}

public sealed class ImportSelectedProducts
{
    private readonly ICatalogImportJobRepository _imports;
    private readonly ICatalogProductRepository _products;
    private readonly IPlatformMerchantCatalogClient _platform;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly CatalogProductGovernanceAuthority _governance;
    private readonly ICatalogGovernanceActorAccessor _actorAccessor;

    public ImportSelectedProducts(
        ICatalogImportJobRepository imports,
        ICatalogProductRepository products,
        IPlatformMerchantCatalogClient platform,
        IPosUnitOfWork unitOfWork,
        IClock clock,
        CatalogProductGovernanceAuthority governance,
        ICatalogGovernanceActorAccessor actorAccessor)
    {
        _imports = imports;
        _products = products;
        _platform = platform;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _governance = governance;
        _actorAccessor = actorAccessor;
    }

    public async Task<ApplicationResult<PosCatalogImportJobDto>> ExecuteAsync(
        Guid organizationId,
        IReadOnlyList<Guid> platformGlobalProductIds,
        string requestedBy,
        string? platformSessionToken,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
        if (!_governance.CanCreateOrganizationStandard(_actorAccessor.GetActor()))
        {
            return ApplicationResult<PosCatalogImportJobDto>.Failure(
                ApplicationErrorCodes.ProductScopeForbidden,
                "Global catalog import creates OrganizationStandard products and requires organization Owner/Administrator.");
        }

        try
        {
            var orgId = PosOrganizationId.From(organizationId);
            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                var existing = await _imports
                    .FindByIdempotencyKeyAsync(orgId, idempotencyKey.Trim(), cancellationToken)
                    .ConfigureAwait(false);
                if (existing is not null)
                {
                    return ApplicationResult<PosCatalogImportJobDto>.Success(CatalogImportQueryService.Map(existing));
                }
            }

            var ids = platformGlobalProductIds
                .Where(id => id != Guid.Empty)
                .Distinct()
                .Take(CatalogImportRules.MaxItemsPerJob)
                .ToList();
            if (ids.Count == 0)
            {
                return ApplicationResult<PosCatalogImportJobDto>.Failure(
                    ApplicationErrorCodes.CatalogImportNoProducts,
                    "At least one Platform global product id is required.");
            }

            var already = await _products
                .ListPlatformGlobalProductIdsAsync(orgId, ids, cancellationToken)
                .ConfigureAwait(false);
            var candidateIds = ids.Where(id => !already.Contains(id)).ToList();
            if (candidateIds.Count == 0)
            {
                return ApplicationResult<PosCatalogImportJobDto>.Failure(
                    ApplicationErrorCodes.CatalogImportProductAlreadyImported,
                    "All selected products are already in your catalog.");
            }

            IReadOnlyList<PlatformMerchantGlobalProductDto> products;
            try
            {
                products = await _platform
                    .GetActiveProductsAsync(candidateIds, platformSessionToken, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex) when (ImportTemplateBatch.TryMapPlatformFailure<PosCatalogImportJobDto>(ex, cancellationToken, out var mapped))
            {
                return mapped!;
            }

            if (products.Count == 0)
            {
                return ApplicationResult<PosCatalogImportJobDto>.Failure(
                    ApplicationErrorCodes.CatalogImportNoProducts,
                    "No entitled Platform products were found for the requested ids.");
            }

            var categoryNames = await LoadCategoryNamesAsync(
                    products
                        .Where(p => p.GlobalCategoryId is Guid)
                        .Select(p => p.GlobalCategoryId!.Value),
                    platformSessionToken,
                    cancellationToken)
                .ConfigureAwait(false);

            var items = products
                .Select((p, index) => CatalogImportItemResult.CreatePending(
                    p.Id,
                    index,
                    p.Name,
                    p.Unit,
                    p.SellingPrice ?? 0m,
                    p.Description,
                    p.Sku,
                    p.Barcode,
                    p.GlobalCategoryId,
                    sourceCategoryName: p.GlobalCategoryId is Guid cid
                        && categoryNames.TryGetValue(cid, out var categoryName)
                        ? categoryName
                        : null,
                    sellingMode: string.IsNullOrWhiteSpace(p.SellingMode) ? "PerItem" : p.SellingMode))
                .ToList();

            var job = CatalogImportJob.CreateQueued(
                orgId,
                PosCatalogImportJobKind.SelectedProducts,
                CatalogSource.GlobalSearch,
                requestedBy,
                items,
                _clock.UtcNow,
                platformTemplateId: null,
                batchNumber: null,
                idempotencyKey);

            await _imports.AddAsync(job, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<PosCatalogImportJobDto>.Success(CatalogImportQueryService.Map(job));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PosCatalogImportJobDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<PosCatalogImportJobDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    private async Task<Dictionary<Guid, string>> LoadCategoryNamesAsync(
        IEnumerable<Guid> categoryIds,
        string? platformSessionToken,
        CancellationToken cancellationToken)
    {
        var wanted = categoryIds.Where(id => id != Guid.Empty).Distinct().ToHashSet();
        if (wanted.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var found = new Dictionary<Guid, string>();
        var page = 1;
        const int pageSize = 100;
        while (found.Count < wanted.Count && page <= 20)
        {
            var batch = await _platform
                .ListActiveCategoriesAsync(
                    search: null,
                    businessTypeCode: null,
                    parentId: null,
                    page: page,
                    pageSize: pageSize,
                    platformSessionToken,
                    cancellationToken)
                .ConfigureAwait(false);

            foreach (var category in batch.Items)
            {
                if (wanted.Contains(category.Id) && !string.IsNullOrWhiteSpace(category.Name))
                {
                    found[category.Id] = category.Name.Trim();
                }
            }

            if (batch.Items.Count < pageSize)
            {
                break;
            }

            page++;
        }

        return found;
    }
}

/// <summary>
/// Batched org-scoped lookup: which Platform global product ids already map to a local product.
/// </summary>
public sealed class ListImportedGlobalProducts
{
    private readonly ICatalogProductRepository _products;

    public ListImportedGlobalProducts(ICatalogProductRepository products) => _products = products;

    public async Task<ApplicationResult<ImportedGlobalProductsDto>> ExecuteAsync(
        Guid organizationId,
        IReadOnlyList<Guid>? platformGlobalProductIds,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var ids = (platformGlobalProductIds ?? Array.Empty<Guid>())
            .Where(id => id != Guid.Empty)
            .Distinct()
            .Take(CatalogImportRules.MaxItemsPerJob)
            .ToList();
        if (ids.Count == 0)
        {
            return ApplicationResult<ImportedGlobalProductsDto>.Success(new ImportedGlobalProductsDto([]));
        }

        var found = await _products
            .ListPlatformGlobalProductIdsAsync(orgId, ids, cancellationToken)
            .ConfigureAwait(false);
        return ApplicationResult<ImportedGlobalProductsDto>.Success(
            new ImportedGlobalProductsDto(found.OrderBy(id => id).ToList()));
    }
}

/// <summary>Processes one claimed POS catalog import job in chunks. Invoked by the hosted worker.</summary>
public sealed class ProcessPosCatalogImportChunk
{
    private readonly ICatalogImportJobRepository _imports;
    private readonly ICatalogProductRepository _products;
    private readonly IProductCategoryRepository _categories;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    /// <summary>
    /// In-chunk category memo. Repository finds use AsNoTracking and cannot see pending Adds,
    /// so multiple items sharing a Platform source category would otherwise insert duplicate
    /// Active names and trip <c>ux_product_categories_org_active_name</c>.
    /// </summary>
    private readonly Dictionary<Guid, ProductCategoryId> _categoriesBySourceId = new();
    private readonly Dictionary<string, ProductCategoryId> _categoriesByNormalizedName =
        new(StringComparer.Ordinal);

    public ProcessPosCatalogImportChunk(
        ICatalogImportJobRepository imports,
        ICatalogProductRepository products,
        IProductCategoryRepository categories,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _imports = imports;
        _products = products;
        _categories = categories;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<bool> ExecuteOnceAsync(CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var job = await _imports
            .ClaimNextAsync(now, TimeSpan.FromSeconds(CatalogImportRules.HeartbeatStaleSeconds), cancellationToken)
            .ConfigureAwait(false);
        if (job is null)
        {
            return false;
        }

        _categoriesBySourceId.Clear();
        _categoriesByNormalizedName.Clear();

        try
        {
            job.BeginProcessing(now);
            await _imports.UpdateAsync(job, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            while (job.PendingCount > 0)
            {
                var chunk = job.Items
                    .Where(i => i.Status == PosCatalogImportItemStatus.Pending)
                    .Take(CatalogImportRules.ProcessingChunkSize)
                    .ToList();

                foreach (var item in chunk)
                {
                    await ProcessItemAsync(job, item, cancellationToken).ConfigureAwait(false);
                }

                job.RecalculateProgress(_clock.UtcNow);
                await _imports.UpdateAsync(job, cancellationToken).ConfigureAwait(false);
                await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            job.Complete(_clock.UtcNow);
            await _imports.UpdateAsync(job, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var fresh = await _imports.GetByIdAsync(job.OrganizationId, job.Id, cancellationToken)
                .ConfigureAwait(false);
            if (fresh is not null && fresh.Status == PosCatalogImportJobStatus.Processing)
            {
                fresh.Fail(
                    $"Import processing failed: {ex.GetType().Name}: {ex.Message}",
                    _clock.UtcNow);
                await _imports.UpdateAsync(fresh, cancellationToken).ConfigureAwait(false);
                await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            return true;
        }
    }

    private async Task ProcessItemAsync(
        CatalogImportJob job,
        CatalogImportItemResult item,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        if (item.Status is not PosCatalogImportItemStatus.Pending)
        {
            return;
        }

        try
        {
            var existingByGlobal = await _products
                .FindByPlatformGlobalProductIdAsync(
                    job.OrganizationId,
                    item.PlatformGlobalProductId,
                    cancellationToken)
                .ConfigureAwait(false);
            if (existingByGlobal is not null)
            {
                item.MarkSkipped(
                    ApplicationErrorCodes.CatalogImportProductAlreadyImported,
                    "Product was already imported from the global catalog.",
                    now);
                return;
            }

            if (!UnitOfMeasures.TryParse(item.UnitOfMeasure, out var unit))
            {
                item.MarkFailed(
                    DomainErrorCodes.InvalidUnitOfMeasure,
                    $"Unrecognized unit of measure '{item.UnitOfMeasure}'.",
                    now);
                return;
            }

            SellingMode sellingMode;
            try
            {
                sellingMode = SellingModes.Parse(item.SellingMode);
                SellingModes.EnsureCompatible(sellingMode, unit);
            }
            catch (DomainException ex)
            {
                item.MarkFailed(ex.ErrorCode, ex.Message, now);
                return;
            }

            string? normalizedSku;
            string? barcode;
            try
            {
                (_, normalizedSku) = CatalogProduct.NormalizeOptionalSku(item.Sku);
            }
            catch (DomainException ex)
            {
                item.MarkFailed(ex.ErrorCode, ex.Message, now);
                return;
            }

            try
            {
                barcode = CatalogProduct.NormalizeOptionalBarcode(item.Barcode);
            }
            catch (DomainException ex) when (string.Equals(
                ex.ErrorCode,
                DomainErrorCodes.InvalidProductBarcode,
                StringComparison.Ordinal))
            {
                // Defensive: legacy Platform snapshots may still carry non-GS1 text.
                // Do not persist invalid barcodes; import without a POS barcode.
                barcode = null;
            }

            var conflict = await CatalogAssignment
                .FindIdentifierConflictAsync(
                    _products,
                    job.OrganizationId,
                    normalizedSku,
                    barcode,
                    selfId: null,
                    cancellationToken)
                .ConfigureAwait(false);
            if (conflict is not null)
            {
                item.MarkSkipped(conflict.ErrorCode!, conflict.ErrorMessage!, now);
                return;
            }

            string normalizedName;
            try
            {
                (_, normalizedName) = CatalogProduct.NormalizeProductName(item.Name);
            }
            catch (DomainException ex)
            {
                item.MarkFailed(ex.ErrorCode, ex.Message, now);
                return;
            }

            var nameConflict = await CatalogAssignment
                .FindProductNameConflictAsync(
                    _products,
                    job.OrganizationId,
                    normalizedName,
                    selfId: null,
                    cancellationToken)
                .ConfigureAwait(false);
            if (nameConflict is not null)
            {
                item.MarkSkipped(nameConflict.ErrorCode!, nameConflict.ErrorMessage!, now);
                return;
            }

            ProductCategoryId? categoryId = null;
            if (item.SourceGlobalCategoryId is Guid sourceCategoryId)
            {
                categoryId = await EnsureCategoryAsync(
                        job.OrganizationId,
                        sourceCategoryId,
                        item.SourceCategoryName,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            else if (!string.IsNullOrWhiteSpace(item.SourceCategoryName))
            {
                categoryId = await EnsureCategoryByNameAsync(
                        job.OrganizationId,
                        item.SourceCategoryName,
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            var product = CatalogProduct.CreateImportedSnapshot(
                job.OrganizationId,
                item.Name,
                unit,
                item.SuggestedPrice,
                item.PlatformGlobalProductId,
                job.CatalogSource,
                now,
                item.Description,
                item.Sku,
                barcode,
                categoryId,
                job.PlatformTemplateId,
                item.SourceGlobalCategoryId,
                sellingMode: sellingMode);
            // Shared Platform template images are referenced by PlatformGlobalProductId.
            // Import must not copy image files or create pos.product_images rows.

            await _products.AddAsync(product, cancellationToken).ConfigureAwait(false);
            item.MarkImported(product.Id, now);
        }
        catch (DomainException ex)
        {
            item.MarkFailed(ex.ErrorCode, ex.Message, now);
        }
        catch (PersistenceConflictException ex)
        {
            item.MarkSkipped(ex.ErrorCode, ex.Message, now);
        }
    }

    private async Task<ProductCategoryId?> EnsureCategoryAsync(
        PosOrganizationId organizationId,
        Guid sourceGlobalCategoryId,
        string? fallbackName,
        CancellationToken cancellationToken)
    {
        if (_categoriesBySourceId.TryGetValue(sourceGlobalCategoryId, out var cached))
        {
            return cached;
        }

        var existing = await _categories
            .FindActiveBySourceGlobalCategoryIdAsync(organizationId, sourceGlobalCategoryId, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            if (!string.IsNullOrWhiteSpace(fallbackName)
                && IsImportedPlaceholderName(existing.Name, sourceGlobalCategoryId))
            {
                try
                {
                    existing.Rename(fallbackName, _clock.UtcNow);
                    await _categories.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
                }
                catch (DomainException)
                {
                    // Keep the placeholder name if rename conflicts or is invalid.
                }
            }

            RememberCategory(existing.Id, existing.NormalizedName, sourceGlobalCategoryId);
            return existing.Id;
        }

        var name = string.IsNullOrWhiteSpace(fallbackName)
            ? ImportedPlaceholderName(sourceGlobalCategoryId)
            : fallbackName;
        return await EnsureCategoryByNameAsync(organizationId, name, cancellationToken, sourceGlobalCategoryId)
            .ConfigureAwait(false);
    }

    /// <summary>Legacy / last-resort category label when Platform did not supply a name.</summary>
    internal static string ImportedPlaceholderName(Guid sourceGlobalCategoryId)
    {
        var raw = $"Imported-{sourceGlobalCategoryId:N}";
        return raw.Length <= ProductCategory.NameMaxLength
            ? raw
            : raw[..ProductCategory.NameMaxLength];
    }

    private static bool IsImportedPlaceholderName(string name, Guid sourceGlobalCategoryId)
    {
        if (string.Equals(name, ImportedPlaceholderName(sourceGlobalCategoryId), StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Match historically truncated "Imported-{guid:N}"[..40] labels (8+32 miscount of "Imported-").
        var legacy = $"Imported-{sourceGlobalCategoryId:N}";
        var legacyTruncated = legacy[..Math.Min(40, legacy.Length)];
        return string.Equals(name, legacyTruncated, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<ProductCategoryId?> EnsureCategoryByNameAsync(
        PosOrganizationId organizationId,
        string name,
        CancellationToken cancellationToken,
        Guid? sourceGlobalCategoryId = null)
    {
        string normalized;
        try
        {
            normalized = ProductCategory.NormalizeForLookup(name);
        }
        catch (DomainException)
        {
            return null;
        }

        if (_categoriesByNormalizedName.TryGetValue(normalized, out var cachedByName))
        {
            if (sourceGlobalCategoryId is Guid sourceId)
            {
                _categoriesBySourceId[sourceId] = cachedByName;
            }

            return cachedByName;
        }

        var existing = await _categories
            .FindActiveByNormalizedNameAsync(organizationId, normalized, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            RememberCategory(existing.Id, existing.NormalizedName, sourceGlobalCategoryId);
            return existing.Id;
        }

        var category = ProductCategory.Create(
            organizationId,
            name,
            _clock.UtcNow,
            sourceGlobalCategoryId: sourceGlobalCategoryId);
        await _categories.AddAsync(category, cancellationToken).ConfigureAwait(false);
        RememberCategory(category.Id, category.NormalizedName, sourceGlobalCategoryId);
        return category.Id;
    }

    private void RememberCategory(
        ProductCategoryId categoryId,
        string normalizedName,
        Guid? sourceGlobalCategoryId)
    {
        _categoriesByNormalizedName[normalizedName] = categoryId;
        if (sourceGlobalCategoryId is Guid sourceId && sourceId != Guid.Empty)
        {
            _categoriesBySourceId[sourceId] = categoryId;
        }
    }
}
