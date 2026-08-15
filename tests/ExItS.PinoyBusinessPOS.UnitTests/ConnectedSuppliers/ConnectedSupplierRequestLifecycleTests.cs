using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.UnitTests.ConnectedSuppliers;

public sealed class ConnectedSupplierRequestLifecycleTests
{
    private sealed class FakeAccess : IPosCommercialAccessAccessor
    {
        public PosCommercialAccess Current { get; set; } = PosCommercialAccess.DevelopmentDefault;
    }

    private sealed class FakeUow : IPosUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<T> ExecuteInSerializableTransactionAsync<T>(
            Func<CancellationToken, Task<T>> action,
            CancellationToken cancellationToken = default) =>
            action(cancellationToken);
    }

    private sealed class InMemoryRelationships : IConnectedSupplierRelationshipRepository
    {
        private readonly List<ConnectedSupplierRelationship> _items = [];

        public Task AddAsync(ConnectedSupplierRelationship relationship, CancellationToken ct = default)
        {
            _items.Add(relationship);
            return Task.CompletedTask;
        }

        public Task<ConnectedSupplierRelationship?> FindOpenAsync(
            PosOrganizationId buyer,
            PosOrganizationId supplier,
            CancellationToken ct = default) =>
            Task.FromResult(_items.FirstOrDefault(x =>
                x.BuyerOrganizationId == buyer
                && x.SupplierOrganizationId == supplier
                && (x.Status is ConnectedSupplierRelationshipStatus.Pending or ConnectedSupplierRelationshipStatus.Active)));

        public Task<ConnectedSupplierRelationship?> GetAsync(
            ConnectedSupplierRelationshipId id,
            CancellationToken ct = default) =>
            Task.FromResult(_items.FirstOrDefault(x => x.Id.Value == id.Value));

        public Task<IReadOnlyList<ConnectedSupplierRelationship>> ListAsync(
            PosOrganizationId organizationId,
            bool supplierView,
            CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ConnectedSupplierRelationship>>(
                _items.Where(x => supplierView
                    ? x.SupplierOrganizationId == organizationId
                    : x.BuyerOrganizationId == organizationId).ToList());

        public Task UpdateAsync(ConnectedSupplierRelationship relationship, CancellationToken ct = default) =>
            Task.CompletedTask;
    }

    [Fact]
    public async Task Buyer_sees_outgoing_pending_and_supplier_sees_incoming()
    {
        var buyer = PosOrganizationId.From(Guid.NewGuid());
        var supplier = PosOrganizationId.From(Guid.NewGuid());
        var repo = new InMemoryRelationships();
        var now = DateTimeOffset.UtcNow;
        await repo.AddAsync(ConnectedSupplierRelationship.Request(
            buyer,
            supplier,
            now,
            buyerDisplayName: "Mica Store",
            buyerPublicOrganizationId: "ORG000111",
            supplierDisplayName: "Wholesale Hub",
            supplierPublicOrganizationId: "ORG000222"));

        var list = new ListRelationships(repo, new FakeAccess());
        var buyerView = await list.ExecuteAsync(buyer.Value, supplierView: false);
        var supplierView = await list.ExecuteAsync(supplier.Value, supplierView: true);

        Assert.True(buyerView.IsSuccess);
        Assert.Single(buyerView.Value!);
        Assert.Equal("Pending", buyerView.Value![0].Status);
        Assert.Equal("Wholesale Hub", buyerView.Value[0].CounterpartyDisplayName);
        Assert.Equal("ORG000222", buyerView.Value[0].CounterpartyPublicOrganizationId);

        Assert.True(supplierView.IsSuccess);
        Assert.Single(supplierView.Value!);
        Assert.Equal("Pending", supplierView.Value![0].Status);
        Assert.Equal("Mica Store", supplierView.Value[0].CounterpartyDisplayName);
        Assert.Equal("ORG000111", supplierView.Value[0].CounterpartyPublicOrganizationId);
    }

    [Fact]
    public async Task Supplier_can_accept_pending_request()
    {
        var buyer = PosOrganizationId.From(Guid.NewGuid());
        var supplier = PosOrganizationId.From(Guid.NewGuid());
        var repo = new InMemoryRelationships();
        var relationship = ConnectedSupplierRelationship.Request(buyer, supplier, DateTimeOffset.UtcNow);
        await repo.AddAsync(relationship);

        var respond = new RespondConnection(repo, new FakeUow(), new FakeAccess());
        var result = await respond.ExecuteAsync(supplier.Value, relationship.Id.Value, approve: true, new RespondConnectionRequest());

        Assert.True(result.IsSuccess, $"{result.ErrorCode}: {result.ErrorMessage}");
        Assert.Equal("Active", result.Value!.Status);
        Assert.Equal(ConnectedSupplierRelationshipStatus.Active, relationship.Status);
    }

    [Fact]
    public async Task Supplier_can_decline_pending_request()
    {
        var buyer = PosOrganizationId.From(Guid.NewGuid());
        var supplier = PosOrganizationId.From(Guid.NewGuid());
        var repo = new InMemoryRelationships();
        var relationship = ConnectedSupplierRelationship.Request(buyer, supplier, DateTimeOffset.UtcNow);
        await repo.AddAsync(relationship);

        var respond = new RespondConnection(repo, new FakeUow(), new FakeAccess());
        var result = await respond.ExecuteAsync(supplier.Value, relationship.Id.Value, approve: false, new RespondConnectionRequest());

        Assert.True(result.IsSuccess, $"{result.ErrorCode}: {result.ErrorMessage}");
        Assert.Equal("Declined", result.Value!.Status);
        Assert.NotEqual(ConnectedSupplierRelationshipStatus.Active, relationship.Status);
    }

    [Fact]
    public async Task Buyer_cannot_accept_as_supplier_actor()
    {
        var buyer = PosOrganizationId.From(Guid.NewGuid());
        var supplier = PosOrganizationId.From(Guid.NewGuid());
        var repo = new InMemoryRelationships();
        var relationship = ConnectedSupplierRelationship.Request(buyer, supplier, DateTimeOffset.UtcNow);
        await repo.AddAsync(relationship);

        var respond = new RespondConnection(repo, new FakeUow(), new FakeAccess());
        var result = await respond.ExecuteAsync(buyer.Value, relationship.Id.Value, approve: true, new RespondConnectionRequest());

        Assert.False(result.IsSuccess);
        Assert.Equal(ConnectedSupplierErrorCodes.NotFound, result.ErrorCode);
        Assert.Contains("no longer available", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Cashier_cannot_manage_supplier_connections()
    {
        var access = new FakeAccess
        {
            Current = new PosCommercialAccess(
                PosSubscriptionStatuses.Active,
                [PosFeatureCodes.StoreSuppliersView],
                IsKnown: true)
        };
        var respond = new RespondConnection(new InMemoryRelationships(), new FakeUow(), access);
        var result = await respond.ExecuteAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            approve: true,
            new RespondConnectionRequest());

        Assert.False(result.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(result.ErrorCode));
    }
}
