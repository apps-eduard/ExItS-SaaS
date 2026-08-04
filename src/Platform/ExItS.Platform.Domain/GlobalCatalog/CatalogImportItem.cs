using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.GlobalCatalog;

/// <summary>One source row in a Platform global-catalog import job.</summary>
public sealed class CatalogImportItem
{
    public CatalogImportItemId Id { get; }
    public int RowNumber { get; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public string? Sku { get; private set; }
    public string? Barcode { get; private set; }
    public Guid? GlobalCategoryId { get; private set; }
    public string? CategoryName { get; private set; }
    public string Unit { get; private set; }
    public decimal? SuggestedPrice { get; private set; }
    public decimal? SuggestedCost { get; private set; }
    public string? ImageReference { get; private set; }
    public string? SearchTagsRaw { get; private set; }
    public string? BusinessTypesRaw { get; private set; }
    public CatalogImportItemStatus Status { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }
    public Guid? CreatedGlobalProductId { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTimeOffset? ProcessedAtUtc { get; private set; }

    private CatalogImportItem(
        CatalogImportItemId id,
        int rowNumber,
        string name,
        string? description,
        string? sku,
        string? barcode,
        Guid? globalCategoryId,
        string? categoryName,
        string unit,
        decimal? suggestedPrice,
        decimal? suggestedCost,
        string? imageReference,
        string? searchTagsRaw,
        string? businessTypesRaw,
        CatalogImportItemStatus status,
        string? errorCode,
        string? errorMessage,
        Guid? createdGlobalProductId,
        int attemptCount,
        DateTimeOffset? processedAtUtc)
    {
        Id = id;
        RowNumber = rowNumber;
        Name = name;
        Description = description;
        Sku = sku;
        Barcode = barcode;
        GlobalCategoryId = globalCategoryId;
        CategoryName = categoryName;
        Unit = unit;
        SuggestedPrice = suggestedPrice;
        SuggestedCost = suggestedCost;
        ImageReference = imageReference;
        SearchTagsRaw = searchTagsRaw;
        BusinessTypesRaw = businessTypesRaw;
        Status = status;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        CreatedGlobalProductId = createdGlobalProductId;
        AttemptCount = attemptCount;
        ProcessedAtUtc = processedAtUtc;
    }

    public static CatalogImportItem CreatePending(
        int rowNumber,
        string name,
        string unit,
        string? description = null,
        string? sku = null,
        string? barcode = null,
        Guid? globalCategoryId = null,
        string? categoryName = null,
        decimal? suggestedPrice = null,
        decimal? suggestedCost = null,
        string? imageReference = null,
        string? searchTagsRaw = null,
        string? businessTypesRaw = null,
        CatalogImportItemId? id = null)
    {
        if (rowNumber < 1)
        {
            throw new DomainException(
                DomainErrorCodes.CatalogImportRowInvalid,
                "Row number must be >= 1.");
        }

        return new CatalogImportItem(
            id ?? CatalogImportItemId.New(),
            rowNumber,
            name,
            description,
            sku,
            barcode,
            globalCategoryId,
            categoryName,
            unit,
            suggestedPrice,
            suggestedCost,
            imageReference,
            searchTagsRaw,
            businessTypesRaw,
            CatalogImportItemStatus.Pending,
            errorCode: null,
            errorMessage: null,
            createdGlobalProductId: null,
            attemptCount: 0,
            processedAtUtc: null);
    }

    public static CatalogImportItem CreateFailed(
        int rowNumber,
        string name,
        string unit,
        string errorCode,
        string errorMessage,
        string? description = null,
        string? sku = null,
        string? barcode = null,
        Guid? globalCategoryId = null,
        string? categoryName = null,
        decimal? suggestedPrice = null,
        decimal? suggestedCost = null,
        string? imageReference = null,
        string? searchTagsRaw = null,
        string? businessTypesRaw = null,
        CatalogImportItemId? id = null)
    {
        if (rowNumber < 1)
        {
            throw new DomainException(
                DomainErrorCodes.CatalogImportRowInvalid,
                "Row number must be >= 1.");
        }

        return new CatalogImportItem(
            id ?? CatalogImportItemId.New(),
            rowNumber,
            name,
            description,
            sku,
            barcode,
            globalCategoryId,
            categoryName,
            string.IsNullOrWhiteSpace(unit) ? "Piece" : unit,
            suggestedPrice,
            suggestedCost,
            imageReference,
            searchTagsRaw,
            businessTypesRaw,
            CatalogImportItemStatus.Failed,
            errorCode,
            CatalogImportRules.NormalizeOptionalError(errorMessage),
            createdGlobalProductId: null,
            attemptCount: 0,
            processedAtUtc: null);
    }

    public static CatalogImportItem CreateSkipped(
        int rowNumber,
        string name,
        string unit,
        string errorCode,
        string errorMessage,
        DateTimeOffset utcNow,
        string? description = null,
        string? sku = null,
        string? barcode = null,
        Guid? globalCategoryId = null,
        string? categoryName = null,
        decimal? suggestedPrice = null,
        decimal? suggestedCost = null,
        string? imageReference = null,
        string? searchTagsRaw = null,
        string? businessTypesRaw = null,
        CatalogImportItemId? id = null)
    {
        var item = CreatePending(
            rowNumber,
            name,
            unit,
            description,
            sku,
            barcode,
            globalCategoryId,
            categoryName,
            suggestedPrice,
            suggestedCost,
            imageReference,
            searchTagsRaw,
            businessTypesRaw,
            id);
        item.MarkSkipped(errorCode, errorMessage, utcNow);
        return item;
    }

    public static CatalogImportItem Rehydrate(
        CatalogImportItemId id,
        int rowNumber,
        string name,
        string? description,
        string? sku,
        string? barcode,
        Guid? globalCategoryId,
        string? categoryName,
        string unit,
        decimal? suggestedPrice,
        decimal? suggestedCost,
        string? imageReference,
        string? searchTagsRaw,
        string? businessTypesRaw,
        CatalogImportItemStatus status,
        string? errorCode,
        string? errorMessage,
        Guid? createdGlobalProductId,
        int attemptCount,
        DateTimeOffset? processedAtUtc) =>
        new(
            id,
            rowNumber,
            name,
            description,
            sku,
            barcode,
            globalCategoryId,
            categoryName,
            unit,
            suggestedPrice,
            suggestedCost,
            imageReference,
            searchTagsRaw,
            businessTypesRaw,
            status,
            errorCode,
            errorMessage,
            createdGlobalProductId,
            attemptCount,
            processedAtUtc);

    public void MarkImported(Guid globalProductId, DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);
        if (Status is CatalogImportItemStatus.Imported)
        {
            return;
        }

        if (Status is not CatalogImportItemStatus.Pending)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCatalogImportStatusTransition,
                $"Cannot mark item as imported from status {Status}.");
        }

        if (globalProductId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGlobalProductId,
                "Created product id cannot be empty.");
        }

        Status = CatalogImportItemStatus.Imported;
        CreatedGlobalProductId = globalProductId;
        ErrorCode = null;
        ErrorMessage = null;
        AttemptCount++;
        ProcessedAtUtc = utcNow;
    }

    public void MarkSkipped(string errorCode, string errorMessage, DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);
        if (Status is CatalogImportItemStatus.Imported or CatalogImportItemStatus.Skipped)
        {
            return;
        }

        Status = CatalogImportItemStatus.Skipped;
        ErrorCode = errorCode;
        ErrorMessage = CatalogImportRules.NormalizeOptionalError(errorMessage);
        AttemptCount++;
        ProcessedAtUtc = utcNow;
    }

    public void MarkFailed(string errorCode, string errorMessage, DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);
        if (Status is CatalogImportItemStatus.Imported)
        {
            return;
        }

        Status = CatalogImportItemStatus.Failed;
        ErrorCode = errorCode;
        ErrorMessage = CatalogImportRules.NormalizeOptionalError(errorMessage);
        AttemptCount++;
        ProcessedAtUtc = utcNow;
    }

    /// <summary>Records a transient failure without leaving Pending so restart can retry.</summary>
    public void RecordTransientAttempt(DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);
        if (Status is not CatalogImportItemStatus.Pending)
        {
            return;
        }

        AttemptCount++;
        ProcessedAtUtc = utcNow;
    }
}
