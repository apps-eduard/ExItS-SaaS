using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.ConnectedSuppliers;

internal sealed class ConnectedSupplierRelationshipRecord
{
    public Guid Id { get; set; } public Guid BuyerOrganizationId { get; set; } public Guid SupplierOrganizationId { get; set; }
    public int Status { get; set; } public DateTimeOffset RequestedAtUtc { get; set; } public Guid? RequestedByUserId { get; set; }
    public DateTimeOffset? RespondedAtUtc { get; set; } public Guid? RespondedByUserId { get; set; }
    public DateTimeOffset? DisconnectedAtUtc { get; set; }
    public string? BuyerDisplayNameSnapshot { get; set; }
    public string? BuyerPublicOrganizationIdSnapshot { get; set; }
    public string? SupplierDisplayNameSnapshot { get; set; }
    public string? SupplierPublicOrganizationIdSnapshot { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; } public uint Xmin { get; set; }
}
internal sealed class SupplierProductExposureRecord
{
    public Guid Id { get; set; } public Guid SupplierOrganizationId { get; set; } public Guid ProductId { get; set; }
    public string? SkuSnapshot { get; set; } public string NameSnapshot { get; set; }=string.Empty;
    public string? CategoryNameSnapshot { get; set; } public string UnitOfMeasureCode { get; set; }=string.Empty;
    public decimal SupplierOrderPrice { get; set; } public bool IsOrderable { get; set; } public bool IsExposed { get; set; }
    public long SyncVersion { get; set; } public DateTimeOffset CreatedAtUtc { get; set; } public DateTimeOffset UpdatedAtUtc { get; set; }
    public uint Xmin { get; set; }
}
internal sealed class ConnectedBuyerProductShareRecord
{
    public Guid Id { get; set; }
    public Guid RelationshipId { get; set; }
    public Guid BuyerOrganizationId { get; set; }
    public Guid SupplierOrganizationId { get; set; }
    public Guid SupplierProductId { get; set; }
    public bool IsShared { get; set; }
    public decimal? BuyerSpecificPoPrice { get; set; }
    public long SyncVersion { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public uint Xmin { get; set; }
}
internal sealed class BuyerSupplierProductLinkRecord
{
    public Guid Id { get; set; } public Guid RelationshipId { get; set; } public Guid BuyerOrganizationId { get; set; }
    public Guid SupplierOrganizationId { get; set; } public Guid BuyerProductId { get; set; } public Guid SupplierProductId { get; set; }
    public string? SupplierSkuSnapshot { get; set; } public string SupplierNameSnapshot { get; set; }=string.Empty;
    public string UnitOfMeasureCode { get; set; }=string.Empty; public decimal LastKnownOrderPrice { get; set; }
    public Guid? BuyerPurchaseUnitId { get; set; }
    public decimal MultiplierToBase { get; set; } = 1m;
    public string? PackageLabel { get; set; }
    public bool IsActive { get; set; } public long SyncVersion { get; set; } public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; } public uint Xmin { get; set; }
}
internal sealed class ConnectedPurchaseOrderRecord
{
    public Guid Id { get; set; } public Guid RelationshipId { get; set; } public Guid BuyerOrganizationId { get; set; }
    public Guid SupplierOrganizationId { get; set; } public Guid BuyerPurchaseOrderId { get; set; } public string? BuyerPoNumber { get; set; }
    public DateOnly OrderDate { get; set; } public string? Notes { get; set; } public int Status { get; set; }
    public decimal TotalAmount { get; set; } public DateTimeOffset CreatedAtUtc { get; set; } public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset? AcceptedAtUtc { get; set; }
    public DateTimeOffset? DeclinedAtUtc { get; set; }
    public DateTimeOffset? PreparingAtUtc { get; set; }
    public DateTimeOffset? FulfilledAtUtc { get; set; }
    public DateTimeOffset? WithdrawnAtUtc { get; set; }
    public int? DeclineReason { get; set; }
    public string? DeclineNote { get; set; }
    public int PaymentTerm { get; set; }
    public DateTimeOffset? ChangesProposedAtUtc { get; set; }
    public Guid? ChangesProposedByUserId { get; set; }
    public DateTimeOffset? BuyerRespondedAtUtc { get; set; }
    public Guid? BuyerRespondedByUserId { get; set; }
    public List<ConnectedPurchaseOrderLineRecord> Lines { get; set; }=[]; public uint Xmin { get; set; }
}
internal sealed class ConnectedPurchaseOrderLineRecord
{
    public Guid ConnectedPurchaseOrderId { get; set; } public int LineNumber { get; set; } public Guid ProductId { get; set; }
    public string NameSnapshot { get; set; }=string.Empty; public string? SkuSnapshot { get; set; }     public decimal Qty { get; set; }
    public decimal? ProposedQty { get; set; }
    public decimal? ConfirmedQty { get; set; }
    public int Availability { get; set; }
    public decimal UnitPriceSnapshot { get; set; } public decimal LineTotal { get; set; } public string UnitOfMeasureCode { get; set; }=string.Empty;
}

internal static class ConnectedSupplierEntityMapper
{
    public static ConnectedSupplierRelationship ToDomain(ConnectedSupplierRelationshipRecord r)=>ConnectedSupplierRelationship.Rehydrate(
        ConnectedSupplierRelationshipId.From(r.Id),PosOrganizationId.From(r.BuyerOrganizationId),PosOrganizationId.From(r.SupplierOrganizationId),
        (ConnectedSupplierRelationshipStatus)r.Status,r.RequestedAtUtc,r.RequestedByUserId,r.RespondedAtUtc,r.RespondedByUserId,
        r.DisconnectedAtUtc,r.CreatedAtUtc,r.UpdatedAtUtc,
        r.BuyerDisplayNameSnapshot,r.BuyerPublicOrganizationIdSnapshot,
        r.SupplierDisplayNameSnapshot,r.SupplierPublicOrganizationIdSnapshot);
    public static ConnectedSupplierRelationshipRecord ToRecord(ConnectedSupplierRelationship x)=>new(){Id=x.Id.Value,
        BuyerOrganizationId=x.BuyerOrganizationId.Value,SupplierOrganizationId=x.SupplierOrganizationId.Value,Status=(int)x.Status,
        RequestedAtUtc=x.RequestedAtUtc,RequestedByUserId=x.RequestedByUserId,RespondedAtUtc=x.RespondedAtUtc,
        RespondedByUserId=x.RespondedByUserId,DisconnectedAtUtc=x.DisconnectedAtUtc,
        BuyerDisplayNameSnapshot=x.BuyerDisplayNameSnapshot,BuyerPublicOrganizationIdSnapshot=x.BuyerPublicOrganizationIdSnapshot,
        SupplierDisplayNameSnapshot=x.SupplierDisplayNameSnapshot,SupplierPublicOrganizationIdSnapshot=x.SupplierPublicOrganizationIdSnapshot,
        CreatedAtUtc=x.CreatedAtUtc,UpdatedAtUtc=x.UpdatedAtUtc};
    public static void Apply(ConnectedSupplierRelationship x,ConnectedSupplierRelationshipRecord r)
    {r.Status=(int)x.Status;r.RespondedAtUtc=x.RespondedAtUtc;r.RespondedByUserId=x.RespondedByUserId;r.DisconnectedAtUtc=x.DisconnectedAtUtc;r.UpdatedAtUtc=x.UpdatedAtUtc;}

    public static SupplierProductExposure ToDomain(SupplierProductExposureRecord r)=>SupplierProductExposure.Rehydrate(
        SupplierProductExposureId.From(r.Id),PosOrganizationId.From(r.SupplierOrganizationId),CatalogProductId.From(r.ProductId),
        r.SkuSnapshot,r.NameSnapshot,r.CategoryNameSnapshot,r.UnitOfMeasureCode,r.SupplierOrderPrice,r.IsOrderable,r.IsExposed,
        r.SyncVersion,r.CreatedAtUtc,r.UpdatedAtUtc);
    public static SupplierProductExposureRecord ToRecord(SupplierProductExposure x)=>new(){Id=x.Id.Value,SupplierOrganizationId=x.SupplierOrganizationId.Value,
        ProductId=x.ProductId.Value,SkuSnapshot=x.SkuSnapshot,NameSnapshot=x.NameSnapshot,CategoryNameSnapshot=x.CategoryNameSnapshot,
        UnitOfMeasureCode=x.UnitOfMeasureCode,SupplierOrderPrice=x.SupplierOrderPrice,IsOrderable=x.IsOrderable,IsExposed=x.IsExposed,
        SyncVersion=x.SyncVersion,CreatedAtUtc=x.CreatedAtUtc,UpdatedAtUtc=x.UpdatedAtUtc};
    public static void Apply(SupplierProductExposure x,SupplierProductExposureRecord r)
    {r.SkuSnapshot=x.SkuSnapshot;r.NameSnapshot=x.NameSnapshot;r.CategoryNameSnapshot=x.CategoryNameSnapshot;r.UnitOfMeasureCode=x.UnitOfMeasureCode;
     r.SupplierOrderPrice=x.SupplierOrderPrice;r.IsOrderable=x.IsOrderable;r.IsExposed=x.IsExposed;r.SyncVersion=x.SyncVersion;r.UpdatedAtUtc=x.UpdatedAtUtc;}

    public static ConnectedBuyerProductShare ToDomain(ConnectedBuyerProductShareRecord r) =>
        ConnectedBuyerProductShare.Rehydrate(
            ConnectedBuyerProductShareId.From(r.Id),
            ConnectedSupplierRelationshipId.From(r.RelationshipId),
            PosOrganizationId.From(r.BuyerOrganizationId),
            PosOrganizationId.From(r.SupplierOrganizationId),
            CatalogProductId.From(r.SupplierProductId),
            r.IsShared,
            r.BuyerSpecificPoPrice,
            r.SyncVersion,
            r.CreatedAtUtc,
            r.UpdatedAtUtc);
    public static ConnectedBuyerProductShareRecord ToRecord(ConnectedBuyerProductShare x) => new()
    {
        Id=x.Id.Value, RelationshipId=x.RelationshipId.Value, BuyerOrganizationId=x.BuyerOrganizationId.Value,
        SupplierOrganizationId=x.SupplierOrganizationId.Value, SupplierProductId=x.SupplierProductId.Value,
        IsShared=x.IsShared, BuyerSpecificPoPrice=x.BuyerSpecificPoPrice, SyncVersion=x.SyncVersion,
        CreatedAtUtc=x.CreatedAtUtc, UpdatedAtUtc=x.UpdatedAtUtc
    };
    public static void Apply(ConnectedBuyerProductShare x, ConnectedBuyerProductShareRecord r)
    {
        r.IsShared=x.IsShared; r.BuyerSpecificPoPrice=x.BuyerSpecificPoPrice;
        r.SyncVersion=x.SyncVersion; r.UpdatedAtUtc=x.UpdatedAtUtc;
    }

    public static BuyerSupplierProductLink ToDomain(BuyerSupplierProductLinkRecord r)=>BuyerSupplierProductLink.Rehydrate(
        BuyerSupplierProductLinkId.From(r.Id),ConnectedSupplierRelationshipId.From(r.RelationshipId),PosOrganizationId.From(r.BuyerOrganizationId),
        PosOrganizationId.From(r.SupplierOrganizationId),CatalogProductId.From(r.BuyerProductId),CatalogProductId.From(r.SupplierProductId),
        r.SupplierSkuSnapshot,r.SupplierNameSnapshot,r.UnitOfMeasureCode,r.LastKnownOrderPrice,r.IsActive,r.SyncVersion,r.CreatedAtUtc,r.UpdatedAtUtc,
        r.BuyerPurchaseUnitId,r.MultiplierToBase,r.PackageLabel);
    public static BuyerSupplierProductLinkRecord ToRecord(BuyerSupplierProductLink x)=>new(){Id=x.Id.Value,RelationshipId=x.RelationshipId.Value,
        BuyerOrganizationId=x.BuyerOrganizationId.Value,SupplierOrganizationId=x.SupplierOrganizationId.Value,BuyerProductId=x.BuyerProductId.Value,
        SupplierProductId=x.SupplierProductId.Value,SupplierSkuSnapshot=x.SupplierSkuSnapshot,SupplierNameSnapshot=x.SupplierNameSnapshot,
        UnitOfMeasureCode=x.UnitOfMeasureCode,LastKnownOrderPrice=x.LastKnownOrderPrice,BuyerPurchaseUnitId=x.BuyerPurchaseUnitId,
        MultiplierToBase=x.MultiplierToBase,PackageLabel=x.PackageLabel,IsActive=x.IsActive,SyncVersion=x.SyncVersion,
        CreatedAtUtc=x.CreatedAtUtc,UpdatedAtUtc=x.UpdatedAtUtc};
    public static void Apply(BuyerSupplierProductLink x,BuyerSupplierProductLinkRecord r)
    {r.SupplierSkuSnapshot=x.SupplierSkuSnapshot;r.SupplierNameSnapshot=x.SupplierNameSnapshot;r.UnitOfMeasureCode=x.UnitOfMeasureCode;
     r.LastKnownOrderPrice=x.LastKnownOrderPrice;r.BuyerPurchaseUnitId=x.BuyerPurchaseUnitId;r.MultiplierToBase=x.MultiplierToBase;
     r.PackageLabel=x.PackageLabel;r.IsActive=x.IsActive;r.SyncVersion=x.SyncVersion;r.UpdatedAtUtc=x.UpdatedAtUtc;}

    public static ConnectedPurchaseOrder ToDomain(ConnectedPurchaseOrderRecord r)
    {
        var status = (ConnectedPurchaseOrderStatus)r.Status;
        var lines = r.Lines.OrderBy(x => x.LineNumber).Select(x =>
        {
            var availability = (ConnectedPoLineAvailability)x.Availability;
            var confirmed = x.ConfirmedQty;
            if (confirmed is null
                && status is ConnectedPurchaseOrderStatus.Accepted
                    or ConnectedPurchaseOrderStatus.Preparing
                    or ConnectedPurchaseOrderStatus.Fulfilled)
            {
                confirmed = x.Qty;
                if (availability == ConnectedPoLineAvailability.Pending)
                {
                    availability = ConnectedPoLineAvailability.Available;
                }
            }

            return new ConnectedPurchaseOrderLine(
                CatalogProductId.From(x.ProductId),
                x.NameSnapshot,
                x.SkuSnapshot,
                x.Qty,
                x.UnitPriceSnapshot,
                x.LineTotal,
                x.UnitOfMeasureCode,
                x.ProposedQty,
                confirmed,
                availability);
        }).ToList();

        return ConnectedPurchaseOrder.Rehydrate(
            ConnectedPurchaseOrderId.From(r.Id),
            ConnectedSupplierRelationshipId.From(r.RelationshipId),
            PosOrganizationId.From(r.BuyerOrganizationId),
            PosOrganizationId.From(r.SupplierOrganizationId),
            PurchaseOrderId.From(r.BuyerPurchaseOrderId),
            r.BuyerPoNumber,
            r.OrderDate,
            r.Notes,
            status,
            r.TotalAmount,
            r.CreatedAtUtc,
            r.UpdatedAtUtc,
            r.AcceptedAtUtc,
            r.DeclinedAtUtc,
            lines,
            r.PreparingAtUtc,
            r.FulfilledAtUtc,
            r.WithdrawnAtUtc,
            r.DeclineReason is int reason ? (ConnectedPoDeclineReason)reason : null,
            r.DeclineNote,
            (ConnectedPoPaymentTerm)r.PaymentTerm,
            r.ChangesProposedAtUtc,
            r.ChangesProposedByUserId,
            r.BuyerRespondedAtUtc,
            r.BuyerRespondedByUserId);
    }

    public static ConnectedPurchaseOrderRecord ToRecord(ConnectedPurchaseOrder x)=>new(){Id=x.Id.Value,RelationshipId=x.RelationshipId.Value,
        BuyerOrganizationId=x.BuyerOrganizationId.Value,SupplierOrganizationId=x.SupplierOrganizationId.Value,BuyerPurchaseOrderId=x.BuyerPurchaseOrderId.Value,
        BuyerPoNumber=x.BuyerPoNumber,OrderDate=x.OrderDate,Notes=x.Notes,Status=(int)x.Status,TotalAmount=x.TotalAmount,
        CreatedAtUtc=x.CreatedAtUtc,UpdatedAtUtc=x.UpdatedAtUtc,AcceptedAtUtc=x.AcceptedAtUtc,DeclinedAtUtc=x.DeclinedAtUtc,
        PreparingAtUtc=x.PreparingAtUtc,FulfilledAtUtc=x.FulfilledAtUtc,WithdrawnAtUtc=x.WithdrawnAtUtc,
        DeclineReason=x.DeclineReason is null ? null : (int)x.DeclineReason.Value,DeclineNote=x.DeclineNote,
        PaymentTerm=(int)x.PaymentTerm,ChangesProposedAtUtc=x.ChangesProposedAtUtc,ChangesProposedByUserId=x.ChangesProposedByUserId,
        BuyerRespondedAtUtc=x.BuyerRespondedAtUtc,BuyerRespondedByUserId=x.BuyerRespondedByUserId,
        Lines=x.Lines.Select((l,i)=>new ConnectedPurchaseOrderLineRecord{ConnectedPurchaseOrderId=x.Id.Value,LineNumber=i+1,ProductId=l.ProductId.Value,
            NameSnapshot=l.NameSnapshot,SkuSnapshot=l.SkuSnapshot,Qty=l.Qty,ProposedQty=l.ProposedQty,ConfirmedQty=l.ConfirmedQty,
            Availability=(int)l.Availability,UnitPriceSnapshot=l.UnitPriceSnapshot,LineTotal=l.LineTotal,
            UnitOfMeasureCode=l.UnitOfMeasureCode}).ToList()};
    public static void Apply(ConnectedPurchaseOrder x,ConnectedPurchaseOrderRecord r)
    {
        r.Status=(int)x.Status;r.UpdatedAtUtc=x.UpdatedAtUtc;r.AcceptedAtUtc=x.AcceptedAtUtc;r.DeclinedAtUtc=x.DeclinedAtUtc;
        r.PreparingAtUtc=x.PreparingAtUtc;r.FulfilledAtUtc=x.FulfilledAtUtc;r.WithdrawnAtUtc=x.WithdrawnAtUtc;
        r.DeclineReason=x.DeclineReason is null ? null : (int)x.DeclineReason.Value;r.DeclineNote=x.DeclineNote;
        r.PaymentTerm=(int)x.PaymentTerm;r.ChangesProposedAtUtc=x.ChangesProposedAtUtc;r.ChangesProposedByUserId=x.ChangesProposedByUserId;
        r.BuyerRespondedAtUtc=x.BuyerRespondedAtUtc;r.BuyerRespondedByUserId=x.BuyerRespondedByUserId;
        var domainLines=x.Lines.ToList();
        foreach(var lineRecord in r.Lines)
        {
            var index=lineRecord.LineNumber-1;
            if(index<0||index>=domainLines.Count) continue;
            var line=domainLines[index];
            lineRecord.ProposedQty=line.ProposedQty;
            lineRecord.ConfirmedQty=line.ConfirmedQty;
            lineRecord.Availability=(int)line.Availability;
        }
    }
}
