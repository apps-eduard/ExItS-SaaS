using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.UnitTests.Sales;

public sealed class B2bCheckoutBuyerAuthorizationTests
{
    private static readonly PosOrganizationId Seller = PosOrganizationId.From(
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly PosOrganizationId Buyer = PosOrganizationId.From(
        Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly Guid ConnectionId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly DateTimeOffset Utc = new(2026, 9, 10, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Authorized_active_connection_resolves_organization_party()
    {
        var relationships = new FakeRelationships(ActiveRelationship());
        var result = await B2bCheckoutBuyerAuthorization.ResolveAsync(
            Seller,
            relationships,
            ConnectionId,
            requestedBuyerOrganizationId: null,
            customerId: null,
            isUtang: false);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal(SaleBuyerPartyKind.Organization, result.Value!.Kind);
        Assert.Equal(Buyer.Value, result.Value.BuyerOrganizationId);
        Assert.Equal("ORG123456", result.Value.BuyerPublicOrganizationId);
        Assert.Equal("ABC Trading", result.Value.DisplayNameSnapshot);
    }

    [Fact]
    public async Task Forged_buyer_organization_without_relationship_is_rejected()
    {
        var relationships = new FakeRelationships();
        var result = await B2bCheckoutBuyerAuthorization.ResolveAsync(
            Seller,
            relationships,
            buyerConnectionId: null,
            requestedBuyerOrganizationId: Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            customerId: null,
            isUtang: false);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorCodes.SaleB2bRelationshipRequired, result.ErrorCode);
    }

    [Fact]
    public async Task Inactive_relationship_is_rejected()
    {
        var pending = ConnectedSupplierRelationship.Rehydrate(
            ConnectedSupplierRelationshipId.From(ConnectionId),
            Buyer,
            Seller,
            ConnectedSupplierRelationshipStatus.Pending,
            Utc,
            null,
            null,
            null,
            null,
            Utc,
            Utc,
            "ABC Trading",
            "ORG123456",
            "Seller Co",
            "ORG000001");
        var relationships = new FakeRelationships(pending);
        var result = await B2bCheckoutBuyerAuthorization.ResolveAsync(
            Seller,
            relationships,
            ConnectionId,
            null,
            null,
            isUtang: false);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorCodes.SaleB2bRelationshipRequired, result.ErrorCode);
    }

    [Fact]
    public async Task Utang_with_b2b_is_blocked()
    {
        var relationships = new FakeRelationships(ActiveRelationship());
        var result = await B2bCheckoutBuyerAuthorization.ResolveAsync(
            Seller,
            relationships,
            ConnectionId,
            null,
            null,
            isUtang: true);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorCodes.SaleB2bUtangNotSupported, result.ErrorCode);
    }

    [Fact]
    public async Task Pos_customer_id_with_b2b_is_rejected()
    {
        var relationships = new FakeRelationships(ActiveRelationship());
        var result = await B2bCheckoutBuyerAuthorization.ResolveAsync(
            Seller,
            relationships,
            ConnectionId,
            null,
            customerId: Guid.NewGuid(),
            isUtang: false);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorCodes.InvalidSaleBuyerParty, result.ErrorCode);
    }

    private static ConnectedSupplierRelationship ActiveRelationship() =>
        ConnectedSupplierRelationship.Rehydrate(
            ConnectedSupplierRelationshipId.From(ConnectionId),
            Buyer,
            Seller,
            ConnectedSupplierRelationshipStatus.Active,
            Utc,
            null,
            Utc,
            null,
            null,
            Utc,
            Utc,
            "ABC Trading",
            "ORG123456",
            "Seller Co",
            "ORG000001");

    private sealed class FakeRelationships : IConnectedSupplierRelationshipRepository
    {
        private readonly Dictionary<Guid, ConnectedSupplierRelationship> _byId = new();

        public FakeRelationships(params ConnectedSupplierRelationship[] rows)
        {
            foreach (var row in rows)
            {
                _byId[row.Id.Value] = row;
            }
        }

        public Task<ConnectedSupplierRelationship?> GetAsync(
            ConnectedSupplierRelationshipId id,
            CancellationToken ct = default) =>
            Task.FromResult(_byId.TryGetValue(id.Value, out var row) ? row : null);

        public Task<ConnectedSupplierRelationship?> FindOpenAsync(
            PosOrganizationId buyer,
            PosOrganizationId supplier,
            CancellationToken ct = default) =>
            Task.FromResult(
                _byId.Values.FirstOrDefault(r =>
                    r.BuyerOrganizationId == buyer
                    && r.SupplierOrganizationId == supplier
                    && r.Status is ConnectedSupplierRelationshipStatus.Pending
                        or ConnectedSupplierRelationshipStatus.Active));

        public Task<IReadOnlyList<ConnectedSupplierRelationship>> ListAsync(
            PosOrganizationId organizationId,
            bool supplierView,
            CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ConnectedSupplierRelationship>>(
                _byId.Values
                    .Where(r =>
                        supplierView
                            ? r.SupplierOrganizationId == organizationId
                            : r.BuyerOrganizationId == organizationId)
                    .ToList());

        public Task AddAsync(ConnectedSupplierRelationship relationship, CancellationToken ct = default)
        {
            _byId[relationship.Id.Value] = relationship;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(ConnectedSupplierRelationship relationship, CancellationToken ct = default)
        {
            _byId[relationship.Id.Value] = relationship;
            return Task.CompletedTask;
        }
    }
}
