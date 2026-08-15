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

    [Fact]
    public async Task SupplierSeesActiveConnectedBuyers_and_excludes_pending_declined()
    {
        var buyerActive = PosOrganizationId.From(Guid.NewGuid());
        var buyerPending = PosOrganizationId.From(Guid.NewGuid());
        var buyerDeclined = PosOrganizationId.From(Guid.NewGuid());
        var otherSupplier = PosOrganizationId.From(Guid.NewGuid());
        var supplier = PosOrganizationId.From(Guid.NewGuid());
        var repo = new InMemoryRelationships();
        var now = DateTimeOffset.UtcNow;

        var active = ConnectedSupplierRelationship.Request(
            buyerActive, supplier, now, buyerDisplayName: "Kizy Meat Shop", buyerPublicOrganizationId: "ORG123456");
        active.Approve(now.AddMinutes(1));
        await repo.AddAsync(active);

        await repo.AddAsync(ConnectedSupplierRelationship.Request(
            buyerPending, supplier, now, buyerDisplayName: "Pending Co", buyerPublicOrganizationId: "ORG000001"));

        var declined = ConnectedSupplierRelationship.Request(
            buyerDeclined, supplier, now, buyerDisplayName: "Declined Co", buyerPublicOrganizationId: "ORG000002");
        declined.Decline(now.AddMinutes(2));
        await repo.AddAsync(declined);

        var foreign = ConnectedSupplierRelationship.Request(
            buyerActive, otherSupplier, now, buyerDisplayName: "Kizy Meat Shop", buyerPublicOrganizationId: "ORG123456");
        foreign.Approve(now.AddMinutes(3));
        await repo.AddAsync(foreign);

        var list = new ListRelationships(repo, new FakeAccess());
        var supplierView = await list.ExecuteAsync(supplier.Value, supplierView: true);
        Assert.True(supplierView.IsSuccess);
        var connected = supplierView.Value!
            .Where(x => string.Equals(x.Status, "Active", StringComparison.OrdinalIgnoreCase))
            .ToList();
        Assert.Single(connected);
        Assert.Equal("Kizy Meat Shop", connected[0].CounterpartyDisplayName);
        Assert.Equal("ORG123456", connected[0].CounterpartyPublicOrganizationId);
        Assert.DoesNotContain(connected, x => x.Status is "Pending" or "Declined");
        Assert.DoesNotContain(supplierView.Value!, x => x.RelationshipId == foreign.Id.Value);
    }

    [Fact]
    public async Task AcceptMovesRequestToConnectedBuyers_and_disconnect_removes_from_active()
    {
        var buyer = PosOrganizationId.From(Guid.NewGuid());
        var supplier = PosOrganizationId.From(Guid.NewGuid());
        var repo = new InMemoryRelationships();
        var relationship = ConnectedSupplierRelationship.Request(
            buyer, supplier, DateTimeOffset.UtcNow, buyerDisplayName: "Mica Store");
        await repo.AddAsync(relationship);

        var respond = new RespondConnection(repo, new FakeUow(), new FakeAccess());
        Assert.True((await respond.ExecuteAsync(supplier.Value, relationship.Id.Value, true, new RespondConnectionRequest())).IsSuccess);

        var list = new ListRelationships(repo, new FakeAccess());
        var afterAccept = (await list.ExecuteAsync(supplier.Value, supplierView: true)).Value!
            .Where(x => x.Status == "Active")
            .ToList();
        Assert.Single(afterAccept);
        Assert.Equal("Mica Store", afterAccept[0].CounterpartyDisplayName);

        var disconnect = new DisconnectConnectedSupplier(repo, new FakeUow(), new FakeAccess());
        Assert.True((await disconnect.ExecuteAsync(supplier.Value, relationship.Id.Value)).IsSuccess);

        var afterDisconnect = (await list.ExecuteAsync(supplier.Value, supplierView: true)).Value!
            .Where(x => x.Status == "Active")
            .ToList();
        Assert.Empty(afterDisconnect);
        Assert.Contains((await list.ExecuteAsync(supplier.Value, supplierView: true)).Value!,
            x => x.Status == "Disconnected");
    }

    [Fact]
    public async Task OwnershipTransferPreservesConnectedBuyers_org_scoped_not_personal()
    {
        var buyer = PosOrganizationId.From(Guid.NewGuid());
        var supplier = PosOrganizationId.From(Guid.NewGuid());
        var repo = new InMemoryRelationships();
        var relationship = ConnectedSupplierRelationship.Request(buyer, supplier, DateTimeOffset.UtcNow);
        relationship.Approve(DateTimeOffset.UtcNow.AddMinutes(1));
        await repo.AddAsync(relationship);

        // New owner of the same Organization still lists Active relationships by SupplierOrganizationId.
        var list = new ListRelationships(repo, new FakeAccess());
        var rows = await list.ExecuteAsync(supplier.Value, supplierView: true);
        Assert.True(rows.IsSuccess);
        Assert.Single(rows.Value!.Where(x => x.Status == "Active"));
        Assert.Equal(supplier.Value, relationship.SupplierOrganizationId.Value);
        Assert.Equal(buyer.Value, relationship.BuyerOrganizationId.Value);
    }

    [Fact]
    public async Task AcceptingSupplierConnectionDoesNotCreateCustomer()
    {
        var respondPath = Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Application",
            "ConnectedSuppliers",
            "ConnectedSupplierUseCases.cs");
        var text = File.ReadAllText(respondPath);
        var start = text.IndexOf("public sealed class RespondConnection", StringComparison.Ordinal);
        var end = text.IndexOf("public sealed class DisconnectConnectedSupplier", StringComparison.Ordinal);
        Assert.True(start > 0 && end > start);
        var body = text[start..end];
        Assert.DoesNotContain("ICustomerRepository", body, StringComparison.Ordinal);
        Assert.DoesNotContain("Customer.Create", body, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateCustomer", body, StringComparison.Ordinal);
        Assert.DoesNotContain("MergeCustomer", body, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ExItS.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }
}
