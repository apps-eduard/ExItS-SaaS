using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Domain.Catalog;

/// <summary>
/// Organization-owned commercial brand. Identity/lifecycle only — no category ownership,
/// supplier ownership, pricing, stock, or logo assets.
/// </summary>
public sealed class ProductBrand
{
    public const int NameMaxLength = 128;

    public ProductBrandId Id { get; }
    public PosOrganizationId OrganizationId { get; }
    public string Name { get; private set; }
    public string NormalizedName { get; private set; }
    public ProductBrandStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private ProductBrand(
        ProductBrandId id,
        PosOrganizationId organizationId,
        string name,
        string normalizedName,
        ProductBrandStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        Name = name;
        NormalizedName = normalizedName;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static ProductBrand Create(
        PosOrganizationId organizationId,
        string name,
        DateTimeOffset utcNow,
        ProductBrandId? id = null)
    {
        CatalogGuards.EnsureUtc(utcNow);
        var display = NormalizeName(name);

        return new ProductBrand(
            id ?? ProductBrandId.New(),
            organizationId,
            display,
            Normalize(display),
            ProductBrandStatus.Active,
            utcNow,
            utcNow);
    }

    public static ProductBrand Rehydrate(
        ProductBrandId id,
        PosOrganizationId organizationId,
        string name,
        string normalizedName,
        ProductBrandStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc) =>
        new(id, organizationId, name, normalizedName, status, createdAtUtc, updatedAtUtc);

    public void Rename(string name, DateTimeOffset utcNow)
    {
        CatalogGuards.EnsureUtc(utcNow);
        if (Status == ProductBrandStatus.Inactive)
        {
            throw new DomainException(
                DomainErrorCodes.BrandNotActive,
                "Inactive brands cannot be edited. Reactivate first.");
        }

        var display = NormalizeName(name);
        Name = display;
        NormalizedName = Normalize(display);
        UpdatedAtUtc = utcNow;
    }

    public void Deactivate(DateTimeOffset utcNow)
    {
        CatalogGuards.EnsureUtc(utcNow);
        if (Status == ProductBrandStatus.Inactive)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidBrandStatusTransition,
                "Brand is already inactive.");
        }

        Status = ProductBrandStatus.Inactive;
        UpdatedAtUtc = utcNow;
    }

    public void Reactivate(DateTimeOffset utcNow)
    {
        CatalogGuards.EnsureUtc(utcNow);
        if (Status == ProductBrandStatus.Active)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidBrandStatusTransition,
                "Brand is already active.");
        }

        Status = ProductBrandStatus.Active;
        UpdatedAtUtc = utcNow;
    }

    public static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException(DomainErrorCodes.InvalidBrandName, "Brand name is required.");
        }

        var trimmed = name.Trim();
        if (trimmed.Length > NameMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidBrandName,
                $"Brand name must be 1–{NameMaxLength} characters.");
        }

        return trimmed;
    }

    /// <summary>Uppercase invariant uniqueness key for a trimmed brand name.</summary>
    public static string Normalize(string trimmedName) => trimmedName.ToUpperInvariant();

    public static string NormalizeForLookup(string name) => Normalize(NormalizeName(name));
}
