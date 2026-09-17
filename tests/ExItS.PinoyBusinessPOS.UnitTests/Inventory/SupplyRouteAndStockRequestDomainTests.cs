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
    public void Supply_route_domain_allows_any_distinct_locations_branch_type_is_application_rule()
    {
        var whToRetail = SupplyRoute.Create(Org, Warehouse, BranchA, Utc, isPreferred: true);
        Assert.True(whToRetail.IsPreferred);
        Assert.True(whToRetail.IsActive);

        // Domain remains location-agnostic; Upsert/CreateStockRequest enforce Warehouse sources.
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
        Assert.Null(request.Lines[0].ApprovedQuantity);
        Assert.Equal("SR-20260905-000001", request.RequestNumber);
    }

    [Fact]
    public void Approve_sets_approved_quantity_without_changing_requested()
    {
        var request = CreatePending();
        request.Approve(Actor, Utc.AddMinutes(1), new Dictionary<Guid, decimal> { [Rice.Value] = 7m });
        Assert.Equal(StockRequestStatus.Approved, request.Status);
        Assert.Equal(10m, request.Lines[0].RequestedQuantity);
        Assert.Equal(7m, request.Lines[0].ApprovedQuantity);
        Assert.Equal(Actor, request.ApprovedBy);
    }

    [Fact]
    public void Approve_rejects_quantity_above_requested()
    {
        var request = CreatePending();
        var ex = Assert.Throws<DomainException>(() =>
            request.Approve(Actor, Utc.AddMinutes(1), new Dictionary<Guid, decimal> { [Rice.Value] = 11m }));
        Assert.Equal(DomainErrorCodes.InvalidStockRequestQuantity, ex.ErrorCode);
    }

    [Fact]
    public void Preparing_and_dispatch_lifecycle()
    {
        var request = CreatePending();
        request.Approve(Actor, Utc.AddMinutes(1), new Dictionary<Guid, decimal> { [Rice.Value] = 10m });
        request.StartPreparing(Actor, Utc.AddMinutes(2));
        Assert.Equal(StockRequestStatus.Preparing, request.Status);

        var transferId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        request.MarkDispatched(Actor, Utc.AddMinutes(3), transferId);
        Assert.Equal(StockRequestStatus.InTransit, request.Status);
        Assert.Equal(transferId, request.LinkedInventoryTransferId);

        request.MarkDispatched(Actor, Utc.AddMinutes(4), transferId);
        Assert.Equal(StockRequestStatus.InTransit, request.Status);
    }

    [Fact]
    public void Fulfilled_quantity_compares_against_approved_quantity()
    {
        var request = CreatePending();
        request.Approve(Actor, Utc.AddMinutes(1), new Dictionary<Guid, decimal> { [Rice.Value] = 6m });
        request.StartPreparing(Actor, Utc.AddMinutes(2));
        request.MarkDispatched(Actor, Utc.AddMinutes(3), Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"));

        request.RecalculateStatusFromReceivedQuantities(
            new Dictionary<Guid, decimal> { [Rice.Value] = 0m },
            Utc.AddMinutes(4));
        Assert.Equal(StockRequestStatus.InTransit, request.Status);

        request.RecalculateStatusFromReceivedQuantities(
            new Dictionary<Guid, decimal> { [Rice.Value] = 4m },
            Utc.AddMinutes(5));
        Assert.Equal(StockRequestStatus.PartiallyFulfilled, request.Status);

        request.RecalculateStatusFromReceivedQuantities(
            new Dictionary<Guid, decimal> { [Rice.Value] = 6m },
            Utc.AddMinutes(6));
        Assert.Equal(StockRequestStatus.Fulfilled, request.Status);
    }

    [Fact]
    public void Cancel_and_reject_only_before_in_transit()
    {
        var request = CreatePending();
        request.Approve(Actor, Utc.AddMinutes(1), new Dictionary<Guid, decimal> { [Rice.Value] = 10m });
        request.Reject(Actor, Utc.AddMinutes(2), "Out of stock");
        Assert.Equal(StockRequestStatus.Rejected, request.Status);

        var again = CreatePending();
        again.Approve(Actor, Utc.AddMinutes(1), new Dictionary<Guid, decimal> { [Rice.Value] = 10m });
        again.StartPreparing(Actor, Utc.AddMinutes(2));
        again.MarkDispatched(Actor, Utc.AddMinutes(3), Guid.NewGuid());
        var cancelEx = Assert.Throws<DomainException>(() => again.Cancel(Actor, Utc.AddMinutes(4)));
        Assert.Equal(DomainErrorCodes.InvalidStockRequestStatusTransition, cancelEx.ErrorCode);
    }

    [Fact]
    public void InProgress_code_parses_as_Preparing()
    {
        Assert.True(StockRequestStatuses.TryParse("InProgress", out var status));
        Assert.Equal(StockRequestStatus.Preparing, status);
        Assert.Equal("Preparing", StockRequestStatuses.ToCode(StockRequestStatus.InProgress));
    }

    private static StockRequest CreatePending() =>
        StockRequest.Create(
            Org,
            BranchA,
            Warehouse,
            [new StockRequestLineDraft(Rice, 10m, "Rice 5kg", UnitOfMeasure.Piece)],
            Actor,
            Utc,
            "SR-20260905-000002");
}
