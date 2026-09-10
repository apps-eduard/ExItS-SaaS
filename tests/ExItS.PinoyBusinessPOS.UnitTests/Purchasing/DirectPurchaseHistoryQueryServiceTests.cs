using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Purchasing;

namespace ExItS.PinoyBusinessPOS.UnitTests.Purchasing;

public sealed class DirectPurchaseHistoryQueryServiceTests
{
    private static readonly Guid BuyerOrg = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid SellerOrg = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid OtherOrg = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid SaleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid LocalId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task List_maps_local_and_b2b_rows()
    {
        var query = new FakeHistoryQuery(
            [
                new DirectPurchaseHistoryRawRow(
                    SaleId,
                    DirectPurchaseHistorySourceTypes.B2B,
                    new DateTimeOffset(2026, 9, 10, 8, 0, 0, TimeSpan.Zero),
                    new DateOnly(2026, 9, 10),
                    "Mica Store",
                    SellerOrg,
                    "ORG000001",
                    "TXN-001245",
                    2,
                    1250m,
                    "Completed",
                    "Cash",
                    "Main Branch"),
                new DirectPurchaseHistoryRawRow(
                    LocalId,
                    DirectPurchaseHistorySourceTypes.Local,
                    new DateTimeOffset(2026, 9, 9, 0, 0, 0, TimeSpan.Zero),
                    new DateOnly(2026, 9, 9),
                    "Public Market",
                    null,
                    null,
                    "DPR-000044",
                    5,
                    2100m,
                    "Completed",
                    null,
                    null)
            ],
            total: 2);

        var service = new DirectPurchaseHistoryQueryService(query);
        var result = await service.ListAsync(BuyerOrg, new DirectPurchaseHistoryFilter(), 1, 20);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal(2, result.Value!.TotalCount);
        Assert.Equal(DirectPurchaseHistorySourceTypes.B2B, result.Value.Items[0].SourceType);
        Assert.Equal(1250m, result.Value.Items[0].TotalAmount);
        Assert.Equal(DirectPurchaseHistorySourceTypes.Local, result.Value.Items[1].SourceType);
        Assert.Equal("2026-09-09", result.Value.Items[1].PurchaseDate);
    }

    [Fact]
    public async Task List_rejects_invalid_source_type()
    {
        var service = new DirectPurchaseHistoryQueryService(new FakeHistoryQuery([], 0));
        var result = await service.ListAsync(
            BuyerOrg,
            new DirectPurchaseHistoryFilter(SourceType: "CustomerOrder"),
            1,
            20);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.DomainViolation, result.ErrorCode);
    }

    [Fact]
    public async Task B2b_detail_fails_closed_when_missing()
    {
        var service = new DirectPurchaseHistoryQueryService(new FakeHistoryQuery([], 0));
        var result = await service.GetB2bDetailAsync(BuyerOrg, SaleId);
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.SaleNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task B2b_detail_returns_buyer_safe_projection()
    {
        var detail = new DirectPurchaseB2bDetailDto(
            SaleId,
            SellerOrg,
            "ORG000001",
            "Mica Store",
            "Main Branch",
            "TXN-001245",
            new DateTimeOffset(2026, 9, 10, 8, 0, 0, TimeSpan.Zero),
            "Completed",
            "Cash",
            1250m,
            0m,
            0m,
            1250m,
            [
                new DirectPurchaseB2bLineDto(1, "Coke", "SKU-C", null, 5m, "Piece", 50m, 0m, 250m),
                new DirectPurchaseB2bLineDto(2, "Rice", "SKU-R", null, 10m, "Kg", 100m, 0m, 1000m)
            ]);
        var service = new DirectPurchaseHistoryQueryService(new FakeHistoryQuery([], 0, detail));
        var result = await service.GetB2bDetailAsync(BuyerOrg, SaleId);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal(1250m, result.Value!.TotalAmount);
        Assert.Equal(2, result.Value.Lines.Count);
        Assert.DoesNotContain(
            typeof(DirectPurchaseB2bDetailDto).GetProperties().Select(p => p.Name),
            name => name.Contains("Cost", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Profit", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Margin", StringComparison.OrdinalIgnoreCase));
        Assert.NotEqual(OtherOrg, result.Value.SellerOrganizationId);
    }

    private sealed class FakeHistoryQuery : IDirectPurchaseHistoryQuery
    {
        private readonly IReadOnlyList<DirectPurchaseHistoryRawRow> _rows;
        private readonly int _total;
        private readonly DirectPurchaseB2bDetailDto? _detail;

        public FakeHistoryQuery(
            IReadOnlyList<DirectPurchaseHistoryRawRow> rows,
            int total,
            DirectPurchaseB2bDetailDto? detail = null)
        {
            _rows = rows;
            _total = total;
            _detail = detail;
        }

        public Task<(IReadOnlyList<DirectPurchaseHistoryRawRow> Items, int TotalCount)> ListAsync(
            Guid buyerOrganizationId,
            DirectPurchaseHistoryFilter filter,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult((_rows, _total));

        public Task<DirectPurchaseB2bDetailDto?> GetB2bDetailAsync(
            Guid buyerOrganizationId,
            Guid saleId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(buyerOrganizationId == BuyerOrg && saleId == SaleId ? _detail : null);
    }
}
