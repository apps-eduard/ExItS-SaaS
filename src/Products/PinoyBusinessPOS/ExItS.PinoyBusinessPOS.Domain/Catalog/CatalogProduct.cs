using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Domain.Catalog;

/// <summary>
/// Organization-owned POS catalog product. Identification, unit of measure, selling price and
/// lifecycle — plus optional Platform external refs for imported snapshots. No stock authority;
/// Platform never overwrites local price/stock/tax/name/category/active after import.
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
    /// <summary>
    /// OrganizationStandard (centrally governed) or BranchLocal (origin-branch governed).
    /// Not Platform Global Catalog.
    /// </summary>
    public CatalogProductScope Scope { get; private set; }
    /// <summary>
    /// Required when <see cref="CatalogProductScope.BranchLocal"/>. May remain after promotion for audit.
    /// Opaque Platform branch id — not a POS FK.
    /// </summary>
    public PosBranchId? OriginBranchId { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public string? Sku { get; private set; }
    public string? NormalizedSku { get; private set; }
    public string? Barcode { get; private set; }
    public ProductCategoryId? CategoryId { get; private set; }
    public ProductBrandId? BrandId { get; private set; }
    public UnitOfMeasure UnitOfMeasure { get; private set; }
    public SellingMode SellingMode { get; private set; }
    public decimal SellingPrice { get; private set; }
    public CatalogProductStatus Status { get; private set; }
    public Guid? PlatformGlobalProductId { get; private set; }
    public Guid? PlatformTemplateId { get; private set; }
    /// <summary>
    /// Snapshot of the Platform template/manufacturer GS1 barcode at import.
    /// Independent of the organization-owned <see cref="Barcode"/> scan code. Historical rows stay null.
    /// </summary>
    public string? PlatformBarcode { get; private set; }
    /// <summary>
    /// Snapshot of the shared Platform image version at import. Live serving may be newer.
    /// </summary>
    public int? PlatformImageVersion { get; private set; }
    public CatalogSource CatalogSource { get; private set; }
    public DateTimeOffset? CatalogImportedAt { get; private set; }
    public int? CatalogSnapshotVersion { get; private set; }
    public Guid? SourceGlobalCategoryId { get; private set; }
    public bool TracksExpiration { get; private set; }
    public int? ExpirationWarningDays { get; private set; }
    /// <summary>
    /// When true, the product is globally blocked from connected-buyer eligibility (Level-2).
    /// Source of truth for the block; <see cref="CanExposeToConnectedBuyers"/> is the persisted inverse.
    /// </summary>
    public bool IsBlockedFromConnectedBuyers { get; private set; }

    /// <summary>
    /// Eligible for connected-buyer Level-2 when not blocked. Always <c>== !IsBlockedFromConnectedBuyers</c>.
    /// Eligibility does not auto-share; sharing still requires an exposure row and Level-2 share.
    /// </summary>
    public bool CanExposeToConnectedBuyers { get; private set; }
    public decimal? DefaultConnectedPoPrice { get; private set; }

    /// <summary>Usage flags are authoritative; defaults match BuyAndSell.</summary>
    public bool CanBePurchased { get; private set; }
    public bool CanBeSold { get; private set; }
    public bool CanBeUsedAsIngredient { get; private set; }
    public bool IsProduced { get; private set; }

    /// <summary>Optional UX preset code (e.g. BuyAndSell, Bulk). Flags remain authoritative.</summary>
    public string? UsagePreset { get; private set; }

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
        ProductBrandId? brandId,
        UnitOfMeasure unitOfMeasure,
        SellingMode sellingMode,
        decimal sellingPrice,
        CatalogProductStatus status,
        Guid? platformGlobalProductId,
        Guid? platformTemplateId,
        CatalogSource catalogSource,
        DateTimeOffset? catalogImportedAt,
        int? catalogSnapshotVersion,
        Guid? sourceGlobalCategoryId,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        bool tracksExpiration = false,
        int? expirationWarningDays = null,
        bool canBePurchased = true,
        bool canBeSold = true,
        bool canBeUsedAsIngredient = false,
        bool isProduced = false,
        string? usagePreset = null,
        bool isBlockedFromConnectedBuyers = false,
        bool canExposeToConnectedBuyers = true,
        decimal? defaultConnectedPoPrice = null,
        string? platformBarcode = null,
        int? platformImageVersion = null,
        CatalogProductScope scope = CatalogProductScope.OrganizationStandard,
        PosBranchId? originBranchId = null)
    {
        CatalogProductScopes.EnsureOriginValid(scope, originBranchId);
        Id = id;
        OrganizationId = organizationId;
        Scope = scope;
        OriginBranchId = originBranchId;
        Name = name;
        Description = description;
        Sku = sku;
        NormalizedSku = normalizedSku;
        Barcode = barcode;
        CategoryId = categoryId;
        BrandId = brandId;
        UnitOfMeasure = unitOfMeasure;
        SellingMode = sellingMode;
        SellingPrice = sellingPrice;
        Status = status;
        PlatformGlobalProductId = platformGlobalProductId;
        PlatformTemplateId = platformTemplateId;
        PlatformBarcode = string.IsNullOrWhiteSpace(platformBarcode) ? null : platformBarcode.Trim();
        PlatformImageVersion = platformImageVersion is > 0 ? platformImageVersion : null;
        CatalogSource = catalogSource;
        CatalogImportedAt = catalogImportedAt;
        CatalogSnapshotVersion = catalogSnapshotVersion;
        SourceGlobalCategoryId = sourceGlobalCategoryId;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        TracksExpiration = tracksExpiration;
        ExpirationWarningDays = tracksExpiration
            ? Inventory.InventoryLot.NormalizeWarningDays(expirationWarningDays)
            : null;
        CanBePurchased = canBePurchased;
        CanBeSold = canBeSold;
        CanBeUsedAsIngredient = canBeUsedAsIngredient;
        IsProduced = isProduced;
        UsagePreset = usagePreset;
        // IsBlocked is the persisted source of truth; CanExpose is always the inverse.
        // Legacy rehydrate callers that only pass canExpose:false still resolve to blocked.
        IsBlockedFromConnectedBuyers = isBlockedFromConnectedBuyers || !canExposeToConnectedBuyers;
        CanExposeToConnectedBuyers = !IsBlockedFromConnectedBuyers;
        DefaultConnectedPoPrice = defaultConnectedPoPrice is null
            ? null
            : NormalizeConnectedPoPrice(defaultConnectedPoPrice.Value);
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
        ProductBrandId? brandId = null,
        CatalogProductId? id = null,
        SellingMode sellingMode = SellingMode.PerItem,
        bool tracksExpiration = false,
        int? expirationWarningDays = null,
        ProductUsageCapabilities? usage = null,
        CatalogProductScope scope = CatalogProductScope.OrganizationStandard,
        PosBranchId? originBranchId = null)
    {
        CatalogGuards.EnsureUtc(utcNow);
        SellingModes.EnsureCompatible(sellingMode, unitOfMeasure);
        CatalogProductScopes.EnsureOriginValid(scope, originBranchId);
        var (displaySku, normalizedSku) = NormalizeOptionalSku(sku);
        var resolvedUsage = usage ?? ProductUsageCapabilities.BuyAndSell;
        resolvedUsage.EnsureValid();

        return new CatalogProduct(
            id ?? CatalogProductId.New(),
            organizationId,
            NormalizeName(name),
            NormalizeOptionalDescription(description),
            displaySku,
            normalizedSku,
            NormalizeOptionalBarcode(barcode),
            categoryId,
            brandId,
            unitOfMeasure,
            sellingMode,
            NormalizeSellingPrice(sellingPrice),
            CatalogProductStatus.Active,
            platformGlobalProductId: null,
            platformTemplateId: null,
            CatalogSource.Manual,
            catalogImportedAt: null,
            catalogSnapshotVersion: null,
            sourceGlobalCategoryId: null,
            utcNow,
            utcNow,
            tracksExpiration,
            expirationWarningDays,
            resolvedUsage.CanBePurchased,
            resolvedUsage.CanBeSold,
            resolvedUsage.CanBeUsedAsIngredient,
            resolvedUsage.IsProduced,
            resolvedUsage.PresetCode ?? ProductUsageCapabilities.BuyAndSellCode,
            scope: scope,
            originBranchId: originBranchId);
    }

    /// <summary>
    /// Creates an editable local snapshot from a Platform global product. Stock is not set —
    /// opening stock must go through <c>StockMovement.OpeningStock</c>.
    /// </summary>
    public static CatalogProduct CreateImportedSnapshot(
        PosOrganizationId organizationId,
        string name,
        UnitOfMeasure unitOfMeasure,
        decimal sellingPrice,
        Guid platformGlobalProductId,
        CatalogSource catalogSource,
        DateTimeOffset utcNow,
        string? description = null,
        string? sku = null,
        string? barcode = null,
        ProductCategoryId? categoryId = null,
        Guid? platformTemplateId = null,
        Guid? sourceGlobalCategoryId = null,
        ProductBrandId? brandId = null,
        int snapshotVersion = CatalogImportRules.SnapshotVersion,
        CatalogProductId? id = null,
        SellingMode sellingMode = SellingMode.PerItem,
        ProductUsageCapabilities? usage = null,
        string? platformBarcode = null,
        int? platformImageVersion = null)
    {
        CatalogGuards.EnsureUtc(utcNow);
        SellingModes.EnsureCompatible(sellingMode, unitOfMeasure);
        if (platformGlobalProductId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCatalogImportItem,
                "PlatformGlobalProductId is required for imported products.");
        }

        if (catalogSource is CatalogSource.Manual)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCatalogSource,
                "Imported snapshots cannot use CatalogSource.Manual.");
        }

        var (displaySku, normalizedSku) = NormalizeOptionalSku(sku);
        var resolvedUsage = usage ?? ProductUsageCapabilities.BuyAndSell;
        resolvedUsage.EnsureValid();

        return new CatalogProduct(
            id ?? CatalogProductId.New(),
            organizationId,
            NormalizeName(name),
            NormalizeOptionalDescription(description),
            displaySku,
            normalizedSku,
            NormalizeOptionalBarcode(barcode),
            categoryId,
            brandId,
            unitOfMeasure,
            sellingMode,
            NormalizeSellingPrice(sellingPrice),
            CatalogProductStatus.Active,
            platformGlobalProductId,
            platformTemplateId == Guid.Empty ? null : platformTemplateId,
            catalogSource,
            utcNow,
            snapshotVersion,
            sourceGlobalCategoryId == Guid.Empty ? null : sourceGlobalCategoryId,
            utcNow,
            utcNow,
            canBePurchased: resolvedUsage.CanBePurchased,
            canBeSold: resolvedUsage.CanBeSold,
            canBeUsedAsIngredient: resolvedUsage.CanBeUsedAsIngredient,
            isProduced: resolvedUsage.IsProduced,
            usagePreset: resolvedUsage.PresetCode ?? ProductUsageCapabilities.BuyAndSellCode,
            platformBarcode: platformBarcode ?? barcode,
            platformImageVersion: platformImageVersion);
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
        DateTimeOffset updatedAtUtc,
        Guid? platformGlobalProductId = null,
        Guid? platformTemplateId = null,
        CatalogSource catalogSource = CatalogSource.Manual,
        DateTimeOffset? catalogImportedAt = null,
        int? catalogSnapshotVersion = null,
        Guid? sourceGlobalCategoryId = null,
        SellingMode sellingMode = SellingMode.PerItem,
        bool tracksExpiration = false,
        int? expirationWarningDays = null,
        bool canBePurchased = true,
        bool canBeSold = true,
        bool canBeUsedAsIngredient = false,
        bool isProduced = false,
        string? usagePreset = null,
        bool isBlockedFromConnectedBuyers = false,
        bool canExposeToConnectedBuyers = true,
        decimal? defaultConnectedPoPrice = null,
        string? platformBarcode = null,
        int? platformImageVersion = null,
        ProductBrandId? brandId = null,
        CatalogProductScope scope = CatalogProductScope.OrganizationStandard,
        PosBranchId? originBranchId = null) =>
        new(
            id,
            organizationId,
            name,
            description,
            sku,
            normalizedSku,
            barcode,
            categoryId,
            brandId,
            unitOfMeasure,
            sellingMode,
            sellingPrice,
            status,
            platformGlobalProductId,
            platformTemplateId,
            catalogSource,
            catalogImportedAt,
            catalogSnapshotVersion,
            sourceGlobalCategoryId,
            createdAtUtc,
            updatedAtUtc,
            tracksExpiration,
            expirationWarningDays,
            canBePurchased,
            canBeSold,
            canBeUsedAsIngredient,
            isProduced,
            usagePreset,
            isBlockedFromConnectedBuyers,
            canExposeToConnectedBuyers,
            defaultConnectedPoPrice,
            platformBarcode,
            platformImageVersion,
            scope,
            originBranchId);

    /// <summary>Updates how the product participates in buy / sell / ingredient / production flows.</summary>
    public void UpdateUsage(ProductUsageCapabilities usage, DateTimeOffset utcNow)
    {
        CatalogGuards.EnsureUtc(utcNow);
        if (Status == CatalogProductStatus.Inactive)
        {
            throw new DomainException(
                DomainErrorCodes.ProductNotActive,
                "Inactive products cannot be edited. Reactivate first.");
        }

        usage.EnsureValid();
        CanBePurchased = usage.CanBePurchased;
        CanBeSold = usage.CanBeSold;
        CanBeUsedAsIngredient = usage.CanBeUsedAsIngredient;
        IsProduced = usage.IsProduced;
        UsagePreset = usage.PresetCode;
        UpdatedAtUtc = utcNow;
    }
    /// <summary>Updates permitted catalog fields. OrganizationId and Platform provenance cannot change.</summary>
    public void UpdateDetails(
        string name,
        string? description,
        string? sku,
        string? barcode,
        ProductCategoryId? categoryId,
        ProductBrandId? brandId,
        UnitOfMeasure unitOfMeasure,
        decimal sellingPrice,
        DateTimeOffset utcNow,
        SellingMode sellingMode = SellingMode.PerItem)
    {
        CatalogGuards.EnsureUtc(utcNow);
        if (Status == CatalogProductStatus.Inactive)
        {
            throw new DomainException(
                DomainErrorCodes.ProductNotActive,
                "Inactive products cannot be edited. Reactivate first.");
        }

        SellingModes.EnsureCompatible(sellingMode, unitOfMeasure);
        var (displaySku, normalizedSku) = NormalizeOptionalSku(sku);
        Name = NormalizeName(name);
        Description = NormalizeOptionalDescription(description);
        Sku = displaySku;
        NormalizedSku = normalizedSku;
        Barcode = NormalizeOptionalBarcode(barcode);
        CategoryId = categoryId;
        BrandId = brandId;
        UnitOfMeasure = unitOfMeasure;
        SellingMode = sellingMode;
        SellingPrice = NormalizeSellingPrice(sellingPrice);
        UpdatedAtUtc = utcNow;
    }

    /// <summary>
    /// Optional per-product expiration tracking. Default is off. Does not change Global Catalog.
    /// </summary>
    public void SetExpirationTracking(bool tracksExpiration, int? expirationWarningDays, DateTimeOffset utcNow)
    {
        CatalogGuards.EnsureUtc(utcNow);
        TracksExpiration = tracksExpiration;
        ExpirationWarningDays = tracksExpiration
            ? Inventory.InventoryLot.NormalizeWarningDays(expirationWarningDays)
            : null;
        UpdatedAtUtc = utcNow;
    }

    public int EffectiveExpirationWarningDays =>
        TracksExpiration
            ? ExpirationWarningDays ?? Inventory.InventoryLot.DefaultWarningDays
            : Inventory.InventoryLot.DefaultWarningDays;

    /// <summary>
    /// Updates only the current catalog selling price (Today's Prices). Does not change identity,
    /// UOM, SellingMode, or Platform provenance. Unchanged normalized price is a no-op.
    /// </summary>
    /// <returns><see langword="true"/> when the price changed; otherwise <see langword="false"/>.</returns>
    public bool UpdateSellingPrice(decimal sellingPrice, DateTimeOffset utcNow)
    {
        CatalogGuards.EnsureUtc(utcNow);
        if (Status == CatalogProductStatus.Inactive)
        {
            throw new DomainException(
                DomainErrorCodes.ProductNotActive,
                "Inactive products cannot be edited. Reactivate first.");
        }

        var normalized = NormalizeSellingPrice(sellingPrice);
        if (normalized == SellingPrice)
        {
            return false;
        }

        SellingPrice = normalized;
        UpdatedAtUtc = utcNow;
        return true;
    }

    /// <summary>
    /// Promotes BranchLocal → OrganizationStandard. Same ProductId; preserves OriginBranchId and SellingPrice.
    /// </summary>
    public void PromoteToOrganizationStandard(DateTimeOffset utcNow)
    {
        CatalogGuards.EnsureUtc(utcNow);
        if (Scope == CatalogProductScope.OrganizationStandard)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCatalogProductPromotion,
                "Product is already OrganizationStandard.");
        }

        if (Scope != CatalogProductScope.BranchLocal)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCatalogProductPromotion,
                "Only BranchLocal products can be promoted.");
        }

        CatalogProductScopes.EnsureOriginValid(CatalogProductScope.OrganizationStandard, OriginBranchId);
        Scope = CatalogProductScope.OrganizationStandard;
        UpdatedAtUtc = utcNow;
    }

    /// <summary>Allows connected-buyer eligibility (clears global block). Does not auto-init PO price.</summary>
    public void AllowForConnectedBuyers(DateTimeOffset utcNow)
    {
        CatalogGuards.EnsureUtc(utcNow);
        IsBlockedFromConnectedBuyers = false;
        CanExposeToConnectedBuyers = true;
        UpdatedAtUtc = utcNow;
    }

    /// <summary>Globally blocks the product from connected-buyer eligibility. Preserves staged PO price.</summary>
    public void BlockFromConnectedBuyers(DateTimeOffset utcNow)
    {
        CatalogGuards.EnsureUtc(utcNow);
        IsBlockedFromConnectedBuyers = true;
        CanExposeToConnectedBuyers = false;
        UpdatedAtUtc = utcNow;
    }

    /// <summary>Allow semantics without auto-copying retail into default connected PO price.</summary>
    public void EnableConnectedBuyerAvailability(DateTimeOffset utcNow) =>
        AllowForConnectedBuyers(utcNow);

    public void SetDefaultConnectedPoPrice(decimal price, DateTimeOffset utcNow)
    {
        CatalogGuards.EnsureUtc(utcNow);
        // Default PO may be staged while blocked or allowed; block/allow leave the price intact.
        DefaultConnectedPoPrice = NormalizeConnectedPoPrice(price);
        UpdatedAtUtc = utcNow;
    }

    /// <summary>Block semantics; preserves staged default connected PO price.</summary>
    public void DisableConnectedBuyerAvailability(DateTimeOffset utcNow) =>
        BlockFromConnectedBuyers(utcNow);

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

    private static decimal NormalizeConnectedPoPrice(decimal price)
    {
        var rounded = Sales.SaleMoney.RoundMoney(price);
        if (rounded < 0m || rounded > SellingPriceMax)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductSellingPrice,
                "Connected PO price must be non-negative and within the supported money range.");
        }
        return rounded;
    }
}
