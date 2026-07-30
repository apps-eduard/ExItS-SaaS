using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Domain.Catalog;

/// <summary>
/// Organization-owned POS catalog product. Identification, unit of measure, selling price and
/// lifecycle only — no stock, sales, tax, discount, supplier, or multi-price state.
/// </summary>
public sealed class CatalogProduct
{
    public const int NameMaxLength = 200;
    public const int DescriptionMaxLength = 512;
    public const int SkuMaxLength = 64;
    public const int BarcodeMinLength = 8;
    public const int BarcodeMaxLength = 14;
    public const decimal SellingPriceMax = 9_999_999_999_999_999.99m;

    public CatalogProductId Id { get; }
    public PosOrganizationId OrganizationId { get; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public string? Sku { get; private set; }
    public string? NormalizedSku { get; private set; }
    public string? Barcode { get; private set; }
    public ProductCategoryId? CategoryId { get; private set; }
    public UnitOfMeasure UnitOfMeasure { get; private set; }
    public decimal SellingPrice { get; private set; }
    public CatalogProductStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private CatalogProduct(
        CatalogProductId id,
        PosOrganizationId organizationId,
        string name,
        string? description,
        string? sku,
        string? normalizedSku,
        string? barcode,
        ProductCategoryId? categoryId,
        UnitOfMeasure unitOfMeasure,
        decimal sellingPrice,
        CatalogProductStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        Name = name;
        Description = description;
        Sku = sku;
        NormalizedSku = normalizedSku;
        Barcode = barcode;
        CategoryId = categoryId;
        UnitOfMeasure = unitOfMeasure;
        SellingPrice = sellingPrice;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static CatalogProduct Create(
        PosOrganizationId organizationId,
        string name,
        UnitOfMeasure unitOfMeasure,
        decimal sellingPrice,
        DateTimeOffset utcNow,
        string? description = null,
        string? sku = null,
        string? barcode = null,
        ProductCategoryId? categoryId = null,
        CatalogProductId? id = null)
    {
        CatalogGuards.EnsureUtc(utcNow);
        var (displaySku, normalizedSku) = NormalizeOptionalSku(sku);

        return new CatalogProduct(
            id ?? CatalogProductId.New(),
            organizationId,
            NormalizeName(name),
            NormalizeOptionalDescription(description),
            displaySku,
            normalizedSku,
            NormalizeOptionalBarcode(barcode),
            categoryId,
            unitOfMeasure,
            NormalizeSellingPrice(sellingPrice),
            CatalogProductStatus.Active,
            utcNow,
            utcNow);
    }

    public static CatalogProduct Rehydrate(
        CatalogProductId id,
        PosOrganizationId organizationId,
        string name,
        string? description,
        string? sku,
        string? normalizedSku,
        string? barcode,
        ProductCategoryId? categoryId,
        UnitOfMeasure unitOfMeasure,
        decimal sellingPrice,
        CatalogProductStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc) =>
        new(
            id,
            organizationId,
            name,
            description,
            sku,
            normalizedSku,
            barcode,
            categoryId,
            unitOfMeasure,
            sellingPrice,
            status,
            createdAtUtc,
            updatedAtUtc);

    /// <summary>Updates permitted catalog fields. OrganizationId cannot change.</summary>
    public void UpdateDetails(
        string name,
        string? description,
        string? sku,
        string? barcode,
        ProductCategoryId? categoryId,
        UnitOfMeasure unitOfMeasure,
        decimal sellingPrice,
        DateTimeOffset utcNow)
    {
        CatalogGuards.EnsureUtc(utcNow);
        if (Status == CatalogProductStatus.Inactive)
        {
            throw new DomainException(
                DomainErrorCodes.ProductNotActive,
                "Inactive products cannot be edited. Reactivate first.");
        }

        var (displaySku, normalizedSku) = NormalizeOptionalSku(sku);
        Name = NormalizeName(name);
        Description = NormalizeOptionalDescription(description);
        Sku = displaySku;
        NormalizedSku = normalizedSku;
        Barcode = NormalizeOptionalBarcode(barcode);
        CategoryId = categoryId;
        UnitOfMeasure = unitOfMeasure;
        SellingPrice = NormalizeSellingPrice(sellingPrice);
        UpdatedAtUtc = utcNow;
    }

    public void Deactivate(DateTimeOffset utcNow)
    {
        CatalogGuards.EnsureUtc(utcNow);
        if (Status == CatalogProductStatus.Inactive)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductStatusTransition,
                "Product is already inactive.");
        }

        Status = CatalogProductStatus.Inactive;
        UpdatedAtUtc = utcNow;
    }

    public void Reactivate(DateTimeOffset utcNow)
    {
        CatalogGuards.EnsureUtc(utcNow);
        if (Status == CatalogProductStatus.Active)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductStatusTransition,
                "Product is already active.");
        }

        Status = CatalogProductStatus.Active;
        UpdatedAtUtc = utcNow;
    }

    public static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException(DomainErrorCodes.InvalidProductName, "Product name is required.");
        }

        var trimmed = name.Trim();
        if (trimmed.Length > NameMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductName,
                $"Product name must be 1–{NameMaxLength} characters.");
        }

        return trimmed;
    }

    public static string? NormalizeOptionalDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var trimmed = description.Trim();
        if (trimmed.Length > DescriptionMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductDescription,
                $"Description must be at most {DescriptionMaxLength} characters.");
        }

        return trimmed;
    }

    /// <summary>
    /// Trims the display SKU and derives the uppercase invariant uniqueness key.
    /// Allowed characters are letters, digits, hyphen, underscore, period and forward slash.
    /// </summary>
    public static (string? Display, string? Normalized) NormalizeOptionalSku(string? sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            return (null, null);
        }

        var trimmed = sku.Trim();
        if (trimmed.Length > SkuMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductSku,
                $"SKU must be at most {SkuMaxLength} characters.");
        }

        foreach (var ch in trimmed)
        {
            if (!char.IsLetterOrDigit(ch) && ch is not ('-' or '_' or '.' or '/'))
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidProductSku,
                    "SKU may only contain letters, digits, hyphens, underscores, periods, or forward slashes.");
            }
        }

        return (trimmed, trimmed.ToUpperInvariant());
    }

    /// <summary>
    /// Normalizes an optional primary barcode to digits only, 8–14 digits. Check digits are
    /// validated for the GS1 fixed-length retail formats (EAN-8, UPC-A, EAN-13, GTIN-14).
    /// </summary>
    public static string? NormalizeOptionalBarcode(string? barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            return null;
        }

        var trimmed = barcode.Trim();
        if (!trimmed.All(char.IsAsciiDigit))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductBarcode,
                "Barcode must contain digits only.");
        }

        if (trimmed.Length is < BarcodeMinLength or > BarcodeMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductBarcode,
                $"Barcode must be {BarcodeMinLength}–{BarcodeMaxLength} digits.");
        }

        if (!BarcodeChecksum.IsValid(trimmed))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductBarcode,
                "Barcode check digit is invalid for its GS1 format (EAN-8, UPC-A, EAN-13, or GTIN-14).");
        }

        return trimmed;
    }

    public static decimal NormalizeSellingPrice(decimal sellingPrice)
    {
        if (sellingPrice < 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductSellingPrice,
                "Selling price cannot be negative.");
        }

        if (sellingPrice > SellingPriceMax)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductSellingPrice,
                "Selling price is too large.");
        }

        if (decimal.Round(sellingPrice, 2, MidpointRounding.ToZero) != sellingPrice)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductSellingPrice,
                "Selling price must have at most 2 decimal places.");
        }

        return decimal.Round(sellingPrice, 2);
    }
}
