using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Catalog;

/// <summary>
/// One Platform global product row queued for local POS snapshot import.
/// Snapshot fields are frozen at job creation so Platform outage does not block processing.
/// </summary>
public sealed class CatalogImportItemResult
{
    public CatalogImportItemResultId Id { get; }
    public Guid PlatformGlobalProductId { get; }
    public int SortOrder { get; }
    public string Name { get; }
    public string? Description { get; }
    public string? Sku { get; }
    public string? Barcode { get; }
    public string UnitOfMeasure { get; }
    public string SellingMode { get; }
    public decimal SuggestedPrice { get; }
    public Guid? SourceGlobalCategoryId { get; }
    public string? SourceCategoryName { get; }
    public PosCatalogImportItemStatus Status { get; private set; }
    public CatalogProductId? LocalProductId { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTimeOffset? ProcessedAtUtc { get; private set; }

    private CatalogImportItemResult(
        CatalogImportItemResultId id,
        Guid platformGlobalProductId,
        int sortOrder,
        string name,
        string? description,
        string? sku,
        string? barcode,
        string unitOfMeasure,
        string sellingMode,
        decimal suggestedPrice,
        Guid? sourceGlobalCategoryId,
        string? sourceCategoryName,
        PosCatalogImportItemStatus status,
        CatalogProductId? localProductId,
        string? errorCode,
        string? errorMessage,
        DateTimeOffset? processedAtUtc)
    {
        Id = id;
        PlatformGlobalProductId = platformGlobalProductId;
        SortOrder = sortOrder;
        Name = name;
        Description = description;
        Sku = sku;
        Barcode = barcode;
        UnitOfMeasure = unitOfMeasure;
        SellingMode = sellingMode;
        SuggestedPrice = suggestedPrice;
        SourceGlobalCategoryId = sourceGlobalCategoryId;
        SourceCategoryName = sourceCategoryName;
        Status = status;
        LocalProductId = localProductId;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        ProcessedAtUtc = processedAtUtc;
    }

    public static CatalogImportItemResult CreatePending(
        Guid platformGlobalProductId,
        int sortOrder,
        string name,
        string unitOfMeasure,
        decimal suggestedPrice,
        string? description = null,
        string? sku = null,
        string? barcode = null,
        Guid? sourceGlobalCategoryId = null,
        string? sourceCategoryName = null,
        string? sellingMode = null,
        CatalogImportItemResultId? id = null)
    {
        if (platformGlobalProductId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCatalogImportItem,
                "PlatformGlobalProductId is required.");
        }

        if (sortOrder < 0)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCatalogImportItem,
                "Sort order cannot be negative.");
        }

        // Snapshot stores raw-normalized values; strict validation runs at process time so
        // one bad Platform row does not reject the entire queued job.
        var snapshotName = string.IsNullOrWhiteSpace(name) ? "Untitled" : name.Trim();
        if (snapshotName.Length > CatalogProduct.NameMaxLength)
        {
            snapshotName = snapshotName[..CatalogProduct.NameMaxLength];
        }

        var unitCode = string.IsNullOrWhiteSpace(unitOfMeasure)
            ? UnitOfMeasures.ToCode(Domain.Catalog.UnitOfMeasure.Piece)
            : unitOfMeasure.Trim();
        var sellingModeCode = string.IsNullOrWhiteSpace(sellingMode)
            ? SellingModes.ToCode(Domain.Catalog.SellingMode.PerItem)
            : sellingMode.Trim();
        var price = suggestedPrice < 0m ? 0m : decimal.Round(suggestedPrice, 2, MidpointRounding.AwayFromZero);
        if (price > CatalogProduct.SellingPriceMax)
        {
            price = CatalogProduct.SellingPriceMax;
        }

        string? categoryName = null;
        if (!string.IsNullOrWhiteSpace(sourceCategoryName))
        {
            categoryName = sourceCategoryName.Trim();
            if (categoryName.Length > CatalogImportRules.CategoryNameMaxLength)
            {
                categoryName = categoryName[..CatalogImportRules.CategoryNameMaxLength];
            }
        }

        string? displaySku = null;
        if (!string.IsNullOrWhiteSpace(sku))
        {
            displaySku = sku.Trim();
            if (displaySku.Length > CatalogProduct.SkuMaxLength)
            {
                displaySku = displaySku[..CatalogProduct.SkuMaxLength];
            }
        }

        string? desc = null;
        if (!string.IsNullOrWhiteSpace(description))
        {
            desc = description.Trim();
            if (desc.Length > CatalogProduct.DescriptionMaxLength)
            {
                desc = desc[..CatalogProduct.DescriptionMaxLength];
            }
        }

        return new CatalogImportItemResult(
            id ?? CatalogImportItemResultId.New(),
            platformGlobalProductId,
            sortOrder,
            snapshotName,
            desc,
            displaySku,
            TryNormalizeBarcode(barcode),
            unitCode,
            sellingModeCode,
            price,
            sourceGlobalCategoryId == Guid.Empty ? null : sourceGlobalCategoryId,
            categoryName,
            PosCatalogImportItemStatus.Pending,
            localProductId: null,
            errorCode: null,
            errorMessage: null,
            processedAtUtc: null);
    }

    public static CatalogImportItemResult Rehydrate(
        CatalogImportItemResultId id,
        Guid platformGlobalProductId,
        int sortOrder,
        string name,
        string? description,
        string? sku,
        string? barcode,
        string unitOfMeasure,
        string sellingMode,
        decimal suggestedPrice,
        Guid? sourceGlobalCategoryId,
        string? sourceCategoryName,
        PosCatalogImportItemStatus status,
        CatalogProductId? localProductId,
        string? errorCode,
        string? errorMessage,
        DateTimeOffset? processedAtUtc) =>
        new(
            id,
            platformGlobalProductId,
            sortOrder,
            name,
            description,
            sku,
            barcode,
            unitOfMeasure,
            sellingMode,
            suggestedPrice,
            sourceGlobalCategoryId,
            sourceCategoryName,
            status,
            localProductId,
            errorCode,
            errorMessage,
            processedAtUtc);

    public void MarkImported(CatalogProductId localProductId, DateTimeOffset utcNow)
    {
        CatalogGuards.EnsureUtc(utcNow);
        EnsurePending();
        LocalProductId = localProductId;
        Status = PosCatalogImportItemStatus.Imported;
        ErrorCode = null;
        ErrorMessage = null;
        ProcessedAtUtc = utcNow;
    }

    public void MarkSkipped(string errorCode, string errorMessage, DateTimeOffset utcNow)
    {
        CatalogGuards.EnsureUtc(utcNow);
        EnsurePending();
        Status = PosCatalogImportItemStatus.Skipped;
        ErrorCode = errorCode;
        ErrorMessage = CatalogImportRules.NormalizeOptionalError(errorMessage);
        ProcessedAtUtc = utcNow;
    }

    public void MarkFailed(string errorCode, string errorMessage, DateTimeOffset utcNow)
    {
        CatalogGuards.EnsureUtc(utcNow);
        EnsurePending();
        Status = PosCatalogImportItemStatus.Failed;
        ErrorCode = errorCode;
        ErrorMessage = CatalogImportRules.NormalizeOptionalError(errorMessage);
        ProcessedAtUtc = utcNow;
    }

    private void EnsurePending()
    {
        if (Status is not PosCatalogImportItemStatus.Pending)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCatalogImportStatusTransition,
                $"Import item is already {Status}.");
        }
    }

    /// <summary>
    /// Soft-normalizes barcode for snapshot storage. Invalid barcodes are kept raw and fail at process time.
    /// </summary>
    private static string? TryNormalizeBarcode(string? barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            return null;
        }

        try
        {
            return CatalogProduct.NormalizeOptionalBarcode(barcode);
        }
        catch (DomainException)
        {
            return barcode.Trim();
        }
    }
}
