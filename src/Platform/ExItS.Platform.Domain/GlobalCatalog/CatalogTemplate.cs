using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.GlobalCatalog;

/// <summary>
/// Platform-owned business template for merchant catalog onboarding.
/// Soft lifecycle only. Composition changes never cascade to POS products —
/// POS stores snapshot refs only (imported later by POS-owned flows).
/// Optimistic concurrency uses <see cref="UpdatedAtUtc"/>.
/// </summary>
public sealed class CatalogTemplate
{
    private readonly List<CatalogTemplateProduct> _products = [];

    public CatalogTemplateId Id { get; }
    public string Name { get; private set; }
    public string Slug { get; private set; }
    public string? Description { get; private set; }
    public string? IconReference { get; private set; }
    public BusinessTypeId PrimaryBusinessTypeId { get; private set; }
    public CatalogTemplateStatus Status { get; private set; }
    public int DefaultBatchSize { get; private set; }
    public SelectionMode SelectionMode { get; private set; }
    public DateTimeOffset? PublishedAtUtc { get; private set; }
    public IReadOnlyList<CatalogTemplateProduct> Products => _products;
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public int ProductCount => _products.Count;
    public int FirstBatchCount => _products.Count(p => p.IsFirstBatch);

    private CatalogTemplate(
        CatalogTemplateId id,
        string name,
        string slug,
        string? description,
        string? iconReference,
        BusinessTypeId primaryBusinessTypeId,
        CatalogTemplateStatus status,
        int defaultBatchSize,
        SelectionMode selectionMode,
        DateTimeOffset? publishedAtUtc,
        IEnumerable<CatalogTemplateProduct> products,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        Name = name;
        Slug = slug;
        Description = description;
        IconReference = iconReference;
        PrimaryBusinessTypeId = primaryBusinessTypeId;
        Status = status;
        DefaultBatchSize = defaultBatchSize;
        SelectionMode = selectionMode;
        PublishedAtUtc = publishedAtUtc;
        _products.AddRange(products.OrderBy(p => p.SortOrder).ThenBy(p => p.Id));
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static CatalogTemplate Create(
        string name,
        BusinessTypeId primaryBusinessTypeId,
        DateTimeOffset utcNow,
        string? slug = null,
        string? description = null,
        string? iconReference = null,
        int? defaultBatchSize = null,
        SelectionMode selectionMode = SelectionMode.Curated,
        CatalogTemplateId? id = null)
    {
        DomainTime.EnsureUtc(utcNow);

        return new CatalogTemplate(
            id ?? CatalogTemplateId.New(),
            GlobalCatalogRules.NormalizeName(name),
            GlobalCatalogRules.NormalizeSlug(slug ?? name),
            GlobalCatalogRules.NormalizeOptionalText(
                description,
                GlobalCatalogRules.DescriptionMaxLength,
                DomainErrorCodes.InvalidGlobalProductDescription),
            GlobalCatalogRules.NormalizeOptionalText(
                iconReference,
                GlobalCatalogRules.IconReferenceMaxLength,
                DomainErrorCodes.InvalidGlobalCategoryIcon),
            GlobalCatalogRules.NormalizePrimaryBusinessTypeId(primaryBusinessTypeId),
            CatalogTemplateStatus.Draft,
            GlobalCatalogRules.NormalizeDefaultBatchSize(defaultBatchSize),
            GlobalCatalogRules.NormalizeSelectionMode(selectionMode),
            publishedAtUtc: null,
            products: Array.Empty<CatalogTemplateProduct>(),
            utcNow,
            utcNow);
    }

    public static CatalogTemplate Rehydrate(
        CatalogTemplateId id,
        string name,
        string slug,
        string? description,
        string? iconReference,
        BusinessTypeId primaryBusinessTypeId,
        CatalogTemplateStatus status,
        int defaultBatchSize,
        SelectionMode selectionMode,
        DateTimeOffset? publishedAtUtc,
        IEnumerable<CatalogTemplateProduct> products,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc) =>
        new(
            id,
            name,
            slug,
            description,
            iconReference,
            primaryBusinessTypeId,
            status,
            defaultBatchSize,
            selectionMode,
            publishedAtUtc,
            products,
            createdAtUtc,
            updatedAtUtc);

    public void Update(
        string name,
        BusinessTypeId primaryBusinessTypeId,
        DateTimeOffset utcNow,
        string? slug = null,
        string? description = null,
        string? iconReference = null,
        int? defaultBatchSize = null,
        SelectionMode? selectionMode = null)
    {
        EnsureMutable(utcNow);

        Name = GlobalCatalogRules.NormalizeName(name);
        Slug = GlobalCatalogRules.NormalizeSlug(slug ?? name);
        Description = GlobalCatalogRules.NormalizeOptionalText(
            description,
            GlobalCatalogRules.DescriptionMaxLength,
            DomainErrorCodes.InvalidGlobalProductDescription);
        IconReference = GlobalCatalogRules.NormalizeOptionalText(
            iconReference,
            GlobalCatalogRules.IconReferenceMaxLength,
            DomainErrorCodes.InvalidGlobalCategoryIcon);
        PrimaryBusinessTypeId = GlobalCatalogRules.NormalizePrimaryBusinessTypeId(primaryBusinessTypeId);
        DefaultBatchSize = GlobalCatalogRules.NormalizeDefaultBatchSize(defaultBatchSize ?? DefaultBatchSize);
        if (selectionMode is not null)
        {
            SelectionMode = GlobalCatalogRules.NormalizeSelectionMode(selectionMode.Value);
        }

        UpdatedAtUtc = utcNow;
    }

    public void Publish(DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);
        if (Status == CatalogTemplateStatus.Published)
        {
            return;
        }

        if (Status != CatalogTemplateStatus.Draft)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCatalogTemplateStatusTransition,
                $"Cannot publish CatalogTemplate from {Status}.");
        }

        if (_products.Count == 0)
        {
            throw new DomainException(
                DomainErrorCodes.CatalogTemplatePublishRequiresProducts,
                "A template must include at least one product before publish.");
        }

        Status = CatalogTemplateStatus.Published;
        PublishedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public void Unpublish(DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);
        if (Status == CatalogTemplateStatus.Draft)
        {
            return;
        }

        if (Status != CatalogTemplateStatus.Published)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCatalogTemplateStatusTransition,
                $"Cannot unpublish CatalogTemplate from {Status}.");
        }

        Status = CatalogTemplateStatus.Draft;
        UpdatedAtUtc = utcNow;
    }

    public void Archive(DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);
        if (Status == CatalogTemplateStatus.Archived)
        {
            return;
        }

        if (Status is not (CatalogTemplateStatus.Draft or CatalogTemplateStatus.Published))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCatalogTemplateStatusTransition,
                $"Cannot archive CatalogTemplate from {Status}.");
        }

        Status = CatalogTemplateStatus.Archived;
        UpdatedAtUtc = utcNow;
    }

    public CatalogTemplateProduct AssignProduct(
        GlobalProductId globalProductId,
        DateTimeOffset utcNow,
        bool isFeatured = false,
        bool isFirstBatch = false,
        int? sortOrder = null)
    {
        EnsureMutable(utcNow);

        if (_products.Any(p => p.GlobalProductId == globalProductId))
        {
            throw new DomainException(
                DomainErrorCodes.CatalogTemplateProductDuplicate,
                "This product is already assigned to the template.");
        }

        var order = sortOrder ?? (_products.Count == 0 ? 0 : _products.Max(p => p.SortOrder) + 1);
        var row = CatalogTemplateProduct.Create(globalProductId, order, isFeatured, isFirstBatch);
        _products.Add(row);
        UpdatedAtUtc = utcNow;
        return row;
    }

    /// <summary>
    /// Idempotent membership link used by bulk import. Returns false when the product is already assigned.
    /// </summary>
    public bool TryAssignProduct(
        GlobalProductId globalProductId,
        DateTimeOffset utcNow,
        bool isFeatured = false,
        bool isFirstBatch = false,
        int? sortOrder = null)
    {
        EnsureMutable(utcNow);

        if (_products.Any(p => p.GlobalProductId == globalProductId))
        {
            return false;
        }

        AssignProduct(globalProductId, utcNow, isFeatured, isFirstBatch, sortOrder);
        return true;
    }

    public void RemoveProduct(GlobalProductId globalProductId, DateTimeOffset utcNow)
    {
        EnsureMutable(utcNow);

        var removed = _products.RemoveAll(p => p.GlobalProductId == globalProductId);
        if (removed == 0)
        {
            throw new DomainException(
                DomainErrorCodes.CatalogTemplateProductNotFound,
                "Product is not part of this template.");
        }

        UpdatedAtUtc = utcNow;
    }

    public void SetProductFlags(
        GlobalProductId globalProductId,
        DateTimeOffset utcNow,
        bool? isFeatured = null,
        bool? isFirstBatch = null)
    {
        EnsureMutable(utcNow);
        var row = FindProduct(globalProductId);
        row.SetFlags(isFeatured, isFirstBatch);
        UpdatedAtUtc = utcNow;
    }

    /// <summary>
    /// Reorders composition to match <paramref name="orderedProductIds"/> exactly (same set, new order).
    /// </summary>
    public void ReorderProducts(IReadOnlyList<GlobalProductId> orderedProductIds, DateTimeOffset utcNow)
    {
        EnsureMutable(utcNow);

        if (orderedProductIds.Count != _products.Count)
        {
            throw new DomainException(
                DomainErrorCodes.CatalogTemplateCompositionOrderInvalid,
                "Reorder list must include every assigned product exactly once.");
        }

        var seen = new HashSet<Guid>();
        foreach (var id in orderedProductIds)
        {
            if (!seen.Add(id.Value))
            {
                throw new DomainException(
                    DomainErrorCodes.CatalogTemplateCompositionOrderInvalid,
                    "Reorder list contains duplicate product ids.");
            }

            if (_products.All(p => p.GlobalProductId != id))
            {
                throw new DomainException(
                    DomainErrorCodes.CatalogTemplateCompositionOrderInvalid,
                    "Reorder list references a product that is not assigned.");
            }
        }

        for (var i = 0; i < orderedProductIds.Count; i++)
        {
            FindProduct(orderedProductIds[i]).SetSortOrder(i);
        }

        _products.Sort((a, b) =>
        {
            var cmp = a.SortOrder.CompareTo(b.SortOrder);
            return cmp != 0 ? cmp : a.Id.CompareTo(b.Id);
        });

        UpdatedAtUtc = utcNow;
    }

    private CatalogTemplateProduct FindProduct(GlobalProductId globalProductId)
    {
        var row = _products.FirstOrDefault(p => p.GlobalProductId == globalProductId);
        if (row is null)
        {
            throw new DomainException(
                DomainErrorCodes.CatalogTemplateProductNotFound,
                "Product is not part of this template.");
        }

        return row;
    }

    private void EnsureMutable(DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);
        if (Status == CatalogTemplateStatus.Archived)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCatalogTemplateStatusTransition,
                "An archived CatalogTemplate cannot be updated.");
        }
    }
}
