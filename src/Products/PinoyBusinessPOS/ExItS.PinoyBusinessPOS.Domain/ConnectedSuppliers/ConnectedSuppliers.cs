using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;

public enum ConnectedSupplierRelationshipStatus { Pending = 0, Active = 1, Declined = 2, Disconnected = 3 }
public enum SupplierConnectionType { External = 0, ConnectedOrganization = 1 }

/// <summary>
/// How a supplier-buyer connection publishes eligible catalog products.
/// Legacy connections default to <see cref="SelectedOnly"/> so visibility does not broaden on migration.
/// </summary>
public enum CatalogSharingMode
{
    /// <summary>Only products with an explicit shared share row are buyer-visible.</summary>
    SelectedOnly = 0,
    /// <summary>All eligible products are shared unless explicitly excluded (IsShared=false).</summary>
    AllEligible = 1
}

/// <summary>Server-side source for the buyer-facing effective purchase price.</summary>
public enum ConnectedCustomerPriceSource
{
    SellingPrice = 0,
    CustomerDiscount = 1,
    ProductOverride = 2,
    DefaultPoPrice = 3
}
public enum ConnectedPurchaseOrderStatus
{
    New = 0,
    Accepted = 1,
    Declined = 2,
    Preparing = 3,
    Fulfilled = 4,
    Withdrawn = 5,
    ChangesProposed = 6
}

/// <summary>
/// Buyer-selected settlement term for a connected purchase order.
/// GCash is persisted as <see cref="ManualGCash"/> — there is no payment-gateway verification.
/// Utang is a B2B payable term only; this package does not post customer-credit debt.
/// </summary>
public enum ConnectedPoPaymentTerm
{
    Cash = 0,
    ManualGCash = 1,
    Utang = 2
}

public enum ConnectedPoLineAvailability
{
    Pending = 0,
    Available = 1,
    Unavailable = 2
}

public enum ConnectedPoDeclineReason
{
    OutOfStock = 0,
    CannotFulfillQuantity = 1,
    PriceOrOrderIssue = 2,
    UnableToFulfill = 3,
    Other = 4
}

public enum ConnectedPoReceivingDiscrepancyKind
{
    None = 0,
    Short = 1,
    Damaged = 2,
    WrongItem = 3,
    Expired = 4,
    Rejected = 5,
    Other = 6
}

public abstract class ConnectedSupplierGuidId<T> : IEquatable<T> where T : ConnectedSupplierGuidId<T>
{
    public Guid Value { get; }
    protected ConnectedSupplierGuidId(Guid value)
    {
        if (value == Guid.Empty) throw new DomainException(ConnectedSupplierDomainErrorCodes.InvalidId, "Identifier cannot be empty.");
        Value = value;
    }
    public bool Equals(T? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => obj is T other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString("D");
    public static bool operator ==(ConnectedSupplierGuidId<T>? left, ConnectedSupplierGuidId<T>? right) => Equals(left, right);
    public static bool operator !=(ConnectedSupplierGuidId<T>? left, ConnectedSupplierGuidId<T>? right) => !Equals(left, right);
}

public sealed class ConnectedSupplierRelationshipId : ConnectedSupplierGuidId<ConnectedSupplierRelationshipId>
{
    private ConnectedSupplierRelationshipId(Guid value) : base(value) { }
    public static ConnectedSupplierRelationshipId New() => new(Guid.NewGuid());
    public static ConnectedSupplierRelationshipId From(Guid value) => new(value);
}
public sealed class SupplierProductExposureId : ConnectedSupplierGuidId<SupplierProductExposureId>
{
    private SupplierProductExposureId(Guid value) : base(value) { }
    public static SupplierProductExposureId New() => new(Guid.NewGuid());
    public static SupplierProductExposureId From(Guid value) => new(value);
}
public sealed class ConnectedBuyerProductShareId : ConnectedSupplierGuidId<ConnectedBuyerProductShareId>
{
    private ConnectedBuyerProductShareId(Guid value) : base(value) { }
    public static ConnectedBuyerProductShareId New() => new(Guid.NewGuid());
    public static ConnectedBuyerProductShareId From(Guid value) => new(value);
}
public sealed class BuyerSupplierProductLinkId : ConnectedSupplierGuidId<BuyerSupplierProductLinkId>
{
    private BuyerSupplierProductLinkId(Guid value) : base(value) { }
    public static BuyerSupplierProductLinkId New() => new(Guid.NewGuid());
    public static BuyerSupplierProductLinkId From(Guid value) => new(value);
}
public sealed class ConnectedPurchaseOrderId : ConnectedSupplierGuidId<ConnectedPurchaseOrderId>
{
    private ConnectedPurchaseOrderId(Guid value) : base(value) { }
    public static ConnectedPurchaseOrderId New() => new(Guid.NewGuid());
    public static ConnectedPurchaseOrderId From(Guid value) => new(value);
}

public static class ConnectedSupplierDomainErrorCodes
{
    public const string InvalidId = "ConnectedSupplier_InvalidId";
    public const string SelfConnection = "ConnectedSupplier_SelfConnection";
    public const string InvalidTransition = "ConnectedSupplier_InvalidTransition";
    public const string InvalidOffer = "ConnectedSupplier_InvalidOffer";
    public const string InvalidOrder = "ConnectedSupplier_InvalidOrder";
}

public sealed class ConnectedSupplierRelationship
{
    public ConnectedSupplierRelationshipId Id { get; }
    public PosOrganizationId BuyerOrganizationId { get; }
    public PosOrganizationId SupplierOrganizationId { get; }
    public ConnectedSupplierRelationshipStatus Status { get; private set; }
    public DateTimeOffset RequestedAtUtc { get; }
    public Guid? RequestedByUserId { get; }
    public DateTimeOffset? RespondedAtUtc { get; private set; }
    public Guid? RespondedByUserId { get; private set; }
    public DateTimeOffset? DisconnectedAtUtc { get; private set; }
    /// <summary>Public business display name of the buyer at request time (safe for supplier inbox).</summary>
    public string? BuyerDisplayNameSnapshot { get; }
    /// <summary>Public organization id (ORG######) of the buyer at request time.</summary>
    public string? BuyerPublicOrganizationIdSnapshot { get; }
    /// <summary>Public business display name of the supplier at request time (safe for buyer list).</summary>
    public string? SupplierDisplayNameSnapshot { get; }
    /// <summary>Public organization id (ORG######) of the supplier at request time.</summary>
    public string? SupplierPublicOrganizationIdSnapshot { get; }
    /// <summary>Legacy connections remain <see cref="CatalogSharingMode.SelectedOnly"/>.</summary>
    public CatalogSharingMode CatalogSharingMode { get; private set; }
    /// <summary>Optional buyer-level discount percent applied to selling/default PO baseline (0–100).</summary>
    public decimal? CustomerDiscountPercent { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private ConnectedSupplierRelationship(ConnectedSupplierRelationshipId id, PosOrganizationId buyerOrganizationId,
        PosOrganizationId supplierOrganizationId, ConnectedSupplierRelationshipStatus status, DateTimeOffset requestedAtUtc,
        Guid? requestedByUserId, DateTimeOffset? respondedAtUtc, Guid? respondedByUserId,
        DateTimeOffset? disconnectedAtUtc, DateTimeOffset createdAtUtc, DateTimeOffset updatedAtUtc,
        string? buyerDisplayNameSnapshot = null, string? buyerPublicOrganizationIdSnapshot = null,
        string? supplierDisplayNameSnapshot = null, string? supplierPublicOrganizationIdSnapshot = null,
        CatalogSharingMode catalogSharingMode = CatalogSharingMode.SelectedOnly,
        decimal? customerDiscountPercent = null)
    {
        Id = id; BuyerOrganizationId = buyerOrganizationId; SupplierOrganizationId = supplierOrganizationId;
        Status = status; RequestedAtUtc = requestedAtUtc; RequestedByUserId = requestedByUserId;
        RespondedAtUtc = respondedAtUtc; RespondedByUserId = respondedByUserId; DisconnectedAtUtc = disconnectedAtUtc;
        BuyerDisplayNameSnapshot = CleanSnapshot(buyerDisplayNameSnapshot, 128);
        BuyerPublicOrganizationIdSnapshot = CleanSnapshot(buyerPublicOrganizationIdSnapshot, 32);
        SupplierDisplayNameSnapshot = CleanSnapshot(supplierDisplayNameSnapshot, 128);
        SupplierPublicOrganizationIdSnapshot = CleanSnapshot(supplierPublicOrganizationIdSnapshot, 32);
        CatalogSharingMode = catalogSharingMode;
        CustomerDiscountPercent = NormalizeDiscount(customerDiscountPercent);
        CreatedAtUtc = createdAtUtc; UpdatedAtUtc = updatedAtUtc;
    }

    public static ConnectedSupplierRelationship Request(PosOrganizationId buyer, PosOrganizationId supplier,
        DateTimeOffset utcNow, Guid? requestedByUserId = null, ConnectedSupplierRelationshipId? id = null,
        string? buyerDisplayName = null, string? buyerPublicOrganizationId = null,
        string? supplierDisplayName = null, string? supplierPublicOrganizationId = null)
    {
        EnsureUtc(utcNow);
        if (buyer == supplier)
        {
            throw new DomainException(
                ConnectedSupplierDomainErrorCodes.SelfConnection,
                "You can't connect your business to itself.");
        }

        return new(id ?? ConnectedSupplierRelationshipId.New(), buyer, supplier,
            ConnectedSupplierRelationshipStatus.Pending, utcNow, requestedByUserId, null, null, null, utcNow, utcNow,
            buyerDisplayName, buyerPublicOrganizationId, supplierDisplayName, supplierPublicOrganizationId);
    }

    public void Approve(DateTimeOffset utcNow, Guid? actorId = null) => Respond(ConnectedSupplierRelationshipStatus.Active, utcNow, actorId);
    public void Decline(DateTimeOffset utcNow, Guid? actorId = null) => Respond(ConnectedSupplierRelationshipStatus.Declined, utcNow, actorId);
    private void Respond(ConnectedSupplierRelationshipStatus status, DateTimeOffset utcNow, Guid? actorId)
    {
        EnsureUtc(utcNow);
        if (Status != ConnectedSupplierRelationshipStatus.Pending) InvalidTransition();
        Status = status; RespondedAtUtc = utcNow; RespondedByUserId = actorId; UpdatedAtUtc = utcNow;
    }
    public void Disconnect(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (Status != ConnectedSupplierRelationshipStatus.Active) InvalidTransition();
        Status = ConnectedSupplierRelationshipStatus.Disconnected; DisconnectedAtUtc = utcNow; UpdatedAtUtc = utcNow;
    }

    /// <summary>
    /// Sets catalog sharing mode and optional customer discount. Active relationships only
    /// (or call during Approve before save). Does not create per-product share rows.
    /// </summary>
    public void ConfigureCatalogSharing(
        CatalogSharingMode mode,
        decimal? customerDiscountPercent,
        DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (Status is not (ConnectedSupplierRelationshipStatus.Pending or ConnectedSupplierRelationshipStatus.Active))
        {
            InvalidTransition();
        }

        CatalogSharingMode = mode;
        CustomerDiscountPercent = NormalizeDiscount(customerDiscountPercent);
        UpdatedAtUtc = utcNow;
    }

    public static ConnectedSupplierRelationship Rehydrate(ConnectedSupplierRelationshipId id, PosOrganizationId buyer,
        PosOrganizationId supplier, ConnectedSupplierRelationshipStatus status, DateTimeOffset requestedAtUtc,
        Guid? requestedBy, DateTimeOffset? respondedAtUtc, Guid? respondedBy, DateTimeOffset? disconnectedAtUtc,
        DateTimeOffset createdAtUtc, DateTimeOffset updatedAtUtc,
        string? buyerDisplayNameSnapshot = null, string? buyerPublicOrganizationIdSnapshot = null,
        string? supplierDisplayNameSnapshot = null, string? supplierPublicOrganizationIdSnapshot = null,
        CatalogSharingMode catalogSharingMode = CatalogSharingMode.SelectedOnly,
        decimal? customerDiscountPercent = null) =>
        new(id, buyer, supplier, status, requestedAtUtc, requestedBy, respondedAtUtc, respondedBy, disconnectedAtUtc,
            createdAtUtc, updatedAtUtc, buyerDisplayNameSnapshot, buyerPublicOrganizationIdSnapshot,
            supplierDisplayNameSnapshot, supplierPublicOrganizationIdSnapshot,
            catalogSharingMode, customerDiscountPercent);

    private static string? CleanSnapshot(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static decimal? NormalizeDiscount(decimal? percent)
    {
        if (percent is null)
        {
            return null;
        }

        var value = decimal.Round(percent.Value, 2, MidpointRounding.AwayFromZero);
        if (value < 0m || value > 100m)
        {
            throw new DomainException(
                ConnectedSupplierDomainErrorCodes.InvalidOffer,
                "Customer discount percent must be between 0 and 100.");
        }

        return value == 0m ? null : value;
    }

    private static void InvalidTransition() => throw new DomainException(ConnectedSupplierDomainErrorCodes.InvalidTransition, "Connected supplier relationship transition is not allowed.");
    internal static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero) throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Timestamp must be UTC.");
    }
}

public sealed class SupplierProductExposure
{
    public SupplierProductExposureId Id { get; }
    public PosOrganizationId SupplierOrganizationId { get; }
    public CatalogProductId ProductId { get; }
    public string? SkuSnapshot { get; private set; }
    public string NameSnapshot { get; private set; }
    public string? CategoryNameSnapshot { get; private set; }
    public string UnitOfMeasureCode { get; private set; }
    public decimal SupplierOrderPrice { get; private set; }
    public bool IsOrderable { get; private set; }
    public bool IsExposed { get; private set; }
    public long SyncVersion { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private SupplierProductExposure(SupplierProductExposureId id, PosOrganizationId supplierOrganizationId, CatalogProductId productId,
        string? sku, string name, string? category, string uom, decimal price, bool orderable, bool exposed,
        long syncVersion, DateTimeOffset createdAtUtc, DateTimeOffset updatedAtUtc)
    {
        Id=id; SupplierOrganizationId=supplierOrganizationId; ProductId=productId; SkuSnapshot=sku; NameSnapshot=name;
        CategoryNameSnapshot=category; UnitOfMeasureCode=uom; SupplierOrderPrice=price; IsOrderable=orderable;
        IsExposed=exposed; SyncVersion=syncVersion; CreatedAtUtc=createdAtUtc; UpdatedAtUtc=updatedAtUtc;
    }
    public static SupplierProductExposure Expose(PosOrganizationId supplier, CatalogProductId productId, string name,
        string uom, decimal supplierOrderPrice, DateTimeOffset utcNow, string? sku = null, string? category = null,
        SupplierProductExposureId? id = null)
    {
        ConnectedSupplierRelationship.EnsureUtc(utcNow);
        return new(id ?? SupplierProductExposureId.New(), supplier, productId, Clean(sku), Required(name, 200),
            Clean(category), Required(uom, 32), Money(supplierOrderPrice), true, true, 1, utcNow, utcNow);
    }
    public void UpdateOffer(string name, string uom, decimal supplierOrderPrice, bool isOrderable,
        DateTimeOffset utcNow, string? sku = null, string? category = null)
    {
        ConnectedSupplierRelationship.EnsureUtc(utcNow);
        NameSnapshot=Required(name,200); UnitOfMeasureCode=Required(uom,32); SupplierOrderPrice=Money(supplierOrderPrice);
        SkuSnapshot=Clean(sku); CategoryNameSnapshot=Clean(category); IsOrderable=isOrderable; IsExposed=true; Touch(utcNow);
    }
    public void Deactivate(DateTimeOffset utcNow) { IsExposed=false; IsOrderable=false; Touch(utcNow); }
    public void MarkNotOrderable(DateTimeOffset utcNow) { IsOrderable=false; Touch(utcNow); }
    public static SupplierProductExposure Rehydrate(SupplierProductExposureId id, PosOrganizationId supplier, CatalogProductId productId,
        string? sku, string name, string? category, string uom, decimal price, bool orderable, bool exposed,
        long syncVersion, DateTimeOffset createdAtUtc, DateTimeOffset updatedAtUtc) =>
        new(id,supplier,productId,sku,name,category,uom,price,orderable,exposed,syncVersion,createdAtUtc,updatedAtUtc);
    private void Touch(DateTimeOffset utcNow) { ConnectedSupplierRelationship.EnsureUtc(utcNow); SyncVersion++; UpdatedAtUtc=utcNow; }
    internal static decimal Money(decimal value)
    {
        var rounded=SaleMoney.RoundMoney(value);
        if (rounded < 0) throw new DomainException(ConnectedSupplierDomainErrorCodes.InvalidOffer, "Supplier order price cannot be negative.");
        return rounded;
    }
    internal static string Required(string value,int max) =>
        string.IsNullOrWhiteSpace(value) || value.Trim().Length>max
            ? throw new DomainException(ConnectedSupplierDomainErrorCodes.InvalidOffer, "Required offer text is invalid.")
            : value.Trim();
    internal static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class ConnectedBuyerProductShare
{
    public ConnectedBuyerProductShareId Id { get; }
    public ConnectedSupplierRelationshipId RelationshipId { get; }
    public PosOrganizationId BuyerOrganizationId { get; }
    public PosOrganizationId SupplierOrganizationId { get; }
    public CatalogProductId SupplierProductId { get; }
    public bool IsShared { get; private set; }
    public decimal? BuyerSpecificPoPrice { get; private set; }
    public long SyncVersion { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private ConnectedBuyerProductShare(
        ConnectedBuyerProductShareId id,
        ConnectedSupplierRelationshipId relationshipId,
        PosOrganizationId buyer,
        PosOrganizationId supplier,
        CatalogProductId supplierProductId,
        bool isShared,
        decimal? buyerSpecificPoPrice,
        long syncVersion,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        RelationshipId = relationshipId;
        BuyerOrganizationId = buyer;
        SupplierOrganizationId = supplier;
        SupplierProductId = supplierProductId;
        IsShared = isShared;
        BuyerSpecificPoPrice = buyerSpecificPoPrice is null ? null : SupplierProductExposure.Money(buyerSpecificPoPrice.Value);
        SyncVersion = syncVersion;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static ConnectedBuyerProductShare Share(
        ConnectedSupplierRelationshipId relationshipId,
        PosOrganizationId buyer,
        PosOrganizationId supplier,
        CatalogProductId supplierProductId,
        DateTimeOffset utcNow,
        decimal? buyerSpecificPoPrice = null,
        ConnectedBuyerProductShareId? id = null)
    {
        ConnectedSupplierRelationship.EnsureUtc(utcNow);
        return new(id ?? ConnectedBuyerProductShareId.New(), relationshipId, buyer, supplier, supplierProductId,
            true, buyerSpecificPoPrice, 1, utcNow, utcNow);
    }

    public void SetShared(bool isShared, DateTimeOffset utcNow)
    {
        IsShared = isShared;
        Touch(utcNow);
    }

    public void SetBuyerSpecificPoPrice(decimal? price, DateTimeOffset utcNow)
    {
        BuyerSpecificPoPrice = price is null ? null : SupplierProductExposure.Money(price.Value);
        Touch(utcNow);
    }

    public void Unshare(DateTimeOffset utcNow, bool clearPrice = false)
    {
        IsShared = false;
        if (clearPrice) BuyerSpecificPoPrice = null;
        Touch(utcNow);
    }

    public static ConnectedBuyerProductShare Rehydrate(
        ConnectedBuyerProductShareId id,
        ConnectedSupplierRelationshipId relationshipId,
        PosOrganizationId buyer,
        PosOrganizationId supplier,
        CatalogProductId supplierProductId,
        bool isShared,
        decimal? buyerSpecificPoPrice,
        long syncVersion,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc) =>
        new(id, relationshipId, buyer, supplier, supplierProductId, isShared, buyerSpecificPoPrice,
            syncVersion, createdAtUtc, updatedAtUtc);

    private void Touch(DateTimeOffset utcNow)
    {
        ConnectedSupplierRelationship.EnsureUtc(utcNow);
        SyncVersion++;
        UpdatedAtUtc = utcNow;
    }
}

public static class ConnectedPoPricing
{
    /// <summary>
    /// Legacy SELECTED_ONLY path (no connection discount / selling-price baseline).
    /// Prefer the overload that accepts sharing mode + discount for new call sites.
    /// </summary>
    public static bool TryResolveEffectivePrice(
        SupplierProductExposure exposure,
        ConnectedBuyerProductShare? share,
        out decimal price) =>
        TryResolveEffectivePrice(
            exposure,
            share,
            CatalogSharingMode.SelectedOnly,
            customerDiscountPercent: null,
            sellingPrice: null,
            out price,
            out _);

    public static bool IsProductShared(CatalogSharingMode mode, ConnectedBuyerProductShare? share) =>
        mode == CatalogSharingMode.AllEligible
            ? share is null || share.IsShared
            : share is not null && share.IsShared;

    /// <summary>
    /// Effective buyer purchase price for the exposure's orderable unit.
    /// Precedence: product override → customer discount on baseline → baseline.
    /// Baseline prefers SellingPrice when &gt; 0, else exposure SupplierOrderPrice (Default PO).
    /// </summary>
    public static bool TryResolveEffectivePrice(
        SupplierProductExposure exposure,
        ConnectedBuyerProductShare? share,
        CatalogSharingMode mode,
        decimal? customerDiscountPercent,
        decimal? sellingPrice,
        out decimal price,
        out ConnectedCustomerPriceSource source)
    {
        price = 0m;
        source = ConnectedCustomerPriceSource.DefaultPoPrice;
        if (!exposure.IsExposed || !exposure.IsOrderable || !IsProductShared(mode, share))
        {
            return false;
        }

        if (share?.BuyerSpecificPoPrice is decimal overridePrice)
        {
            price = RoundMoney(overridePrice);
            source = ConnectedCustomerPriceSource.ProductOverride;
            return true;
        }

        var baseline = sellingPrice is > 0m ? sellingPrice.Value : exposure.SupplierOrderPrice;
        var baselineSource = sellingPrice is > 0m
            ? ConnectedCustomerPriceSource.SellingPrice
            : ConnectedCustomerPriceSource.DefaultPoPrice;

        if (customerDiscountPercent is decimal discount && discount > 0m)
        {
            price = RoundMoney(baseline * (1m - (discount / 100m)));
            source = ConnectedCustomerPriceSource.CustomerDiscount;
            return true;
        }

        price = RoundMoney(baseline);
        source = baselineSource;
        return true;
    }

    public static decimal RoundMoney(decimal value) =>
        decimal.Round(value, 2, MidpointRounding.AwayFromZero);
}

public sealed class BuyerSupplierProductLink
{
    public const int PackageLabelMaxLength = 64;

    public BuyerSupplierProductLinkId Id { get; }
    public ConnectedSupplierRelationshipId RelationshipId { get; }
    public PosOrganizationId BuyerOrganizationId { get; }
    public PosOrganizationId SupplierOrganizationId { get; }
    public CatalogProductId BuyerProductId { get; }
    public CatalogProductId SupplierProductId { get; }
    public string? SupplierSkuSnapshot { get; private set; }
    public string SupplierNameSnapshot { get; private set; }
    public string UnitOfMeasureCode { get; private set; }
    public decimal LastKnownOrderPrice { get; private set; }
    public Guid? BuyerPurchaseUnitId { get; private set; }
    public decimal MultiplierToBase { get; private set; }
    public string? PackageLabel { get; private set; }
    public bool IsActive { get; private set; }
    public long SyncVersion { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private BuyerSupplierProductLink(BuyerSupplierProductLinkId id, ConnectedSupplierRelationshipId relationshipId,
        PosOrganizationId buyer, PosOrganizationId supplier, CatalogProductId buyerProductId, CatalogProductId supplierProductId,
        string? sku, string name, string uom, decimal price, bool active, long version,
        DateTimeOffset createdAtUtc, DateTimeOffset updatedAtUtc,
        Guid? buyerPurchaseUnitId = null, decimal multiplierToBase = 1m, string? packageLabel = null)
    {
        Id = id;
        RelationshipId = relationshipId;
        BuyerOrganizationId = buyer;
        SupplierOrganizationId = supplier;
        BuyerProductId = buyerProductId;
        SupplierProductId = supplierProductId;
        SupplierSkuSnapshot = sku;
        SupplierNameSnapshot = name;
        UnitOfMeasureCode = uom;
        LastKnownOrderPrice = price;
        IsActive = active;
        SyncVersion = version;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        BuyerPurchaseUnitId = buyerPurchaseUnitId == Guid.Empty ? null : buyerPurchaseUnitId;
        MultiplierToBase = CatalogProductUnit.NormalizeMultiplier(multiplierToBase);
        PackageLabel = NormalizePackageLabel(packageLabel);
    }

    public static BuyerSupplierProductLink Create(
        ConnectedSupplierRelationshipId relationshipId,
        PosOrganizationId buyer,
        PosOrganizationId supplier,
        CatalogProductId buyerProductId,
        SupplierProductExposure exposure,
        DateTimeOffset utcNow,
        BuyerSupplierProductLinkId? id = null,
        Guid? buyerPurchaseUnitId = null,
        decimal multiplierToBase = 1m,
        string? packageLabel = null,
        decimal? effectiveOrderPrice = null)
    {
        if (exposure.SupplierOrganizationId != supplier)
        {
            throw new DomainException(
                ConnectedSupplierDomainErrorCodes.InvalidOffer,
                "Exposure belongs to another supplier.");
        }

        return new(
            id ?? BuyerSupplierProductLinkId.New(),
            relationshipId,
            buyer,
            supplier,
            buyerProductId,
            exposure.ProductId,
            exposure.SkuSnapshot,
            exposure.NameSnapshot,
            exposure.UnitOfMeasureCode,
            SupplierProductExposure.Money(effectiveOrderPrice ?? exposure.SupplierOrderPrice),
            true,
            1,
            utcNow,
            utcNow,
            buyerPurchaseUnitId,
            multiplierToBase,
            packageLabel);
    }

    public void Refresh(
        SupplierProductExposure exposure,
        DateTimeOffset utcNow,
        Guid? buyerPurchaseUnitId = null,
        decimal? multiplierToBase = null,
        string? packageLabel = null)
    {
        SupplierSkuSnapshot = exposure.SkuSnapshot;
        SupplierNameSnapshot = exposure.NameSnapshot;
        UnitOfMeasureCode = exposure.UnitOfMeasureCode;
        LastKnownOrderPrice = exposure.SupplierOrderPrice;
        IsActive = exposure.IsExposed && exposure.IsOrderable;
        if (buyerPurchaseUnitId is not null)
        {
            BuyerPurchaseUnitId = buyerPurchaseUnitId == Guid.Empty ? null : buyerPurchaseUnitId;
        }

        if (multiplierToBase is not null)
        {
            MultiplierToBase = CatalogProductUnit.NormalizeMultiplier(multiplierToBase.Value);
        }

        if (packageLabel is not null)
        {
            PackageLabel = NormalizePackageLabel(packageLabel);
        }

        SyncVersion++;
        UpdatedAtUtc = utcNow;
    }

    public void Unlink(DateTimeOffset utcNow)
    {
        IsActive = false;
        SyncVersion++;
        UpdatedAtUtc = utcNow;
    }

    public static BuyerSupplierProductLink Rehydrate(
        BuyerSupplierProductLinkId id,
        ConnectedSupplierRelationshipId relationshipId,
        PosOrganizationId buyer,
        PosOrganizationId supplier,
        CatalogProductId buyerProductId,
        CatalogProductId supplierProductId,
        string? sku,
        string name,
        string uom,
        decimal price,
        bool active,
        long version,
        DateTimeOffset created,
        DateTimeOffset updated,
        Guid? buyerPurchaseUnitId = null,
        decimal multiplierToBase = 1m,
        string? packageLabel = null) =>
        new(
            id,
            relationshipId,
            buyer,
            supplier,
            buyerProductId,
            supplierProductId,
            sku,
            name,
            uom,
            price,
            active,
            version,
            created,
            updated,
            buyerPurchaseUnitId,
            multiplierToBase,
            packageLabel);

    private static string? NormalizePackageLabel(string? packageLabel)
    {
        if (string.IsNullOrWhiteSpace(packageLabel))
        {
            return null;
        }

        var trimmed = packageLabel.Trim();
        return trimmed.Length > PackageLabelMaxLength
            ? trimmed[..PackageLabelMaxLength]
            : trimmed;
    }
}

public sealed record ConnectedPoLineProposal(CatalogProductId ProductId, decimal ProposedQty, bool Unavailable);

public static class ConnectedPoPaymentTerms
{
    public static ConnectedPoPaymentTerm Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return ConnectedPoPaymentTerm.Cash;
        }

        var trimmed = value.Trim();
        if (trimmed.Equals("Cash", StringComparison.OrdinalIgnoreCase))
        {
            return ConnectedPoPaymentTerm.Cash;
        }

        if (trimmed.Equals("GCash", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("ManualGCash", StringComparison.OrdinalIgnoreCase))
        {
            return ConnectedPoPaymentTerm.ManualGCash;
        }

        if (trimmed.Equals("Utang", StringComparison.OrdinalIgnoreCase))
        {
            return ConnectedPoPaymentTerm.Utang;
        }

        throw new DomainException(
            ConnectedSupplierDomainErrorCodes.InvalidOrder,
            "Payment term must be Cash, GCash, or Utang.");
    }

    public static string ToApi(ConnectedPoPaymentTerm term) => term.ToString();

    /// <summary>Merchant-facing label. GCash is never implied to be gateway-verified.</summary>
    public static string ToUiLabel(ConnectedPoPaymentTerm term) =>
        term == ConnectedPoPaymentTerm.ManualGCash ? "GCash" : term.ToString();
}

public sealed record ConnectedPurchaseOrderLine(
    CatalogProductId ProductId,
    string NameSnapshot,
    string? SkuSnapshot,
    decimal Qty,
    decimal UnitPriceSnapshot,
    decimal LineTotal,
    string UnitOfMeasureCode,
    decimal? ProposedQty = null,
    decimal? ConfirmedQty = null,
    ConnectedPoLineAvailability Availability = ConnectedPoLineAvailability.Pending)
{
    public decimal RequestedQty => Qty;
    public decimal EffectiveProposedQty => ProposedQty ?? Qty;
    public decimal ProposedLineTotal => SaleMoney.RoundMoney(EffectiveProposedQty * UnitPriceSnapshot);
    public decimal ConfirmedLineTotal => SaleMoney.RoundMoney((ConfirmedQty ?? 0m) * UnitPriceSnapshot);
    public decimal FulfillmentQty => ConfirmedQty ?? 0m;
    public bool HasSupplierChange =>
        Availability == ConnectedPoLineAvailability.Unavailable
        || (ProposedQty is decimal proposed && proposed != Qty);

    public static ConnectedPurchaseOrderLine Create(CatalogProductId productId,string name,string? sku,decimal qty,decimal unitPrice,string uom)
    {
        if (qty<=0) throw new DomainException(ConnectedSupplierDomainErrorCodes.InvalidOrder,"Order quantity must be positive.");
        var price=SupplierProductExposure.Money(unitPrice);
        return new(productId,SupplierProductExposure.Required(name,200),SupplierProductExposure.Clean(sku),qty,price,
            SaleMoney.RoundMoney(qty*price),SupplierProductExposure.Required(uom,32));
    }

    public ConnectedPurchaseOrderLine ConfirmRequested() =>
        this with
        {
            ProposedQty = Qty,
            ConfirmedQty = Qty,
            Availability = ConnectedPoLineAvailability.Available
        };

    public ConnectedPurchaseOrderLine ApplyProposal(decimal proposedQty, bool unavailable)
    {
        if (unavailable)
        {
            if (proposedQty < 0m)
            {
                throw new DomainException(
                    ConnectedSupplierDomainErrorCodes.InvalidOrder,
                    "Unavailable lines cannot have a negative quantity.");
            }

            return this with
            {
                ProposedQty = 0m,
                ConfirmedQty = null,
                Availability = ConnectedPoLineAvailability.Unavailable
            };
        }

        if (proposedQty < 0m)
        {
            throw new DomainException(
                ConnectedSupplierDomainErrorCodes.InvalidOrder,
                "Proposed quantity cannot be negative.");
        }

        if (proposedQty > Qty)
        {
            throw new DomainException(
                ConnectedSupplierDomainErrorCodes.InvalidOrder,
                "Supplier cannot increase quantity above the buyer's original request.");
        }

        if (proposedQty == 0m)
        {
            throw new DomainException(
                ConnectedSupplierDomainErrorCodes.InvalidOrder,
                "Mark the line unavailable instead of proposing zero.");
        }

        return this with
        {
            ProposedQty = proposedQty,
            ConfirmedQty = null,
            Availability = ConnectedPoLineAvailability.Available
        };
    }

    public ConnectedPurchaseOrderLine ConfirmProposal()
    {
        if (Availability == ConnectedPoLineAvailability.Unavailable)
        {
            return this with { ConfirmedQty = 0m, ProposedQty = ProposedQty ?? 0m };
        }

        var qty = ProposedQty ?? Qty;
        return this with
        {
            ConfirmedQty = qty,
            ProposedQty = qty,
            Availability = ConnectedPoLineAvailability.Available
        };
    }
}

public sealed class ConnectedPurchaseOrder
{
    public const int DeclineNoteMaxLength = 280;

    private readonly List<ConnectedPurchaseOrderLine> _lines;
    public ConnectedPurchaseOrderId Id { get; }
    public ConnectedSupplierRelationshipId RelationshipId { get; }
    public PosOrganizationId BuyerOrganizationId { get; }
    public PosOrganizationId SupplierOrganizationId { get; }
    public PurchaseOrderId BuyerPurchaseOrderId { get; }
    public string? BuyerPoNumber { get; }
    public DateOnly OrderDate { get; }
    public string? Notes { get; }
    public ConnectedPurchaseOrderStatus Status { get; private set; }
    /// <summary>Original requested total. Never overwritten by supplier proposals.</summary>
    public decimal TotalAmount { get; }
    public ConnectedPoPaymentTerm PaymentTerm { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? AcceptedAtUtc { get; private set; }
    public DateTimeOffset? DeclinedAtUtc { get; private set; }
    public DateTimeOffset? PreparingAtUtc { get; private set; }
    public DateTimeOffset? FulfilledAtUtc { get; private set; }
    public DateTimeOffset? WithdrawnAtUtc { get; private set; }
    public DateTimeOffset? ChangesProposedAtUtc { get; private set; }
    public Guid? ChangesProposedByUserId { get; private set; }
    public DateTimeOffset? BuyerRespondedAtUtc { get; private set; }
    public Guid? BuyerRespondedByUserId { get; private set; }
    public ConnectedPoDeclineReason? DeclineReason { get; private set; }
    public string? DeclineNote { get; private set; }
    public IReadOnlyList<ConnectedPurchaseOrderLine> Lines => _lines;
    public decimal ProposedTotalAmount => SaleMoney.RoundMoney(_lines.Sum(x => x.ProposedLineTotal));
    public decimal ConfirmedTotalAmount => SaleMoney.RoundMoney(_lines.Sum(x => x.ConfirmedLineTotal));
    public bool HasProposedLineChanges => _lines.Any(x => x.HasSupplierChange);

    private ConnectedPurchaseOrder(
        ConnectedPurchaseOrderId id,
        ConnectedSupplierRelationshipId relationshipId,
        PosOrganizationId buyer,
        PosOrganizationId supplier,
        PurchaseOrderId buyerPoId,
        string? poNumber,
        DateOnly orderDate,
        string? notes,
        ConnectedPurchaseOrderStatus status,
        decimal total,
        DateTimeOffset created,
        DateTimeOffset updated,
        DateTimeOffset? accepted,
        DateTimeOffset? declined,
        DateTimeOffset? preparing,
        DateTimeOffset? fulfilled,
        DateTimeOffset? withdrawn,
        ConnectedPoDeclineReason? declineReason,
        string? declineNote,
        List<ConnectedPurchaseOrderLine> lines,
        ConnectedPoPaymentTerm paymentTerm = ConnectedPoPaymentTerm.Cash,
        DateTimeOffset? changesProposedAtUtc = null,
        Guid? changesProposedByUserId = null,
        DateTimeOffset? buyerRespondedAtUtc = null,
        Guid? buyerRespondedByUserId = null)
    {
        Id = id;
        RelationshipId = relationshipId;
        BuyerOrganizationId = buyer;
        SupplierOrganizationId = supplier;
        BuyerPurchaseOrderId = buyerPoId;
        BuyerPoNumber = poNumber;
        OrderDate = orderDate;
        Notes = notes;
        Status = status;
        TotalAmount = total;
        PaymentTerm = paymentTerm;
        CreatedAtUtc = created;
        UpdatedAtUtc = updated;
        AcceptedAtUtc = accepted;
        DeclinedAtUtc = declined;
        PreparingAtUtc = preparing;
        FulfilledAtUtc = fulfilled;
        WithdrawnAtUtc = withdrawn;
        DeclineReason = declineReason;
        DeclineNote = declineNote;
        ChangesProposedAtUtc = changesProposedAtUtc;
        ChangesProposedByUserId = changesProposedByUserId;
        BuyerRespondedAtUtc = buyerRespondedAtUtc;
        BuyerRespondedByUserId = buyerRespondedByUserId;
        _lines = lines;
    }

    public static ConnectedPurchaseOrder CreateFromBuyerSubmission(
        ConnectedSupplierRelationship relationship,
        PurchaseOrderId buyerPoId,
        string? poNumber,
        DateOnly orderDate,
        string? notes,
        IReadOnlyList<ConnectedPurchaseOrderLine> lines,
        DateTimeOffset utcNow,
        ConnectedPurchaseOrderId? id = null,
        ConnectedPoPaymentTerm paymentTerm = ConnectedPoPaymentTerm.Cash)
    {
        if (relationship.Status != ConnectedSupplierRelationshipStatus.Active || lines.Count == 0)
        {
            throw new DomainException(
                ConnectedSupplierDomainErrorCodes.InvalidOrder,
                "An active relationship and order lines are required.");
        }

        var total = SaleMoney.RoundMoney(lines.Sum(x => x.LineTotal));
        return new(
            id ?? ConnectedPurchaseOrderId.New(),
            relationship.Id,
            relationship.BuyerOrganizationId,
            relationship.SupplierOrganizationId,
            buyerPoId,
            poNumber,
            orderDate,
            SupplierProductExposure.Clean(notes),
            ConnectedPurchaseOrderStatus.New,
            total,
            utcNow,
            utcNow,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            lines.ToList(),
            paymentTerm);
    }

    public void Accept(DateTimeOffset utcNow)
    {
        ConnectedSupplierRelationship.EnsureUtc(utcNow);
        EnsureNew();
        ReplaceLines(_lines.Select(x => x.ConfirmRequested()).ToList());
        Status = ConnectedPurchaseOrderStatus.Accepted;
        AcceptedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public void ProposeLineChanges(
        IReadOnlyList<ConnectedPoLineProposal> proposals,
        DateTimeOffset utcNow,
        Guid? actorId = null)
    {
        ConnectedSupplierRelationship.EnsureUtc(utcNow);
        EnsureNew();
        if (proposals is null || proposals.Count == 0)
        {
            throw new DomainException(
                ConnectedSupplierDomainErrorCodes.InvalidOrder,
                "A proposal is required for every order line.");
        }

        var byProduct = new Dictionary<Guid, ConnectedPoLineProposal>();
        foreach (var proposal in proposals)
        {
            if (!byProduct.TryAdd(proposal.ProductId.Value, proposal))
            {
                throw new DomainException(
                    ConnectedSupplierDomainErrorCodes.InvalidOrder,
                    "Duplicate product in supplier proposal.");
            }
        }

        if (byProduct.Count != _lines.Count || _lines.Any(line => !byProduct.ContainsKey(line.ProductId.Value)))
        {
            throw new DomainException(
                ConnectedSupplierDomainErrorCodes.InvalidOrder,
                "Supplier proposal must include every original order line.");
        }

        var updated = new List<ConnectedPurchaseOrderLine>(_lines.Count);
        foreach (var line in _lines)
        {
            var proposal = byProduct[line.ProductId.Value];
            updated.Add(line.ApplyProposal(proposal.ProposedQty, proposal.Unavailable));
        }

        if (!updated.Any(x => x.HasSupplierChange))
        {
            throw new DomainException(
                ConnectedSupplierDomainErrorCodes.InvalidOrder,
                "No quantity changes to propose. Confirm the order instead.");
        }

        if (!updated.Any(x =>
                x.Availability != ConnectedPoLineAvailability.Unavailable
                && x.EffectiveProposedQty > 0m))
        {
            throw new DomainException(
                ConnectedSupplierDomainErrorCodes.InvalidOrder,
                "At least one line must remain fulfillable. Decline the order if nothing can be supplied.");
        }

        ReplaceLines(updated);
        Status = ConnectedPurchaseOrderStatus.ChangesProposed;
        ChangesProposedAtUtc = utcNow;
        ChangesProposedByUserId = actorId;
        UpdatedAtUtc = utcNow;
    }

    public void AcceptProposedChanges(DateTimeOffset utcNow, Guid? actorId = null)
    {
        ConnectedSupplierRelationship.EnsureUtc(utcNow);
        if (Status != ConnectedPurchaseOrderStatus.ChangesProposed)
        {
            throw new DomainException(
                ConnectedSupplierDomainErrorCodes.InvalidTransition,
                "Only a proposed revision can be accepted by the buyer.");
        }

        var confirmed = _lines.Select(x => x.ConfirmProposal()).ToList();
        if (!confirmed.Any(x => x.FulfillmentQty > 0m))
        {
            throw new DomainException(
                ConnectedSupplierDomainErrorCodes.InvalidOrder,
                "A revised order must contain at least one fulfillable line.");
        }

        ReplaceLines(confirmed);
        Status = ConnectedPurchaseOrderStatus.Accepted;
        AcceptedAtUtc = utcNow;
        BuyerRespondedAtUtc = utcNow;
        BuyerRespondedByUserId = actorId;
        UpdatedAtUtc = utcNow;
    }

    public void RejectProposedChanges(DateTimeOffset utcNow, Guid? actorId = null)
    {
        ConnectedSupplierRelationship.EnsureUtc(utcNow);
        if (Status != ConnectedPurchaseOrderStatus.ChangesProposed)
        {
            throw new DomainException(
                ConnectedSupplierDomainErrorCodes.InvalidTransition,
                "Only a proposed revision can be rejected by the buyer.");
        }

        Status = ConnectedPurchaseOrderStatus.Withdrawn;
        WithdrawnAtUtc = utcNow;
        BuyerRespondedAtUtc = utcNow;
        BuyerRespondedByUserId = actorId;
        UpdatedAtUtc = utcNow;
    }

    public void Decline(DateTimeOffset utcNow, ConnectedPoDeclineReason? reason = null, string? note = null)
    {
        ConnectedSupplierRelationship.EnsureUtc(utcNow);
        EnsureNew();
        Status = ConnectedPurchaseOrderStatus.Declined;
        DeclinedAtUtc = utcNow;
        DeclineReason = reason;
        DeclineNote = NormalizeDeclineNote(note);
        UpdatedAtUtc = utcNow;
    }

    public void StartPreparing(DateTimeOffset utcNow)
    {
        ConnectedSupplierRelationship.EnsureUtc(utcNow);
        if (Status != ConnectedPurchaseOrderStatus.Accepted)
        {
            throw new DomainException(
                ConnectedSupplierDomainErrorCodes.InvalidTransition,
                "Only an accepted order can move to preparing.");
        }

        Status = ConnectedPurchaseOrderStatus.Preparing;
        PreparingAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public void MarkFulfilled(DateTimeOffset utcNow)
    {
        ConnectedSupplierRelationship.EnsureUtc(utcNow);
        if (Status is not (ConnectedPurchaseOrderStatus.Accepted or ConnectedPurchaseOrderStatus.Preparing))
        {
            throw new DomainException(
                ConnectedSupplierDomainErrorCodes.InvalidTransition,
                "Only accepted or preparing orders can be marked fulfilled.");
        }

        if (Status == ConnectedPurchaseOrderStatus.Accepted)
        {
            PreparingAtUtc ??= utcNow;
        }

        Status = ConnectedPurchaseOrderStatus.Fulfilled;
        FulfilledAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    /// <summary>Buyer withdraw while supplier has not yet accepted (New or awaiting buyer approval).</summary>
    public void WithdrawByBuyer(DateTimeOffset utcNow)
    {
        ConnectedSupplierRelationship.EnsureUtc(utcNow);
        if (Status is not (ConnectedPurchaseOrderStatus.New or ConnectedPurchaseOrderStatus.ChangesProposed))
        {
            throw new DomainException(
                ConnectedSupplierDomainErrorCodes.InvalidTransition,
                "Incoming order has already been answered.");
        }

        Status = ConnectedPurchaseOrderStatus.Withdrawn;
        WithdrawnAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public bool CanBuyerWithdraw => Status is ConnectedPurchaseOrderStatus.New
        or ConnectedPurchaseOrderStatus.ChangesProposed;
    public bool CanBuyerReceive => Status is ConnectedPurchaseOrderStatus.Accepted
        or ConnectedPurchaseOrderStatus.Preparing
        or ConnectedPurchaseOrderStatus.Fulfilled;

    private void ReplaceLines(List<ConnectedPurchaseOrderLine> lines)
    {
        _lines.Clear();
        _lines.AddRange(lines);
    }

    private void EnsureNew()
    {
        if (Status != ConnectedPurchaseOrderStatus.New)
        {
            throw new DomainException(
                ConnectedSupplierDomainErrorCodes.InvalidTransition,
                "Incoming order has already been answered.");
        }
    }

    private static string? NormalizeDeclineNote(string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return null;
        }

        var trimmed = note.Trim();
        if (trimmed.Length > DeclineNoteMaxLength)
        {
            throw new DomainException(
                ConnectedSupplierDomainErrorCodes.InvalidOrder,
                $"Decline note must be at most {DeclineNoteMaxLength} characters.");
        }

        return trimmed;
    }

    public static ConnectedPurchaseOrder Rehydrate(
        ConnectedPurchaseOrderId id,
        ConnectedSupplierRelationshipId relationshipId,
        PosOrganizationId buyer,
        PosOrganizationId supplier,
        PurchaseOrderId buyerPoId,
        string? poNumber,
        DateOnly orderDate,
        string? notes,
        ConnectedPurchaseOrderStatus status,
        decimal total,
        DateTimeOffset created,
        DateTimeOffset updated,
        DateTimeOffset? accepted,
        DateTimeOffset? declined,
        IReadOnlyList<ConnectedPurchaseOrderLine> lines,
        DateTimeOffset? preparing = null,
        DateTimeOffset? fulfilled = null,
        DateTimeOffset? withdrawn = null,
        ConnectedPoDeclineReason? declineReason = null,
        string? declineNote = null,
        ConnectedPoPaymentTerm paymentTerm = ConnectedPoPaymentTerm.Cash,
        DateTimeOffset? changesProposedAtUtc = null,
        Guid? changesProposedByUserId = null,
        DateTimeOffset? buyerRespondedAtUtc = null,
        Guid? buyerRespondedByUserId = null) =>
        new(
            id,
            relationshipId,
            buyer,
            supplier,
            buyerPoId,
            poNumber,
            orderDate,
            notes,
            status,
            total,
            created,
            updated,
            accepted,
            declined,
            preparing,
            fulfilled,
            withdrawn,
            declineReason,
            declineNote,
            lines.ToList(),
            paymentTerm,
            changesProposedAtUtc,
            changesProposedByUserId,
            buyerRespondedAtUtc,
            buyerRespondedByUserId);
}
