using ExItS.Platform.Domain.GlobalCatalog;
using ExItS.Platform.Infrastructure.Persistence.GlobalCatalog;

namespace ExItS.Platform.Infrastructure.Persistence;

internal static class GlobalCatalogEntityMapper
{
    public static GlobalCategory ToDomain(GlobalCategoryRecord record) =>
        GlobalCategory.Rehydrate(
            GlobalCategoryId.From(record.Id),
            record.Name,
            record.ParentId is null ? null : GlobalCategoryId.From(record.ParentId.Value),
            record.IconReference,
            record.SortOrder,
            Enum.Parse<GlobalCategoryStatus>(record.Status),
            record.BusinessTypes.Select(b => Enum.Parse<BusinessType>(b.BusinessType)),
            record.CreatedAtUtc,
            record.UpdatedAtUtc);

    public static GlobalCategoryRecord ToRecord(GlobalCategory category) =>
        new()
        {
            Id = category.Id.Value,
            Name = category.Name,
            NormalizedName = category.Name.ToUpperInvariant(),
            ParentId = category.ParentId?.Value,
            IconReference = category.IconReference,
            SortOrder = category.SortOrder,
            Status = category.Status.ToString(),
            CreatedAtUtc = category.CreatedAtUtc,
            UpdatedAtUtc = category.UpdatedAtUtc,
            BusinessTypes = category.BusinessTypes
                .Select(t => new GlobalCategoryBusinessTypeRecord
                {
                    CategoryId = category.Id.Value,
                    BusinessType = t.ToString()
                })
                .ToList()
        };

    public static void ApplyToRecord(GlobalCategory category, GlobalCategoryRecord record)
    {
        record.Name = category.Name;
        record.NormalizedName = category.Name.ToUpperInvariant();
        record.ParentId = category.ParentId?.Value;
        record.IconReference = category.IconReference;
        record.SortOrder = category.SortOrder;
        record.Status = category.Status.ToString();
        record.UpdatedAtUtc = category.UpdatedAtUtc;
        record.BusinessTypes.Clear();
        foreach (var type in category.BusinessTypes)
        {
            record.BusinessTypes.Add(new GlobalCategoryBusinessTypeRecord
            {
                CategoryId = category.Id.Value,
                BusinessType = type.ToString()
            });
        }
    }

    public static GlobalProduct ToDomain(GlobalProductRecord record) =>
        GlobalProduct.Rehydrate(
            GlobalProductId.From(record.Id),
            record.Name,
            record.Description,
            record.Sku,
            record.Barcode,
            record.Brand,
            record.GlobalCategoryId is null ? null : GlobalCategoryId.From(record.GlobalCategoryId.Value),
            Enum.Parse<ProductUnit>(record.Unit),
            record.CostPrice,
            record.SellingPrice,
            record.ImageReference,
            Enum.Parse<GlobalProductStatus>(record.Status),
            record.SearchTags ?? [],
            record.BusinessTypes.Select(b => Enum.Parse<BusinessType>(b.BusinessType)),
            record.CreatedAtUtc,
            record.UpdatedAtUtc);

    public static GlobalProductRecord ToRecord(GlobalProduct product) =>
        new()
        {
            Id = product.Id.Value,
            Name = product.Name,
            Description = product.Description,
            Sku = product.Sku,
            Barcode = product.Barcode,
            Brand = product.Brand,
            GlobalCategoryId = product.GlobalCategoryId?.Value,
            Unit = product.Unit.ToString(),
            SellingPrice = product.SellingPrice,
            CostPrice = product.CostPrice,
            ImageReference = product.ImageReference,
            Status = product.Status.ToString(),
            SearchTags = product.SearchTags.ToArray(),
            CreatedAtUtc = product.CreatedAtUtc,
            UpdatedAtUtc = product.UpdatedAtUtc,
            BusinessTypes = product.BusinessTypes
                .Select(t => new GlobalProductBusinessTypeRecord
                {
                    ProductId = product.Id.Value,
                    BusinessType = t.ToString()
                })
                .ToList()
        };

    public static void ApplyToRecord(GlobalProduct product, GlobalProductRecord record)
    {
        record.Name = product.Name;
        record.Description = product.Description;
        record.Sku = product.Sku;
        record.Barcode = product.Barcode;
        record.Brand = product.Brand;
        record.GlobalCategoryId = product.GlobalCategoryId?.Value;
        record.Unit = product.Unit.ToString();
        record.SellingPrice = product.SellingPrice;
        record.CostPrice = product.CostPrice;
        record.ImageReference = product.ImageReference;
        record.Status = product.Status.ToString();
        record.SearchTags = product.SearchTags.ToArray();
        record.UpdatedAtUtc = product.UpdatedAtUtc;
        record.BusinessTypes.Clear();
        foreach (var type in product.BusinessTypes)
        {
            record.BusinessTypes.Add(new GlobalProductBusinessTypeRecord
            {
                ProductId = product.Id.Value,
                BusinessType = type.ToString()
            });
        }
    }

    public static CatalogTemplate ToDomain(CatalogTemplateRecord record) =>
        CatalogTemplate.Rehydrate(
            CatalogTemplateId.From(record.Id),
            record.Name,
            record.Slug,
            record.Description,
            record.IconReference,
            Enum.Parse<BusinessType>(record.PrimaryBusinessType),
            Enum.Parse<CatalogTemplateStatus>(record.Status),
            record.DefaultBatchSize,
            Enum.Parse<SelectionMode>(record.SelectionMode),
            record.PublishedAtUtc,
            (record.Products ?? []).Select(p => CatalogTemplateProduct.Rehydrate(
                p.Id,
                GlobalProductId.From(p.GlobalProductId),
                p.SortOrder,
                p.IsFeatured,
                p.IsFirstBatch)),
            record.CreatedAtUtc,
            record.UpdatedAtUtc);

    public static CatalogTemplateRecord ToRecord(CatalogTemplate template) =>
        new()
        {
            Id = template.Id.Value,
            Name = template.Name,
            Slug = template.Slug,
            Description = template.Description,
            IconReference = template.IconReference,
            PrimaryBusinessType = template.PrimaryBusinessType.ToString(),
            Status = template.Status.ToString(),
            DefaultBatchSize = template.DefaultBatchSize,
            SelectionMode = template.SelectionMode.ToString(),
            PublishedAtUtc = template.PublishedAtUtc,
            CreatedAtUtc = template.CreatedAtUtc,
            UpdatedAtUtc = template.UpdatedAtUtc,
            Products = template.Products
                .Select(p => ToRecord(template.Id.Value, p))
                .ToList()
        };

    /// <summary>
    /// Applies scalar template state only. Composition rows are reconciled by the repository:
    /// their <c>Id</c> is domain-assigned, so replacing a tracked collection would make EF treat
    /// new rows as pre-existing ones and update rows that do not exist yet.
    /// </summary>
    public static void ApplyToRecord(CatalogTemplate template, CatalogTemplateRecord record)
    {
        record.Name = template.Name;
        record.Slug = template.Slug;
        record.Description = template.Description;
        record.IconReference = template.IconReference;
        record.PrimaryBusinessType = template.PrimaryBusinessType.ToString();
        record.Status = template.Status.ToString();
        record.DefaultBatchSize = template.DefaultBatchSize;
        record.SelectionMode = template.SelectionMode.ToString();
        record.PublishedAtUtc = template.PublishedAtUtc;
        record.UpdatedAtUtc = template.UpdatedAtUtc;
    }

    public static CatalogTemplateProductRecord ToRecord(Guid templateId, CatalogTemplateProduct product) =>
        new()
        {
            Id = product.Id,
            CatalogTemplateId = templateId,
            GlobalProductId = product.GlobalProductId.Value,
            SortOrder = product.SortOrder,
            IsFeatured = product.IsFeatured,
            IsFirstBatch = product.IsFirstBatch
        };

    public static void ApplyToRecord(CatalogTemplateProduct product, CatalogTemplateProductRecord record)
    {
        record.SortOrder = product.SortOrder;
        record.IsFeatured = product.IsFeatured;
        record.IsFirstBatch = product.IsFirstBatch;
    }

    public static CatalogImportJob ToDomain(CatalogImportJobRecord record) =>
        CatalogImportJob.Rehydrate(
            CatalogImportJobId.From(record.Id),
            record.FileName,
            Enum.Parse<CatalogImportFileFormat>(record.FileFormat),
            record.ContentType,
            record.FileSizeBytes,
            record.FileSha256,
            record.IdempotencyKey,
            record.RequestedBy,
            Enum.Parse<CatalogImportJobStatus>(record.Status),
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

    public static CatalogImportItem ToDomain(CatalogImportItemRecord record) =>
        CatalogImportItem.Rehydrate(
            CatalogImportItemId.From(record.Id),
            record.RowNumber,
            record.Name,
            record.Description,
            record.Sku,
            record.Barcode,
            record.GlobalCategoryId,
            record.CategoryName,
            record.Unit,
            record.SellingPrice,
            record.CostPrice,
            record.ImageReference,
            record.SearchTagsRaw,
            record.BusinessTypesRaw,
            Enum.Parse<CatalogImportItemStatus>(record.Status),
            record.ErrorCode,
            record.ErrorMessage,
            record.CreatedGlobalProductId,
            record.AttemptCount,
            record.ProcessedAtUtc);

    public static CatalogImportJobRecord ToRecord(CatalogImportJob job) =>
        new()
        {
            Id = job.Id.Value,
            FileName = job.FileName,
            FileFormat = job.FileFormat.ToString(),
            ContentType = job.ContentType,
            FileSizeBytes = job.FileSizeBytes,
            FileSha256 = job.FileSha256,
            IdempotencyKey = job.IdempotencyKey,
            RequestedBy = job.RequestedBy,
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
            Items = job.Items.Select(i => ToItemRecord(job.Id.Value, i)).ToList()
        };

    public static void ApplyToRecord(CatalogImportJob job, CatalogImportJobRecord record)
    {
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

        var byId = record.Items.ToDictionary(i => i.Id);
        foreach (var item in job.Items)
        {
            if (byId.TryGetValue(item.Id.Value, out var existing))
            {
                ApplyItemToRecord(item, existing);
            }
            else
            {
                record.Items.Add(ToItemRecord(job.Id.Value, item));
            }
        }
    }

    private static CatalogImportItemRecord ToItemRecord(Guid jobId, CatalogImportItem item) =>
        new()
        {
            Id = item.Id.Value,
            CatalogImportJobId = jobId,
            RowNumber = item.RowNumber,
            Name = item.Name,
            Description = item.Description,
            Sku = item.Sku,
            Barcode = item.Barcode,
            GlobalCategoryId = item.GlobalCategoryId,
            CategoryName = item.CategoryName,
            Unit = item.Unit,
            SellingPrice = item.SellingPrice,
            CostPrice = item.CostPrice,
            ImageReference = item.ImageReference,
            SearchTagsRaw = item.SearchTagsRaw,
            BusinessTypesRaw = item.BusinessTypesRaw,
            Status = item.Status.ToString(),
            ErrorCode = item.ErrorCode,
            ErrorMessage = item.ErrorMessage,
            CreatedGlobalProductId = item.CreatedGlobalProductId,
            AttemptCount = item.AttemptCount,
            ProcessedAtUtc = item.ProcessedAtUtc
        };

    private static void ApplyItemToRecord(CatalogImportItem item, CatalogImportItemRecord record)
    {
        record.Name = item.Name;
        record.Description = item.Description;
        record.Sku = item.Sku;
        record.Barcode = item.Barcode;
        record.GlobalCategoryId = item.GlobalCategoryId;
        record.CategoryName = item.CategoryName;
        record.Unit = item.Unit;
        record.SellingPrice = item.SellingPrice;
        record.CostPrice = item.CostPrice;
        record.ImageReference = item.ImageReference;
        record.SearchTagsRaw = item.SearchTagsRaw;
        record.BusinessTypesRaw = item.BusinessTypesRaw;
        record.Status = item.Status.ToString();
        record.ErrorCode = item.ErrorCode;
        record.ErrorMessage = item.ErrorMessage;
        record.CreatedGlobalProductId = item.CreatedGlobalProductId;
        record.AttemptCount = item.AttemptCount;
        record.ProcessedAtUtc = item.ProcessedAtUtc;
    }
}
