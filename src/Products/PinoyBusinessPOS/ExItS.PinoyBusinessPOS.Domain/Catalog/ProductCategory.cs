using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Domain.Catalog;

/// <summary>
/// Organization-owned flat product category. Grouping and lifecycle only — no pricing, stock,
/// hierarchy, tax, or supplier state.
/// </summary>
public sealed class ProductCategory
{
    public const int NameMaxLength = 128;

    public ProductCategoryId Id { get; }
    public PosOrganizationId OrganizationId { get; }
    public string Name { get; private set; }
    public string NormalizedName { get; private set; }
    public ProductCategoryStatus Status { get; private set; }
    /// <summary>External Platform global category id only — never a cross-database FK.</summary>
    public Guid? SourceGlobalCategoryId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private ProductCategory(
        ProductCategoryId id,
        PosOrganizationId organizationId,
        string name,
        string normalizedName,
        ProductCategoryStatus status,
        Guid? sourceGlobalCategoryId,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        Name = name;
        NormalizedName = normalizedName;
        Status = status;
        SourceGlobalCategoryId = sourceGlobalCategoryId;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static ProductCategory Create(
        PosOrganizationId organizationId,
        string name,
        DateTimeOffset utcNow,
        ProductCategoryId? id = null,
        Guid? sourceGlobalCategoryId = null)
    {
        CatalogGuards.EnsureUtc(utcNow);
        var display = NormalizeName(name);

        return new ProductCategory(
            id ?? ProductCategoryId.New(),
            organizationId,
            display,
            Normalize(display),
            ProductCategoryStatus.Active,
            sourceGlobalCategoryId == Guid.Empty ? null : sourceGlobalCategoryId,
            utcNow,
            utcNow);
    }

    public static ProductCategory Rehydrate(
        ProductCategoryId id,
        PosOrganizationId organizationId,
        string name,
        string normalizedName,
        ProductCategoryStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        Guid? sourceGlobalCategoryId = null) =>
        new(id, organizationId, name, normalizedName, status, sourceGlobalCategoryId, createdAtUtc, updatedAtUtc);

    public void Rename(string name, DateTimeOffset utcNow)
    {
        CatalogGuards.EnsureUtc(utcNow);
        if (Status == ProductCategoryStatus.Inactive)
        {
            throw new DomainException(
                DomainErrorCodes.CategoryNotActive,
                "Inactive categories cannot be edited. Reactivate first.");
        }

        var display = NormalizeName(name);
        Name = display;
        NormalizedName = Normalize(display);
        UpdatedAtUtc = utcNow;
    }

    public void Deactivate(DateTimeOffset utcNow)
    {
        CatalogGuards.EnsureUtc(utcNow);
        if (Status == ProductCategoryStatus.Inactive)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCategoryStatusTransition,
                "Category is already inactive.");
        }

        Status = ProductCategoryStatus.Inactive;
        UpdatedAtUtc = utcNow;
    }

    public void Reactivate(DateTimeOffset utcNow)
    {
        CatalogGuards.EnsureUtc(utcNow);
        if (Status == ProductCategoryStatus.Active)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCategoryStatusTransition,
                "Category is already active.");
        }

        Status = ProductCategoryStatus.Active;
        UpdatedAtUtc = utcNow;
    }

    public static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException(DomainErrorCodes.InvalidCategoryName, "Category name is required.");
        }

        var trimmed = name.Trim();
        if (trimmed.Length > NameMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCategoryName,
                $"Category name must be 1–{NameMaxLength} characters.");
        }

        return trimmed;
    }

    /// <summary>Uppercase invariant uniqueness key for a trimmed category name.</summary>
    public static string Normalize(string trimmedName) => trimmedName.ToUpperInvariant();

    public static string NormalizeForLookup(string name) => Normalize(NormalizeName(name));
}
