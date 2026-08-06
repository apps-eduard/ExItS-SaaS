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
            item.SuggestedPrice,
            item.Status.ToString(),
            item.LocalProductId?.Value,
            item.ErrorCode,
            item.ErrorMessage,
            item.ProcessedAtUtc);
}

public sealed class ImportTemplateBatch
{
    private readonly ICatalogImportJobRepository _imports;
    private readonly ICatalogProductRepository _products;
    private readonly IPlatformMerchantCatalogClient _platform;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ImportTemplateBatch(
        ICatalogImportJobRepository imports,
        ICatalogProductRepository products,
        IPlatformMerchantCatalogClient platform,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _imports = imports;
        _products = products;
        _platform = platform;
        _unitOfWork = unitOfWork;
        _clock = clock;
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
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return ApplicationResult<PosCatalogImportJobDto>.Failure(
                    ApplicationErrorCodes.CatalogImportPlatformUnavailable,
                    "Platform catalog is temporarily unavailable. Existing POS selling is unaffected.");
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

            IReadOnlyList<PlatformMerchantGlobalProductDto> products;
            try
            {
                products = await _platform
                    .GetActiveProductsAsync(
                        remaining.Select(r => r.GlobalProductId).ToList(),
                        platformSessionToken,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return ApplicationResult<PosCatalogImportJobDto>.Failure(
                    ApplicationErrorCodes.CatalogImportPlatformUnavailable,
                    "Platform catalog is temporarily unavailable. Existing POS selling is unaffected.");
            }

            var byId = products.ToDictionary(p => p.Id);
            var items = new List<CatalogImportItemResult>();
            var sort = 0;
            foreach (var link in remaining.OrderBy(r => r.SortOrder))
            {
                if (byId.TryGetValue(link.GlobalProductId, out var product))
                {
                    items.Add(CatalogImportItemResult.CreatePending(
                        product.Id,
                        sort++,
                        product.Name,
                        product.Unit,
                        product.SellingPrice ?? 0m,
                        product.Description,
                        product.Sku,
                        product.Barcode,
                        product.GlobalCategoryId,
                        sourceCategoryName: null));
                    continue;
                }

                // Fall back to enriched template snapshot when live product GET misses an active row.
                if (TryCreatePendingFromTemplateLink(link, sort, out var snapshot))
                {
                    items.Add(snapshot!);
                    sort++;
                }
            }

            if (items.Count == 0)
            {
                return ApplicationResult<PosCatalogImportJobDto>.Failure(
                    ApplicationErrorCodes.CatalogImportNoProducts,
                    "No active Platform products were available for import.");
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
        if (batchNumber <= 1)
        {
            return template.Products.Where(p => p.IsFirstBatch).OrderBy(p => p.SortOrder).ToList();
        }

        return template.Products.Where(p => !p.IsFirstBatch).OrderBy(p => p.SortOrder).ToList();
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
            sourceCategoryName: link.CategoryName);
        return true;
    }
}

public sealed class ImportSelectedProducts
{
    private readonly ICatalogImportJobRepository _imports;
    private readonly IPlatformMerchantCatalogClient _platform;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ImportSelectedProducts(
        ICatalogImportJobRepository imports,
        IPlatformMerchantCatalogClient platform,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _imports = imports;
        _platform = platform;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PosCatalogImportJobDto>> ExecuteAsync(
        Guid organizationId,
        IReadOnlyList<Guid> platformGlobalProductIds,
        string requestedBy,
        string? platformSessionToken,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
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

            IReadOnlyList<PlatformMerchantGlobalProductDto> products;
            try
            {
                products = await _platform
                    .GetActiveProductsAsync(ids, platformSessionToken, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return ApplicationResult<PosCatalogImportJobDto>.Failure(
                    ApplicationErrorCodes.CatalogImportPlatformUnavailable,
                    "Platform catalog is temporarily unavailable. Existing POS selling is unaffected.");
            }

            if (products.Count == 0)
            {
                return ApplicationResult<PosCatalogImportJobDto>.Failure(
                    ApplicationErrorCodes.CatalogImportNoProducts,
                    "No active Platform products were found for the requested ids.");
            }

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
                    sourceCategoryName: null))
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

            string? normalizedSku;
            string? barcode;
            try
            {
                (_, normalizedSku) = CatalogProduct.NormalizeOptionalSku(item.Sku);
                barcode = CatalogProduct.NormalizeOptionalBarcode(item.Barcode);
            }
            catch (DomainException ex)
            {
                item.MarkFailed(ex.ErrorCode, ex.Message, now);
                return;
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
                item.SourceGlobalCategoryId);

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
            RememberCategory(existing.Id, existing.NormalizedName, sourceGlobalCategoryId);
            return existing.Id;
        }

        var name = string.IsNullOrWhiteSpace(fallbackName)
            ? $"Imported-{sourceGlobalCategoryId:N}"[..Math.Min(ProductCategory.NameMaxLength, 8 + 32)]
            : fallbackName;
        return await EnsureCategoryByNameAsync(organizationId, name, cancellationToken, sourceGlobalCategoryId)
            .ConfigureAwait(false);
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
