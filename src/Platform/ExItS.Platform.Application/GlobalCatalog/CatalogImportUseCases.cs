using ExItS.Platform.Application.Audit;
using ExItS.Platform.Application.Authorization;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.GlobalCatalog;

namespace ExItS.Platform.Application.GlobalCatalog;

public sealed class CatalogImportQueryService
{
    private readonly ICatalogImportJobRepository _imports;

    public CatalogImportQueryService(ICatalogImportJobRepository imports) => _imports = imports;

    public async Task<CatalogImportJobDto?> GetByIdAsync(
        Guid id,
        bool includePreview = true,
        CancellationToken cancellationToken = default)
    {
        var job = await _imports.GetByIdAsync(CatalogImportJobId.From(id), cancellationToken)
            .ConfigureAwait(false);
        return job is null ? null : GlobalCatalogDtoMaps.Map(job, includePreview);
    }

    public async Task<PagedResult<CatalogImportJobDto>> ListAsync(
        CatalogImportJobStatus? status,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = CatalogPagination.Normalize(page, pageSize);
        var (items, total) = await _imports.ListAsync(status, skip, take, cancellationToken)
            .ConfigureAwait(false);
        return new PagedResult<CatalogImportJobDto>(
            items.Select(j => GlobalCatalogDtoMaps.Map(j, includePreviewItems: false)).ToList(),
            total,
            Math.Max(page ?? 1, 1),
            take);
    }

    public async Task<PagedResult<CatalogImportErrorDto>> ListErrorsAsync(
        Guid id,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var jobId = CatalogImportJobId.From(id);
        var existing = await _imports.GetByIdAsync(jobId, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            return new PagedResult<CatalogImportErrorDto>([], 0, Math.Max(page ?? 1, 1), pageSize ?? 50);
        }

        var (skip, take) = CatalogPagination.Normalize(page, pageSize);
        var errors = await _imports.ListErrorsAsync(jobId, skip, take, cancellationToken)
            .ConfigureAwait(false);
        var total = existing.FailedCount + existing.SkippedCount;
        return new PagedResult<CatalogImportErrorDto>(errors, total, Math.Max(page ?? 1, 1), take);
    }
}

public sealed class CreateCatalogImport
{
    private readonly ICatalogImportJobRepository _imports;
    private readonly ICatalogImportFileParser _parser;
    private readonly IGlobalCategoryRepository _categories;
    private readonly IGlobalProductRepository _products;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IPlatformActorAccessor _actor;

    public CreateCatalogImport(
        ICatalogImportJobRepository imports,
        ICatalogImportFileParser parser,
        IGlobalCategoryRepository categories,
        IGlobalProductRepository products,
        IPlatformUnitOfWork unitOfWork,
        IClock clock,
        IPlatformActorAccessor actor)
    {
        _imports = imports;
        _parser = parser;
        _categories = categories;
        _products = products;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _actor = actor;
    }

    public async Task<ApplicationResult<CatalogImportJobDto>> ExecuteAsync(
        Stream content,
        string fileName,
        string? contentType,
        long? declaredLength,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                var existing = await _imports
                    .GetByIdempotencyKeyAsync(
                        CatalogImportRules.NormalizeIdempotencyKey(idempotencyKey),
                        cancellationToken)
                    .ConfigureAwait(false);
                if (existing is not null)
                {
                    return ApplicationResult<CatalogImportJobDto>.Success(
                        GlobalCatalogDtoMaps.Map(existing, includePreviewItems: true));
                }
            }

            await using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
            var bytes = buffer.ToArray();
            var length = declaredLength is > 0 ? declaredLength.Value : bytes.LongLength;
            CatalogImportRules.EnsureFileSize(length);
            if (bytes.LongLength != length && declaredLength is > 0)
            {
                CatalogImportRules.EnsureFileSize(bytes.LongLength);
            }

            if (bytes.Length == 0)
            {
                return ApplicationResult<CatalogImportJobDto>.Failure(
                    ApplicationErrorCodes.CatalogImportEmpty,
                    "Uploaded file is empty.");
            }

            var normalizedName = CatalogImportRules.NormalizeFileName(fileName);
            CatalogImportFileFormat format;
            try
            {
                format = CatalogImportRules.ResolveFormat(normalizedName, contentType);
            }
            catch (DomainException ex)
            {
                return ApplicationResult<CatalogImportJobDto>.Failure(
                    ApplicationErrorCodes.CatalogImportUnsupportedType,
                    ex.Message);
            }

            if (bytes.LongLength > CatalogImportRules.MaxFileBytes)
            {
                return ApplicationResult<CatalogImportJobDto>.Failure(
                    ApplicationErrorCodes.CatalogImportFileTooLarge,
                    $"File exceeds the maximum size of {CatalogImportRules.MaxFileBytes / (1024 * 1024)} MB.");
            }

            buffer.Position = 0;
            IReadOnlyList<CatalogImportRawRow> rows;
            try
            {
                rows = await _parser.ParseAsync(buffer, format, cancellationToken).ConfigureAwait(false);
            }
            catch (DomainException ex)
            {
                return ApplicationResult<CatalogImportJobDto>.Failure(ex.ErrorCode, ex.Message);
            }

            if (rows.Count == 0)
            {
                return ApplicationResult<CatalogImportJobDto>.Failure(
                    ApplicationErrorCodes.CatalogImportEmpty,
                    "Import file contains no data rows.");
            }

            if (rows.Count > CatalogImportRules.MaxRows)
            {
                return ApplicationResult<CatalogImportJobDto>.Failure(
                    ApplicationErrorCodes.CatalogImportTooManyRows,
                    $"Import exceeds the maximum of {CatalogImportRules.MaxRows} rows.");
            }

            var now = _clock.UtcNow;
            var items = await CatalogImportRowMapper
                .MapRowsAsync(rows, _categories, _products, now, cancellationToken)
                .ConfigureAwait(false);

            var actor = _actor.GetCurrent().ActorIdentifier;
            var job = CatalogImportJob.CreateValidated(
                normalizedName,
                format,
                bytes.LongLength,
                CatalogImportRowMapper.ComputeSha256Hex(bytes),
                actor,
                items,
                now,
                contentType,
                string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey);

            await _imports.AddAsync(job, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<CatalogImportJobDto>.Success(
                GlobalCatalogDtoMaps.Map(job, includePreviewItems: true));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<CatalogImportJobDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                var existing = await _imports
                    .GetByIdempotencyKeyAsync(
                        CatalogImportRules.NormalizeIdempotencyKey(idempotencyKey),
                        cancellationToken)
                    .ConfigureAwait(false);
                if (existing is not null)
                {
                    return ApplicationResult<CatalogImportJobDto>.Success(
                        GlobalCatalogDtoMaps.Map(existing, includePreviewItems: true));
                }
            }

            return ApplicationResult<CatalogImportJobDto>.Failure(
                ApplicationErrorCodes.CatalogImportIdempotencyConflict,
                ex.Message);
        }
    }
}

public sealed class ConfirmCatalogImport
{
    private readonly ICatalogImportJobRepository _imports;
    private readonly ICatalogTemplateRepository _templates;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ConfirmCatalogImport(
        ICatalogImportJobRepository imports,
        ICatalogTemplateRepository templates,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _imports = imports;
        _templates = templates;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public Task<ApplicationResult<CatalogImportJobDto>> ExecuteAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(id, request: null, cancellationToken);

    public async Task<ApplicationResult<CatalogImportJobDto>> ExecuteAsync(
        Guid id,
        ConfirmCatalogImportRequest? request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var job = await _imports.GetByIdAsync(CatalogImportJobId.From(id), cancellationToken)
                .ConfigureAwait(false);
            if (job is null)
            {
                return ApplicationResult<CatalogImportJobDto>.Failure(
                    ApplicationErrorCodes.CatalogImportJobNotFound,
                    "Import job was not found.");
            }

            if (job.Status is CatalogImportJobStatus.Queued
                or CatalogImportJobStatus.Processing
                or CatalogImportJobStatus.Completed
                or CatalogImportJobStatus.CompletedWithWarnings)
            {
                // Idempotent confirm / re-fetch.
                return ApplicationResult<CatalogImportJobDto>.Success(
                    await MapWithTemplateAsync(job, cancellationToken).ConfigureAwait(false));
            }

            CatalogTemplate? targetTemplate = null;
            var targetTemplateId = request?.TargetTemplateId;
            if (targetTemplateId is Guid templateId)
            {
                targetTemplate = await _templates
                    .GetByIdAsync(CatalogTemplateId.From(templateId), cancellationToken)
                    .ConfigureAwait(false);
                if (targetTemplate is null)
                {
                    return ApplicationResult<CatalogImportJobDto>.Failure(
                        ApplicationErrorCodes.CatalogTemplateNotFound,
                        "Target catalog template was not found.");
                }

                if (targetTemplate.Status == CatalogTemplateStatus.Archived)
                {
                    return ApplicationResult<CatalogImportJobDto>.Failure(
                        DomainErrorCodes.InvalidCatalogTemplateStatusTransition,
                        "An archived CatalogTemplate cannot receive import assignments.");
                }
            }

            job.Confirm(_clock.UtcNow, targetTemplateId);
            await _imports.UpdateAsync(job, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<CatalogImportJobDto>.Success(
                GlobalCatalogDtoMaps.Map(job, includePreviewItems: true, targetTemplate));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<CatalogImportJobDto>.Failure(
                ex.ErrorCode == DomainErrorCodes.CatalogImportNoConfirmableRows
                    ? ApplicationErrorCodes.CatalogImportNotConfirmable
                    : ex.ErrorCode,
                ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<CatalogImportJobDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    private async Task<CatalogImportJobDto> MapWithTemplateAsync(
        CatalogImportJob job,
        CancellationToken cancellationToken)
    {
        CatalogTemplate? template = null;
        if (job.TargetTemplateId is Guid templateId)
        {
            template = await _templates
                .GetByIdAsync(CatalogTemplateId.From(templateId), cancellationToken)
                .ConfigureAwait(false);
        }

        return GlobalCatalogDtoMaps.Map(job, includePreviewItems: true, template);
    }
}

/// <summary>Processes one claimed import job in chunks. Invoked by the hosted worker.</summary>
public sealed class ProcessCatalogImportChunk
{
    private readonly ICatalogImportJobRepository _imports;
    private readonly IGlobalProductRepository _products;
    private readonly IGlobalCategoryRepository _categories;
    private readonly ICatalogTemplateRepository _templates;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IAuditWriter _auditWriter;
    private readonly Dictionary<string, GlobalCategoryId> _categoryCache =
        new(StringComparer.Ordinal);

    public ProcessCatalogImportChunk(
        ICatalogImportJobRepository imports,
        IGlobalProductRepository products,
        IGlobalCategoryRepository categories,
        IPlatformUnitOfWork unitOfWork,
        IClock clock,
        IAuditWriter auditWriter,
        ICatalogTemplateRepository? templates = null)
    {
        _imports = imports;
        _products = products;
        _categories = categories;
        _templates = templates ?? new NullCatalogTemplateRepository();
        _unitOfWork = unitOfWork;
        _clock = clock;
        _auditWriter = auditWriter;
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

        _categoryCache.Clear();

        try
        {
            job.BeginProcessing(now);
            await _imports.UpdateAsync(job, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            while (job.PendingCount > 0)
            {
                var chunk = job.Items
                    .Where(i => i.Status == CatalogImportItemStatus.Pending)
                    .Take(CatalogImportRules.ProcessingChunkSize)
                    .ToList();

                foreach (var item in chunk)
                {
                    await ProcessItemAsync(job, item, cancellationToken).ConfigureAwait(false);
                }

                await LinkResolvedProductsToTemplateAsync(job, cancellationToken).ConfigureAwait(false);

                job.RecalculateProgress(_clock.UtcNow);
                await _imports.UpdateAsync(job, cancellationToken).ConfigureAwait(false);
                await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            await LinkResolvedProductsToTemplateAsync(job, cancellationToken).ConfigureAwait(false);
            job.Complete(_clock.UtcNow);
            await _imports.UpdateAsync(job, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Re-load and fail permanently only for non-transient unexpected errors after reclaim path.
            var fresh = await _imports.GetByIdAsync(job.Id, cancellationToken).ConfigureAwait(false);
            if (fresh is not null && fresh.Status == CatalogImportJobStatus.Processing)
            {
                // Leave as Processing so heartbeat reclaim can retry; only fail when no pending remain?
                // For unexpected exceptions, mark Failed with summary.
                fresh.Fail($"Import processing failed: {ex.GetType().Name}", _clock.UtcNow);
                await _imports.UpdateAsync(fresh, cancellationToken).ConfigureAwait(false);
                await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            return true;
        }
    }

    private async Task LinkResolvedProductsToTemplateAsync(
        CatalogImportJob job,
        CancellationToken cancellationToken)
    {
        if (job.TargetTemplateId is not Guid templateId)
        {
            return;
        }

        var productIds = job.Items
            .Where(i => i.CreatedGlobalProductId is Guid)
            .Select(i => i.CreatedGlobalProductId!.Value)
            .Distinct()
            .ToList();
        if (productIds.Count == 0)
        {
            return;
        }

        var template = await _templates
            .GetByIdAsync(CatalogTemplateId.From(templateId), cancellationToken)
            .ConfigureAwait(false);
        if (template is null || template.Status == CatalogTemplateStatus.Archived)
        {
            return;
        }

        var linkNow = _clock.UtcNow;
        var linkedAny = false;
        foreach (var productId in productIds)
        {
            if (template.TryAssignProduct(GlobalProductId.From(productId), linkNow))
            {
                linkedAny = true;
            }
        }

        if (linkedAny)
        {
            await _templates.UpdateAsync(template, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ProcessItemAsync(
        CatalogImportJob job,
        CatalogImportItem item,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;

        // Idempotent: already finished.
        if (item.Status is not CatalogImportItemStatus.Pending)
        {
            return;
        }

        try
        {
            if (!Enum.TryParse<ProductUnit>(item.Unit, ignoreCase: true, out var unit))
            {
                item.MarkFailed(
                    DomainErrorCodes.InvalidGlobalProductUnit,
                    $"Unrecognized product unit '{item.Unit}'.",
                    now);
                return;
            }

            GlobalCategoryId categoryId;
            try
            {
                var resolvedCategoryId = await ResolveOrCreateCategoryAsync(job, item, now, cancellationToken)
                    .ConfigureAwait(false);
                if (resolvedCategoryId is null)
                {
                    item.MarkFailed(
                        DomainErrorCodes.InvalidGlobalProductCategory,
                        "Category is required.",
                        now);
                    return;
                }

                categoryId = resolvedCategoryId;
            }
            catch (DomainException ex)
            {
                item.MarkFailed(ex.ErrorCode, ex.Message, now);
                return;
            }

            if (string.IsNullOrWhiteSpace(item.Sku) || string.IsNullOrWhiteSpace(item.Barcode))
            {
                item.MarkFailed(
                    DomainErrorCodes.CatalogImportRowInvalid,
                    "SKU and barcode are required.",
                    now);
                return;
            }

            if (await _products.ExistsWithBarcodeAsync(item.Barcode, excludingId: null, cancellationToken)
                    .ConfigureAwait(false))
            {
                var existingId = await ResolveExistingProductIdAsync(
                        barcode: item.Barcode,
                        sku: null,
                        cancellationToken)
                    .ConfigureAwait(false);
                item.MarkSkipped(
                    ApplicationErrorCodes.DuplicateGlobalProductBarcode,
                    $"Barcode '{item.Barcode}' already exists in the global catalog.",
                    now,
                    existingId);
                return;
            }

            if (await _products.ExistsWithSkuAsync(item.Sku, excludingId: null, cancellationToken)
                    .ConfigureAwait(false))
            {
                var existingId = await ResolveExistingProductIdAsync(
                        barcode: null,
                        sku: item.Sku,
                        cancellationToken)
                    .ConfigureAwait(false);
                item.MarkSkipped(
                    ApplicationErrorCodes.DuplicateGlobalProductSku,
                    $"SKU '{item.Sku}' already exists in the global catalog.",
                    now,
                    existingId);
                return;
            }

            var (rawTags, targetStatus) = CatalogImportRowMapper.SplitTagsAndStatus(item.SearchTagsRaw);
            var tags = rawTags.ToList();
            var brand = CatalogImportRowMapper.ExtractBrand(tags);
            var product = GlobalProduct.Create(
                item.Name,
                unit,
                item.Sku,
                item.Barcode,
                brand,
                categoryId,
                now,
                item.CostPrice,
                item.SellingPrice,
                item.Description,
                item.ImageReference,
                tags,
                CatalogImportRowMapper.ParseBusinessTypes(item.BusinessTypesRaw));

            if (targetStatus != GlobalProductStatus.Draft)
            {
                product.SetStatus(targetStatus, now);
            }

            await _products.AddAsync(product, cancellationToken).ConfigureAwait(false);
            item.MarkImported(product.Id.Value, now);
        }
        catch (DomainException ex)
        {
            item.MarkFailed(ex.ErrorCode, ex.Message, now);
        }
        catch (PersistenceConflictException ex)
        {
            // Unique conflict mid-import → skip as duplicate (partial success).
            item.MarkSkipped(ex.ErrorCode, ex.Message, now);
        }
        catch (Exception ex) when (IsTransient(ex))
        {
            item.RecordTransientAttempt(now);
            if (item.AttemptCount >= CatalogImportRules.MaxTransientAttempts)
            {
                item.MarkFailed(
                    ApplicationErrorCodes.CatalogImportNotConfirmable,
                    $"Transient failure exceeded retry limit: {ex.Message}",
                    now);
            }
            else
            {
                // Leave Pending for restart-safe retry on next chunk/claim.
                throw;
            }
        }
    }

    private async Task<GlobalCategoryId?> ResolveOrCreateCategoryAsync(
        CatalogImportJob job,
        CatalogImportItem item,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (item.GlobalCategoryId is Guid cid)
        {
            var category = await _categories
                .GetByIdAsync(GlobalCategoryId.From(cid), cancellationToken)
                .ConfigureAwait(false);
            if (category is null)
            {
                throw new DomainException(
                    ApplicationErrorCodes.GlobalCategoryNotFound,
                    "Category was not found.");
            }

            return category.Id;
        }

        if (string.IsNullOrWhiteSpace(item.CategoryName))
        {
            return null;
        }

        var name = GlobalCatalogRules.NormalizeName(item.CategoryName);
        var cacheKey = name.ToUpperInvariant();
        if (_categoryCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var resolved = await FindCategoryByNormalizedNameAsync(name, cancellationToken)
            .ConfigureAwait(false);
        if (resolved is not null)
        {
            _categoryCache[cacheKey] = resolved.Id;
            return resolved.Id;
        }

        try
        {
            var created = GlobalCategory.Create(name, now);
            await _categories.AddAsync(created, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await _auditWriter.WriteAsync(
                job.RequestedBy,
                AuditActorType.PlatformUser,
                PlatformAuditActions.GlobalCategoryCreated,
                nameof(GlobalCategory),
                created.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                summary: $"Created global category '{created.Name}' during catalog import {job.Id.Value:D}.",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            _categoryCache[cacheKey] = created.Id;
            return created.Id;
        }
        catch (PersistenceConflictException)
        {
            // Concurrent import created the same root name — reuse the winner.
            var afterConflict = await FindCategoryByNormalizedNameAsync(name, cancellationToken)
                .ConfigureAwait(false);
            if (afterConflict is null)
            {
                throw;
            }

            _categoryCache[cacheKey] = afterConflict.Id;
            return afterConflict.Id;
        }
    }

    private async Task<GlobalCategory?> FindCategoryByNormalizedNameAsync(
        string normalizedName,
        CancellationToken cancellationToken)
    {
        var matches = await _categories
            .FindByNormalizedNameAsync(normalizedName.ToUpperInvariant(), cancellationToken)
            .ConfigureAwait(false);
        if (matches.Count == 0)
        {
            return null;
        }

        var resolved = CatalogImportRowMapper.ResolveUniqueCategoryMatch(matches, normalizedName);
        if (resolved is null)
        {
            throw new DomainException(
                DomainErrorCodes.CatalogImportRowInvalid,
                $"Category name '{normalizedName}' is ambiguous; resolve duplicates before import.");
        }

        return resolved;
    }

    private async Task<Guid?> ResolveExistingProductIdAsync(
        string? barcode,
        string? sku,
        CancellationToken cancellationToken)
    {
        var (items, _) = await _products
            .ListAsync(
                status: null,
                categoryId: null,
                businessType: null,
                search: null,
                barcode: barcode,
                sku: sku,
                skip: 0,
                take: 1,
                cancellationToken)
            .ConfigureAwait(false);
        return items.Count > 0 ? items[0].Id.Value : null;
    }

    private static bool IsTransient(Exception ex) =>
        ex is TimeoutException
            or OperationCanceledException
            || ex.GetType().Name.Contains("Timeout", StringComparison.OrdinalIgnoreCase)
            || ex.InnerException is not null && IsTransient(ex.InnerException);
}

file sealed class NullCatalogTemplateRepository : ICatalogTemplateRepository
{
    public Task<CatalogTemplate?> GetByIdAsync(CatalogTemplateId id, CancellationToken cancellationToken = default) =>
        Task.FromResult<CatalogTemplate?>(null);

    public Task<bool> ExistsWithSlugAsync(
        string slug,
        CatalogTemplateId? excludingId = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task<(IReadOnlyList<CatalogTemplate> Items, int TotalCount)> ListAsync(
        CatalogTemplateStatus? status,
        BusinessType? primaryBusinessType,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default,
        CatalogTemplateListSortBy sortBy = CatalogTemplateListSortBy.Name,
        bool sortDescending = false) =>
        Task.FromResult<(IReadOnlyList<CatalogTemplate>, int)>(([], 0));

    public Task AddAsync(CatalogTemplate template, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task UpdateAsync(CatalogTemplate template, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
