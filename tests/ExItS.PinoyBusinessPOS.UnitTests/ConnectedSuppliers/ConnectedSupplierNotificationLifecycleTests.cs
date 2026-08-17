using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;

namespace ExItS.PinoyBusinessPOS.UnitTests.ConnectedSuppliers;

public sealed class ConnectedSupplierNotificationLifecycleTests
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

    private sealed class RecordingPublisher : IOrganizationBusinessNotificationPublisher
    {
        public List<(Guid Source, Guid Recipient, string Type, string RelatedId, string Title, string Preview)> Published { get; } = [];
        public List<(Guid Org, string Type, string RelatedId)> Marked { get; } = [];

        public Task PublishAsync(
            Guid sourceOrganizationId,
            Guid recipientOrganizationId,
            string relatedType,
            string relatedId,
            string title,
            string preview,
            CancellationToken cancellationToken = default)
        {
            Published.Add((sourceOrganizationId, recipientOrganizationId, relatedType, relatedId, title, preview));
            return Task.CompletedTask;
        }

        public Task MarkRelatedReadAsync(
            Guid organizationId,
            string relatedType,
            string relatedId,
            CancellationToken cancellationToken = default)
        {
            Marked.Add((organizationId, relatedType, relatedId));
            return Task.CompletedTask;
        }
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
    public async Task Accept_marks_request_notification_and_publishes_buyer_accepted()
    {
        var buyer = PosOrganizationId.From(Guid.NewGuid());
        var supplier = PosOrganizationId.From(Guid.NewGuid());
        var repo = new InMemoryRelationships();
        var relationship = ConnectedSupplierRelationship.Request(
            buyer,
            supplier,
            DateTimeOffset.UtcNow,
            supplierDisplayName: "Paul Distribution",
            supplierPublicOrganizationId: "ORG000999");
        await repo.AddAsync(relationship);
        var publisher = new RecordingPublisher();

        var respond = new RespondConnection(repo, new FakeUow(), new FakeAccess(), publisher);
        var result = await respond.ExecuteAsync(
            supplier.Value,
            relationship.Id.Value,
            approve: true,
            new RespondConnectionRequest());

        Assert.True(result.IsSuccess, $"{result.ErrorCode}: {result.ErrorMessage}");
        Assert.Single(publisher.Marked);
        Assert.Equal(SupplierConnectionNotificationTypes.Requested, publisher.Marked[0].Type);
        Assert.Equal(relationship.Id.Value.ToString("D"), publisher.Marked[0].RelatedId);
        Assert.Equal(2, publisher.Published.Count);
        Assert.Contains(publisher.Published, p =>
            p.Type == SupplierConnectionNotificationTypes.Accepted && p.Recipient == buyer.Value);
        Assert.Contains(publisher.Published, p =>
            p.Type == SupplierConnectionNotificationTypes.AcceptedConfirmation
            && p.Recipient == supplier.Value
            && p.Source == supplier.Value);
        Assert.Contains("connected buyer", publisher.Published
            .Single(p => p.Type == SupplierConnectionNotificationTypes.AcceptedConfirmation).Preview,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Decline_marks_request_notification_and_publishes_buyer_declined()
    {
        var buyer = PosOrganizationId.From(Guid.NewGuid());
        var supplier = PosOrganizationId.From(Guid.NewGuid());
        var repo = new InMemoryRelationships();
        var relationship = ConnectedSupplierRelationship.Request(
            buyer,
            supplier,
            DateTimeOffset.UtcNow,
            supplierDisplayName: "Paul Distribution");
        await repo.AddAsync(relationship);
        var publisher = new RecordingPublisher();

        var respond = new RespondConnection(repo, new FakeUow(), new FakeAccess(), publisher);
        var result = await respond.ExecuteAsync(
            supplier.Value,
            relationship.Id.Value,
            approve: false,
            new RespondConnectionRequest());

        Assert.True(result.IsSuccess);
        Assert.Equal(2, publisher.Published.Count);
        Assert.Contains(publisher.Published, p => p.Type == SupplierConnectionNotificationTypes.Declined);
        Assert.Contains(publisher.Published, p => p.Type == SupplierConnectionNotificationTypes.DeclinedConfirmation);
        Assert.Contains(
            "declined",
            publisher.Published.Single(p => p.Type == SupplierConnectionNotificationTypes.Declined).Preview,
            StringComparison.OrdinalIgnoreCase);
    }

    private sealed class InMemoryConnectedOrders : IConnectedPurchaseOrderRepository
    {
        public List<ConnectedPurchaseOrder> Items { get; } = [];

        public Task AddAsync(ConnectedPurchaseOrder order, CancellationToken ct = default)
        {
            Items.Add(order);
            return Task.CompletedTask;
        }

        public Task<ConnectedPurchaseOrder?> GetAsync(ConnectedPurchaseOrderId id, CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(x => x.Id.Value == id.Value));

        public Task<ConnectedPurchaseOrder?> GetByBuyerPurchaseOrderAsync(PurchaseOrderId id, CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(x => x.BuyerPurchaseOrderId.Value == id.Value));

        public Task<IReadOnlyList<ConnectedPurchaseOrder>> ListIncomingAsync(PosOrganizationId supplier, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ConnectedPurchaseOrder>>(
                Items.Where(x => x.SupplierOrganizationId == supplier).ToList());

        public Task UpdateAsync(ConnectedPurchaseOrder order, CancellationToken ct = default) => Task.CompletedTask;
    }

    private static ConnectedPurchaseOrder SeedOrder(PosOrganizationId buyer, PosOrganizationId supplier)
    {
        var relationship = ConnectedSupplierRelationship.Request(
            buyer, supplier, DateTimeOffset.UtcNow, supplierDisplayName: "Paul Supply", buyerDisplayName: "Mica Store");
        relationship.Approve(DateTimeOffset.UtcNow);
        var line = ConnectedPurchaseOrderLine.Create(
            CatalogProductId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")), "Apple", null, 10m, 50m, "kg");
        return ConnectedPurchaseOrder.CreateFromBuyerSubmission(
            relationship,
            PurchaseOrderId.New(),
            "PO-00123",
            DateOnly.FromDateTime(DateTime.UtcNow),
            null,
            [line],
            DateTimeOffset.UtcNow,
            paymentTerm: ConnectedPoPaymentTerm.Cash);
    }

    [Fact]
    public async Task Accept_unchanged_notifies_buyer_once_and_retry_does_not_duplicate()
    {
        var buyer = PosOrganizationId.From(Guid.NewGuid());
        var supplier = PosOrganizationId.From(Guid.NewGuid());
        var order = SeedOrder(buyer, supplier);
        var repo = new InMemoryConnectedOrders();
        await repo.AddAsync(order);
        var publisher = new RecordingPublisher();
        var relationships = new InMemoryRelationships();
        await relationships.AddAsync(ConnectedSupplierRelationship.Rehydrate(
            order.RelationshipId, buyer, supplier, ConnectedSupplierRelationshipStatus.Active,
            DateTimeOffset.UtcNow, null, DateTimeOffset.UtcNow, null, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
            "Mica Store", null, "Paul Supply", null));
        var use = new RespondIncomingOrder(repo, new FakeUow(), new FakeAccess(), notifications: publisher, relationships: relationships);

        var first = await use.ExecuteAsync(supplier.Value, order.Id.Value, accept: true);
        var second = await use.ExecuteAsync(supplier.Value, order.Id.Value, accept: true);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(ConnectedPurchaseOrderStatus.Accepted, first.Value!.Status is "Accepted" ? ConnectedPurchaseOrderStatus.Accepted : ConnectedPurchaseOrderStatus.Accepted);
        Assert.Single(publisher.Published);
        Assert.Equal(ConnectedPurchaseOrderNotificationTypes.Accepted, publisher.Published[0].Type);
        Assert.Equal(buyer.Value, publisher.Published[0].Recipient);
        Assert.Equal(order.BuyerPurchaseOrderId.Value.ToString("D"), publisher.Published[0].RelatedId);
    }

    [Fact]
    public async Task Propose_changes_notifies_buyer_once()
    {
        var buyer = PosOrganizationId.From(Guid.NewGuid());
        var supplier = PosOrganizationId.From(Guid.NewGuid());
        var order = SeedOrder(buyer, supplier);
        var repo = new InMemoryConnectedOrders();
        await repo.AddAsync(order);
        var publisher = new RecordingPublisher();
        var use = new ProposeIncomingOrderChanges(repo, new FakeUow(), new FakeAccess(), notifications: publisher);

        var request = new ProposeIncomingOrderChangesRequest(
            [new ProposeIncomingOrderLineRequest(order.Lines[0].ProductId.Value, 8m)]);
        var first = await use.ExecuteAsync(supplier.Value, order.Id.Value, request);
        var second = await use.ExecuteAsync(supplier.Value, order.Id.Value, request);

        Assert.True(first.IsSuccess, $"{first.ErrorCode}: {first.ErrorMessage}");
        Assert.True(second.IsSuccess);
        Assert.Equal("ChangesProposed", first.Value!.Status);
        Assert.Single(publisher.Published);
        Assert.Equal(ConnectedPurchaseOrderNotificationTypes.ChangesProposed, publisher.Published[0].Type);
        Assert.Equal(buyer.Value, publisher.Published[0].Recipient);
        Assert.Equal(order.BuyerPurchaseOrderId.Value.ToString("D"), publisher.Published[0].RelatedId);
    }

    [Fact]
    public async Task Decline_notifies_buyer()
    {
        var buyer = PosOrganizationId.From(Guid.NewGuid());
        var supplier = PosOrganizationId.From(Guid.NewGuid());
        var order = SeedOrder(buyer, supplier);
        var repo = new InMemoryConnectedOrders();
        await repo.AddAsync(order);
        var publisher = new RecordingPublisher();
        var use = new RespondIncomingOrder(repo, new FakeUow(), new FakeAccess(), notifications: publisher);

        var result = await use.ExecuteAsync(supplier.Value, order.Id.Value, accept: false);

        Assert.True(result.IsSuccess);
        Assert.Single(publisher.Published);
        Assert.Equal(ConnectedPurchaseOrderNotificationTypes.Declined, publisher.Published[0].Type);
        Assert.Equal(buyer.Value, publisher.Published[0].Recipient);
    }

    [Fact]
    public async Task Wrong_organization_cannot_accept_incoming_order()
    {
        var buyer = PosOrganizationId.From(Guid.NewGuid());
        var supplier = PosOrganizationId.From(Guid.NewGuid());
        var stranger = PosOrganizationId.From(Guid.NewGuid());
        var order = SeedOrder(buyer, supplier);
        var repo = new InMemoryConnectedOrders();
        await repo.AddAsync(order);
        var use = new RespondIncomingOrder(repo, new FakeUow(), new FakeAccess());

        var result = await use.ExecuteAsync(stranger.Value, order.Id.Value, accept: true);

        Assert.False(result.IsSuccess);
        Assert.Equal(ConnectedSupplierErrorCodes.IncomingOrderNotFound, result.ErrorCode);
    }
}
