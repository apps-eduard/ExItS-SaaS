using ExItS.PinoyBusinessPOS.Application.Reporting;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;

namespace ExItS.PinoyBusinessPOS.UnitTests.Reporting;

public sealed class ManagementOverviewQueryServiceTests
{
    [Fact]
    public async Task GetAsync_uses_clock_business_date_and_does_not_invent_rows()
    {
        var store = new FakeStore();
        var clock = new FixedClock(new DateTimeOffset(2026, 8, 13, 15, 0, 0, TimeSpan.Zero));
        var service = new ManagementOverviewQueryService(store, clock);
        var orgId = Guid.Parse("aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee");

        var dto = await service.GetAsync(orgId);

        Assert.Equal(orgId, store.OrganizationId);
        Assert.Equal(new DateOnly(2026, 8, 13), store.BusinessDate);
        Assert.Equal(new DateOnly(2026, 8, 13), dto.BusinessDate);
        Assert.Equal(12.5m, dto.TodaySalesTotal);
        Assert.Equal(3, dto.TodaySaleCount);
    }

    private sealed class FakeStore : IManagementOverviewReadStore
    {
        public Guid OrganizationId { get; private set; }
        public DateOnly BusinessDate { get; private set; }

        public Task<PosManagementOverviewDto> GetAsync(
            Guid organizationId,
            DateOnly businessDate,
            CancellationToken cancellationToken = default)
        {
            OrganizationId = organizationId;
            BusinessDate = businessDate;
            return Task.FromResult(new PosManagementOverviewDto(
                businessDate,
                12.5m,
                3,
                10m,
                2.5m,
                1m,
                40m,
                2,
                1,
                4,
                0,
                1,
                2));
        }
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
