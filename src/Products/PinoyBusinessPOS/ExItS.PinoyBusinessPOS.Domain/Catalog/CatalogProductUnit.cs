using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.Catalog;

/// <summary>
/// Organization-owned product-specific purchase or sell unit. Multiplier converts entered quantity
/// into the product's authoritative base inventory unit (<see cref="CatalogProduct.UnitOfMeasure"/>).
/// </summary>
public sealed class CatalogProductUnit
{
    public const int DisplayNameMaxLength = 64;
    public const int ShortLabelMaxLength = 16;
    public const int MultiplierMaxDecimals = 3;
    public const decimal SellingPriceMax = CatalogProduct.SellingPriceMax;

    public ProductUnitId Id { get; }
    public PosOrganizationId OrganizationId { get; }
    public CatalogProductId ProductId { get; }
    public ProductUnitKind Kind { get; private set; }
    public string DisplayName { get; private set; }
    public string ShortLabel { get; private set; }
    public decimal MultiplierToBase { get; private set; }
    public decimal? SellingPrice { get; private set; }
    public bool AllowsCustomQuantity { get; private set; }
    public bool IsActive { get; private set; }
    public int SortOrder { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private CatalogProductUnit(
        ProductUnitId id,
        PosOrganizationId organizationId,
        CatalogProductId productId,
        ProductUnitKind kind,
        string displayName,
        string shortLabel,
        decimal multiplierToBase,
        decimal? sellingPrice,
        bool allowsCustomQuantity,
        bool isActive,
        int sortOrder,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        ProductId = productId;
        Kind = kind;
        DisplayName = displayName;
        ShortLabel = shortLabel;
        MultiplierToBase = multiplierToBase;
        SellingPrice = sellingPrice;
        AllowsCustomQuantity = allowsCustomQuantity;
        IsActive = isActive;
        SortOrder = sortOrder;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static CatalogProductUnit Create(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        ProductUnitKind kind,
        string displayName,
        string shortLabel,
        decimal multiplierToBase,
        DateTimeOffset utcNow,
        decimal? sellingPrice = null,
        bool allowsCustomQuantity = false,
        int sortOrder = 0,
        ProductUnitId? id = null)
    {
        CatalogGuards.EnsureUtc(utcNow);
        EnsureKind(kind);
        var normalizedPrice = NormalizeSellingPriceForKind(kind, sellingPrice);

        return new CatalogProductUnit(
            id ?? ProductUnitId.New(),
            organizationId,
            productId,
            kind,
            NormalizeDisplayName(displayName),
            NormalizeShortLabel(shortLabel),
            NormalizeMultiplier(multiplierToBase),
            normalizedPrice,
            allowsCustomQuantity,
            isActive: true,
            NormalizeSortOrder(sortOrder),
            utcNow,
            utcNow);
    }

    public static CatalogProductUnit Rehydrate(
        ProductUnitId id,
        PosOrganizationId organizationId,
        CatalogProductId productId,
        ProductUnitKind kind,
        string displayName,
        string shortLabel,
        decimal multiplierToBase,
        decimal? sellingPrice,
        bool allowsCustomQuantity,
        bool isActive,
        int sortOrder,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc) =>
        new(
            id,
            organizationId,
            productId,
            kind,
            displayName,
            shortLabel,
            multiplierToBase,
            sellingPrice,
            allowsCustomQuantity,
            isActive,
            sortOrder,
            createdAtUtc,
            updatedAtUtc);

    public void Update(
        string displayName,
        string shortLabel,
        decimal multiplierToBase,
        DateTimeOffset utcNow,
        decimal? sellingPrice = null,
        bool? allowsCustomQuantity = null,
        int? sortOrder = null,
        ProductUnitKind? kind = null)
    {
        CatalogGuards.EnsureUtc(utcNow);
        if (!IsActive)
        {
            throw new DomainException(
                DomainErrorCodes.ProductUnitNotActive,
                "Inactive product units cannot be edited. Reactivate or create a new unit.");
        }

        if (kind is not null)
        {
            EnsureKind(kind.Value);
            Kind = kind.Value;
        }

        DisplayName = NormalizeDisplayName(displayName);
        ShortLabel = NormalizeShortLabel(shortLabel);
        MultiplierToBase = NormalizeMultiplier(multiplierToBase);
        SellingPrice = NormalizeSellingPriceForKind(Kind, sellingPrice);
        if (allowsCustomQuantity is not null)
        {
            AllowsCustomQuantity = allowsCustomQuantity.Value;
        }

        if (sortOrder is not null)
        {
            SortOrder = NormalizeSortOrder(sortOrder.Value);
        }

        UpdatedAtUtc = utcNow;
    }

    public void Deactivate(DateTimeOffset utcNow)
    {
        CatalogGuards.EnsureUtc(utcNow);
        if (!IsActive)
        {
            throw new DomainException(
                DomainErrorCodes.ProductUnitNotActive,
                "Product unit is already inactive.");
        }

        IsActive = false;
        UpdatedAtUtc = utcNow;
    }

    public static decimal NormalizeMultiplier(decimal multiplierToBase)
    {
        ProductUnitConversion.EnsureValidMultiplier(multiplierToBase);

        if (!SaleMoney.HasAtMostDecimals(multiplierToBase, MultiplierMaxDecimals))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductUnitMultiplier,
                $"Unit multiplier may have at most {MultiplierMaxDecimals} decimal places.");
        }

        return multiplierToBase;
    }

    private static void EnsureKind(ProductUnitKind kind)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductUnitKind,
                "Product unit kind is invalid.");
        }
    }

    private static decimal? NormalizeSellingPriceForKind(ProductUnitKind kind, decimal? sellingPrice)
    {
        if (kind == ProductUnitKind.Sell)
        {
            if (sellingPrice is null)
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidProductUnitSellingPrice,
                    "Sell units require a selling price (zero is allowed).");
            }

            return NormalizeSellingPrice(sellingPrice.Value);
        }

        return sellingPrice is null ? null : NormalizeSellingPrice(sellingPrice.Value);
    }

    private static decimal NormalizeSellingPrice(decimal sellingPrice)
    {
        if (sellingPrice < 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductUnitSellingPrice,
                "Unit selling price cannot be negative.");
        }

        if (sellingPrice > SellingPriceMax)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductUnitSellingPrice,
                "Unit selling price is too large.");
        }

        if (!SaleMoney.HasAtMostDecimals(sellingPrice, SaleMoney.MonetaryDecimals))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductUnitSellingPrice,
                "Unit selling price must have at most 2 decimal places.");
        }

        return SaleMoney.RoundMoney(sellingPrice);
    }

    private static string NormalizeDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductUnitName,
                "Product unit display name is required.");
        }

        var trimmed = displayName.Trim();
        if (trimmed.Length > DisplayNameMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductUnitName,
                $"Product unit display name must be at most {DisplayNameMaxLength} characters.");
        }

        return trimmed;
    }

    private static string NormalizeShortLabel(string shortLabel)
    {
        if (string.IsNullOrWhiteSpace(shortLabel))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductUnitName,
                "Product unit short label is required.");
        }

        var trimmed = shortLabel.Trim();
        if (trimmed.Length > ShortLabelMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductUnitName,
                $"Product unit short label must be at most {ShortLabelMaxLength} characters.");
        }

        return trimmed;
    }

    private static int NormalizeSortOrder(int sortOrder)
    {
        if (sortOrder < 0)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductUnitSortOrder,
                "Product unit sort order cannot be negative.");
        }

        return sortOrder;
    }
}
