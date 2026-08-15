using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;

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
        Assert.Single(publisher.Published);
        Assert.Equal(SupplierConnectionNotificationTypes.Accepted, publisher.Published[0].Type);
        Assert.Equal(buyer.Value, publisher.Published[0].Recipient);
        Assert.Contains("Paul Distribution accepted", publisher.Published[0].Preview, StringComparison.Ordinal);
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
        Assert.Equal(SupplierConnectionNotificationTypes.Declined, publisher.Published[0].Type);
        Assert.Contains("declined", publisher.Published[0].Preview, StringComparison.OrdinalIgnoreCase);
    }
}
