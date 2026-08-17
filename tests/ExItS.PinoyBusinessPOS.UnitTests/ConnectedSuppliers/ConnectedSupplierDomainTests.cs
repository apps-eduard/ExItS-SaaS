using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;

namespace ExItS.PinoyBusinessPOS.UnitTests.ConnectedSuppliers;

public sealed class ConnectedSupplierDomainTests
{
    private static readonly PosOrganizationId Buyer=PosOrganizationId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly PosOrganizationId Supplier=PosOrganizationId.From(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    private static readonly DateTimeOffset Now=new(2026,8,14,18,0,0,TimeSpan.Zero);

    [Fact]
    public void Relationship_ids_with_the_same_guid_are_equal_across_instances()
    {
        var guid = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var left = ConnectedSupplierRelationshipId.From(guid);
        var right = ConnectedSupplierRelationshipId.From(guid);
        Assert.Equal(left, right);
        Assert.True(left == right);
        Assert.False(left != right);
    }

    [Fact]
    public void Self_connection_is_denied()
    {
        var ex=Assert.Throws<DomainException>(()=>ConnectedSupplierRelationship.Request(Buyer,Buyer,Now));
        Assert.Equal(ConnectedSupplierDomainErrorCodes.SelfConnection,ex.ErrorCode);
    }

    [Fact]
    public void Only_pending_can_be_approved_and_only_active_can_disconnect()
    {
        var relationship=ConnectedSupplierRelationship.Request(Buyer,Supplier,Now);
        relationship.Approve(Now.AddMinutes(1));
        Assert.Equal(ConnectedSupplierRelationshipStatus.Active,relationship.Status);
        Assert.Throws<DomainException>(()=>relationship.Approve(Now.AddMinutes(2)));
        relationship.Disconnect(Now.AddMinutes(3));
        Assert.Equal(ConnectedSupplierRelationshipStatus.Disconnected,relationship.Status);
        Assert.Throws<DomainException>(()=>relationship.Disconnect(Now.AddMinutes(4)));
    }

    [Fact]
    public void Exposure_price_and_order_line_totals_use_RoundMoney()
    {
        var product=CatalogProductId.New();
        var exposure=SupplierProductExposure.Expose(Supplier,product,"Wholesale rice","Piece",10.126m,Now);
        var line=ConnectedPurchaseOrderLine.Create(product,"Wholesale rice",null,3m,exposure.SupplierOrderPrice,"Piece");
        Assert.Equal(10.13m,exposure.SupplierOrderPrice);
        Assert.Equal(30.39m,line.LineTotal);
    }

    [Fact]
    public void Link_rejects_an_exposure_owned_by_another_supplier()
    {
        var relationship=ConnectedSupplierRelationship.Request(Buyer,Supplier,Now);
        relationship.Approve(Now.AddMinutes(1));
        var foreign=PosOrganizationId.From(Guid.Parse("33333333-3333-3333-3333-333333333333"));
        var exposure=SupplierProductExposure.Expose(foreign,CatalogProductId.New(),"Foreign","Piece",1m,Now);
        Assert.Throws<DomainException>(()=>BuyerSupplierProductLink.Create(
            relationship.Id,Buyer,Supplier,CatalogProductId.New(),exposure,Now));
    }

    [Fact]
    public void Accept_confirms_requested_quantities_without_changing_original_request()
    {
        var relationship=ConnectedSupplierRelationship.Request(Buyer,Supplier,Now);
        relationship.Approve(Now.AddMinutes(1));
        var line=ConnectedPurchaseOrderLine.Create(CatalogProductId.New(),"Item",null,2m,5m,"Piece");
        var order=ConnectedPurchaseOrder.CreateFromBuyerSubmission(relationship,PurchaseOrderId.New(),"PO-1",
            DateOnly.FromDateTime(Now.UtcDateTime),null,[line],Now.AddMinutes(2));
        order.Accept(Now.AddMinutes(3));
        Assert.Equal(ConnectedPurchaseOrderStatus.Accepted,order.Status);
        Assert.Equal(10m,order.TotalAmount);
        var accepted=Assert.Single(order.Lines);
        Assert.Equal(line.ProductId,accepted.ProductId);
        Assert.Equal(2m,accepted.Qty);
        Assert.Equal(2m,accepted.ConfirmedQty);
        Assert.Equal(5m,accepted.UnitPriceSnapshot);
        Assert.Equal(ConnectedPoLineAvailability.Available,accepted.Availability);
        Assert.Null(order.DeclinedAtUtc);
    }

    [Fact]
    public void Deactivated_exposure_is_unavailable_and_advances_sync_version()
    {
        var exposure=SupplierProductExposure.Expose(Supplier,CatalogProductId.New(),"Item","Piece",5m,Now);
        exposure.Deactivate(Now.AddMinutes(1));
        Assert.False(exposure.IsExposed);
        Assert.False(exposure.IsOrderable);
        Assert.Equal(2,exposure.SyncVersion);
    }

    [Fact]
    public void Effective_price_uses_buyer_override_then_supplier_default_and_requires_share()
    {
        var relationship=ConnectedSupplierRelationship.Request(Buyer,Supplier,Now);
        relationship.Approve(Now.AddMinutes(1));
        var productId=CatalogProductId.New();
        var exposure=SupplierProductExposure.Expose(Supplier,productId,"Rice","Kilogram",50m,Now);
        var share=ConnectedBuyerProductShare.Share(relationship.Id,Buyer,Supplier,productId,Now,45.126m);

        Assert.True(ConnectedPoPricing.TryResolveEffectivePrice(exposure,share,out var overridden));
        Assert.Equal(45.13m,overridden);

        share.SetBuyerSpecificPoPrice(null,Now.AddMinutes(2));
        Assert.True(ConnectedPoPricing.TryResolveEffectivePrice(exposure,share,out var fallback));
        Assert.Equal(50m,fallback);

        share.Unshare(Now.AddMinutes(3));
        Assert.False(ConnectedPoPricing.TryResolveEffectivePrice(exposure,share,out _));
    }
}
