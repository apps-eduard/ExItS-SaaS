namespace ExItS.PinoyBusinessPOS.Application.Catalog;

public sealed record PosProductCategoryDto(
    Guid CategoryId,
    Guid OrganizationId,
    string Name,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record PosCatalogProductDto(
    Guid ProductId,
    Guid OrganizationId,
    string Name,
    string? Description,
    string? Sku,
    string? Barcode,
    Guid? CategoryId,
    string UnitOfMeasure,
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
    /// checkout must not create stock movements for the product.
    /// </summary>
    bool IsTracked = false,
    decimal OnHandQuantity = 0m,
    /// <summary>
    /// Derived tracked stock state code (<c>InStock</c> / <c>LowStock</c> / <c>OutOfStock</c>).
    /// Meaningful only when <see cref="IsTracked"/> is true.
    /// </summary>
    string StockStatus = "InStock");

public sealed record CreatePosProductCategoryRequest(string Name, Guid? CategoryId = null);

public sealed record UpdatePosProductCategoryRequest(
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
    Guid? ProductId = null);

public sealed record UpdatePosCatalogProductRequest(
    string Name,
    string UnitOfMeasure,
    decimal SellingPrice,
    string? Description = null,
    string? Sku = null,
    string? Barcode = null,
    Guid? CategoryId = null,
    DateTimeOffset? ExpectedUpdatedAtUtc = null);

public sealed record PosProductCategoryPagedResult(
    List<PosProductCategoryDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record PosCatalogProductPagedResult(
    List<PosCatalogProductDto> Items,
    int TotalCount,
    int Page,
    int PageSize);
