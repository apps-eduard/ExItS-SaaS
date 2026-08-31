using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.UnitTests.Catalog;

public sealed class CatalogProductCommercialOfferingGateTests
{
    private static readonly PosOrganizationId Org =
        PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

    private static readonly PosBranchId BranchA =
        PosBranchId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

    private static readonly PosBranchId BranchB =
        PosBranchId.From(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"));

    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-08-31T20:00:00Z");

    [Fact]
    public async Task PGA_SELL_03_not_offered_Standard_rejected()
    {
        var product = CatalogProduct.Create(Org, "Coke", UnitOfMeasure.Piece, 50m, Now);
        var rows = new List<BranchProductAvailability>
        {
            BranchProductAvailability.Create(Org, BranchA, product.Id, false, Now)
        };
        var resolver = new CatalogProductAvailabilityResolver(new FixedAvailabilityRepository(rows));

        var result = await CatalogProductCommercialOfferingGate.EnsureOfferedAsync(
            resolver, Org, BranchA.Value, [product], CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.ProductNotOfferedAtBranch, result.ErrorCode);
    }

    [Fact]
    public async Task PGA_SELL_04_foreign_Local_rejected()
    {
        var product = CatalogProduct.Create(
            Org, "Bangus", UnitOfMeasure.Kilogram, 180m, Now,
            scope: CatalogProductScope.BranchLocal, originBranchId: BranchA);
        var resolver = new CatalogProductAvailabilityResolver(new FixedAvailabilityRepository([]));

        var result = await CatalogProductCommercialOfferingGate.EnsureOfferedAsync(
            resolver, Org, BranchB.Value, [product], CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.ProductNotOfferedAtBranch, result.ErrorCode);
    }

    [Fact]
    public async Task PGA_ORDER_bulk_multi_line_single_availability_query()
    {
        var products = Enumerable.Range(0, 8)
            .Select(i => CatalogProduct.Create(Org, $"P{i}", UnitOfMeasure.Piece, 1m, Now))
            .ToList();
        var repo = new CountingAvailabilityRepository();
        var resolver = new CatalogProductAvailabilityResolver(repo);

        var result = await CatalogProductCommercialOfferingGate.EnsureOfferedAsync(
            resolver, Org, BranchA.Value, products, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, repo.ListByProductIdsCalls);
    }

    private sealed class FixedAvailabilityRepository : IBranchProductAvailabilityRepository
    {
        private readonly IReadOnlyList<BranchProductAvailability> _rows;

        public FixedAvailabilityRepository(IReadOnlyList<BranchProductAvailability> rows) => _rows = rows;

        public Task AddAsync(BranchProductAvailability availability, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task DeleteAsync(
            PosOrganizationId organizationId,
            PosBranchId branchId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<BranchProductAvailability?> GetAsync(
            PosOrganizationId organizationId,
            PosBranchId branchId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_rows.FirstOrDefault(r =>
                r.OrganizationId == organizationId
                && r.BranchId == branchId
                && r.ProductId == productId));

        public Task<IReadOnlyList<BranchProductAvailability>> ListByBranchAsync(
            PosOrganizationId organizationId,
            PosBranchId branchId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BranchProductAvailability>>(
                _rows.Where(r => r.OrganizationId == organizationId && r.BranchId == branchId).ToList());

        public Task<IReadOnlyList<BranchProductAvailability>> ListByProductIdsAsync(
            PosOrganizationId organizationId,
            PosBranchId branchId,
            IReadOnlyCollection<CatalogProductId> productIds,
            CancellationToken cancellationToken = default)
        {
            var wanted = productIds.Select(p => p.Value).ToHashSet();
            return Task.FromResult<IReadOnlyList<BranchProductAvailability>>(
                _rows.Where(r =>
                        r.OrganizationId == organizationId
                        && r.BranchId == branchId
                        && wanted.Contains(r.ProductId.Value))
                    .ToList());
        }

        public Task UpdateAsync(BranchProductAvailability availability, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class CountingAvailabilityRepository : IBranchProductAvailabilityRepository
    {
        public int ListByProductIdsCalls { get; private set; }

        public Task AddAsync(BranchProductAvailability availability, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task DeleteAsync(
            PosOrganizationId organizationId,
            PosBranchId branchId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<BranchProductAvailability?> GetAsync(
            PosOrganizationId organizationId,
            PosBranchId branchId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<BranchProductAvailability?>(null);

        public Task<IReadOnlyList<BranchProductAvailability>> ListByBranchAsync(
            PosOrganizationId organizationId,
            PosBranchId branchId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BranchProductAvailability>>([]);

        public Task<IReadOnlyList<BranchProductAvailability>> ListByProductIdsAsync(
            PosOrganizationId organizationId,
            PosBranchId branchId,
            IReadOnlyCollection<CatalogProductId> productIds,
            CancellationToken cancellationToken = default)
        {
            ListByProductIdsCalls++;
            return Task.FromResult<IReadOnlyList<BranchProductAvailability>>([]);
        }

        public Task UpdateAsync(BranchProductAvailability availability, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
