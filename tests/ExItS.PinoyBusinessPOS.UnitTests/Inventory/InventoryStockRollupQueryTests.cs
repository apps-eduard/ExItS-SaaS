using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.UnitTests.TestDoubles;

namespace ExItS.PinoyBusinessPOS.UnitTests.Inventory;

/// <summary>
/// AREA-02 hierarchical stock read. Areas group and report; the branch keeps stock authority.
/// </summary>
public sealed class InventoryStockRollupQueryTests
{
    private static readonly DateTimeOffset Utc = new(2026, 9, 3, 8, 0, 0, TimeSpan.Zero);
    private static readonly Guid Org = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Product = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Panay = Guid.Parse("aaaa1111-1111-1111-1111-111111111111");
    private static readonly Guid Visayas = Guid.Parse("aaaa2222-2222-2222-2222-222222222222");
    private static readonly Guid MainBranch = Guid.Parse("bbbb1111-1111-1111-1111-111111111111");
    private static readonly Guid IloiloBranch = Guid.Parse("bbbb2222-2222-2222-2222-222222222222");
    private static readonly Guid CebuBranch = Guid.Parse("bbbb3333-3333-3333-3333-333333333333");
    private static readonly Guid ManilaBranch = Guid.Parse("bbbb4444-4444-4444-4444-444444444444");

    [Fact]
    public async Task AREA02_05_organization_values_come_from_account_authority_not_area_sums()
    {
        var harness = Harness.WithAreas();

        var result = await harness.Query.GetProductAsync(Org, Product);

        Assert.True(result.IsSuccess);
        var rollup = result.Value!;
        // Account authority: 120 on hand even though authorized branch balances sum to 90.
        Assert.Equal(120m, rollup.OrganizationOnHandQuantity);
        Assert.Equal(10m, rollup.OrganizationReservedQuantity);
        Assert.Equal(110m, rollup.OrganizationAvailableQuantity);
        Assert.NotEqual(rollup.Areas.Sum(a => a.OnHandQuantity), rollup.OrganizationOnHandQuantity);
    }

    [Fact]
    public async Task AREA02_06_area_values_are_derived_sums_of_branch_balances()
    {
        var harness = Harness.WithAreas();

        var rollup = (await harness.Query.GetProductAsync(Org, Product)).Value!;

        var panay = rollup.Areas.Single(a => a.AreaId == Panay);
        Assert.Equal("PANAY", panay.AreaName);
        Assert.Equal(70m, panay.OnHandQuantity);
        Assert.Equal(6m, panay.ReservedQuantity);
        Assert.Equal(2, panay.Branches.Count);

        var visayas = rollup.Areas.Single(a => a.AreaId == Visayas);
        Assert.Equal(20m, visayas.OnHandQuantity);
        Assert.Equal(0m, visayas.ReservedQuantity);
    }

    [Fact]
    public async Task AREA02_07_available_is_on_hand_minus_reserved_at_every_level()
    {
        var harness = Harness.WithAreas();

        var rollup = (await harness.Query.GetProductAsync(Org, Product)).Value!;

        foreach (var area in rollup.Areas)
        {
            Assert.Equal(area.OnHandQuantity - area.ReservedQuantity, area.AvailableQuantity);
            foreach (var branch in area.Branches)
            {
                Assert.Equal(branch.OnHandQuantity - branch.ReservedQuantity, branch.AvailableQuantity);
            }
        }
    }

    [Fact]
    public async Task AREA02_08_unassigned_branches_roll_up_separately_and_sort_last()
    {
        var harness = Harness.WithAreas();

        var rollup = (await harness.Query.GetProductAsync(Org, Product)).Value!;

        var unassigned = rollup.Areas.Single(a => a.IsUnassigned);
        Assert.Null(unassigned.AreaId);
        Assert.Null(unassigned.AreaName);
        Assert.Equal(ManilaBranch, Assert.Single(unassigned.Branches).BranchId);
        Assert.Equal(rollup.Areas[^1], unassigned);
        Assert.Equal(["PANAY", "VISAYAS"], rollup.Areas.Where(a => !a.IsUnassigned).Select(a => a.AreaName));
    }

    [Fact]
    public async Task AREA02_09_inaccessible_branch_stock_never_reaches_an_area_subtotal()
    {
        // Actor authorized for PANAY only; Cebu and Manila balances exist but stay invisible.
        var harness = Harness.WithAreas(authorized:
        [
            new AuthorizedBranchGrouping(MainBranch, "Main", Panay, "PANAY"),
            new AuthorizedBranchGrouping(IloiloBranch, "Iloilo", Panay, "PANAY")
        ]);

        var rollup = (await harness.Query.GetProductAsync(Org, Product)).Value!;

        var area = Assert.Single(rollup.Areas);
        Assert.Equal(Panay, area.AreaId);
        Assert.Equal(70m, area.OnHandQuantity);
        Assert.DoesNotContain(rollup.Areas.SelectMany(a => a.Branches), b => b.BranchId == CebuBranch);
        Assert.DoesNotContain(rollup.Areas.SelectMany(a => a.Branches), b => b.BranchId == ManilaBranch);
    }

    [Fact]
    public async Task AREA02_10_reads_branch_balances_once_for_the_product()
    {
        var harness = Harness.WithAreas();

        await harness.Query.GetProductAsync(Org, Product);

        Assert.Equal(1, harness.Balances.ListByProductIdsCallCount);
        Assert.Equal(0, harness.Balances.GetCallCount);
    }

    [Fact]
    public async Task AREA02_11_resolves_branch_grouping_once_per_read()
    {
        var harness = Harness.WithAreas();

        await harness.Query.GetProductAsync(Org, Product);

        Assert.Equal(1, harness.Grouping.CallCount);
    }

    [Fact]
    public async Task AREA02_12_untracked_product_reports_not_tracked_without_totals()
    {
        var harness = Harness.WithAreas(tracked: false);

        var rollup = (await harness.Query.GetProductAsync(Org, Product)).Value!;

        Assert.False(rollup.IsTracked);
        Assert.Empty(rollup.Areas);
        Assert.Equal(0m, rollup.OrganizationOnHandQuantity);
        Assert.False(rollup.HasAreas);
    }

    [Fact]
    public async Task AREA02_13_moving_a_branch_between_areas_reshapes_the_rollup_without_touching_stock()
    {
        var harness = Harness.WithAreas();
        var before = (await harness.Query.GetProductAsync(Org, Product)).Value!;
        var iloiloBefore = before.Areas
            .SelectMany(a => a.Branches)
            .Single(b => b.BranchId == IloiloBranch);

        // Same balances, new grouping: Iloilo now reports under VISAYAS.
        harness.Grouping.Rows =
        [
            new AuthorizedBranchGrouping(MainBranch, "Main", Panay, "PANAY"),
            new AuthorizedBranchGrouping(IloiloBranch, "Iloilo", Visayas, "VISAYAS"),
            new AuthorizedBranchGrouping(CebuBranch, "Cebu", Visayas, "VISAYAS"),
            new AuthorizedBranchGrouping(ManilaBranch, "Manila", null, null)
        ];

        var after = (await harness.Query.GetProductAsync(Org, Product)).Value!;

        Assert.Equal(40m, after.Areas.Single(a => a.AreaId == Panay).OnHandQuantity);
        Assert.Equal(50m, after.Areas.Single(a => a.AreaId == Visayas).OnHandQuantity);
        var iloiloAfter = after.Areas.SelectMany(a => a.Branches).Single(b => b.BranchId == IloiloBranch);
        Assert.Equal(iloiloBefore.OnHandQuantity, iloiloAfter.OnHandQuantity);
        Assert.Equal(iloiloBefore.ReservedQuantity, iloiloAfter.ReservedQuantity);
        Assert.Equal(before.OrganizationOnHandQuantity, after.OrganizationOnHandQuantity);
        Assert.Empty(harness.Balances.Upserts);
    }

    [Fact]
    public async Task AREA02_14_unknown_product_fails_without_reading_balances()
    {
        var harness = Harness.WithAreas();

        var result = await harness.Query.GetProductAsync(Org, Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(0, harness.Balances.ListByProductIdsCallCount);
    }

    [Fact]
    public async Task AREA02_18_organization_without_areas_keeps_a_single_flat_branch_group()
    {
        var harness = Harness.WithAreas(authorized:
        [
            new AuthorizedBranchGrouping(MainBranch, "Main", null, null),
            new AuthorizedBranchGrouping(IloiloBranch, "Iloilo", null, null)
        ]);

        var rollup = (await harness.Query.GetProductAsync(Org, Product)).Value!;

        Assert.False(rollup.HasAreas);
        var only = Assert.Single(rollup.Areas);
        Assert.True(only.IsUnassigned);
        Assert.Equal(2, only.Branches.Count);
    }

    [Fact]
    public async Task AREAH_09_authorized_branch_without_a_balance_row_reports_zero_inside_its_area()
    {
        // Iloilo is authorized under PANAY but has never held stock for this product.
        var harness = Harness.WithAreas(branchesWithBalances: [MainBranch, CebuBranch, ManilaBranch]);

        var rollup = (await harness.Query.GetProductAsync(Org, Product)).Value!;

        var panay = rollup.Areas.Single(a => a.AreaId == Panay);
        var iloilo = panay.Branches.Single(b => b.BranchId == IloiloBranch);
        Assert.Equal(0m, iloilo.OnHandQuantity);
        Assert.Equal(0m, iloilo.ReservedQuantity);
        Assert.Equal(0m, iloilo.AvailableQuantity);
        Assert.Equal(40m, panay.OnHandQuantity);
    }

    [Fact]
    public async Task AREAH_10_unassigned_branch_without_a_balance_row_still_appears()
    {
        var harness = Harness.WithAreas(branchesWithBalances: [MainBranch]);

        var rollup = (await harness.Query.GetProductAsync(Org, Product)).Value!;

        var unassigned = rollup.Areas.Single(a => a.IsUnassigned);
        var manila = Assert.Single(unassigned.Branches);
        Assert.Equal(ManilaBranch, manila.BranchId);
        Assert.Equal(0m, manila.OnHandQuantity);
        Assert.Equal(0m, unassigned.AvailableQuantity);
    }

    [Fact]
    public async Task AREAH_11_area_group_structure_survives_when_no_branch_holds_stock()
    {
        var harness = Harness.WithAreas(branchesWithBalances: []);

        var rollup = (await harness.Query.GetProductAsync(Org, Product)).Value!;

        Assert.True(rollup.HasAreas);
        Assert.Equal(["PANAY", "VISAYAS"], rollup.Areas.Where(a => !a.IsUnassigned).Select(a => a.AreaName));
        Assert.Equal(4, rollup.Areas.Sum(a => a.Branches.Count));
        Assert.All(rollup.Areas, area => Assert.Equal(0m, area.OnHandQuantity));
        // Reads never materialize the missing balance rows.
        Assert.Empty(harness.Balances.Upserts);
    }

    [Fact]
    public async Task AREAH_12_organization_wide_viewer_sees_authoritative_account_totals()
    {
        var harness = Harness.WithAreas(organizationWide: true);

        var rollup = (await harness.Query.GetProductAsync(Org, Product)).Value!;

        Assert.True(rollup.OrganizationTotalsVisible);
        Assert.Equal(120m, rollup.OrganizationOnHandQuantity);
        Assert.Equal(10m, rollup.OrganizationReservedQuantity);
        Assert.Equal(110m, rollup.OrganizationAvailableQuantity);
    }

    [Fact]
    public async Task AREAH_13_partial_access_staff_never_receive_organization_totals()
    {
        // Area-scoped staff: authorized for every branch, yet still not organization-wide.
        var harness = Harness.WithAreas(organizationWide: false);

        var rollup = (await harness.Query.GetProductAsync(Org, Product)).Value!;

        Assert.False(rollup.OrganizationTotalsVisible);
        Assert.Null(rollup.OrganizationOnHandQuantity);
        Assert.Null(rollup.OrganizationReservedQuantity);
        Assert.Null(rollup.OrganizationAvailableQuantity);
    }

    [Fact]
    public async Task AREAH_14_partial_access_accessible_totals_are_derived_from_authorized_branches_only()
    {
        var harness = Harness.WithAreas(
            authorized:
            [
                new AuthorizedBranchGrouping(MainBranch, "Main", Panay, "PANAY"),
                new AuthorizedBranchGrouping(IloiloBranch, "Iloilo", Panay, "PANAY")
            ],
            organizationWide: false);

        var rollup = (await harness.Query.GetProductAsync(Org, Product)).Value!;

        Assert.Equal(70m, rollup.AccessibleOnHandQuantity);
        Assert.Equal(6m, rollup.AccessibleReservedQuantity);
        Assert.Equal(64m, rollup.AccessibleAvailableQuantity);
        Assert.Equal(rollup.Areas.Sum(a => a.OnHandQuantity), rollup.AccessibleOnHandQuantity);
        Assert.Null(rollup.OrganizationOnHandQuantity);
    }

    [Fact]
    public async Task AREAH_15_untracked_product_hides_organization_totals_from_partial_access_staff()
    {
        var harness = Harness.WithAreas(tracked: false, organizationWide: false);

        var rollup = (await harness.Query.GetProductAsync(Org, Product)).Value!;

        Assert.False(rollup.IsTracked);
        Assert.False(rollup.OrganizationTotalsVisible);
        Assert.Null(rollup.OrganizationOnHandQuantity);
        Assert.Equal(0m, rollup.AccessibleOnHandQuantity);
    }

    [Fact]
    public async Task AREAH_16_zero_stock_shape_still_costs_one_balance_read_and_one_grouping_read()
    {
        var harness = Harness.WithAreas(branchesWithBalances: []);

        await harness.Query.GetProductAsync(Org, Product);

        Assert.Equal(1, harness.Balances.ListByProductIdsCallCount);
        Assert.Equal(0, harness.Balances.GetCallCount);
        Assert.Equal(1, harness.Grouping.CallCount);
    }

    private sealed class Harness
    {
        public required InventoryStockRollupQuery Query { get; init; }
        public required RollupBalances Balances { get; init; }
        public required RollupGrouping Grouping { get; init; }

        public static Harness WithAreas(
            IReadOnlyList<AuthorizedBranchGrouping>? authorized = null,
            bool tracked = true,
            bool organizationWide = true,
            IReadOnlyList<Guid>? branchesWithBalances = null)
        {
            var productId = CatalogProductId.From(Product);
            var orgId = PosOrganizationId.From(Org);
            var products = new RollupProducts(CatalogProduct.Create(
                orgId,
                "Milk 1L",
                UnitOfMeasure.Piece,
                55m,
                Utc,
                id: productId));
            var inventory = new RollupInventory(InventoryAccount.Rehydrate(
                InventoryAccountId.New(),
                orgId,
                productId,
                tracked,
                reorderLevel: null,
                reorderQuantity: null,
                onHandQuantity: tracked ? 120m : 0m,
                createdAtUtc: Utc,
                updatedAtUtc: Utc,
                reservedQuantity: tracked ? 10m : 0m));
            var allBalances = new[]
            {
                Balance(MainBranch, 40m, 6m),
                Balance(IloiloBranch, 30m, 0m),
                Balance(CebuBranch, 20m, 0m),
                Balance(ManilaBranch, 15m, 5m)
            };
            var balances = new RollupBalances();
            balances.Items.AddRange(branchesWithBalances is null
                ? allBalances
                : allBalances.Where(b => branchesWithBalances.Contains(b.BranchId.Value)));
            var grouping = new RollupGrouping
            {
                Rows = authorized ??
                [
                    new AuthorizedBranchGrouping(MainBranch, "Main", Panay, "PANAY"),
                    new AuthorizedBranchGrouping(IloiloBranch, "Iloilo", Panay, "PANAY"),
                    new AuthorizedBranchGrouping(CebuBranch, "Cebu", Visayas, "VISAYAS"),
                    new AuthorizedBranchGrouping(ManilaBranch, "Manila", null, null)
                ],
                IsOrganizationWide = organizationWide
            };

            return new Harness
            {
                Query = new InventoryStockRollupQuery(inventory, products, balances, grouping),
                Balances = balances,
                Grouping = grouping
            };
        }

        private static InventoryBranchBalance Balance(Guid branchId, decimal onHand, decimal reserved) =>
            InventoryBranchBalance.Rehydrate(
                PosOrganizationId.From(Org),
                PosBranchId.From(branchId),
                CatalogProductId.From(Product),
                onHand,
                Utc,
                reserved);
    }

    private sealed class RollupInventory(InventoryAccount account) : CostResolverInventoryStub
    {
        public override Task<InventoryAccount?> GetByProductIdAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(productId == account.ProductId ? account : null);
    }

    private sealed class RollupProducts(CatalogProduct product) : ICatalogProductRepository
    {
        public Task<CatalogProduct?> GetByIdAsync(
            PosOrganizationId organizationId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(productId == product.Id ? product : null);

        public Task<IReadOnlyList<CatalogProduct>> ListByIdsAsync(PosOrganizationId organizationId, IReadOnlyCollection<CatalogProductId> productIds, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<CatalogProduct?> FindByNormalizedSkuAsync(PosOrganizationId organizationId, string normalizedSku, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<CatalogProduct?> FindByBarcodeAsync(PosOrganizationId organizationId, string barcode, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<CatalogProduct> Items, int TotalCount)> ListAsync(PosOrganizationId organizationId, CatalogProductFilter filter, int skip, int take, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Guid>> ListIdsAsync(PosOrganizationId organizationId, CatalogProductFilter filter, int skip, int take, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(int TotalCount, int AvailableCount, int NotAvailableCount)> CountConnectedBuyerAvailabilityAsync(PosOrganizationId organizationId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<(Guid? CategoryId, int Count)>> ListConnectedBuyerAvailabilityCategoryFacetsAsync(PosOrganizationId organizationId, CatalogProductFilter filter, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<CatalogProduct?> FindByPlatformGlobalProductIdAsync(PosOrganizationId organizationId, Guid platformGlobalProductId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlySet<Guid>> ListPlatformGlobalProductIdsAsync(PosOrganizationId organizationId, IReadOnlyCollection<Guid> platformGlobalProductIds, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAsync(CatalogProduct product, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(CatalogProduct product, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class RollupBalances : IInventoryBranchBalanceRepository
    {
        public List<InventoryBranchBalance> Items { get; } = [];
        public List<InventoryBranchBalance> Upserts { get; } = [];
        public int ListByProductIdsCallCount { get; private set; }
        public int GetCallCount { get; private set; }

        public Task<InventoryBranchBalance?> GetAsync(
            PosOrganizationId organizationId,
            PosBranchId branchId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default)
        {
            GetCallCount++;
            return Task.FromResult(Items.FirstOrDefault(b => b.BranchId == branchId && b.ProductId == productId));
        }

        public Task<IReadOnlyList<InventoryBranchBalance>> ListByProductIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<CatalogProductId> productIds,
            CancellationToken cancellationToken = default)
        {
            ListByProductIdsCallCount++;
            return Task.FromResult<IReadOnlyList<InventoryBranchBalance>>(
                Items.Where(b => productIds.Contains(b.ProductId)).ToList());
        }

        public Task UpsertAsync(InventoryBranchBalance balance, CancellationToken cancellationToken = default)
        {
            Upserts.Add(balance);
            return Task.CompletedTask;
        }
    }

    private sealed class RollupGrouping : IAuthorizedBranchGroupingDirectory
    {
        public required IReadOnlyList<AuthorizedBranchGrouping> Rows { get; set; }
        public bool IsOrganizationWide { get; set; } = true;
        public int CallCount { get; private set; }

        public Task<AuthorizedBranchScope> ListAuthorizedAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(new AuthorizedBranchScope(IsOrganizationWide, Rows));
        }
    }
}
