using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.GlobalCatalog;

/// <summary>
/// Platform-owned global merchandise product. Soft lifecycle only — never hard-deleted.
/// Barcode/SKU uniqueness is enforced by the repository / application layer.
/// Optimistic concurrency uses <see cref="UpdatedAtUtc"/>.
/// </summary>
public sealed class GlobalProduct
{
    private readonly List<BusinessType> _businessTypes = [];
    private readonly List<string> _searchTags = [];

    public GlobalProductId Id { get; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public string? Sku { get; private set; }
    public string? Barcode { get; private set; }
    public GlobalCategoryId? GlobalCategoryId { get; private set; }
    public ProductUnit Unit { get; private set; }
    public decimal? SuggestedPrice { get; private set; }
    public decimal? SuggestedCost { get; private set; }
    public string? ImageReference { get; private set; }
    public GlobalProductStatus Status { get; private set; }
    public IReadOnlyList<string> SearchTags => _searchTags;
    public IReadOnlyList<BusinessType> BusinessTypes => _businessTypes;
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private GlobalProduct(
        GlobalProductId id,
        string name,
        string? description,
        string? sku,
        string? barcode,
        GlobalCategoryId? globalCategoryId,
        ProductUnit unit,
        decimal? suggestedPrice,
        decimal? suggestedCost,
        string? imageReference,
        GlobalProductStatus status,
        IEnumerable<string> searchTags,
        IEnumerable<BusinessType> businessTypes,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        Name = name;
        Description = description;
        Sku = sku;
        Barcode = barcode;
        GlobalCategoryId = globalCategoryId;
        Unit = unit;
        SuggestedPrice = suggestedPrice;
        SuggestedCost = suggestedCost;
        ImageReference = imageReference;
        Status = status;
        _searchTags.AddRange(GlobalCatalogRules.NormalizeSearchTags(searchTags));
        _businessTypes.AddRange(GlobalCatalogRules.NormalizeBusinessTypes(businessTypes));
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static GlobalProduct Create(
        string name,
        ProductUnit unit,
        DateTimeOffset utcNow,
        string? description = null,
        string? sku = null,
        string? barcode = null,
        GlobalCategoryId? globalCategoryId = null,
        decimal? suggestedPrice = null,
        decimal? suggestedCost = null,
        string? imageReference = null,
        IEnumerable<string>? searchTags = null,
        IEnumerable<BusinessType>? businessTypes = null,
        GlobalProductId? id = null)
    {
        DomainTime.EnsureUtc(utcNow);
        EnsureValidUnit(unit);

        return new GlobalProduct(
            id ?? GlobalProductId.New(),
            GlobalCatalogRules.NormalizeName(name),
            GlobalCatalogRules.NormalizeOptionalText(
                description,
                GlobalCatalogRules.DescriptionMaxLength,
                DomainErrorCodes.InvalidGlobalProductDescription),
            GlobalCatalogRules.NormalizeSku(sku),
            GlobalCatalogRules.NormalizeBarcode(barcode),
            globalCategoryId,
            unit,
            GlobalCatalogRules.NormalizeMoney(suggestedPrice, "SuggestedPrice"),
            GlobalCatalogRules.NormalizeMoney(suggestedCost, "SuggestedCost"),
            GlobalCatalogRules.NormalizeOptionalText(
                imageReference,
                GlobalCatalogRules.ImageReferenceMaxLength,
                DomainErrorCodes.InvalidGlobalProductImage),
            GlobalProductStatus.Draft,
            searchTags ?? Array.Empty<string>(),
            businessTypes ?? Array.Empty<BusinessType>(),
            utcNow,
            utcNow);
    }

    public static GlobalProduct Rehydrate(
        GlobalProductId id,
        string name,
        string? description,
        string? sku,
        string? barcode,
        GlobalCategoryId? globalCategoryId,
        ProductUnit unit,
        decimal? suggestedPrice,
        decimal? suggestedCost,
        string? imageReference,
        GlobalProductStatus status,
        IEnumerable<string> searchTags,
        IEnumerable<BusinessType> businessTypes,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc) =>
        new(
            id,
            name,
            description,
            sku,
            barcode,
            globalCategoryId,
            unit,
            suggestedPrice,
            suggestedCost,
            imageReference,
            status,
            searchTags,
            businessTypes,
            createdAtUtc,
            updatedAtUtc);

    public void Update(
        string name,
        ProductUnit unit,
        DateTimeOffset utcNow,
        string? description = null,
        string? sku = null,
        string? barcode = null,
        GlobalCategoryId? globalCategoryId = null,
        decimal? suggestedPrice = null,
        decimal? suggestedCost = null,
        string? imageReference = null,
        IEnumerable<string>? searchTags = null,
        IEnumerable<BusinessType>? businessTypes = null)
    {
        EnsureMutable(utcNow);
        EnsureValidUnit(unit);

        Name = GlobalCatalogRules.NormalizeName(name);
        Description = GlobalCatalogRules.NormalizeOptionalText(
            description,
            GlobalCatalogRules.DescriptionMaxLength,
            DomainErrorCodes.InvalidGlobalProductDescription);
        Sku = GlobalCatalogRules.NormalizeSku(sku);
        Barcode = GlobalCatalogRules.NormalizeBarcode(barcode);
        GlobalCategoryId = globalCategoryId;
        Unit = unit;
        SuggestedPrice = GlobalCatalogRules.NormalizeMoney(suggestedPrice, "SuggestedPrice");
        SuggestedCost = GlobalCatalogRules.NormalizeMoney(suggestedCost, "SuggestedCost");
        ImageReference = GlobalCatalogRules.NormalizeOptionalText(
            imageReference,
            GlobalCatalogRules.ImageReferenceMaxLength,
            DomainErrorCodes.InvalidGlobalProductImage);

        _searchTags.Clear();
        _searchTags.AddRange(GlobalCatalogRules.NormalizeSearchTags(searchTags));
        _businessTypes.Clear();
        _businessTypes.AddRange(GlobalCatalogRules.NormalizeBusinessTypes(businessTypes));

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
