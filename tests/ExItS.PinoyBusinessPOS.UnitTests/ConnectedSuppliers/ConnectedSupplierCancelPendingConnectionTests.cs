using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Suppliers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;

namespace ExItS.PinoyBusinessPOS.UnitTests.ConnectedSuppliers;

public sealed class ConnectedSupplierCancelPendingConnectionTests
{
    private static readonly Guid BuyerA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid BuyerB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid SupplierOrg = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    private static readonly Guid Iloilo = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid Cebu = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

    private static readonly string SupplierPublicOrgId = "ORG123456";

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

    private sealed class OrgWideBranchAccess : IAuthorizedBranchGroupingDirectory
    {
        public static readonly OrgWideBranchAccess Instance = new();

        public Task<AuthorizedBranchScope> ListAuthorizedAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new AuthorizedBranchScope(IsOrganizationWide: true, []));
    }

    private sealed class InMemoryRelationships : IConnectedSupplierRelationshipRepository
    {
        private readonly List<ConnectedSupplierRelationship> _items = [];

        public IReadOnlyList<ConnectedSupplierRelationship> Items => _items;

        public Task AddAsync(ConnectedSupplierRelationship relationship, CancellationToken ct = default)
        {
            _items.Add(relationship);
            return Task.CompletedTask;
        }

        public Task<ConnectedSupplierRelationship?> GetAsync(
            ConnectedSupplierRelationshipId id,
            CancellationToken ct = default) =>
            Task.FromResult(_items.FirstOrDefault(x => x.Id == id));

        public Task<ConnectedSupplierRelationship?> FindOpenAsync(
            PosOrganizationId buyer,
            PosOrganizationId supplier,
            CancellationToken ct = default) =>
            Task.FromResult(_items.FirstOrDefault(x =>
                x.BuyerOrganizationId == buyer
                && x.SupplierOrganizationId == supplier
                && (x.Status is ConnectedSupplierRelationshipStatus.Pending or ConnectedSupplierRelationshipStatus.Active)));

        public Task<IReadOnlyList<ConnectedSupplierRelationship>> ListAsync(
            PosOrganizationId organizationId,
            bool supplierView,
            CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ConnectedSupplierRelationship>>(
                _items.Where(x =>
                        supplierView
                            ? x.SupplierOrganizationId == organizationId
                            : x.BuyerOrganizationId == organizationId)
                    .ToList());

        public Task UpdateAsync(ConnectedSupplierRelationship relationship, CancellationToken ct = default) =>
            Task.CompletedTask;
    }

    private sealed class InMemorySuppliers : ISupplierRepository
    {
        private readonly List<Supplier> _items = [];
        private int _n;

        public Supplier? LastAdded { get; private set; }

        public Task<Supplier?> GetByIdAsync(
            PosOrganizationId organizationId,
            SupplierId supplierId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                _items.FirstOrDefault(x =>
                    x.OrganizationId == organizationId
                    && x.Id == supplierId));

        public Task<(IReadOnlyList<Supplier> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            SupplierFilter filter,
            int skip,
            int take,
            IReadOnlyCollection<Guid>? restrictToSupplierIds = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(((IReadOnlyList<Supplier>)[], 0));

        public Task<Supplier?> FindActiveByNormalizedNameAsync(
            PosOrganizationId organizationId,
            string normalizedName,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Supplier?>(null);

        public Task<Supplier?> FindActiveByNormalizedEmailAsync(
            PosOrganizationId organizationId,
            string normalizedEmail,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Supplier?>(null);

        public Task<Supplier?> FindActiveByNormalizedMobileAsync(
            PosOrganizationId organizationId,
            string normalizedMobile,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Supplier?>(null);

        public Task<Supplier?> FindActiveByNormalizedTaxAsync(
            PosOrganizationId organizationId,
            string normalizedTax,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Supplier?>(null);

        public Task<string> AllocateNextSupplierCodeAsync(
            PosOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult($"SUP-{++_n:D6}");

        public Task AddAsync(Supplier supplier, CancellationToken cancellationToken = default)
        {
            _items.Add(supplier);
            LastAdded = supplier;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Supplier supplier, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesByIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<Guid> supplierIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(new Dictionary<Guid, string>());
    }

    private sealed class FakeResolve : IPlatformOrganizationPublicResolve
    {
        public required Guid SupplierOrgId { get; init; }

        public Task<ApplicationResult<PlatformOrganizationPublicResolveResult>> ResolveOrganizationForConnectedSupplierAsync(
            string publicOrganizationIdOrQrPayload,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                ApplicationResult<PlatformOrganizationPublicResolveResult>.Success(
                    new PlatformOrganizationPublicResolveResult(
                        SupplierOrgId,
                        SupplierPublicOrgId,
                        "ABC Wholesale")));

        public Task<ApplicationResult<PlatformOrganizationPublicResolveResult>> GetOrganizationPublicIdentityAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                ApplicationResult<PlatformOrganizationPublicResolveResult>.Success(
                    new PlatformOrganizationPublicResolveResult(
                        organizationId,
                        "ORG000001",
                        "Mica Store")));
    }

    private sealed class FakeLocations : IPlatformSupplierLocationDirectory
    {
        public required IReadOnlyList<PlatformSupplierLocationDto> Locations { get; init; }

        public Task<ApplicationResult<IReadOnlyList<PlatformSupplierLocationDto>>> ListActiveLocationsAsync(
            string publicOrganizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(ApplicationResult<IReadOnlyList<PlatformSupplierLocationDto>>.Success(Locations));
    }

    private sealed class FakeBuyerConsoleClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow;
    }

    [Fact]
    public async Task Cancel_pending_request_allows_new_request_again_after_cancellation()
    {
        var relationships = new InMemoryRelationships();
        var suppliers = new InMemorySuppliers();
        var uow = new FakeUow();
        var access = new FakeAccess();
        var resolve = new FakeResolve { SupplierOrgId = SupplierOrg };
        var locations = new FakeLocations
        {
            Locations = [
                new PlatformSupplierLocationDto(Iloilo, "Iloilo Branch", "BR-ILO", IsPrimary: false),
                new PlatformSupplierLocationDto(Cebu, "Cebu Branch", "BR-CEB", IsPrimary: false),
            ]
        };

        var request = new RequestConnection(
            relationships,
            suppliers,
            uow,
            access,
            resolve,
            locations);

        var first = await request.ExecuteAsync(
            BuyerA,
            new RequestConnectionRequest(
                SupplierPublicOrganizationIdOrQrPayload: SupplierPublicOrgId,
                SupplierBranchId: Iloilo));

        Assert.True(first.IsSuccess, $"{first.ErrorCode}: {first.ErrorMessage}");
        Assert.Single(relationships.Items);
        Assert.Equal("Pending", first.Value!.Status);
        Assert.Equal(ConnectedSupplierRelationshipStatus.Pending, relationships.Items[0].Status);

        var duplicate = await request.ExecuteAsync(
            BuyerA,
            new RequestConnectionRequest(
                SupplierPublicOrganizationIdOrQrPayload: SupplierPublicOrgId,
                SupplierBranchId: Cebu));

        Assert.False(duplicate.IsSuccess);
        Assert.Equal(ConnectedSupplierErrorCodes.DuplicateRelationship, duplicate.ErrorCode);
        Assert.Single(relationships.Items); // no new relationship row created

        var openBeforeCancel = await relationships.FindOpenAsync(
            PosOrganizationId.From(BuyerA),
            PosOrganizationId.From(SupplierOrg));
        Assert.NotNull(openBeforeCancel);

        var pendingRel = relationships.Items.Single();
        var cancel = new CancelPendingConnection(relationships, uow, access);
        var cancelResult = await cancel.ExecuteAsync(
            BuyerA,
            pendingRel.Id.Value,
            new CancelConnectionRequest());

        Assert.True(cancelResult.IsSuccess, $"{cancelResult.ErrorCode}: {cancelResult.ErrorMessage}");
        Assert.Equal(ConnectedSupplierRelationshipStatus.Declined, pendingRel.Status);

        var openAfterCancel = await relationships.FindOpenAsync(
            PosOrganizationId.From(BuyerA),
            PosOrganizationId.From(SupplierOrg));
        Assert.Null(openAfterCancel);

        var second = await request.ExecuteAsync(
            BuyerA,
            new RequestConnectionRequest(
                SupplierPublicOrganizationIdOrQrPayload: SupplierPublicOrgId,
                SupplierBranchId: Cebu));

        Assert.True(second.IsSuccess, $"{second.ErrorCode}: {second.ErrorMessage}");
        Assert.Equal(2, relationships.Items.Count);

        var declined = relationships.Items.First(x => x.Status == ConnectedSupplierRelationshipStatus.Declined);
        var newPending = relationships.Items.First(x => x.Status == ConnectedSupplierRelationshipStatus.Pending);
        Assert.Equal(Iloilo, declined.SupplierBranchId);
        Assert.Equal(Cebu, newPending.SupplierBranchId);
    }

    [Fact]
    public async Task Duplicate_while_active_is_blocked_and_cancel_rejects_non_pending_and_supplier_deactivate_does_not_cancel()
    {
        // Pending -> Active
        var relationships = new InMemoryRelationships();
        var suppliers = new InMemorySuppliers();
        var uow = new FakeUow();
        var access = new FakeAccess();
        var resolve = new FakeResolve { SupplierOrgId = SupplierOrg };
        var locations = new FakeLocations
        {
            Locations = [
                new PlatformSupplierLocationDto(Iloilo, "Iloilo Branch", "BR-ILO", IsPrimary: false),
            ]
        };

        var request = new RequestConnection(
            relationships,
            suppliers,
            uow,
            access,
            resolve,
            locations);

        var pending = await request.ExecuteAsync(
            BuyerA,
            new RequestConnectionRequest(
                SupplierPublicOrganizationIdOrQrPayload: SupplierPublicOrgId,
                SupplierBranchId: Iloilo));
        Assert.True(pending.IsSuccess);

        var relationshipId = pending.Value!.RelationshipId;

        var respond = new RespondConnection(
            relationships,
            uow,
            access,
            OrgWideBranchAccess.Instance);

        var approved = await respond.ExecuteAsync(
            SupplierOrg,
            relationshipId,
            approve: true,
            new RespondConnectionRequest());

        Assert.True(approved.IsSuccess, $"{approved.ErrorCode}: {approved.ErrorMessage}");
        Assert.Equal(ConnectedSupplierRelationshipStatus.Active, relationships.Items.Single().Status);

        // Active duplicate: blocked
        var dup = await request.ExecuteAsync(
            BuyerA,
            new RequestConnectionRequest(
                SupplierPublicOrganizationIdOrQrPayload: SupplierPublicOrgId,
                SupplierBranchId: Iloilo));

        Assert.False(dup.IsSuccess);
        Assert.Equal(ConnectedSupplierErrorCodes.DuplicateRelationship, dup.ErrorCode);

        // Cancel Active: rejected
        var cancel = new CancelPendingConnection(relationships, uow, access);
        var cancelActive = await cancel.ExecuteAsync(
            BuyerA,
            relationships.Items.Single().Id.Value,
            new CancelConnectionRequest());

        Assert.False(cancelActive.IsSuccess);
        Assert.Equal(ConnectedSupplierErrorCodes.CancelNotPending, cancelActive.ErrorCode);

        // Supplier master deactivate must not affect connection row status.
        var pending2Relationships = new InMemoryRelationships();
        var suppliers2 = new InMemorySuppliers();
        var request2 = new RequestConnection(
            pending2Relationships,
            suppliers2,
            uow,
            access,
            resolve,
            locations);

        var pending2 = await request2.ExecuteAsync(
            BuyerA,
            new RequestConnectionRequest(
                SupplierPublicOrganizationIdOrQrPayload: SupplierPublicOrgId,
                SupplierBranchId: Iloilo));
        Assert.True(pending2.IsSuccess);

        var supplierMasterId = suppliers2.LastAdded!.Id.Value;
        Assert.Equal(ConnectedSupplierRelationshipStatus.Pending, pending2Relationships.Items.Single().Status);

        var deactivate = new DeactivateSupplier(suppliers2, uow, access);
        var deactivated = await deactivate.ExecuteAsync(BuyerA, supplierMasterId);
        Assert.True(deactivated.IsSuccess, $"{deactivated.ErrorCode}: {deactivated.ErrorMessage}");

        Assert.Equal(ConnectedSupplierRelationshipStatus.Pending, pending2Relationships.Items.Single().Status);
    }

    [Fact]
    public async Task Cancel_pending_request_is_tenant_scoped_and_requires_ownership()
    {
        var relationships = new InMemoryRelationships();
        var suppliers = new InMemorySuppliers();
        var uow = new FakeUow();
        var access = new FakeAccess();
        var resolve = new FakeResolve { SupplierOrgId = SupplierOrg };
        var locations = new FakeLocations
        {
            Locations = [
                new PlatformSupplierLocationDto(Iloilo, "Iloilo Branch", "BR-ILO", IsPrimary: false),
            ]
        };

        var request = new RequestConnection(
            relationships,
            suppliers,
            uow,
            access,
            resolve,
            locations);

        var pending = await request.ExecuteAsync(
            BuyerA,
            new RequestConnectionRequest(
                SupplierPublicOrganizationIdOrQrPayload: SupplierPublicOrgId,
                SupplierBranchId: Iloilo));
        Assert.True(pending.IsSuccess);

        var relationshipId = relationships.Items.Single().Id.Value;
        Assert.NotNull(await relationships.FindOpenAsync(
            PosOrganizationId.From(BuyerA),
            PosOrganizationId.From(SupplierOrg)));

        // Wrong buyer cannot cancel someone else's Pending request.
        var cancel = new CancelPendingConnection(relationships, uow, access);
        var forbidden = await cancel.ExecuteAsync(
            BuyerB,
            relationshipId,
            new CancelConnectionRequest());

        Assert.False(forbidden.IsSuccess);
        Assert.Equal(ConnectedSupplierErrorCodes.NotFound, forbidden.ErrorCode);

        // Relationship remains Pending for the rightful buyer.
        Assert.Equal(ConnectedSupplierRelationshipStatus.Pending, relationships.Items.Single().Status);
        Assert.Null(await relationships.FindOpenAsync(
            PosOrganizationId.From(BuyerB),
            PosOrganizationId.From(SupplierOrg)));
    }
}

