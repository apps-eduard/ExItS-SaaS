using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.UnitTests.Inventory;

/// <summary>
/// Coverage-derived commitment math: draft does not release; dispatch OpenInTransit does.
/// </summary>
public sealed class StockRequestCommitmentCoverageTests
{
    private static readonly Guid Org = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Warehouse = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Branch = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Actor = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly DateTimeOffset Utc = new(2026, 9, 6, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Draft_transfer_does_not_reduce_remaining_to_dispatch()
    {
        var productId = CatalogProductId.New();
        var request = CreateApprovedRequest(productId, 70m);
        var draft = InventoryTransfer.CreateDraft(
            PosOrganizationId.From(Org),
            PosBranchId.From(Warehouse),
            PosBranchId.From(Branch),
            [new InventoryTransferLineDraft(productId, 70m, "Test Product", UnitOfMeasure.Piece)],
            Actor,
            Utc,
            stockRequestId: request.Id);

        var coverage = StockRequestDispatchCoverage.Compute(request, [draft]);
        Assert.Equal(70m, coverage.RemainingToDispatchByProduct[productId.Value]);
        Assert.Equal(0m, coverage.OpenInTransitByProduct.GetValueOrDefault(productId.Value));
    }

    [Fact]
    public void Full_dispatch_zeroes_remaining_commitment_while_in_transit()
    {
        var productId = CatalogProductId.New();
        var request = CreateApprovedRequest(productId, 30m);
        var transfer = InventoryTransfer.CreateDraft(
            PosOrganizationId.From(Org),
            PosBranchId.From(Warehouse),
            PosBranchId.From(Branch),
            [new InventoryTransferLineDraft(productId, 30m, "Test Product", UnitOfMeasure.Piece)],
            Actor,
            Utc,
            stockRequestId: request.Id);
        transfer.Dispatch("260906-001", Actor, Utc.AddMinutes(1));

        var coverage = StockRequestDispatchCoverage.Compute(request, [transfer]);
        Assert.Equal(0m, coverage.RemainingToDispatchByProduct[productId.Value]);
        Assert.Equal(30m, coverage.OpenInTransitByProduct[productId.Value]);
        Assert.Equal(0m, coverage.ReceivedByProduct.GetValueOrDefault(productId.Value));
    }

    [Fact]
    public void Partial_receive_keeps_open_in_transit_out_of_commitment()
    {
        var productId = CatalogProductId.New();
        var request = CreateApprovedRequest(productId, 70m);
        var transfer = InventoryTransfer.CreateDraft(
            PosOrganizationId.From(Org),
            PosBranchId.From(Warehouse),
            PosBranchId.From(Branch),
            [new InventoryTransferLineDraft(productId, 70m, "Test Product", UnitOfMeasure.Piece)],
            Actor,
            Utc,
            stockRequestId: request.Id);
        transfer.Dispatch("260906-002", Actor, Utc.AddMinutes(1));
        transfer.Receive(
            [new InventoryTransferReceiveLineDraft(productId, GoodQty: 40m)],
            Actor,
            Utc.AddMinutes(2));

        var coverage = StockRequestDispatchCoverage.Compute(request, [transfer]);
        // Remaining = Target 70 - Good 40 - OpenInTransit 30 - Waived 0 = 0
        // Open in transit still covers the undelivered 30, so no recommit.
        Assert.Equal(40m, coverage.ReceivedByProduct[productId.Value]);
        Assert.Equal(30m, coverage.OpenInTransitByProduct[productId.Value]);
        Assert.Equal(0m, coverage.RemainingToDispatchByProduct[productId.Value]);
    }

    [Fact]
    public void Full_receive_zeroes_commitment()
    {
        var productId = CatalogProductId.New();
        var request = CreateApprovedRequest(productId, 20m);
        var transfer = InventoryTransfer.CreateDraft(
            PosOrganizationId.From(Org),
            PosBranchId.From(Warehouse),
            PosBranchId.From(Branch),
            [new InventoryTransferLineDraft(productId, 20m, "Test Product", UnitOfMeasure.Piece)],
            Actor,
            Utc,
            stockRequestId: request.Id);
        transfer.Dispatch("260906-003", Actor, Utc.AddMinutes(1));
        transfer.Receive(
            [new InventoryTransferReceiveLineDraft(productId, GoodQty: 20m)],
            Actor,
            Utc.AddMinutes(2));

        var coverage = StockRequestDispatchCoverage.Compute(request, [transfer]);
        Assert.Equal(0m, coverage.RemainingToDispatchByProduct[productId.Value]);
        Assert.Equal(20m, coverage.ReceivedByProduct[productId.Value]);
    }

    private static StockRequest CreateApprovedRequest(CatalogProductId productId, decimal qty)
    {
        var request = StockRequest.Create(
            PosOrganizationId.From(Org),
            PosBranchId.From(Branch),
            PosBranchId.From(Warehouse),
            [new StockRequestLineDraft(productId, qty, "Test Product", UnitOfMeasure.Piece)],
            Actor,
            Utc,
            "260906-020");
        request.Approve(Actor, Utc.AddMinutes(1), new Dictionary<Guid, decimal> { [productId.Value] = qty });
        return request;
    }
}
