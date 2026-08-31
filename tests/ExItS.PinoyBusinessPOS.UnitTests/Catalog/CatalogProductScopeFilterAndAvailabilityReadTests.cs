using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Permissions;

namespace ExItS.PinoyBusinessPOS.UnitTests.Catalog;

public sealed class CatalogProductScopeFilterAndAvailabilityReadTests
{
    private static readonly Guid OrgGuid = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly PosOrganizationId Org = PosOrganizationId.From(OrgGuid);
    private static readonly PosBranchId BranchA =
        PosBranchId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly PosBranchId BranchB =
        PosBranchId.From(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"));
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-09-01T00:00:00Z");

    [Fact]
    public void Scope_and_origin_filters_are_part_of_membership_model_before_pagination()
    {
        var filter = new CatalogProductFilter(
            Scope: CatalogProductScope.BranchLocal,
            OriginBranchId: BranchA.Value,
            RestrictBranchLocalToActingBranch: true,
            ActingBranchId: BranchA.Value);

        Assert.Equal(CatalogProductScope.BranchLocal, filter.Scope);
        Assert.Equal(BranchA.Value, filter.OriginBranchId);
        Assert.True(filter.RestrictBranchLocalToActingBranch);
    }

    [Fact]
    public async Task QueryProductBranchAvailability_returns_sparse_overrides_for_Standard()
    {
        var product = CatalogProduct.Create(Org, "Coke", UnitOfMeasure.Piece, 50m, Now);
        var overrideRow = BranchProductAvailability.Create(Org, BranchB, product.Id, false, Now);
        var useCase = new QueryProductBranchAvailability(
            new MemoryProducts([product]),
            new MemoryAvailability([overrideRow]),
            new CatalogProductGovernanceAuthority(),
            FixedCatalogGovernanceActorAccessor.Owner());

        var result = await useCase.ExecuteAsync(OrgGuid, product.Id.Value);

        Assert.True(result.IsSuccess);
        Assert.Equal(nameof(CatalogProductScope.OrganizationStandard), result.Value!.Scope);
        Assert.Single(result.Value.ExplicitRows);
        Assert.Equal(BranchB.Value, result.Value.ExplicitRows[0].BranchId);
        Assert.False(result.Value.ExplicitRows[0].IsOffered);
    }

    [Fact]
    public async Task QueryProductBranchAvailability_BranchLocal_returns_origin_only()
    {
        var local = CatalogProduct.Create(
            Org, "Bangus", UnitOfMeasure.Kilogram, 180m, Now,
            scope: CatalogProductScope.BranchLocal, originBranchId: BranchA);
        var useCase = new QueryProductBranchAvailability(
            new MemoryProducts([local]),
            new MemoryAvailability([]),
            new CatalogProductGovernanceAuthority(),
            FixedCatalogGovernanceActorAccessor.Owner());

        var result = await useCase.ExecuteAsync(OrgGuid, local.Id.Value);

        Assert.True(result.IsSuccess);
        Assert.Equal(nameof(CatalogProductScope.BranchLocal), result.Value!.Scope);
        Assert.Equal(BranchA.Value, result.Value.OriginBranchId);
        Assert.Single(result.Value.ExplicitRows);
        Assert.True(result.Value.ExplicitRows[0].IsOffered);
        Assert.Equal(BranchA.Value, result.Value.ExplicitRows[0].BranchId);
    }

    [Fact]
    public async Task QueryProductBranchAvailability_foreign_Local_hidden_from_branch_actor()
    {
        var local = CatalogProduct.Create(
            Org, "Bangus", UnitOfMeasure.Kilogram, 180m, Now,
            scope: CatalogProductScope.BranchLocal, originBranchId: BranchA);
        var useCase = new QueryProductBranchAvailability(
            new MemoryProducts([local]),
            new MemoryAvailability([]),
            new CatalogProductGovernanceAuthority(),
            FixedCatalogGovernanceActorAccessor.StoreManager(BranchB.Value));

        var result = await useCase.ExecuteAsync(OrgGuid, local.Id.Value);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.ProductNotFound, result.ErrorCode);
    }

    private sealed class MemoryProducts(IReadOnlyList<CatalogProduct> items) : ICatalogProductRepository
    {
        public Task AddAsync(CatalogProduct product, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<CatalogProduct?> FindByBarcodeAsync(
            PosOrganizationId organizationId, string barcode, CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogProduct?>(null);

        public Task<CatalogProduct?> FindByNormalizedSkuAsync(
            PosOrganizationId organizationId, string normalizedSku, CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogProduct?>(null);

        public Task<CatalogProduct?> FindByPlatformGlobalProductIdAsync(
            PosOrganizationId organizationId, Guid platformGlobalProductId, CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogProduct?>(null);

        public Task<CatalogProduct?> GetByIdAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(items.FirstOrDefault(p => p.OrganizationId == organizationId && p.Id == productId));

        public Task<(IReadOnlyList<CatalogProduct> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            CatalogProductFilter filter,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<CatalogProduct> q = items.Where(p => p.OrganizationId == organizationId);
            if (filter.Scope is not null)
            {
                q = q.Where(p => p.Scope == filter.Scope);
            }

            if (filter.OriginBranchId is not null)
            {
                q = q.Where(p => p.OriginBranchId?.Value == filter.OriginBranchId);
            }

            var list = q.OrderBy(p => p.Name).ThenBy(p => p.Id.Value).ToList();
            return Task.FromResult<(IReadOnlyList<CatalogProduct>, int)>(
                (list.Skip(skip).Take(take).ToList(), list.Count));
        }

        public Task<IReadOnlyList<CatalogProduct>> ListByIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<CatalogProductId> productIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CatalogProduct>>(
                items.Where(p => p.OrganizationId == organizationId && productIds.Contains(p.Id)).ToList());

        public Task<(int TotalCount, int AvailableCount, int NotAvailableCount)> CountConnectedBuyerAvailabilityAsync(
            PosOrganizationId organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult((0, 0, 0));

        public Task<IReadOnlyList<(Guid? CategoryId, int Count)>> ListConnectedBuyerAvailabilityCategoryFacetsAsync(
            PosOrganizationId organizationId, CatalogProductFilter filter, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<(Guid?, int)>>([]);

        public Task<IReadOnlyList<Guid>> ListIdsAsync(
            PosOrganizationId organizationId, CatalogProductFilter filter, int skip, int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);

        public Task<IReadOnlySet<Guid>> ListPlatformGlobalProductIdsAsync(
            PosOrganizationId organizationId, IReadOnlyCollection<Guid> platformGlobalProductIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlySet<Guid>>(new HashSet<Guid>());

        public Task UpdateAsync(CatalogProduct product, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class MemoryAvailability(IReadOnlyList<BranchProductAvailability> rows)
        : IBranchProductAvailabilityRepository
    {
        public Task AddAsync(BranchProductAvailability availability, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task DeleteAsync(
            PosOrganizationId organizationId, PosBranchId branchId, CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<BranchProductAvailability?> GetAsync(
            PosOrganizationId organizationId, PosBranchId branchId, CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(rows.FirstOrDefault(r =>
                r.OrganizationId == organizationId && r.BranchId == branchId && r.ProductId == productId));

        public Task<IReadOnlyList<BranchProductAvailability>> ListByBranchAsync(
            PosOrganizationId organizationId, PosBranchId branchId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BranchProductAvailability>>(
                rows.Where(r => r.OrganizationId == organizationId && r.BranchId == branchId).ToList());

        public Task<IReadOnlyList<BranchProductAvailability>> ListByProductAsync(
            PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BranchProductAvailability>>(
                rows.Where(r => r.OrganizationId == organizationId && r.ProductId == productId).ToList());

        public Task<IReadOnlyList<BranchProductAvailability>> ListByProductIdsAsync(
            PosOrganizationId organizationId, PosBranchId branchId, IReadOnlyCollection<CatalogProductId> productIds,
            CancellationToken cancellationToken = default)
        {
            var wanted = productIds.Select(p => p.Value).ToHashSet();
            return Task.FromResult<IReadOnlyList<BranchProductAvailability>>(
                rows.Where(r =>
                        r.OrganizationId == organizationId
                        && r.BranchId == branchId
                        && wanted.Contains(r.ProductId.Value))
                    .ToList());
        }

        public Task UpdateAsync(BranchProductAvailability availability, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
