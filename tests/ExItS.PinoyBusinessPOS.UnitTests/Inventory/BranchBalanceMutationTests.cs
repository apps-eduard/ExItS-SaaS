using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.UnitTests.Inventory;

public sealed class BranchBalanceMutationTests
{
    private static readonly PosOrganizationId Org = PosOrganizationId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly CatalogProductId Product = CatalogProductId.From(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    private static readonly PosBranchId Primary = PosBranchId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly PosBranchId Other = PosBranchId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly DateTimeOffset T0 = DateTimeOffset.Parse("2026-08-30T12:00:00Z");

    [Fact]
    public async Task Inflow_materializes_primary_from_pre_mutation_org_before_credit()
    {
        var repo = new RecordingBalances();
        var branches = new FixedPrimary(Primary.Value);

        await BranchBalanceMutation.ApplyAsync(
            repo,
            branches,
            Org,
            Primary,
            Product,
            organizationOnHandBeforeDelta: 100m,
            signedQuantity: 20m,
            utcNow: T0);

        var balance = Assert.Single(repo.Items);
        Assert.Equal(Primary, balance.BranchId);
        Assert.Equal(120m, balance.OnHandQuantity);
    }

    [Fact]
    public async Task Secondary_inflow_materializes_zero_before_credit()
    {
        var repo = new RecordingBalances();
        var branches = new FixedPrimary(Primary.Value);

        await BranchBalanceMutation.ApplyAsync(
            repo,
            branches,
            Org,
            Other,
            Product,
            organizationOnHandBeforeDelta: 100m,
            signedQuantity: 10m,
            utcNow: T0);

        var balance = Assert.Single(repo.Items);
        Assert.Equal(Other, balance.BranchId);
        Assert.Equal(10m, balance.OnHandQuantity);
    }

    [Fact]
    public async Task Ensure_seeds_unallocated_on_primary_then_applies_outflow()
    {
        var repo = new RecordingBalances();
        var branches = new FixedPrimary(Primary.Value);

        await BranchBalanceMutation.ApplyAsync(
            repo,
            branches,
            Org,
            Primary,
            Product,
            organizationOnHandBeforeDelta: 20m,
            signedQuantity: -5m,
            utcNow: T0);

        var balance = Assert.Single(repo.Items);
        Assert.Equal(Primary, balance.BranchId);
        Assert.Equal(15m, balance.OnHandQuantity);
    }

    [Fact]
    public async Task Non_primary_does_not_receive_unallocated_org_stock()
    {
        var repo = new RecordingBalances();
        var branches = new FixedPrimary(Primary.Value);

        await Assert.ThrowsAsync<DomainException>(() =>
            BranchBalanceMutation.ApplyAsync(
                repo,
                branches,
                Org,
                Other,
                Product,
                organizationOnHandBeforeDelta: 20m,
                signedQuantity: -1m,
                utcNow: T0));
        Assert.Empty(repo.Items);
    }

    [Fact]
    public async Task Existing_balance_is_not_double_seeded()
    {
        var repo = new RecordingBalances();
        repo.Items.Add(InventoryBranchBalance.Create(Org, Primary, Product, 8m, T0));
        var branches = new FixedPrimary(Primary.Value);

        await BranchBalanceMutation.ApplyAsync(
            repo,
            branches,
            Org,
            Primary,
            Product,
            organizationOnHandBeforeDelta: 20m,
            signedQuantity: -3m,
            utcNow: T0);

        Assert.Single(repo.Items);
        Assert.Equal(5m, repo.Items[0].OnHandQuantity);
    }

    private sealed class FixedPrimary(Guid primaryId) : IOrganizationBranchDirectory
    {
        public Task<bool> ExistsInOrganizationAsync(
            Guid organizationId,
            Guid branchId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<IReadOnlyDictionary<Guid, string>> GetNamesAsync(
            Guid organizationId,
            IReadOnlyCollection<Guid> branchIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(
                branchIds.ToDictionary(id => id, id => id.ToString("D")));

        public Task<Guid?> GetPrimaryBranchIdAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Guid?>(primaryId);
    }

    private sealed class RecordingBalances : IInventoryBranchBalanceRepository
    {
        public List<InventoryBranchBalance> Items { get; } = [];

        public Task<InventoryBranchBalance?> GetAsync(
            PosOrganizationId organizationId,
            PosBranchId branchId,
            CatalogProductId productId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(b =>
                b.OrganizationId == organizationId && b.BranchId == branchId && b.ProductId == productId));

        public Task<IReadOnlyList<InventoryBranchBalance>> ListByProductIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<CatalogProductId> productIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<InventoryBranchBalance>>(
                Items.Where(b => b.OrganizationId == organizationId && productIds.Contains(b.ProductId)).ToList());

        public Task UpsertAsync(InventoryBranchBalance balance, CancellationToken cancellationToken = default)
        {
            Items.RemoveAll(b =>
                b.OrganizationId == balance.OrganizationId
                && b.BranchId == balance.BranchId
                && b.ProductId == balance.ProductId);
            Items.Add(balance);
            return Task.CompletedTask;
        }
    }
}
