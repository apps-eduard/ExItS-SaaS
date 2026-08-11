using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.GlobalCatalog;

/// <summary>
/// Platform-owned global merchandise product. Soft lifecycle only — never hard-deleted.
/// Barcode/SKU uniqueness is enforced by the repository / application layer.
/// Optimistic concurrency uses <see cref="UpdatedAtUtc"/>.
/// Required: Name, Category, Unit, SKU, Brand, CostPrice, SellingPrice.
/// Optional: Barcode (GS1 digits when present; flexible codes belong in SKU).
/// </summary>
public sealed class GlobalProduct
{
    private readonly List<BusinessTypeId> _businessTypeIds = [];
    private readonly List<string> _searchTags = [];

    public GlobalProductId Id { get; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public string Sku { get; private set; }
    public string? Barcode { get; private set; }
    public string Brand { get; private set; }
    public GlobalCategoryId? GlobalCategoryId { get; private set; }
    public ProductUnit Unit { get; private set; }
    public ProductSellingMode SellingMode { get; private set; }
    public decimal? CostPrice { get; private set; }
    public decimal? SellingPrice { get; private set; }
    public string? ImageReference { get; private set; }
    public GlobalProductStatus Status { get; private set; }
    public IReadOnlyList<string> SearchTags => _searchTags;
    public IReadOnlyList<BusinessTypeId> BusinessTypeIds => _businessTypeIds;
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private GlobalProduct(
        GlobalProductId id,
        string name,
        string? description,
        string sku,
        string? barcode,
        string brand,
        GlobalCategoryId? globalCategoryId,
        ProductUnit unit,
        ProductSellingMode sellingMode,
        decimal? costPrice,
        decimal? sellingPrice,
        string? imageReference,
        GlobalProductStatus status,
        IEnumerable<string> searchTags,
        IEnumerable<BusinessTypeId> businessTypeIds,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        Name = name;
        Description = description;
        Sku = sku;
        Barcode = barcode;
        Brand = brand;
        GlobalCategoryId = globalCategoryId;
        Unit = unit;
        SellingMode = sellingMode;
        CostPrice = costPrice;
        SellingPrice = sellingPrice;
        ImageReference = imageReference;
        Status = status;
        _searchTags.AddRange(GlobalCatalogRules.NormalizeSearchTags(searchTags));
        _businessTypeIds.AddRange(GlobalCatalogRules.NormalizeBusinessTypeIds(businessTypeIds));
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static GlobalProduct Create(
        string name,
        ProductUnit unit,
        string sku,
        string? barcode,
        string brand,
        GlobalCategoryId globalCategoryId,
        DateTimeOffset utcNow,
        decimal? costPrice,
        decimal? sellingPrice,
        string? description = null,
        string? imageReference = null,
        IEnumerable<string>? searchTags = null,
        IEnumerable<BusinessTypeId>? businessTypeIds = null,
        GlobalProductId? id = null,
        ProductSellingMode sellingMode = ProductSellingMode.PerItem)
    {
        DomainTime.EnsureUtc(utcNow);
        EnsureValidUnit(unit);
        ProductSellingModes.EnsureCompatible(sellingMode, unit);
        GlobalCatalogRules.RequireCategory(globalCategoryId);
        var (normalizedCost, normalizedSelling) = GlobalCatalogRules.NormalizeProductPrices(costPrice, sellingPrice);

        return new GlobalProduct(
            id ?? GlobalProductId.New(),
            GlobalCatalogRules.NormalizeName(name),
            GlobalCatalogRules.NormalizeOptionalText(
                description,
                GlobalCatalogRules.DescriptionMaxLength,
                DomainErrorCodes.InvalidGlobalProductDescription),
            GlobalCatalogRules.NormalizeSku(sku),
            GlobalCatalogRules.NormalizeOptionalBarcode(barcode),
            GlobalCatalogRules.NormalizeBrand(brand),
            globalCategoryId,
            unit,
            sellingMode,
            normalizedCost,
            normalizedSelling,
            GlobalCatalogRules.NormalizeOptionalText(
                imageReference,
                GlobalCatalogRules.ImageReferenceMaxLength,
                DomainErrorCodes.InvalidGlobalProductImage),
            GlobalProductStatus.Draft,
            searchTags ?? Array.Empty<string>(),
            businessTypeIds ?? Array.Empty<BusinessTypeId>(),
            utcNow,
            utcNow);
    }

    /// <summary>
    /// Loads persisted state. Legacy rows may have blank SKU/Barcode/Brand, null category, or null prices;
    /// <see cref="Update"/> requires valid values before save.
    /// </summary>
    public static GlobalProduct Rehydrate(
        GlobalProductId id,
        string name,
        string? description,
        string? sku,
        string? barcode,
        string? brand,
        GlobalCategoryId? globalCategoryId,
        ProductUnit unit,
        decimal? costPrice,
        decimal? sellingPrice,
        string? imageReference,
        GlobalProductStatus status,
        IEnumerable<string> searchTags,
        IEnumerable<BusinessTypeId> businessTypeIds,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        ProductSellingMode sellingMode = ProductSellingMode.PerItem) =>
        new(
            id,
            name,
            description,
            sku ?? string.Empty,
            string.IsNullOrWhiteSpace(barcode) ? null : barcode,
            brand ?? string.Empty,
            globalCategoryId,
            unit,
            sellingMode,
            costPrice,
            sellingPrice,
            imageReference,
            status,
            searchTags,
            businessTypeIds,
            createdAtUtc,
            updatedAtUtc);

    public void Update(
        string name,
        ProductUnit unit,
        string sku,
        string? barcode,
        string brand,
        GlobalCategoryId globalCategoryId,
        DateTimeOffset utcNow,
        decimal? costPrice,
        decimal? sellingPrice,
        string? description = null,
        string? imageReference = null,
        IEnumerable<string>? searchTags = null,
        IEnumerable<BusinessTypeId>? businessTypeIds = null,
        ProductSellingMode sellingMode = ProductSellingMode.PerItem)
    {
        EnsureMutable(utcNow);
        EnsureValidUnit(unit);
        ProductSellingModes.EnsureCompatible(sellingMode, unit);
        GlobalCatalogRules.RequireCategory(globalCategoryId);
        var (normalizedCost, normalizedSelling) = GlobalCatalogRules.NormalizeProductPrices(costPrice, sellingPrice);

        Name = GlobalCatalogRules.NormalizeName(name);
        Description = GlobalCatalogRules.NormalizeOptionalText(
            description,
            GlobalCatalogRules.DescriptionMaxLength,
            DomainErrorCodes.InvalidGlobalProductDescription);
        Sku = GlobalCatalogRules.NormalizeSku(sku);
        Barcode = GlobalCatalogRules.NormalizeOptionalBarcode(barcode);
        Brand = GlobalCatalogRules.NormalizeBrand(brand);
        GlobalCategoryId = globalCategoryId;
        Unit = unit;
        SellingMode = sellingMode;
        CostPrice = normalizedCost;
        SellingPrice = normalizedSelling;
        ImageReference = GlobalCatalogRules.NormalizeOptionalText(
            imageReference,
            GlobalCatalogRules.ImageReferenceMaxLength,
            DomainErrorCodes.InvalidGlobalProductImage);

        _searchTags.Clear();
        _searchTags.AddRange(GlobalCatalogRules.NormalizeSearchTags(searchTags));
        _businessTypeIds.Clear();
        _businessTypeIds.AddRange(GlobalCatalogRules.NormalizeBusinessTypeIds(businessTypeIds));

        UpdatedAtUtc = utcNow;
    }

    public void AssignBusinessTypes(IEnumerable<BusinessTypeId> businessTypeIds, DateTimeOffset utcNow)
    {
        EnsureMutable(utcNow);
        _businessTypeIds.Clear();
        _businessTypeIds.AddRange(GlobalCatalogRules.NormalizeBusinessTypeIds(businessTypeIds));
        UpdatedAtUtc = utcNow;
    }

    public void AddBusinessTypes(IEnumerable<BusinessTypeId> businessTypeIds, DateTimeOffset utcNow)
    {
        EnsureMutable(utcNow);
        var merged = _businessTypeIds.Concat(businessTypeIds ?? Array.Empty<BusinessTypeId>());
        _businessTypeIds.Clear();
        _businessTypeIds.AddRange(GlobalCatalogRules.NormalizeBusinessTypeIds(merged));
        UpdatedAtUtc = utcNow;
    }

    public void RemoveBusinessTypes(IEnumerable<BusinessTypeId> businessTypeIds, DateTimeOffset utcNow)
    {
        EnsureMutable(utcNow);
        var remove = new HashSet<Guid>((businessTypeIds ?? Array.Empty<BusinessTypeId>()).Select(i => i.Value));
        _businessTypeIds.RemoveAll(i => remove.Contains(i.Value));
        UpdatedAtUtc = utcNow;
    }

    public void SetStatus(GlobalProductStatus status, DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);
        if (Status == status)
        {
            return;
        }

        var allowed = Status switch
        {
            GlobalProductStatus.Draft => status is GlobalProductStatus.Active or GlobalProductStatus.Archived,
            GlobalProductStatus.Active => status is GlobalProductStatus.Draft or GlobalProductStatus.Archived,
            GlobalProductStatus.Archived => false,
            _ => false
        };

        if (!allowed)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGlobalProductStatusTransition,
                $"Cannot transition GlobalProduct from {Status} to {status}.");
        }

        Status = status;
        UpdatedAtUtc = utcNow;
    }

    private void EnsureMutable(DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);
        if (Status == GlobalProductStatus.Archived)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGlobalProductStatusTransition,
                "An archived GlobalProduct cannot be updated.");
        }
    }

    private static void EnsureValidUnit(ProductUnit unit)
    {
        if (!Enum.IsDefined(unit))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGlobalProductUnit,
                $"Unrecognized product unit '{unit}'.");
        }
    }
}
