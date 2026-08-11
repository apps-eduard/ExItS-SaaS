using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.GlobalCatalog;

namespace ExItS.Platform.Application.GlobalCatalog;

public sealed record BusinessTypeDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string Status,
    int SortOrder,
    string? IconReference,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    /// <summary>True when this type is the organization primary (merchant listing only).</summary>
    bool IsPrimary = false);

public sealed record CreateBusinessTypeRequest(
    string Code,
    string Name,
    string? Description = null,
    int SortOrder = 0,
    string? IconReference = null);

public sealed record UpdateBusinessTypeRequest(
    string Name,
    string? Description = null,
    int SortOrder = 0,
    string? IconReference = null,
    DateTimeOffset? ExpectedUpdatedAtUtc = null);

public sealed record SetBusinessTypeStatusRequest(
    string Status,
    DateTimeOffset? ExpectedUpdatedAtUtc = null);

public sealed record BulkAssignCategoryBusinessTypesRequest(
    string Mode = "Replace",
    IReadOnlyList<string>? BusinessTypes = null,
    IReadOnlyList<Guid>? BusinessTypeIds = null,
    DateTimeOffset? ExpectedUpdatedAtUtc = null);

public sealed record GlobalCategoryDto(
    Guid Id,
    string Name,
    Guid? ParentId,
    string? IconReference,
    int SortOrder,
    string Status,
    IReadOnlyList<string> BusinessTypes,
    IReadOnlyList<Guid> BusinessTypeIds,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record GlobalProductDto(
    Guid Id,
    string Name,
    string? Description,
    string Sku,
    string? Barcode,
    string Brand,
    Guid? GlobalCategoryId,
    string Unit,
    decimal? CostPrice,
    decimal? SellingPrice,
    string? ImageReference,
    string Status,
    IReadOnlyList<string> SearchTags,
    IReadOnlyList<string> BusinessTypes,
    IReadOnlyList<Guid> BusinessTypeIds,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record CreateGlobalCategoryRequest(
    string Name,
    Guid? ParentId = null,
    string? IconReference = null,
    int SortOrder = 0,
    IReadOnlyList<string>? BusinessTypes = null,
    IReadOnlyList<Guid>? BusinessTypeIds = null);

public sealed record UpdateGlobalCategoryRequest(
    string Name,
    Guid? ParentId = null,
    string? IconReference = null,
    int SortOrder = 0,
    IReadOnlyList<string>? BusinessTypes = null,
    IReadOnlyList<Guid>? BusinessTypeIds = null,
    DateTimeOffset? ExpectedUpdatedAtUtc = null);

public sealed record SetGlobalCategoryStatusRequest(
    string Status,
    DateTimeOffset? ExpectedUpdatedAtUtc = null);

public sealed record CreateGlobalProductRequest(
    string Name,
    string Unit,
    string Sku,
    string? Barcode,
    string Brand,
    Guid GlobalCategoryId,
    string? Description = null,
    decimal? CostPrice = null,
    decimal? SellingPrice = null,
    string? ImageReference = null,
    IReadOnlyList<string>? SearchTags = null,
    IReadOnlyList<string>? BusinessTypes = null,
    IReadOnlyList<Guid>? BusinessTypeIds = null);

public sealed record UpdateGlobalProductRequest(
    string Name,
    string Unit,
    string Sku,
    string? Barcode,
    string Brand,
    Guid GlobalCategoryId,
    string? Description = null,
    decimal? CostPrice = null,
    decimal? SellingPrice = null,
    string? ImageReference = null,
    IReadOnlyList<string>? SearchTags = null,
    IReadOnlyList<string>? BusinessTypes = null,
    IReadOnlyList<Guid>? BusinessTypeIds = null,
    DateTimeOffset? ExpectedUpdatedAtUtc = null);

public sealed record SetGlobalProductStatusRequest(
    string Status,
    DateTimeOffset? ExpectedUpdatedAtUtc = null);

public sealed record CatalogTemplateProductDto(
    Guid Id,
    Guid GlobalProductId,
    int SortOrder,
    bool IsFeatured,
    bool IsFirstBatch,
    string? ProductName = null,
    string? Sku = null,
    string? Barcode = null,
    string? Brand = null,
    Guid? CategoryId = null,
    string? CategoryName = null,
    string? Status = null,
    string? Unit = null,
    decimal? CostPrice = null,
    decimal? SellingPrice = null);

public sealed record CatalogTemplateDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? IconReference,
    string PrimaryBusinessType,
    Guid PrimaryBusinessTypeId,
    string Status,
    int DefaultBatchSize,
    string SelectionMode,
    DateTimeOffset? PublishedAtUtc,
    int ProductCount,
    int FirstBatchCount,
    IReadOnlyList<CatalogTemplateProductDto> Products,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record CatalogTemplateSummaryDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? IconReference,
    string PrimaryBusinessType,
    Guid PrimaryBusinessTypeId,
    string Status,
    int DefaultBatchSize,
    string SelectionMode,
    DateTimeOffset? PublishedAtUtc,
    int ProductCount,
    int FirstBatchCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record CreateCatalogTemplateRequest(
    string Name,
    string? PrimaryBusinessType = null,
    Guid? PrimaryBusinessTypeId = null,
    string? Slug = null,
    string? Description = null,
    string? IconReference = null,
    int? DefaultBatchSize = null,
    string? SelectionMode = null);

public sealed record UpdateCatalogTemplateRequest(
    string Name,
    string? PrimaryBusinessType = null,
    Guid? PrimaryBusinessTypeId = null,
    string? Slug = null,
    string? Description = null,
    string? IconReference = null,
    int? DefaultBatchSize = null,
    string? SelectionMode = null,
    DateTimeOffset? ExpectedUpdatedAtUtc = null);

public sealed record AssignCatalogTemplateProductRequest(
    Guid GlobalProductId,
    bool IsFeatured = false,
    bool IsFirstBatch = false,
    int? SortOrder = null,
    DateTimeOffset? ExpectedUpdatedAtUtc = null);

public sealed record BulkAssignCatalogTemplateProductsRequest(
    IReadOnlyList<Guid> GlobalProductIds,
    bool IsFeatured = false,
    bool IsFirstBatch = false,
    DateTimeOffset? ExpectedUpdatedAtUtc = null);

public sealed record BulkRemoveCatalogTemplateProductsRequest(
    IReadOnlyList<Guid> GlobalProductIds,
    DateTimeOffset? ExpectedUpdatedAtUtc = null);

public sealed record ReorderCatalogTemplateProductsRequest(
    IReadOnlyList<Guid> OrderedGlobalProductIds,
    DateTimeOffset? ExpectedUpdatedAtUtc = null);

public sealed record UpdateCatalogTemplateProductFlagsRequest(
    bool? IsFeatured = null,
    bool? IsFirstBatch = null,
    DateTimeOffset? ExpectedUpdatedAtUtc = null);

public sealed record CatalogTemplateLifecycleRequest(
    DateTimeOffset? ExpectedUpdatedAtUtc = null);

public sealed record CatalogImportJobDto(
    Guid Id,
    string FileName,
    string FileFormat,
    string? ContentType,
    long FileSizeBytes,
    string FileSha256,
    string? IdempotencyKey,
    string RequestedBy,
    string Status,
    int TotalCount,
    int ProcessedCount,
    int ImportedCount,
    int SkippedCount,
    int FailedCount,
    int PendingCount,
    int ValidProductCount,
    int ExistingCategoriesReferencedCount,
    int NewCategoriesToCreateCount,
    int WarningCount,
    string? PreviewSummary,
    string? CurrentStage,
    string? ErrorSummary,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    DateTimeOffset? LastHeartbeatAtUtc,
    IReadOnlyList<CatalogImportItemDto>? PreviewItems,
    Guid? TargetTemplateId = null,
    string? TargetTemplateName = null,
    int? TargetTemplateProductCount = null,
    int? EstimatedTemplateLinks = null,
    int? ProductsAlreadyInTemplate = null);

public sealed record CatalogImportItemDto(
    Guid Id,
    int RowNumber,
    string Name,
    string? Description,
    string? Sku,
    string? Barcode,
    Guid? GlobalCategoryId,
    string? CategoryName,
    string Unit,
    decimal? CostPrice,
    decimal? SellingPrice,
    string? ImageReference,
    string? SearchTagsRaw,
    string? BusinessTypesRaw,
    string Status,
    string? ErrorCode,
    string? ErrorMessage,
    bool WillCreateCategory,
    Guid? CreatedGlobalProductId,
    int AttemptCount,
    DateTimeOffset? ProcessedAtUtc);

public sealed record CatalogImportPreviewSummary(
    int TotalRows,
    int ValidProductCount,
    int ExistingCategoriesReferencedCount,
    int NewCategoriesToCreateCount,
    int WarningCount,
    int FailedCount,
    int SkippedCount,
    string SummaryText);

public sealed record CatalogImportErrorDto(
    Guid Id,
    int RowNumber,
    string Name,
    string? Sku,
    string? Barcode,
    string Status,
    string? ErrorCode,
    string? ErrorMessage);

public sealed record ConfirmCatalogImportRequest(
    string? IdempotencyKey = null,
    Guid? TargetTemplateId = null);

public sealed record CatalogImportRawRow(
    int RowNumber,
    IReadOnlyDictionary<string, string> Cells);

internal static class GlobalCatalogDtoMaps
{
    public static BusinessTypeDto Map(BusinessType businessType) =>
        new(
            businessType.Id.Value,
            businessType.Code,
            businessType.Name,
            businessType.Description,
            businessType.Status.ToString(),
            businessType.SortOrder,
            businessType.IconReference,
            businessType.CreatedAtUtc,
            businessType.UpdatedAtUtc);

    public static GlobalCategoryDto Map(
        GlobalCategory category,
        IReadOnlyDictionary<Guid, string>? codeLookup = null) =>
        new(
            category.Id.Value,
            category.Name,
            category.ParentId?.Value,
            category.IconReference,
            category.SortOrder,
            category.Status.ToString(),
            MapCodes(category.BusinessTypeIds, codeLookup),
            category.BusinessTypeIds.Select(i => i.Value).ToList(),
            category.CreatedAtUtc,
            category.UpdatedAtUtc);

    public static GlobalProductDto Map(
        GlobalProduct product,
        IReadOnlyDictionary<Guid, string>? codeLookup = null) =>
        new(
            product.Id.Value,
            product.Name,
            product.Description,
            product.Sku,
            product.Barcode,
            product.Brand,
            product.GlobalCategoryId?.Value,
            product.Unit.ToString(),
            product.CostPrice,
            product.SellingPrice,
            product.ImageReference,
            product.Status.ToString(),
            product.SearchTags.ToList(),
            MapCodes(product.BusinessTypeIds, codeLookup),
            product.BusinessTypeIds.Select(i => i.Value).ToList(),
            product.CreatedAtUtc,
            product.UpdatedAtUtc);

    public static CatalogTemplateProductDto Map(CatalogTemplateProduct product) =>
        new(
            product.Id,
            product.GlobalProductId.Value,
            product.SortOrder,
            product.IsFeatured,
            product.IsFirstBatch);

    public static CatalogTemplateDto Map(
        CatalogTemplate template,
        IReadOnlyDictionary<Guid, string>? codeLookup = null) =>
        new(
            template.Id.Value,
            template.Name,
            template.Slug,
            template.Description,
            template.IconReference,
            ResolveCode(template.PrimaryBusinessTypeId, codeLookup),
            template.PrimaryBusinessTypeId.Value,
            template.Status.ToString(),
            template.DefaultBatchSize,
            template.SelectionMode.ToString(),
            template.PublishedAtUtc,
            template.ProductCount,
            template.FirstBatchCount,
            template.Products.Select(Map).ToList(),
            template.CreatedAtUtc,
            template.UpdatedAtUtc);

    public static CatalogTemplateSummaryDto MapSummary(
        CatalogTemplate template,
        IReadOnlyDictionary<Guid, string>? codeLookup = null) =>
        new(
            template.Id.Value,
            template.Name,
            template.Slug,
            template.Description,
            template.IconReference,
            ResolveCode(template.PrimaryBusinessTypeId, codeLookup),
            template.PrimaryBusinessTypeId.Value,
            template.Status.ToString(),
            template.DefaultBatchSize,
            template.SelectionMode.ToString(),
            template.PublishedAtUtc,
            template.ProductCount,
            template.FirstBatchCount,
            template.CreatedAtUtc,
            template.UpdatedAtUtc);

    private static IReadOnlyList<string> MapCodes(
        IReadOnlyList<BusinessTypeId> ids,
        IReadOnlyDictionary<Guid, string>? codeLookup)
    {
        if (ids.Count == 0)
        {
            return Array.Empty<string>();
        }

        if (codeLookup is null)
        {
            return ids.Select(i => i.Value.ToString("D")).ToList();
        }

        return ids.Select(i => codeLookup.TryGetValue(i.Value, out var code) ? code : i.Value.ToString("D"))
            .ToList();
    }

    private static string ResolveCode(
        BusinessTypeId id,
        IReadOnlyDictionary<Guid, string>? codeLookup) =>
        codeLookup is not null && codeLookup.TryGetValue(id.Value, out var code)
            ? code
            : id.Value.ToString("D");

    public static CatalogImportJobDto Map(
        CatalogImportJob job,
        bool includePreviewItems,
        CatalogTemplate? targetTemplate = null,
        int? productsAlreadyInTemplate = null)
    {
        IReadOnlyList<CatalogImportItemDto>? preview = null;
        if (includePreviewItems)
        {
            preview = job.Items
                .Take(CatalogImportRules.MaxPreviewRows)
                .Select(Map)
                .ToList();
        }

        var summary = CatalogImportRowMapper.BuildPreviewSummary(job.Items);
        var estimatedLinks = job.TargetTemplateId is null
            ? (int?)null
            : job.Items.Count(i =>
                i.Status is CatalogImportItemStatus.Pending or CatalogImportItemStatus.Imported
                || (i.Status == CatalogImportItemStatus.Skipped && i.CreatedGlobalProductId is not null));

        return new CatalogImportJobDto(
            job.Id.Value,
            job.FileName,
            job.FileFormat.ToString(),
            job.ContentType,
            job.FileSizeBytes,
            job.FileSha256,
            job.IdempotencyKey,
            job.RequestedBy,
            job.Status.ToString(),
            job.TotalCount,
            job.ProcessedCount,
            job.ImportedCount,
            job.SkippedCount,
            job.FailedCount,
            job.PendingCount,
            summary.ValidProductCount,
            summary.ExistingCategoriesReferencedCount,
            summary.NewCategoriesToCreateCount,
            summary.WarningCount,
            job.Status == CatalogImportJobStatus.Validated ? summary.SummaryText : null,
            job.CurrentStage,
            job.ErrorSummary,
            job.CreatedAtUtc,
            job.UpdatedAtUtc,
            job.StartedAtUtc,
            job.CompletedAtUtc,
            job.LastHeartbeatAtUtc,
            preview,
            job.TargetTemplateId,
            targetTemplate?.Name,
            targetTemplate?.ProductCount,
            estimatedLinks,
            productsAlreadyInTemplate);
    }

    public static CatalogImportItemDto Map(CatalogImportItem item)
    {
        var willCreate = CatalogImportRowMapper.WillCreateCategory(item);
        var status = item.Status.ToString();
        string? errorCode = item.ErrorCode;
        string? errorMessage = item.ErrorMessage;
        if (willCreate)
        {
            status = "ValidWithNewCategory";
            errorCode ??= ApplicationErrorCodes.CatalogImportCategoryWillCreate;
            errorMessage ??= $"New category will be created: {item.CategoryName}";
        }
        else if (item.Status == CatalogImportItemStatus.Pending)
        {
            status = "Valid";
        }

        return new CatalogImportItemDto(
            item.Id.Value,
            item.RowNumber,
            item.Name,
            item.Description,
            item.Sku,
            item.Barcode,
            item.GlobalCategoryId,
            item.CategoryName,
            item.Unit,
            item.CostPrice,
            item.SellingPrice,
            item.ImageReference,
            item.SearchTagsRaw,
            item.BusinessTypesRaw,
            status,
            errorCode,
            errorMessage,
            willCreate,
            item.CreatedGlobalProductId,
            item.AttemptCount,
            item.ProcessedAtUtc);
    }

    public static CatalogImportErrorDto MapError(CatalogImportItem item) =>
        new(
            item.Id.Value,
            item.RowNumber,
            item.Name,
            item.Sku,
            item.Barcode,
            item.Status.ToString(),
            item.ErrorCode,
            item.ErrorMessage);
}
