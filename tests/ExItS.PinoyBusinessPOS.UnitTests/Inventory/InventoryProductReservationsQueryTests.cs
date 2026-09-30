using ExItS.PinoyBusinessPOS.Application.Inventory;

namespace ExItS.PinoyBusinessPOS.UnitTests.Inventory;

public sealed class InventoryProductReservationsQueryTests
{
    [Fact]
    public void Transfer_commitment_totals_do_not_count_as_reserved()
    {
        var productId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var peer = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var now = new DateTimeOffset(2026, 9, 24, 20, 29, 0, TimeSpan.Zero);

        var (outbound, inbound) = InventoryTransferCommitmentTotals.Sum(
        [
            new InventoryTransferOpenCommitment(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "TR-260924-002-R1",
                productId,
                3m,
                "Outbound",
                peer,
                now),
            new InventoryTransferOpenCommitment(
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                "TR-260924-003",
                productId,
                2m,
                "Inbound",
                peer,
                now),
        ]);

        Assert.Equal(3m, outbound);
        Assert.Equal(2m, inbound);
    }

    [Fact]
    public void Empty_commitments_yield_zero_transit_totals()
    {
        var (outbound, inbound) = InventoryTransferCommitmentTotals.Sum([]);
        Assert.Equal(0m, outbound);
        Assert.Equal(0m, inbound);
    }
}
