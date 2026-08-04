using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.GlobalCatalog;

/// <summary>
/// Composition row linking a global product into a catalog template.
/// Uniqueness of (template, product) is enforced by the aggregate and persistence.
/// </summary>
public sealed class CatalogTemplateProduct
{
    public Guid Id { get; }
    public GlobalProductId GlobalProductId { get; }
    public int SortOrder { get; private set; }
    public bool IsFeatured { get; private set; }
    public bool IsFirstBatch { get; private set; }

    private CatalogTemplateProduct(
        Guid id,
        GlobalProductId globalProductId,
        int sortOrder,
        bool isFeatured,
        bool isFirstBatch)
    {
        Id = id;
        GlobalProductId = globalProductId;
        SortOrder = sortOrder;
        IsFeatured = isFeatured;
        IsFirstBatch = isFirstBatch;
    }

    public static CatalogTemplateProduct Create(
        GlobalProductId globalProductId,
        int sortOrder,
        bool isFeatured = false,
        bool isFirstBatch = false,
        Guid? id = null) =>
        new(id ?? Guid.NewGuid(), globalProductId, sortOrder, isFeatured, isFirstBatch);

    public static CatalogTemplateProduct Rehydrate(
        Guid id,
        GlobalProductId globalProductId,
        int sortOrder,
        bool isFeatured,
        bool isFirstBatch) =>
        new(id, globalProductId, sortOrder, isFeatured, isFirstBatch);

    internal void SetSortOrder(int sortOrder) => SortOrder = sortOrder;

    internal void SetFlags(bool? isFeatured, bool? isFirstBatch)
    {
        if (isFeatured is not null)
        {
            IsFeatured = isFeatured.Value;
        }

        if (isFirstBatch is not null)
        {
            IsFirstBatch = isFirstBatch.Value;
        }
    }
}
