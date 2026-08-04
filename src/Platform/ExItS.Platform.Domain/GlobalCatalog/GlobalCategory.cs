using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.GlobalCatalog;

/// <summary>
/// Platform-owned global merchandise category. Soft lifecycle only — never hard-deleted.
/// Name uniqueness within a parent scope is enforced by the repository / application layer.
/// </summary>
public sealed class GlobalCategory
{
    private readonly List<BusinessType> _businessTypes = [];

    public GlobalCategoryId Id { get; }
    public string Name { get; private set; }
    public GlobalCategoryId? ParentId { get; private set; }
    public string? IconReference { get; private set; }
    public int SortOrder { get; private set; }
    public GlobalCategoryStatus Status { get; private set; }
    public IReadOnlyList<BusinessType> BusinessTypes => _businessTypes;
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private GlobalCategory(
        GlobalCategoryId id,
        string name,
        GlobalCategoryId? parentId,
        string? iconReference,
        int sortOrder,
        GlobalCategoryStatus status,
        IEnumerable<BusinessType> businessTypes,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        Name = name;
        ParentId = parentId;
        IconReference = iconReference;
        SortOrder = sortOrder;
        Status = status;
        _businessTypes.AddRange(GlobalCatalogRules.NormalizeBusinessTypes(businessTypes));
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static GlobalCategory Create(
        string name,
        DateTimeOffset utcNow,
        GlobalCategoryId? parentId = null,
        string? iconReference = null,
        int sortOrder = 0,
        IEnumerable<BusinessType>? businessTypes = null,
        GlobalCategoryId? id = null)
    {
        DomainTime.EnsureUtc(utcNow);
        var categoryId = id ?? GlobalCategoryId.New();
        EnsureNotSelfParent(categoryId, parentId);

        return new GlobalCategory(
            categoryId,
            GlobalCatalogRules.NormalizeName(name),
            parentId,
            GlobalCatalogRules.NormalizeOptionalText(
                iconReference,
                GlobalCatalogRules.IconReferenceMaxLength,
                DomainErrorCodes.InvalidGlobalCategoryIcon),
            sortOrder,
            GlobalCategoryStatus.Active,
            businessTypes ?? Array.Empty<BusinessType>(),
            utcNow,
            utcNow);
    }

    public static GlobalCategory Rehydrate(
        GlobalCategoryId id,
        string name,
        GlobalCategoryId? parentId,
        string? iconReference,
        int sortOrder,
        GlobalCategoryStatus status,
        IEnumerable<BusinessType> businessTypes,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc) =>
        new(
            id,
            name,
            parentId,
            iconReference,
            sortOrder,
            status,
            businessTypes,
            createdAtUtc,
            updatedAtUtc);

    public void Rename(string name, DateTimeOffset utcNow)
    {
        EnsureMutable(utcNow);
        Name = GlobalCatalogRules.NormalizeName(name);
        UpdatedAtUtc = utcNow;
    }

    public void SetParent(GlobalCategoryId? parentId, DateTimeOffset utcNow)
    {
        EnsureMutable(utcNow);
        EnsureNotSelfParent(Id, parentId);
        ParentId = parentId;
        UpdatedAtUtc = utcNow;
    }

    public void SetSortOrder(int sortOrder, DateTimeOffset utcNow)
    {
        EnsureMutable(utcNow);
        SortOrder = sortOrder;
        UpdatedAtUtc = utcNow;
    }

    public void SetIcon(string? iconReference, DateTimeOffset utcNow)
    {
        EnsureMutable(utcNow);
        IconReference = GlobalCatalogRules.NormalizeOptionalText(
            iconReference,
            GlobalCatalogRules.IconReferenceMaxLength,
            DomainErrorCodes.InvalidGlobalCategoryIcon);
        UpdatedAtUtc = utcNow;
    }

    public void SetStatus(GlobalCategoryStatus status, DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);
        if (Status == status)
        {
            return;
        }

        var allowed = Status switch
        {
            GlobalCategoryStatus.Active => status is GlobalCategoryStatus.Inactive or GlobalCategoryStatus.Archived,
            GlobalCategoryStatus.Inactive => status is GlobalCategoryStatus.Active or GlobalCategoryStatus.Archived,
            GlobalCategoryStatus.Archived => false,
            _ => false
        };

        if (!allowed)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGlobalCategoryStatusTransition,
                $"Cannot transition GlobalCategory from {Status} to {status}.");
        }

        Status = status;
        UpdatedAtUtc = utcNow;
    }

    public void AssignBusinessTypes(IEnumerable<BusinessType> businessTypes, DateTimeOffset utcNow)
    {
        EnsureMutable(utcNow);
        _businessTypes.Clear();
        _businessTypes.AddRange(GlobalCatalogRules.NormalizeBusinessTypes(businessTypes));
        UpdatedAtUtc = utcNow;
    }

    private void EnsureMutable(DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);
        if (Status == GlobalCategoryStatus.Archived)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGlobalCategoryStatusTransition,
                "An archived GlobalCategory cannot be updated.");
        }
    }

    private static void EnsureNotSelfParent(GlobalCategoryId id, GlobalCategoryId? parentId)
    {
        if (parentId is not null && parentId == id)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGlobalCategoryParent,
                "A GlobalCategory cannot be its own parent.");
        }
    }
}
