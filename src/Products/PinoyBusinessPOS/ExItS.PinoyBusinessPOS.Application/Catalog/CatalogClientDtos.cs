namespace ExItS.PinoyBusinessPOS.Application.Catalog;

public sealed record PosProductCategoryDto(
    Guid CategoryId,
    Guid OrganizationId,
    string Name,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record PosProductBrandDto(
    Guid BrandId,
    Guid OrganizationId,
    string Name,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record PosCatalogProductUnitDto(
    Guid UnitId,
    Guid ProductId,
    string Kind,
    string DisplayName,
    string ShortLabel,
    decimal MultiplierToBase,
    decimal? SellingPrice,
    bool AllowsCustomQuantity,
    bool IsActive,
    int SortOrder);

public sealed record PosCatalogProductUnitInput(
    string Kind,
    string DisplayName,
    string ShortLabel,
    decimal MultiplierToBase,
    decimal? SellingPrice = null,
    bool AllowsCustomQuantity = false,
    int SortOrder = 0,
    Guid? UnitId = null);

public sealed record PosCatalogProductDto(
    Guid ProductId,
    Guid OrganizationId,
    string Name,
    string? Description,
    string? Sku,
    string? Barcode,
    Guid? CategoryId,
    string UnitOfMeasure,
    string SellingMode,
    decimal SellingPrice,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    Guid? PlatformGlobalProductId = null,
    Guid? PlatformTemplateId = null,
    string CatalogSource = "Manual",
    DateTimeOffset? CatalogImportedAt = null,
    int? CatalogSnapshotVersion = null,
    Guid? SourceGlobalCategoryId = null,
    /// <summary>
    /// Mirrors <c>InventoryAccount.IsTracked</c>. When false, on-hand is not authoritative and
    /// selling must not create stock movements for the product.
    /// </summary>
    bool IsTracked = false,
    decimal OnHandQuantity = 0m,
    /// <summary>
    /// Derived tracked stock state code (<c>InStock</c> / <c>LowStock</c> / <c>OutOfStock</c>).
    /// Meaningful only when <see cref="IsTracked"/> is true.
    /// </summary>
    string StockStatus = "InStock",
    bool TracksExpiration = false,
    int? ExpirationWarningDays = null,
    bool CanBePurchased = true,
    bool CanBeSold = true,
    bool CanBeUsedAsIngredient = false,
    bool IsProduced = false,
    string? UsagePreset = "BuyAndSell",
    IReadOnlyList<PosCatalogProductUnitDto>? Units = null,
    bool CanExposeToConnectedBuyers = false,
    decimal? DefaultConnectedPoPrice = null,
    bool HasImage = false,
    int? ImageVersion = null,
    string ImageSource = CatalogProductImageSources.None,
    bool HasMerchantImageOverride = false,
    string? PlatformBarcode = null,
    Guid? BrandId = null,
    string? BrandName = null,
    /// <summary>Buyer-owned business usage: Resale | Ingredient | InternalUse.</summary>
    string BusinessUsage = "Resale",
    /// <summary>OrganizationStandard or BranchLocal (MB2 product governance).</summary>
    string Scope = "OrganizationStandard",
    Guid? OriginBranchId = null,
    /// <summary>Populated when listing a commercially offered branch assortment; otherwise null.</summary>
    bool? IsOfferedAtBranch = null);

public sealed record CreatePosProductCategoryRequest(string Name, Guid? CategoryId = null);

public sealed record UpdatePosProductCategoryRequest(
    string Name,
    DateTimeOffset? ExpectedUpdatedAtUtc = null);

public sealed record CreatePosProductBrandRequest(string Name, Guid? BrandId = null);

public sealed record UpdatePosProductBrandRequest(
    string Name,
    DateTimeOffset? ExpectedUpdatedAtUtc = null);

public sealed record CreatePosCatalogProductRequest(
    string Name,
    string UnitOfMeasure,
    decimal SellingPrice,
    string? Description = null,
    string? Sku = null,
    string? Barcode = null,
    Guid? CategoryId = null,
    Guid? BrandId = null,
    Guid? ProductId = null,
    string? SellingMode = null,
    bool TracksExpiration = false,
    int? ExpirationWarningDays = null,
    bool? CanBePurchased = null,
    bool? CanBeSold = null,
    bool? CanBeUsedAsIngredient = null,
    bool? IsProduced = null,
    string? UsagePreset = null,
    IReadOnlyList<PosCatalogProductUnitInput>? Units = null,
    bool CanExposeToConnectedBuyers = false,
    decimal? DefaultConnectedPoPrice = null,
    string? BusinessUsage = null,
    /// <summary>Optional create scope: OrganizationStandard | BranchLocal. Omitted → actor default.</summary>
    string? Scope = null);

public sealed record UpdatePosCatalogProductRequest(
    string Name,
    string UnitOfMeasure,
    decimal SellingPrice,
    string? Description = null,
    string? Sku = null,
    string? Barcode = null,
    Guid? CategoryId = null,
    Guid? BrandId = null,
    DateTimeOffset? ExpectedUpdatedAtUtc = null,
    string? SellingMode = null,
    bool? TracksExpiration = null,
    int? ExpirationWarningDays = null,
    bool? CanBePurchased = null,
    bool? CanBeSold = null,
    bool? CanBeUsedAsIngredient = null,
    bool? IsProduced = null,
    string? UsagePreset = null,
    IReadOnlyList<PosCatalogProductUnitInput>? Units = null,
    bool? CanExposeToConnectedBuyers = null,
    decimal? DefaultConnectedPoPrice = null,
    string? BusinessUsage = null);

/// <summary>One row for Today's Prices bulk current-price update (price only).</summary>
public sealed record UpdatePosCatalogProductPriceItem(
    Guid ProductId,
    decimal SellingPrice,
    DateTimeOffset? ExpectedUpdatedAtUtc = null);

public sealed record UpdatePosCatalogProductPricesRequest(
    IReadOnlyList<UpdatePosCatalogProductPriceItem> Items);

public sealed record UpdatePosCatalogProductPriceResultItem(
    Guid ProductId,
    bool Succeeded,
    bool Changed,
    PosCatalogProductDto? Product = null,
    string? ErrorCode = null,
    string? ErrorMessage = null);

public sealed record UpdatePosCatalogProductPricesResponse(
    IReadOnlyList<UpdatePosCatalogProductPriceResultItem> Results,
    int SucceededCount,
    int FailedCount,
    int ChangedCount);

public sealed record PosProductCategoryPagedResult(
    List<PosProductCategoryDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record PosProductBrandPagedResult(
    List<PosProductBrandDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record PosCatalogProductPagedResult(
    List<PosCatalogProductDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record SetBranchProductAvailabilityRequest(bool IsOffered);

/// <summary>
/// Bulk branch-offering read for one product. ExplicitRows are sparse overrides only;
/// React merges with Platform Active branches (missing = offered by default for Standard).
/// </summary>
public sealed record ProductBranchAvailabilityReadDto(
    Guid ProductId,
    string Scope,
    Guid? OriginBranchId,
    IReadOnlyList<ProductBranchOfferingItemDto> ExplicitRows);

public sealed record ProductBranchOfferingItemDto(
    Guid BranchId,
    bool IsOffered,
    string Reason,
    bool HasExplicitOverride);
