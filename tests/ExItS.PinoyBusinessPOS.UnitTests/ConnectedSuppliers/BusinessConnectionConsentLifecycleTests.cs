using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;

namespace ExItS.PinoyBusinessPOS.UnitTests.ConnectedSuppliers;

public sealed class BusinessConnectionConsentLifecycleTests
{
    private static readonly PosOrganizationId Kizy = PosOrganizationId.From(Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111"));
    private static readonly PosOrganizationId Mica = PosOrganizationId.From(Guid.Parse("bbbbbbbb-2222-2222-2222-222222222222"));
    private static readonly PosOrganizationId Other = PosOrganizationId.From(Guid.Parse("cccccccc-3333-3333-3333-333333333333"));
    private static readonly DateTimeOffset Now = new(2026, 9, 10, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Buyer_initiated_request_is_pending_with_InitiatedByParty_Buyer()
    {
        var r = ConnectedSupplierRelationship.Request(Kizy, Mica, Now);
        Assert.Equal(ConnectedSupplierRelationshipStatus.Pending, r.Status);
        Assert.Equal(ConnectionInitiatedByParty.Buyer, r.InitiatedByParty);
        Assert.Equal(Kizy, r.InitiatorOrganizationId);
        Assert.Equal(Mica, r.RecipientOrganizationId);
    }

    [Fact]
    public void Seller_invite_is_pending_with_buyer_kizy_supplier_mica()
    {
        var r = ConnectedSupplierRelationship.InviteBuyer(
            Kizy,
            Mica,
            Now,
            buyerDisplayName: "Kizy Bakery",
            buyerPublicOrganizationId: "ORG622085",
            supplierDisplayName: "Mica Store",
            supplierPublicOrganizationId: "ORG123456",
            supplierBranchId: Guid.Parse("dddddddd-4444-4444-4444-444444444444"),
            supplierBranchName: "Main");
        Assert.Equal(ConnectedSupplierRelationshipStatus.Pending, r.Status);
        Assert.Equal(Kizy, r.BuyerOrganizationId);
        Assert.Equal(Mica, r.SupplierOrganizationId);
        Assert.Equal(ConnectionInitiatedByParty.Supplier, r.InitiatedByParty);
        Assert.Equal(Mica, r.InitiatorOrganizationId);
        Assert.Equal(Kizy, r.RecipientOrganizationId);
    }

    [Fact]
    public void Only_recipient_may_accept_seller_invite()
    {
        var r = ConnectedSupplierRelationship.InviteBuyer(Kizy, Mica, Now);
        Assert.True(r.IsRecipient(Kizy));
        Assert.False(r.IsRecipient(Mica));
        Assert.False(r.IsInitiator(Kizy));
        Assert.True(r.IsInitiator(Mica));
        r.Approve(Now.AddMinutes(1));
        Assert.Equal(ConnectedSupplierRelationshipStatus.Active, r.Status);
    }

    [Fact]
    public void Only_recipient_may_accept_buyer_request()
    {
        var r = ConnectedSupplierRelationship.Request(Kizy, Mica, Now);
        Assert.True(r.IsRecipient(Mica));
        Assert.False(r.IsRecipient(Kizy));
    }

    [Fact]
    public async Task Seller_cannot_accept_own_invitation()
    {
        var repo = new InMemoryRelationships();
        var invite = ConnectedSupplierRelationship.InviteBuyer(Kizy, Mica, Now);
        await repo.AddAsync(invite);
        var access = new FakeAccess();
        var respond = new RespondConnection(
            repo,
            new FakeUow(),
            access,
            OrgWideBranchAccess.Instance,
            clock: new FixedClock(Now));
        var result = await respond.ExecuteAsync(
            Mica.Value,
            invite.Id.Value,
            approve: true,
            new RespondConnectionRequest());
        Assert.False(result.IsSuccess);
        Assert.Equal(ConnectedSupplierErrorCodes.NotFound, result.ErrorCode);
    }

    [Fact]
    public async Task Buyer_cannot_accept_own_request()
    {
        var repo = new InMemoryRelationships();
        var request = ConnectedSupplierRelationship.Request(Kizy, Mica, Now);
        await repo.AddAsync(request);
        var respond = new RespondConnection(
            repo,
            new FakeUow(),
            new FakeAccess(),
            OrgWideBranchAccess.Instance,
            clock: new FixedClock(Now));
        var result = await respond.ExecuteAsync(
            Kizy.Value,
            request.Id.Value,
            approve: true,
            new RespondConnectionRequest(CatalogSharingMode: "SelectedOnly"));
        Assert.False(result.IsSuccess);
        Assert.Equal(ConnectedSupplierErrorCodes.NotFound, result.ErrorCode);
    }

    [Fact]
    public async Task Unrelated_org_cannot_respond()
    {
        var repo = new InMemoryRelationships();
        var invite = ConnectedSupplierRelationship.InviteBuyer(Kizy, Mica, Now);
        await repo.AddAsync(invite);
        var respond = new RespondConnection(
            repo,
            new FakeUow(),
            new FakeAccess(),
            OrgWideBranchAccess.Instance,
            clock: new FixedClock(Now));
        var result = await respond.ExecuteAsync(
            Other.Value,
            invite.Id.Value,
            approve: false,
            new RespondConnectionRequest());
        Assert.False(result.IsSuccess);
        Assert.Equal(ConnectedSupplierErrorCodes.NotFound, result.ErrorCode);
    }

    [Fact]
    public async Task Only_initiator_may_cancel_pending()
    {
        var repo = new InMemoryRelationships();
        var invite = ConnectedSupplierRelationship.InviteBuyer(Kizy, Mica, Now);
        await repo.AddAsync(invite);
        var cancel = new CancelPendingConnection(repo, new FakeUow(), new FakeAccess(), clock: new FixedClock(Now));

        var recipientCancel = await cancel.ExecuteAsync(
            Kizy.Value,
            invite.Id.Value,
            new CancelConnectionRequest());
        Assert.False(recipientCancel.IsSuccess);

        var initiatorCancel = await cancel.ExecuteAsync(
            Mica.Value,
            invite.Id.Value,
            new CancelConnectionRequest());
        Assert.True(initiatorCancel.IsSuccess);
        Assert.Equal(ConnectedSupplierRelationshipStatus.Declined, (await repo.GetAsync(invite.Id))!.Status);
    }

    [Fact]
    public async Task Buyer_accept_seller_invite_creates_exactly_one_supplier_master()
    {
        var repo = new InMemoryRelationships();
        var suppliers = new InMemorySuppliers();
        var invite = ConnectedSupplierRelationship.InviteBuyer(
            Kizy,
            Mica,
            Now,
            supplierDisplayName: "Mica Store",
            supplierPublicOrganizationId: "ORG123456");
        await repo.AddAsync(invite);
        var respond = new RespondConnection(
            repo,
            new FakeUow(),
            new FakeAccess(),
            OrgWideBranchAccess.Instance,
            clock: new FixedClock(Now),
            suppliers: suppliers);
        var result = await respond.ExecuteAsync(
            Kizy.Value,
            invite.Id.Value,
            approve: true,
            new RespondConnectionRequest());
        Assert.True(result.IsSuccess);
        Assert.Equal(ConnectedSupplierRelationshipStatus.Active, (await repo.GetAsync(invite.Id))!.Status);
        Assert.Equal(CatalogSharingMode.AllEligible, (await repo.GetAsync(invite.Id))!.CatalogSharingMode);
        Assert.Null((await repo.GetAsync(invite.Id))!.CustomerDiscountPercent);
        Assert.Single(suppliers.Items);
        Assert.Equal(invite.Id, suppliers.Items[0].ConnectedRelationshipId);

        // Retry Accept must not create a second master (invalid transition after Active).
        var again = await respond.ExecuteAsync(
            Kizy.Value,
            invite.Id.Value,
            approve: true,
            new RespondConnectionRequest());
        Assert.False(again.IsSuccess);
        Assert.Single(suppliers.Items);
    }

    [Fact]
    public async Task Buyer_decline_seller_invite_creates_no_supplier_master()
    {
        var repo = new InMemoryRelationships();
        var suppliers = new InMemorySuppliers();
        var invite = ConnectedSupplierRelationship.InviteBuyer(Kizy, Mica, Now);
        await repo.AddAsync(invite);
        var respond = new RespondConnection(
            repo,
            new FakeUow(),
            new FakeAccess(),
            OrgWideBranchAccess.Instance,
            clock: new FixedClock(Now),
            suppliers: suppliers);
        var result = await respond.ExecuteAsync(
            Kizy.Value,
            invite.Id.Value,
            approve: false,
            new RespondConnectionRequest());
        Assert.True(result.IsSuccess);
        Assert.Empty(suppliers.Items);
        Assert.Equal(ConnectedSupplierRelationshipStatus.Declined, (await repo.GetAsync(invite.Id))!.Status);
    }

    [Fact]
    public async Task Open_relationship_blocks_duplicate_invite()
    {
        var repo = new InMemoryRelationships();
        await repo.AddAsync(ConnectedSupplierRelationship.InviteBuyer(Kizy, Mica, Now));
        var resolve = new StubOrgResolve(Kizy.Value, "ORG622085", "Kizy Bakery");
        var locations = new StubLocations(Guid.Parse("dddddddd-4444-4444-4444-444444444444"), "Main");
        var invite = new InviteBusinessCustomerConnection(
            repo,
            new FakeUow(),
            new FakeAccess(),
            resolve,
            locations,
            clock: new FixedClock(Now));
        var result = await invite.ExecuteAsync(
            Mica.Value,
            new InviteBusinessCustomerRequest(BuyerPublicOrganizationIdOrQrPayload: "ORG622085"));
        Assert.False(result.IsSuccess);
        Assert.Equal(ConnectedSupplierErrorCodes.DuplicateRelationship, result.ErrorCode);
    }

    [Fact]
    public async Task Pending_buyer_request_surfaces_specific_error_for_seller_invite()
    {
        var repo = new InMemoryRelationships();
        await repo.AddAsync(ConnectedSupplierRelationship.Request(Kizy, Mica, Now));
        var invite = new InviteBusinessCustomerConnection(
            repo,
            new FakeUow(),
            new FakeAccess(),
            new StubOrgResolve(Kizy.Value, "ORG622085", "Kizy Bakery"),
            new StubLocations(Guid.Parse("dddddddd-4444-4444-4444-444444444444"), "Main"),
            clock: new FixedClock(Now));
        var result = await invite.ExecuteAsync(
            Mica.Value,
            new InviteBusinessCustomerRequest(BuyerPublicOrganizationIdOrQrPayload: "ORG622085"));
        Assert.False(result.IsSuccess);
        Assert.Equal(ConnectedSupplierErrorCodes.PendingBuyerRequestExists, result.ErrorCode);
    }

    [Fact]
    public async Task Incoming_list_includes_buyer_recipient_seller_invites()
    {
        var repo = new InMemoryRelationships();
        await repo.AddAsync(ConnectedSupplierRelationship.InviteBuyer(Kizy, Mica, Now));
        await repo.AddAsync(ConnectedSupplierRelationship.Request(Other, Kizy, Now));
        var list = new ListIncomingConnectionRequests(repo, new FakeAccess(), OrgWideBranchAccess.Instance);
        var result = await list.ExecuteAsync(Kizy.Value);
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);
    }

    private sealed class FakeAccess : IPosCommercialAccessAccessor
    {
        public PosCommercialAccess Current { get; set; } = PosCommercialAccess.DevelopmentDefault;
    }

    private sealed class OrgWideBranchAccess : IAuthorizedBranchGroupingDirectory
    {
        public static readonly OrgWideBranchAccess Instance = new();
        public Task<AuthorizedBranchScope> ListAuthorizedAsync(Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AuthorizedBranchScope(IsOrganizationWide: true, []));
    }

    private sealed class FakeUow : IPosUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<T> ExecuteInSerializableTransactionAsync<T>(
            Func<CancellationToken, Task<T>> action,
            CancellationToken cancellationToken = default) =>
            action(cancellationToken);
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
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
            PosOrganizationId buyer, PosOrganizationId supplier, CancellationToken ct = default) =>
            Task.FromResult(_items.FirstOrDefault(x =>
                x.BuyerOrganizationId == buyer
                && x.SupplierOrganizationId == supplier
                && (x.Status is ConnectedSupplierRelationshipStatus.Pending or ConnectedSupplierRelationshipStatus.Active)));
        public Task<ConnectedSupplierRelationship?> GetAsync(
            ConnectedSupplierRelationshipId id, CancellationToken ct = default) =>
            Task.FromResult(_items.FirstOrDefault(x => x.Id.Value == id.Value));
        public Task<IReadOnlyList<ConnectedSupplierRelationship>> ListAsync(
            PosOrganizationId organizationId, bool supplierView, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ConnectedSupplierRelationship>>(
                _items.Where(x => supplierView
                    ? x.SupplierOrganizationId == organizationId
                    : x.BuyerOrganizationId == organizationId).ToList());
        public Task UpdateAsync(ConnectedSupplierRelationship relationship, CancellationToken ct = default) =>
            Task.CompletedTask;
    }

    private sealed class InMemorySuppliers : ISupplierRepository
    {
        public List<Supplier> Items { get; } = [];
        public Task AddAsync(Supplier supplier, CancellationToken cancellationToken = default)
        {
            Items.Add(supplier);
            return Task.CompletedTask;
        }
        public Task UpdateAsync(Supplier supplier, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<Supplier?> GetByIdAsync(
            PosOrganizationId organizationId, SupplierId supplierId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(x => x.OrganizationId == organizationId && x.Id == supplierId));
        public Task<(IReadOnlyList<Supplier> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            SupplierFilter filter,
            int skip,
            int take,
            IReadOnlyCollection<Guid>? restrictToSupplierIds = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<Supplier>, int)>((Items.Where(x => x.OrganizationId == organizationId).ToList(), Items.Count));
        public Task<Supplier?> FindActiveByNormalizedNameAsync(
            PosOrganizationId organizationId, string normalizedName, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(x =>
                x.OrganizationId == organizationId
                && x.Status == SupplierStatus.Active
                && string.Equals(Supplier.Normalize(x.Name), normalizedName, StringComparison.Ordinal)));
        public Task<Supplier?> FindActiveByNormalizedEmailAsync(
            PosOrganizationId organizationId, string normalizedEmail, CancellationToken cancellationToken = default) =>
            Task.FromResult<Supplier?>(null);
        public Task<Supplier?> FindActiveByNormalizedMobileAsync(
            PosOrganizationId organizationId, string normalizedMobile, CancellationToken cancellationToken = default) =>
            Task.FromResult<Supplier?>(null);
        public Task<Supplier?> FindActiveByNormalizedTaxAsync(
            PosOrganizationId organizationId, string normalizedTax, CancellationToken cancellationToken = default) =>
            Task.FromResult<Supplier?>(null);
        public Task<Supplier?> FindByConnectedRelationshipIdAsync(
            PosOrganizationId organizationId,
            ConnectedSupplierRelationshipId relationshipId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(x =>
                x.OrganizationId == organizationId && x.ConnectedRelationshipId == relationshipId));
        public Task<string> AllocateNextSupplierCodeAsync(
            PosOrganizationId organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult($"SUP-{Items.Count + 1:D6}");
        public Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesByIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<Guid> supplierIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(new Dictionary<Guid, string>());
    }

    private sealed class StubOrgResolve(
        Guid organizationId,
        string publicId,
        string displayName) : IPlatformOrganizationPublicResolve
    {
        public Task<ApplicationResult<PlatformOrganizationPublicResolveResult>> ResolveOrganizationForConnectedSupplierAsync(
            string publicOrganizationIdOrQrPayload, CancellationToken cancellationToken = default) =>
            Task.FromResult(ApplicationResult<PlatformOrganizationPublicResolveResult>.Success(
                new PlatformOrganizationPublicResolveResult(organizationId, publicId, displayName)));

        public Task<ApplicationResult<PlatformOrganizationPublicResolveResult>> GetOrganizationPublicIdentityAsync(
            Guid organizationIdValue, CancellationToken cancellationToken = default) =>
            Task.FromResult(ApplicationResult<PlatformOrganizationPublicResolveResult>.Success(
                new PlatformOrganizationPublicResolveResult(organizationIdValue, publicId, displayName)));
    }

    private sealed class StubLocations(Guid branchId, string name) : IPlatformSupplierLocationDirectory
    {
        public Task<ApplicationResult<IReadOnlyList<PlatformSupplierLocationDto>>> ListActiveLocationsAsync(
            string publicOrganizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(ApplicationResult<IReadOnlyList<PlatformSupplierLocationDto>>.Success(
                [new PlatformSupplierLocationDto(branchId, name, "MAIN", IsPrimary: true)]));
    }
}
