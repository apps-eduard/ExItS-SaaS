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

        await BranchBalanceMutation.ApplyAsync(
            repo,
            Org,
            Primary,
            Primary.Value,
            Product,
            organizationOnHandBeforeDelta: 100m,
            signedQuantity: 20m,
            utcNow: T0);

        var balance = Assert.Single(repo.Items);
        Assert.Equal(Primary, balance.BranchId);
        Assert.Equal(120m, balance.OnHandQuantity);
        Assert.Equal(0m, balance.ReservedQuantity);
    }

    [Fact]
    public async Task Secondary_inflow_materializes_zero_before_credit()
    {
        var repo = new RecordingBalances();

        await BranchBalanceMutation.ApplyAsync(
            repo,
            Org,
            Other,
            Primary.Value,
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

        await BranchBalanceMutation.ApplyAsync(
            repo,
            Org,
            Primary,
            Primary.Value,
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

        await Assert.ThrowsAsync<DomainException>(() =>
            BranchBalanceMutation.ApplyAsync(
                repo,
                Org,
                Other,
                Primary.Value,
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

        await BranchBalanceMutation.ApplyAsync(
            repo,
            Org,
            Primary,
            Primary.Value,
            Product,
            organizationOnHandBeforeDelta: 20m,
            signedQuantity: -3m,
            utcNow: T0);

        Assert.Single(repo.Items);
        Assert.Equal(5m, repo.Items[0].OnHandQuantity);
    }

    [Fact]
    public async Task H1_WRITE_PRIMARY_missing_balance_unknown_primary_does_not_create_row()
    {
        var repo = new RecordingBalances();

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            BranchBalanceMutation.ApplyAsync(
                repo,
                Org,
                Primary,
                primaryBranchId: null,
                Product,
                organizationOnHandBeforeDelta: 100m,
                signedQuantity: 20m,
                utcNow: T0));

        Assert.Equal(DomainErrorCodes.InventoryPrimaryUnavailable, ex.ErrorCode);
        Assert.Empty(repo.Items);
    }

    [Fact]
    public async Task H1_WRITE_PRIMARY_explicit_remote_can_mutate_when_primary_unknown()
    {
        var repo = new RecordingBalances();
        repo.Items.Add(InventoryBranchBalance.Create(Org, Other, Product, 25m, T0));

        await BranchBalanceMutation.ApplyAsync(
            repo,
            Org,
            Other,
            primaryBranchId: null,
            Product,
            organizationOnHandBeforeDelta: 125m,
            signedQuantity: -5m,
            utcNow: T0);

        Assert.Equal(20m, repo.Items[0].OnHandQuantity);
    }

    [Fact]
    public async Task Reserve_does_not_change_on_hand()
    {
        var repo = new RecordingBalances();
        repo.Items.Add(InventoryBranchBalance.Create(Org, Other, Product, 5m, T0));

        await BranchBalanceMutation.ApplyReservationAsync(
            repo,
            Org,
            Other,
            Primary.Value,
            Product,
            organizationOnHandBeforeReservation: 85m,
            quantity: 4m,
            BranchReservationEffect.Reserve,
            T0);

        var balance = Assert.Single(repo.Items);
        Assert.Equal(5m, balance.OnHandQuantity);
        Assert.Equal(4m, balance.ReservedQuantity);
        Assert.Equal(1m, balance.AvailableQuantity);
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
