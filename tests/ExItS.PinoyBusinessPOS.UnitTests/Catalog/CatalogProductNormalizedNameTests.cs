using System.Text;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.UnitTests.Catalog;

public sealed class CatalogProductNormalizedNameDomainTests
{
    private static readonly PosOrganizationId Org =
        PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-09-01T00:00:00Z");

    [Theory]
    [InlineData("Coke 1L", "Coke 1L", "COKE 1L")]
    [InlineData(" coke 1l ", "coke 1l", "COKE 1L")]
    [InlineData("Coke   1L", "Coke 1L", "COKE 1L")]
    [InlineData("Coke\t1L", "Coke 1L", "COKE 1L")]
    [InlineData(" COKE 1L ", "COKE 1L", "COKE 1L")]
    public void PNAME_DOM_normalize_display_and_identity(string input, string display, string normalized)
    {
        var (d, n) = CatalogProduct.NormalizeProductName(input);
        Assert.Equal(display, d);
        Assert.Equal(normalized, n);
    }

    [Fact]
    public void PNAME_DOM_05_UpdateDetails_maintains_NormalizedName()
    {
        var product = CatalogProduct.Create(Org, "Pepsi 1L", UnitOfMeasure.Piece, 40m, Now);
        product.UpdateDetails(" coke   1l ", null, null, null, null, null, UnitOfMeasure.Piece, 50m, Now);
        Assert.Equal("coke 1l", product.Name);
        Assert.Equal("COKE 1L", product.NormalizedName);
    }

    [Fact]
    public void PNAME_DOM_06_Rehydrate_preserves_normalized_identity()
    {
        var id = CatalogProductId.New();
        var product = CatalogProduct.Rehydrate(
            id, Org, "Coke 1L", null, null, null, null, null,
            UnitOfMeasure.Piece, 50m, CatalogProductStatus.Active, Now, Now,
            normalizedName: "COKE 1L");
        Assert.Equal("COKE 1L", product.NormalizedName);
    }

    [Fact]
    public void PNAME_DOM_07_different_legitimate_names_remain_different()
    {
        var a = CatalogProduct.NormalizeProductName("Coke 1L").Normalized;
        var b = CatalogProduct.NormalizeProductName("Coke Zero 1L").Normalized;
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void PNAME_DOM_unicode_nfc_equivalence()
    {
        var composed = "Café";
        var decomposed = "Cafe\u0301";
        var a = CatalogProduct.NormalizeProductName(composed).Normalized;
        var b = CatalogProduct.NormalizeProductName(decomposed).Normalized;
        Assert.Equal(a, b);
        Assert.Equal(composed.Normalize(NormalizationForm.FormC).ToUpperInvariant(), a);
    }
}

public sealed class CatalogProductNormalizedNameApplicationTests
{
    private static readonly PosOrganizationId Org =
        PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly PosBranchId BranchA =
        PosBranchId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly PosBranchId BranchB =
        PosBranchId.From(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"));
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-09-01T00:00:00Z");

    [Fact]
    public async Task PNAME_APP_01_Standard_same_normalized_Standard_create_conflicts()
    {
        var existing = CatalogProduct.Create(Org, "Coke 1L", UnitOfMeasure.Piece, 50m, Now);
        var conflict = await CatalogAssignment.FindProductNameConflictAsync(
            new MemoryProducts([existing]), Org, "COKE 1L", null, default);
        Assert.NotNull(conflict);
        Assert.Equal(ApplicationErrorCodes.ProductNameConflict, conflict!.ErrorCode);
    }

    [Fact]
    public async Task PNAME_APP_02_Standard_vs_Local_normalized_conflicts()
    {
        var existing = CatalogProduct.Create(Org, "Coke 1L", UnitOfMeasure.Piece, 50m, Now);
        var conflict = await CatalogAssignment.FindProductNameConflictAsync(
            new MemoryProducts([existing]), Org, "COKE 1L", null, default);
        Assert.NotNull(conflict);
    }

    [Fact]
    public async Task PNAME_APP_03_Local_cross_branch_conflicts()
    {
        var existing = CatalogProduct.Create(
            Org, "Fresh Bangus", UnitOfMeasure.Kilogram, 180m, Now,
            scope: CatalogProductScope.BranchLocal, originBranchId: BranchA);
        var conflict = await CatalogAssignment.FindProductNameConflictAsync(
            new MemoryProducts([existing]), Org, "FRESH BANGUS", null, default);
        Assert.NotNull(conflict);
        Assert.Equal(ApplicationErrorCodes.ProductNameConflict, conflict!.ErrorCode);
    }

    [Fact]
    public async Task PNAME_APP_04_Inactive_reserves_name()
    {
        var existing = CatalogProduct.Create(Org, "Coke 1L", UnitOfMeasure.Piece, 50m, Now);
        existing.Deactivate(Now);
        var conflict = await CatalogAssignment.FindProductNameConflictAsync(
            new MemoryProducts([existing]), Org, "COKE 1L", null, default);
        Assert.NotNull(conflict);
    }

    [Fact]
    public async Task PNAME_APP_05_self_rename_spacing_allowed()
    {
        var product = CatalogProduct.Create(Org, "Coke 1L", UnitOfMeasure.Piece, 50m, Now);
        var conflict = await CatalogAssignment.FindProductNameConflictAsync(
            new MemoryProducts([product]), Org, product.NormalizedName, product.Id, default);
        Assert.Null(conflict);
    }

    [Fact]
    public async Task PNAME_APP_06_rename_to_other_product_name_conflicts()
    {
        var a = CatalogProduct.Create(Org, "Coke 1L", UnitOfMeasure.Piece, 50m, Now);
        var b = CatalogProduct.Create(Org, "Pepsi 1L", UnitOfMeasure.Piece, 40m, Now);
        var conflict = await CatalogAssignment.FindProductNameConflictAsync(
            new MemoryProducts([a, b]), Org, "COKE 1L", b.Id, default);
        Assert.NotNull(conflict);
        Assert.Equal(ApplicationErrorCodes.ProductNameConflict, conflict!.ErrorCode);
    }

    [Fact]
    public async Task PNAME_APP_07_different_organizations_same_name_allowed()
    {
        var org2 = PosOrganizationId.From(Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"));
        var a = CatalogProduct.Create(Org, "Coke 1L", UnitOfMeasure.Piece, 50m, Now);
        var conflict = await CatalogAssignment.FindProductNameConflictAsync(
            new MemoryProducts([a]), org2, "COKE 1L", null, default);
        Assert.Null(conflict);
    }

    [Fact]
    public void PNAME_PRIV_01_foreign_Local_not_viewable_by_other_branch_actor()
    {
        var authority = new CatalogProductGovernanceAuthority();
        var actor = FixedCatalogGovernanceActorAccessor.StoreManager(BranchB.Value).GetActor();
        Assert.False(authority.CanViewBranchLocalInManagement(actor, BranchA));
        Assert.True(authority.CanViewBranchLocalInManagement(
            FixedCatalogGovernanceActorAccessor.Owner().GetActor(), BranchA));
    }

    private sealed class MemoryProducts(IEnumerable<CatalogProduct> items) : ICatalogProductRepository
    {
        private readonly List<CatalogProduct> _items = items.ToList();

        public Task AddAsync(CatalogProduct product, CancellationToken cancellationToken = default)
        {
            _items.Add(product);
            return Task.CompletedTask;
        }

        public Task<CatalogProduct?> FindByBarcodeAsync(
            PosOrganizationId organizationId, string barcode, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(p => p.OrganizationId == organizationId && p.Barcode == barcode));

        public Task<CatalogProduct?> FindByNormalizedNameAsync(
            PosOrganizationId organizationId, string normalizedName, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(p =>
                p.OrganizationId == organizationId && p.NormalizedName == normalizedName));

        public Task<CatalogProduct?> FindByNormalizedSkuAsync(
            PosOrganizationId organizationId, string normalizedSku, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(p =>
                p.OrganizationId == organizationId && p.NormalizedSku == normalizedSku));

        public Task<CatalogProduct?> FindByPlatformGlobalProductIdAsync(
            PosOrganizationId organizationId, Guid platformGlobalProductId, CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogProduct?>(null);

        public Task<CatalogProduct?> GetByIdAsync(
            PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(p => p.OrganizationId == organizationId && p.Id == productId));

        public Task<(IReadOnlyList<CatalogProduct> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId, CatalogProductFilter filter, int skip, int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<CatalogProduct>, int)>(
                (_items.Where(p => p.OrganizationId == organizationId).Skip(skip).Take(take).ToList(), _items.Count));

        public Task<IReadOnlyList<CatalogProduct>> ListByIdsAsync(
            PosOrganizationId organizationId, IReadOnlyCollection<CatalogProductId> productIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CatalogProduct>>(
                _items.Where(p => p.OrganizationId == organizationId && productIds.Contains(p.Id)).ToList());

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
}
