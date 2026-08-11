using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Catalog;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence;

internal static class CatalogEntityMapper
{
    public static ProductCategory ToDomain(ProductCategoryRecord record) =>
        ProductCategory.Rehydrate(
            ProductCategoryId.From(record.Id),
            PosOrganizationId.From(record.OrganizationId),
            record.Name,
            record.NormalizedName,
            Enum.Parse<ProductCategoryStatus>(record.Status, ignoreCase: true),
            record.CreatedAtUtc,
            record.UpdatedAtUtc,
            record.SourceGlobalCategoryId);

    public static ProductCategoryRecord ToRecord(ProductCategory category) =>
        new()
        {
            Id = category.Id.Value,
            OrganizationId = category.OrganizationId.Value,
            Name = category.Name,
            NormalizedName = category.NormalizedName,
            Status = category.Status.ToString(),
            SourceGlobalCategoryId = category.SourceGlobalCategoryId,
            CreatedAtUtc = category.CreatedAtUtc,
            UpdatedAtUtc = category.UpdatedAtUtc
        };

    public static void ApplyToRecord(ProductCategory category, ProductCategoryRecord record)
    {
        record.Name = category.Name;
        record.NormalizedName = category.NormalizedName;
        record.Status = category.Status.ToString();
        record.SourceGlobalCategoryId = category.SourceGlobalCategoryId;
        record.UpdatedAtUtc = category.UpdatedAtUtc;
    }

    public static CatalogProduct ToDomain(CatalogProductRecord record) =>
        CatalogProduct.Rehydrate(
            CatalogProductId.From(record.Id),
            PosOrganizationId.From(record.OrganizationId),
            record.Name,
            record.Description,
            record.Sku,
            record.NormalizedSku,
            record.Barcode,
            record.CategoryId is null ? null : ProductCategoryId.From(record.CategoryId.Value),
            UnitOfMeasures.Parse(record.UnitOfMeasure),
            record.SellingPrice,
            Enum.Parse<CatalogProductStatus>(record.Status, ignoreCase: true),
            record.CreatedAtUtc,
            record.UpdatedAtUtc,
            record.PlatformGlobalProductId,
            record.PlatformTemplateId,
            Enum.Parse<CatalogSource>(record.CatalogSource, ignoreCase: true),
            record.CatalogImportedAt,
            record.CatalogSnapshotVersion,
            record.SourceGlobalCategoryId,
            sellingMode: SellingModes.TryParse(record.SellingMode, out var productMode)
                ? productMode
                : SellingMode.PerItem);

    public static CatalogProductRecord ToRecord(CatalogProduct product) =>
        new()
        {
            Id = product.Id.Value,
            OrganizationId = product.OrganizationId.Value,
            Name = product.Name,
            Description = product.Description,
            Sku = product.Sku,
            NormalizedSku = product.NormalizedSku,
            Barcode = product.Barcode,
            CategoryId = product.CategoryId?.Value,
            UnitOfMeasure = UnitOfMeasures.ToCode(product.UnitOfMeasure),
            SellingMode = SellingModes.ToCode(product.SellingMode),
            SellingPrice = product.SellingPrice,
            Status = product.Status.ToString(),
            PlatformGlobalProductId = product.PlatformGlobalProductId,
            PlatformTemplateId = product.PlatformTemplateId,
            CatalogSource = product.CatalogSource.ToString(),
            CatalogImportedAt = product.CatalogImportedAt,
            CatalogSnapshotVersion = product.CatalogSnapshotVersion,
            SourceGlobalCategoryId = product.SourceGlobalCategoryId,
            CreatedAtUtc = product.CreatedAtUtc,
            UpdatedAtUtc = product.UpdatedAtUtc
        };

    public static void ApplyToRecord(CatalogProduct product, CatalogProductRecord record)
    {
        record.Name = product.Name;
        record.Description = product.Description;
        record.Sku = product.Sku;
        record.NormalizedSku = product.NormalizedSku;
        record.Barcode = product.Barcode;
        record.CategoryId = product.CategoryId?.Value;
        record.UnitOfMeasure = UnitOfMeasures.ToCode(product.UnitOfMeasure);
        record.SellingMode = SellingModes.ToCode(product.SellingMode);
        record.SellingPrice = product.SellingPrice;
        record.Status = product.Status.ToString();
        record.PlatformGlobalProductId = product.PlatformGlobalProductId;
        record.PlatformTemplateId = product.PlatformTemplateId;
        record.CatalogSource = product.CatalogSource.ToString();
        record.CatalogImportedAt = product.CatalogImportedAt;
        record.CatalogSnapshotVersion = product.CatalogSnapshotVersion;
        record.SourceGlobalCategoryId = product.SourceGlobalCategoryId;
        record.UpdatedAtUtc = product.UpdatedAtUtc;
    }

    public static CatalogImportJob ToDomain(CatalogImportJobRecord record) =>
        CatalogImportJob.Rehydrate(
            CatalogImportJobId.From(record.Id),
            PosOrganizationId.From(record.OrganizationId),
            Enum.Parse<PosCatalogImportJobKind>(record.JobKind, ignoreCase: true),
            record.PlatformTemplateId,
            record.BatchNumber,
            Enum.Parse<CatalogSource>(record.CatalogSource, ignoreCase: true),
            record.RequestedBy,
            record.IdempotencyKey,
            Enum.Parse<PosCatalogImportJobStatus>(record.Status, ignoreCase: true),
            record.TotalCount,
            record.ProcessedCount,
            record.ImportedCount,
            record.SkippedCount,
            record.FailedCount,
            record.CurrentStage,
            record.ErrorSummary,
            record.CreatedAtUtc,
            record.UpdatedAtUtc,
            record.StartedAtUtc,
            record.CompletedAtUtc,
            record.LastHeartbeatAtUtc,
            (record.Items ?? []).Select(ToDomain));

    public static CatalogImportItemResult ToDomain(CatalogImportItemResultRecord record) =>
        CatalogImportItemResult.Rehydrate(
            CatalogImportItemResultId.From(record.Id),
            record.PlatformGlobalProductId,
            record.SortOrder,
            record.Name,
            record.Description,
            record.Sku,
            record.Barcode,
            record.UnitOfMeasure,
            SellingModes.TryParse(record.SellingMode, out var itemMode)
                ? SellingModes.ToCode(itemMode)
                : SellingModes.ToCode(SellingMode.PerItem),
            record.SuggestedPrice,
            record.SourceGlobalCategoryId,
            record.SourceCategoryName,
            Enum.Parse<PosCatalogImportItemStatus>(record.Status, ignoreCase: true),
            record.LocalProductId is null ? null : CatalogProductId.From(record.LocalProductId.Value),
            record.ErrorCode,
            record.ErrorMessage,
            record.ProcessedAtUtc);

    public static CatalogImportJobRecord ToRecord(CatalogImportJob job) =>
        new()
        {
            Id = job.Id.Value,
            OrganizationId = job.OrganizationId.Value,
            JobKind = job.JobKind.ToString(),
            PlatformTemplateId = job.PlatformTemplateId,
            BatchNumber = job.BatchNumber,
            CatalogSource = job.CatalogSource.ToString(),
            RequestedBy = job.RequestedBy,
            IdempotencyKey = job.IdempotencyKey,
            Status = job.Status.ToString(),
            TotalCount = job.TotalCount,
            ProcessedCount = job.ProcessedCount,
            ImportedCount = job.ImportedCount,
            SkippedCount = job.SkippedCount,
            FailedCount = job.FailedCount,
            CurrentStage = job.CurrentStage,
            ErrorSummary = job.ErrorSummary,
            CreatedAtUtc = job.CreatedAtUtc,
            UpdatedAtUtc = job.UpdatedAtUtc,
            StartedAtUtc = job.StartedAtUtc,
            CompletedAtUtc = job.CompletedAtUtc,
            LastHeartbeatAtUtc = job.LastHeartbeatAtUtc,
            Items = job.Items.Select(ToRecord).ToList()
        };

    public static CatalogImportItemResultRecord ToRecord(CatalogImportItemResult item) =>
        new()
        {
            Id = item.Id.Value,
            PlatformGlobalProductId = item.PlatformGlobalProductId,
            SortOrder = item.SortOrder,
            Name = item.Name,
            Description = item.Description,
            Sku = item.Sku,
            Barcode = item.Barcode,
            UnitOfMeasure = item.UnitOfMeasure,
            SellingMode = SellingModes.TryParse(item.SellingMode, out var mode)
                ? SellingModes.ToCode(mode)
                : SellingModes.ToCode(SellingMode.PerItem),
            SuggestedPrice = item.SuggestedPrice,
            SourceGlobalCategoryId = item.SourceGlobalCategoryId,
            SourceCategoryName = item.SourceCategoryName,
            Status = item.Status.ToString(),
            LocalProductId = item.LocalProductId?.Value,
            ErrorCode = item.ErrorCode,
            ErrorMessage = item.ErrorMessage,
            ProcessedAtUtc = item.ProcessedAtUtc
        };

    public static void ApplyToRecord(CatalogImportJob job, CatalogImportJobRecord record)
    {
        record.JobKind = job.JobKind.ToString();
        record.PlatformTemplateId = job.PlatformTemplateId;
        record.BatchNumber = job.BatchNumber;
        record.CatalogSource = job.CatalogSource.ToString();
        record.RequestedBy = job.RequestedBy;
        record.IdempotencyKey = job.IdempotencyKey;
        record.Status = job.Status.ToString();
        record.TotalCount = job.TotalCount;
        record.ProcessedCount = job.ProcessedCount;
        record.ImportedCount = job.ImportedCount;
        record.SkippedCount = job.SkippedCount;
        record.FailedCount = job.FailedCount;
        record.CurrentStage = job.CurrentStage;
        record.ErrorSummary = job.ErrorSummary;
        record.UpdatedAtUtc = job.UpdatedAtUtc;
        record.StartedAtUtc = job.StartedAtUtc;
        record.CompletedAtUtc = job.CompletedAtUtc;
        record.LastHeartbeatAtUtc = job.LastHeartbeatAtUtc;

        var existing = record.Items.ToDictionary(i => i.Id);
        foreach (var item in job.Items)
        {
            if (existing.TryGetValue(item.Id.Value, out var itemRecord))
            {
                ApplyToRecord(item, itemRecord);
            }
            else
            {
                var created = ToRecord(item);
                created.CatalogImportJobId = job.Id.Value;
                record.Items.Add(created);
            }
        }
    }

    public static void ApplyToRecord(CatalogImportItemResult item, CatalogImportItemResultRecord record)
    {
        record.PlatformGlobalProductId = item.PlatformGlobalProductId;
        record.SortOrder = item.SortOrder;
        record.Name = item.Name;
        record.Description = item.Description;
        record.Sku = item.Sku;
        record.Barcode = item.Barcode;
        record.UnitOfMeasure = item.UnitOfMeasure;
        record.SellingMode = SellingModes.TryParse(item.SellingMode, out var mode)
            ? SellingModes.ToCode(mode)
            : SellingModes.ToCode(SellingMode.PerItem);
        record.SuggestedPrice = item.SuggestedPrice;
        record.SourceGlobalCategoryId = item.SourceGlobalCategoryId;
        record.SourceCategoryName = item.SourceCategoryName;
        record.Status = item.Status.ToString();
        record.LocalProductId = item.LocalProductId?.Value;
        record.ErrorCode = item.ErrorCode;
        record.ErrorMessage = item.ErrorMessage;
        record.ProcessedAtUtc = item.ProcessedAtUtc;
    }
}
