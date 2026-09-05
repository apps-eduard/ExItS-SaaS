using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.UnitTests.Inventory;

public sealed class SupplyRouteAndStockRequestDomainTests
{
    private static readonly PosOrganizationId Org = PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly PosBranchId Warehouse = PosBranchId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly PosBranchId BranchA = PosBranchId.From(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    private static readonly PosBranchId BranchB = PosBranchId.From(Guid.Parse("33333333-3333-3333-3333-333333333333"));
    private static readonly CatalogProductId Rice = CatalogProductId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly Guid Actor = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly DateTimeOffset Utc = new(2026, 9, 5, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Supply_route_rejects_same_location()
    {
        var ex = Assert.Throws<DomainException>(() =>
            SupplyRoute.Create(Org, Warehouse, Warehouse, Utc));
        Assert.Equal(DomainErrorCodes.SupplyRouteSameLocation, ex.ErrorCode);
    }

    [Fact]
    public void Supply_route_allows_warehouse_to_retail_and_retail_to_retail()
    {
        var whToRetail = SupplyRoute.Create(Org, Warehouse, BranchA, Utc, isPreferred: true);
        Assert.True(whToRetail.IsPreferred);
        Assert.True(whToRetail.IsActive);

        var retailToRetail = SupplyRoute.Create(Org, BranchA, BranchB, Utc);
        Assert.Equal(BranchA, retailToRetail.SourceLocationId);
        Assert.Equal(BranchB, retailToRetail.DestinationLocationId);
    }

    [Fact]
    public void Deactivate_clears_preferred()
    {
        var route = SupplyRoute.Create(Org, Warehouse, BranchA, Utc, isPreferred: true);
        route.Deactivate(Utc.AddMinutes(1));
        Assert.False(route.IsActive);
        Assert.False(route.IsPreferred);
    }

    [Fact]
    public void Stock_request_number_formats()
    {
        Assert.Equal("SR-20260905-000001", StockRequestNumbers.Format(new DateOnly(2026, 9, 5), 1));
        Assert.Equal("SR-20260905-000001", StockRequestNumbers.Normalize(" sr-20260905-000001 "));
    }

    [Fact]
    public void Creating_stock_request_does_not_require_inventory_and_starts_pending()
    {
        var request = StockRequest.Create(
            Org,
            BranchA,
            Warehouse,
            [new StockRequestLineDraft(Rice, 10m, "Rice 5kg", UnitOfMeasure.Piece)],
            Actor,
            Utc,
            "SR-20260905-000001");
        Assert.Equal(StockRequestStatus.Pending, request.Status);
        Assert.Equal(10m, request.Lines[0].RequestedQuantity);
        Assert.Equal("SR-20260905-000001", request.RequestNumber);
    }

    [Fact]
    public void Fulfilled_quantity_is_derived_from_received_not_draft_sent()
    {
        var request = StockRequest.Create(
            Org,
            BranchA,
            Warehouse,
            [new StockRequestLineDraft(Rice, 10m, "Rice 5kg", UnitOfMeasure.Piece)],
            Actor,
            Utc,
            "SR-20260905-000002");

        request.MarkInProgress(Utc.AddMinutes(1));
        Assert.Equal(StockRequestStatus.InProgress, request.Status);

        request.RecalculateStatusFromReceivedQuantities(
            new Dictionary<Guid, decimal> { [Rice.Value] = 0m },
            Utc.AddMinutes(2));
        Assert.Equal(StockRequestStatus.InProgress, request.Status);

        request.RecalculateStatusFromReceivedQuantities(
            new Dictionary<Guid, decimal> { [Rice.Value] = 6m },
            Utc.AddMinutes(3));
        Assert.Equal(StockRequestStatus.PartiallyFulfilled, request.Status);

        request.RecalculateStatusFromReceivedQuantities(
            new Dictionary<Guid, decimal> { [Rice.Value] = 10m },
            Utc.AddMinutes(4));
        Assert.Equal(StockRequestStatus.Fulfilled, request.Status);
    }

    [Fact]
    public void Reject_and_cancel_are_terminal()
    {
        var request = StockRequest.Create(
            Org,
            BranchA,
            Warehouse,
            [new StockRequestLineDraft(Rice, 4m, "Rice 5kg", UnitOfMeasure.Piece)],
            Actor,
            Utc,
            "SR-20260905-000003");
        request.Reject(Actor, Utc.AddMinutes(1), "Out of stock");
        Assert.Equal(StockRequestStatus.Rejected, request.Status);

        var again = Assert.Throws<DomainException>(() => request.Cancel(Actor, Utc.AddMinutes(2)));
        Assert.Equal(DomainErrorCodes.InvalidStockRequestStatusTransition, again.ErrorCode);
    }
}
