using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Permissions;

namespace ExItS.PinoyBusinessPOS.UnitTests.Catalog;

public sealed class CatalogProductAvailabilityResolverTests
{
    private static readonly PosOrganizationId Org =
        PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

    private static readonly PosBranchId BranchA =
        PosBranchId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

    private static readonly PosBranchId BranchB =
        PosBranchId.From(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"));

    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-08-31T20:00:00Z");

    [Fact]
    public void PGA_AVL_01_Standard_no_row_is_offered()
    {
        var product = CatalogProduct.Create(Org, "Coke", UnitOfMeasure.Piece, 50m, Now);
        var result = CatalogProductAvailabilityResolver.ResolveOne(BranchA, product, null);
        Assert.True(result.IsOffered);
        Assert.Equal(CatalogProductOfferingReason.DefaultOrganizationStandard, result.Reason);
    }

    [Fact]
    public void PGA_AVL_02_Standard_false_is_not_offered()
    {
        var product = CatalogProduct.Create(Org, "Coke", UnitOfMeasure.Piece, 50m, Now);
        var row = BranchProductAvailability.Create(Org, BranchA, product.Id, false, Now);
        var result = CatalogProductAvailabilityResolver.ResolveOne(BranchA, product, row);
        Assert.False(result.IsOffered);
        Assert.Equal(CatalogProductOfferingReason.ExplicitlyNotOffered, result.Reason);
    }

    [Fact]
    public void PGA_AVL_03_Standard_true_is_offered()
    {
        var product = CatalogProduct.Create(Org, "Coke", UnitOfMeasure.Piece, 50m, Now);
        var row = BranchProductAvailability.Create(Org, BranchA, product.Id, true, Now);
        var result = CatalogProductAvailabilityResolver.ResolveOne(BranchA, product, row);
        Assert.True(result.IsOffered);
        Assert.Equal(CatalogProductOfferingReason.ExplicitlyOffered, result.Reason);
    }

    [Fact]
    public void PGA_AVL_04_Local_at_origin_is_offered()
    {
        var product = CatalogProduct.Create(
            Org, "Bangus", UnitOfMeasure.Kilogram, 180m, Now,
            scope: CatalogProductScope.BranchLocal, originBranchId: BranchA);
        var result = CatalogProductAvailabilityResolver.ResolveOne(BranchA, product, null);
        Assert.True(result.IsOffered);
        Assert.Equal(CatalogProductOfferingReason.BranchLocalOrigin, result.Reason);
    }

    [Fact]
    public void PGA_AVL_05_Local_at_foreign_branch_not_offered()
    {
        var product = CatalogProduct.Create(
            Org, "Bangus", UnitOfMeasure.Kilogram, 180m, Now,
            scope: CatalogProductScope.BranchLocal, originBranchId: BranchA);
        var result = CatalogProductAvailabilityResolver.ResolveOne(BranchB, product, null);
        Assert.False(result.IsOffered);
        Assert.Equal(CatalogProductOfferingReason.BranchLocalForeignBranch, result.Reason);
    }

    [Fact]
    public void PGA_AVL_06_malicious_true_row_does_not_cross_share_Local()
    {
        var product = CatalogProduct.Create(
            Org, "Bangus", UnitOfMeasure.Kilogram, 180m, Now,
            scope: CatalogProductScope.BranchLocal, originBranchId: BranchA);
        var malicious = BranchProductAvailability.Create(Org, BranchB, product.Id, true, Now);
        var result = CatalogProductAvailabilityResolver.ResolveOne(BranchB, product, malicious);
        Assert.False(result.IsOffered);
        Assert.Equal(CatalogProductOfferingReason.BranchLocalForeignBranch, result.Reason);
    }

    [Fact]
    public void PGA_AVL_09_bulk_resolve_uses_in_memory_rows()
    {
        var p1 = CatalogProduct.Create(Org, "A", UnitOfMeasure.Piece, 1m, Now);
        var p2 = CatalogProduct.Create(
            Org, "B", UnitOfMeasure.Piece, 2m, Now,
            scope: CatalogProductScope.BranchLocal, originBranchId: BranchA);
        var rows = new[]
        {
            BranchProductAvailability.Create(Org, BranchA, p1.Id, false, Now)
        };
        var resolver = new CatalogProductAvailabilityResolver(new CountingAvailabilityRepo());
        var results = resolver.Resolve(BranchA, [p1, p2], rows);
        Assert.Equal(2, results.Count);
        Assert.False(results[0].IsOffered);
        Assert.True(results[1].IsOffered);
    }

    private sealed class CountingAvailabilityRepo : IBranchProductAvailabilityRepository
    {
        public int Calls { get; private set; }

        public Task<BranchProductAvailability?> GetAsync(
            PosOrganizationId organizationId,
            PosBranchId branchId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult<BranchProductAvailability?>(null);
        }

        public Task<IReadOnlyList<BranchProductAvailability>> ListByBranchAsync(
            PosOrganizationId organizationId,
            PosBranchId branchId,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult<IReadOnlyList<BranchProductAvailability>>([]);
        }

        public Task<IReadOnlyList<BranchProductAvailability>> ListByProductIdsAsync(
            PosOrganizationId organizationId,
            PosBranchId branchId,
            IReadOnlyCollection<CatalogProductId> productIds,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult<IReadOnlyList<BranchProductAvailability>>([]);
        }

        public Task AddAsync(BranchProductAvailability availability, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task UpdateAsync(BranchProductAvailability availability, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task DeleteAsync(
            PosOrganizationId organizationId,
            PosBranchId branchId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}

public sealed class CatalogProductGovernanceAuthorityTests
{
    private readonly CatalogProductGovernanceAuthority _authority = new();

    private static readonly PosOrganizationId Org =
        PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

    private static readonly PosBranchId BranchA =
        PosBranchId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

    private static readonly PosBranchId BranchB =
        PosBranchId.From(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"));

    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-08-31T20:00:00Z");

    [Fact]
    public void PGA_AUTH_01_Owner_can_edit_Standard()
    {
        var actor = new CatalogGovernanceActor(PosRole.Owner, false, BranchA.Value);
        var product = CatalogProduct.Create(Org, "X", UnitOfMeasure.Piece, 1m, Now);
        Assert.True(_authority.EnsureCanEditMaster(actor, product).IsSuccess);
    }

    [Fact]
    public void PGA_AUTH_02_Admin_can_edit_Standard()
    {
        var actor = new CatalogGovernanceActor(PosRole.Admin, false, null);
        var product = CatalogProduct.Create(Org, "X", UnitOfMeasure.Piece, 1m, Now);
        Assert.True(_authority.EnsureCanEditMaster(actor, product).IsSuccess);
    }

    [Fact]
    public void PGA_AUTH_03_StoreManager_cannot_edit_Standard()
    {
        var actor = new CatalogGovernanceActor(PosRole.StoreManager, false, BranchA.Value);
        var product = CatalogProduct.Create(Org, "X", UnitOfMeasure.Piece, 1m, Now);
        var result = _authority.EnsureCanEditMaster(actor, product);
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.ProductScopeForbidden, result.ErrorCode);
    }

    [Fact]
    public void PGA_AUTH_04_origin_branch_can_edit_Local()
    {
        var actor = new CatalogGovernanceActor(PosRole.StoreManager, false, BranchA.Value);
        var product = CatalogProduct.Create(
            Org, "L", UnitOfMeasure.Piece, 1m, Now,
            scope: CatalogProductScope.BranchLocal, originBranchId: BranchA);
        Assert.True(_authority.EnsureCanEditMaster(actor, product).IsSuccess);
    }

    [Fact]
    public void PGA_AUTH_05_other_branch_cannot_edit_Local()
    {
        var actor = new CatalogGovernanceActor(PosRole.StoreManager, false, BranchB.Value);
        var product = CatalogProduct.Create(
            Org, "L", UnitOfMeasure.Piece, 1m, Now,
            scope: CatalogProductScope.BranchLocal, originBranchId: BranchA);
        var result = _authority.EnsureCanEditMaster(actor, product);
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.ProductOriginBranchForbidden, result.ErrorCode);
    }

    [Fact]
    public void PGA_AUTH_06_Owner_governs_any_Local()
    {
        var actor = new CatalogGovernanceActor(PosRole.Owner, false, BranchB.Value);
        var product = CatalogProduct.Create(
            Org, "L", UnitOfMeasure.Piece, 1m, Now,
            scope: CatalogProductScope.BranchLocal, originBranchId: BranchA);
        Assert.True(_authority.EnsureCanEditMaster(actor, product).IsSuccess);
        Assert.True(_authority.CanPromote(actor));
    }

    [Fact]
    public void PGA_AUTH_07_branch_cannot_create_OrganizationStandard()
    {
        var actor = new CatalogGovernanceActor(PosRole.StoreManager, false, BranchA.Value);
        var result = _authority.ResolveCreateScope(actor, "OrganizationStandard");
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.ProductScopeForbidden, result.ErrorCode);
    }

    [Fact]
    public void PGA_AUTH_08_omitted_scope_branch_creates_Local_at_acting_branch()
    {
        var actor = new CatalogGovernanceActor(PosRole.StoreManager, false, BranchA.Value);
        var result = _authority.ResolveCreateScope(actor, null);
        Assert.True(result.IsSuccess);
        Assert.Equal(CatalogProductScope.BranchLocal, result.Value!.Scope);
        Assert.Equal(BranchA, result.Value.Origin);
    }

    [Fact]
    public void PGA_CREATE_01_Owner_omitted_scope_is_Standard()
    {
        var actor = new CatalogGovernanceActor(PosRole.Owner, false, BranchA.Value);
        var result = _authority.ResolveCreateScope(actor, null);
        Assert.True(result.IsSuccess);
        Assert.Equal(CatalogProductScope.OrganizationStandard, result.Value!.Scope);
        Assert.Null(result.Value.Origin);
    }

    [Fact]
    public void PGA_PRICE_01_StoreManager_cannot_mutate_Standard_price()
    {
        var actor = new CatalogGovernanceActor(PosRole.StoreManager, false, BranchA.Value);
        var product = CatalogProduct.Create(Org, "X", UnitOfMeasure.Piece, 10m, Now);
        var result = _authority.EnsureCanMutateSellingPrice(actor, product);
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.ProductScopeForbidden, result.ErrorCode);
    }

    [Fact]
    public void PGA_PROMOTE_02_StoreManager_denied_promotion()
    {
        var actor = new CatalogGovernanceActor(PosRole.StoreManager, false, BranchA.Value);
        Assert.False(_authority.CanPromote(actor));
    }

    [Fact]
    public void PGA_PROMOTE_01_Owner_can_promote()
    {
        var actor = new CatalogGovernanceActor(PosRole.Owner, false, null);
        Assert.True(_authority.CanPromote(actor));
    }

    [Fact]
    public void PGA_PROMOTE_domain_preserves_identity_and_price()
    {
        var product = CatalogProduct.Create(
            Org, "Fresh Bangus", UnitOfMeasure.Kilogram, 180m, Now,
            sku: "BANGUS-1",
            barcode: "4006381333931",
            scope: CatalogProductScope.BranchLocal,
            originBranchId: BranchA);
        var id = product.Id;
        var created = product.CreatedAtUtc;
        var later = Now.AddMinutes(5);

        product.PromoteToOrganizationStandard(later);

        Assert.Equal(CatalogProductScope.OrganizationStandard, product.Scope);
        Assert.Equal(id, product.Id);
        Assert.Equal(BranchA, product.OriginBranchId);
        Assert.Equal(180m, product.SellingPrice);
        Assert.Equal("BANGUS-1", product.Sku);
        Assert.Equal("4006381333931", product.Barcode);
        Assert.Equal(created, product.CreatedAtUtc);
        Assert.Equal(later, product.UpdatedAtUtc);
    }

    [Fact]
    public void PGA_PROMOTE_10_promoted_Standard_default_offered_at_foreign_branch()
    {
        var product = CatalogProduct.Create(
            Org, "Bangus", UnitOfMeasure.Kilogram, 180m, Now,
            scope: CatalogProductScope.BranchLocal, originBranchId: BranchA);
        product.PromoteToOrganizationStandard(Now.AddSeconds(1));
        var result = CatalogProductAvailabilityResolver.ResolveOne(BranchB, product, null);
        Assert.True(result.IsOffered);
        Assert.Equal(CatalogProductOfferingReason.DefaultOrganizationStandard, result.Reason);
    }

    [Fact]
    public async Task PGA_AVL_09_bulk_resolve_uses_single_repository_call()
    {
        var repo = new CountingAvailabilityRepository();
        var resolver = new CatalogProductAvailabilityResolver(repo);
        var products = Enumerable.Range(0, 25)
            .Select(i => CatalogProduct.Create(Org, $"P{i}", UnitOfMeasure.Piece, 1m + i, Now))
            .ToList();

        var results = await resolver.ResolveForBranchAsync(Org, BranchA, products);

        Assert.Equal(25, results.Count);
        Assert.Equal(1, repo.ListByProductIdsCalls);
        Assert.All(results.Values, r => Assert.True(r.IsOffered));
    }

    [Fact]
    public void PGA_CREATE_05_client_cannot_spoof_origin_via_omitted_scope_uses_acting_branch()
    {
        var actor = new CatalogGovernanceActor(PosRole.StoreManager, false, BranchA.Value);
        var result = _authority.ResolveCreateScope(actor, "BranchLocal");
        Assert.True(result.IsSuccess);
        Assert.Equal(BranchA, result.Value!.Origin);
        Assert.NotEqual(BranchB, result.Value.Origin);
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
