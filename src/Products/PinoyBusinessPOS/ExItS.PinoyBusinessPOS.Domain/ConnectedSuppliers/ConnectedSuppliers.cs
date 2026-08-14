using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;

public enum ConnectedSupplierRelationshipStatus { Pending = 0, Active = 1, Declined = 2, Disconnected = 3 }
public enum SupplierConnectionType { External = 0, ConnectedOrganization = 1 }
public enum ConnectedPurchaseOrderStatus { New = 0, Accepted = 1, Declined = 2 }

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
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private ConnectedSupplierRelationship(ConnectedSupplierRelationshipId id, PosOrganizationId buyerOrganizationId,
        PosOrganizationId supplierOrganizationId, ConnectedSupplierRelationshipStatus status, DateTimeOffset requestedAtUtc,
        Guid? requestedByUserId, DateTimeOffset? respondedAtUtc, Guid? respondedByUserId,
        DateTimeOffset? disconnectedAtUtc, DateTimeOffset createdAtUtc, DateTimeOffset updatedAtUtc)
    {
        Id = id; BuyerOrganizationId = buyerOrganizationId; SupplierOrganizationId = supplierOrganizationId;
        Status = status; RequestedAtUtc = requestedAtUtc; RequestedByUserId = requestedByUserId;
        RespondedAtUtc = respondedAtUtc; RespondedByUserId = respondedByUserId; DisconnectedAtUtc = disconnectedAtUtc;
        CreatedAtUtc = createdAtUtc; UpdatedAtUtc = updatedAtUtc;
    }

    public static ConnectedSupplierRelationship Request(PosOrganizationId buyer, PosOrganizationId supplier,
        DateTimeOffset utcNow, Guid? requestedByUserId = null, ConnectedSupplierRelationshipId? id = null)
    {
        EnsureUtc(utcNow);
        if (buyer == supplier) throw new DomainException(ConnectedSupplierDomainErrorCodes.SelfConnection, "An organization cannot connect to itself.");
        return new(id ?? ConnectedSupplierRelationshipId.New(), buyer, supplier,
            ConnectedSupplierRelationshipStatus.Pending, utcNow, requestedByUserId, null, null, null, utcNow, utcNow);
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
    public static ConnectedSupplierRelationship Rehydrate(ConnectedSupplierRelationshipId id, PosOrganizationId buyer,
        PosOrganizationId supplier, ConnectedSupplierRelationshipStatus status, DateTimeOffset requestedAtUtc,
        Guid? requestedBy, DateTimeOffset? respondedAtUtc, Guid? respondedBy, DateTimeOffset? disconnectedAtUtc,
        DateTimeOffset createdAtUtc, DateTimeOffset updatedAtUtc) =>
        new(id, buyer, supplier, status, requestedAtUtc, requestedBy, respondedAtUtc, respondedBy, disconnectedAtUtc, createdAtUtc, updatedAtUtc);
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
        string? packageLabel = null)
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
            exposure.SupplierOrderPrice,
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

public sealed record ConnectedPurchaseOrderLine(CatalogProductId ProductId, string NameSnapshot, string? SkuSnapshot,
    decimal Qty, decimal UnitPriceSnapshot, decimal LineTotal, string UnitOfMeasureCode)
{
    public static ConnectedPurchaseOrderLine Create(CatalogProductId productId,string name,string? sku,decimal qty,decimal unitPrice,string uom)
    {
        if (qty<=0) throw new DomainException(ConnectedSupplierDomainErrorCodes.InvalidOrder,"Order quantity must be positive.");
        var price=SupplierProductExposure.Money(unitPrice);
        return new(productId,SupplierProductExposure.Required(name,200),SupplierProductExposure.Clean(sku),qty,price,
            SaleMoney.RoundMoney(qty*price),SupplierProductExposure.Required(uom,32));
    }
}

public sealed class ConnectedPurchaseOrder
{
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
    public decimal TotalAmount { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? AcceptedAtUtc { get; private set; }
    public DateTimeOffset? DeclinedAtUtc { get; private set; }
    public IReadOnlyList<ConnectedPurchaseOrderLine> Lines => _lines;
    private ConnectedPurchaseOrder(ConnectedPurchaseOrderId id,ConnectedSupplierRelationshipId relationshipId,PosOrganizationId buyer,
        PosOrganizationId supplier,PurchaseOrderId buyerPoId,string? poNumber,DateOnly orderDate,string? notes,
        ConnectedPurchaseOrderStatus status,decimal total,DateTimeOffset created,DateTimeOffset updated,
        DateTimeOffset? accepted,DateTimeOffset? declined,List<ConnectedPurchaseOrderLine> lines)
    { Id=id;RelationshipId=relationshipId;BuyerOrganizationId=buyer;SupplierOrganizationId=supplier;BuyerPurchaseOrderId=buyerPoId;
      BuyerPoNumber=poNumber;OrderDate=orderDate;Notes=notes;Status=status;TotalAmount=total;CreatedAtUtc=created;UpdatedAtUtc=updated;
      AcceptedAtUtc=accepted;DeclinedAtUtc=declined;_lines=lines; }
    public static ConnectedPurchaseOrder CreateFromBuyerSubmission(ConnectedSupplierRelationship relationship,PurchaseOrderId buyerPoId,
        string? poNumber,DateOnly orderDate,string? notes,IReadOnlyList<ConnectedPurchaseOrderLine> lines,DateTimeOffset utcNow,
        ConnectedPurchaseOrderId? id=null)
    {
        if (relationship.Status!=ConnectedSupplierRelationshipStatus.Active || lines.Count==0)
            throw new DomainException(ConnectedSupplierDomainErrorCodes.InvalidOrder,"An active relationship and order lines are required.");
        var total=SaleMoney.RoundMoney(lines.Sum(x=>x.LineTotal));
        return new(id??ConnectedPurchaseOrderId.New(),relationship.Id,relationship.BuyerOrganizationId,relationship.SupplierOrganizationId,
            buyerPoId,poNumber,orderDate,SupplierProductExposure.Clean(notes),ConnectedPurchaseOrderStatus.New,total,utcNow,utcNow,null,null,lines.ToList());
    }
    public void Accept(DateTimeOffset utcNow) { EnsureNew(); Status=ConnectedPurchaseOrderStatus.Accepted;AcceptedAtUtc=utcNow;UpdatedAtUtc=utcNow; }
    public void Decline(DateTimeOffset utcNow) { EnsureNew(); Status=ConnectedPurchaseOrderStatus.Declined;DeclinedAtUtc=utcNow;UpdatedAtUtc=utcNow; }
    private void EnsureNew() { if(Status!=ConnectedPurchaseOrderStatus.New) throw new DomainException(ConnectedSupplierDomainErrorCodes.InvalidTransition,"Incoming order has already been answered."); }
    public static ConnectedPurchaseOrder Rehydrate(ConnectedPurchaseOrderId id,ConnectedSupplierRelationshipId relationshipId,
        PosOrganizationId buyer,PosOrganizationId supplier,PurchaseOrderId buyerPoId,string? poNumber,DateOnly orderDate,string? notes,
        ConnectedPurchaseOrderStatus status,decimal total,DateTimeOffset created,DateTimeOffset updated,DateTimeOffset? accepted,
        DateTimeOffset? declined,IReadOnlyList<ConnectedPurchaseOrderLine> lines) =>
        new(id,relationshipId,buyer,supplier,buyerPoId,poNumber,orderDate,notes,status,total,created,updated,accepted,declined,lines.ToList());
}
