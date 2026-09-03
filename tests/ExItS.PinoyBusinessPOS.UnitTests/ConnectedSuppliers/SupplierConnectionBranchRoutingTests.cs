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

/// <summary>SUPBRREQ-01..14 supplier connection branch routing.</summary>
public sealed class SupplierConnectionBranchRoutingTests
{
    private static readonly Guid Manila = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Cebu = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid Iloilo = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

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

    private sealed class FakeRelationships : IConnectedSupplierRelationshipRepository
    {
        public List<ConnectedSupplierRelationship> Items { get; } = [];
        public ConnectedSupplierRelationship? LastAdded { get; private set; }

        public Task AddAsync(ConnectedSupplierRelationship relationship, CancellationToken ct = default)
        {
            LastAdded = relationship;
            Items.Add(relationship);
            return Task.CompletedTask;
        }

        public Task<ConnectedSupplierRelationship?> FindOpenAsync(
            PosOrganizationId buyer,
            PosOrganizationId supplier,
            CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(x =>
                x.BuyerOrganizationId == buyer
                && x.SupplierOrganizationId == supplier
                && x.Status is ConnectedSupplierRelationshipStatus.Pending
                    or ConnectedSupplierRelationshipStatus.Active));

        public Task<ConnectedSupplierRelationship?> GetAsync(
            ConnectedSupplierRelationshipId id,
            CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(x => x.Id == id));

        public Task<IReadOnlyList<ConnectedSupplierRelationship>> ListAsync(
            PosOrganizationId organizationId,
            bool supplierView,
            CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ConnectedSupplierRelationship>>(
                Items.Where(x =>
                        supplierView
                            ? x.SupplierOrganizationId == organizationId
                            : x.BuyerOrganizationId == organizationId)
                    .ToList());

        public Task UpdateAsync(ConnectedSupplierRelationship relationship, CancellationToken ct = default) =>
            Task.CompletedTask;
    }

    private sealed class FakeSuppliers : ISupplierRepository
    {
        private int _n;

        public Task AddAsync(Supplier supplier, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<string> AllocateNextSupplierCodeAsync(
            PosOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult($"SUP-{++_n:D6}");

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

        public Task<Supplier?> FindActiveByNormalizedNameAsync(
            PosOrganizationId organizationId,
            string normalizedName,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Supplier?>(null);

        public Task<Supplier?> FindActiveByNormalizedTaxAsync(
            PosOrganizationId organizationId,
            string normalizedTax,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Supplier?>(null);

        public Task<Supplier?> GetByIdAsync(
            PosOrganizationId organizationId,
            SupplierId supplierId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Supplier?>(null);

        public Task<(IReadOnlyList<Supplier> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            SupplierFilter filter,
            int skip,
            int take,
            IReadOnlyCollection<Guid>? restrictToSupplierIds = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<Supplier>, int)>(([], 0));

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
            Task.FromResult(ApplicationResult<PlatformOrganizationPublicResolveResult>.Success(
                new PlatformOrganizationPublicResolveResult(SupplierOrgId, "ORG123456", "ABC Wholesale")));

        public Task<ApplicationResult<PlatformOrganizationPublicResolveResult>> GetOrganizationPublicIdentityAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(ApplicationResult<PlatformOrganizationPublicResolveResult>.Success(
                new PlatformOrganizationPublicResolveResult(organizationId, "ORG000001", "Mica Store")));
    }

    private sealed class FakeLocations : IPlatformSupplierLocationDirectory
    {
        public required IReadOnlyList<PlatformSupplierLocationDto> Locations { get; init; }

        public Task<ApplicationResult<IReadOnlyList<PlatformSupplierLocationDto>>> ListActiveLocationsAsync(
            string publicOrganizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(ApplicationResult<IReadOnlyList<PlatformSupplierLocationDto>>.Success(Locations));
    }

    private sealed class CapturingNotifications : IOrganizationBusinessNotificationPublisher
    {
        public Guid? LastTargetBranchId { get; private set; }
        public string? LastPreview { get; private set; }
        public string? LastRelatedType { get; private set; }

        public Task PublishAsync(
            Guid sourceOrganizationId,
            Guid recipientOrganizationId,
            string relatedType,
            string relatedId,
            string title,
            string preview,
            CancellationToken cancellationToken = default,
            Guid? targetBranchId = null)
        {
            LastRelatedType = relatedType;
            LastPreview = preview;
            LastTargetBranchId = targetBranchId;
            return Task.CompletedTask;
        }

        public Task MarkRelatedReadAsync(
            Guid organizationId,
            string relatedType,
            string relatedId,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FakeBranchAccess : IAuthorizedBranchGroupingDirectory
    {
        public required AuthorizedBranchScope Scope { get; init; }

        public Task<AuthorizedBranchScope> ListAuthorizedAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Scope);
    }

    private static ConnectedSupplierRelationship PendingIloilo(
        PosOrganizationId buyer,
        PosOrganizationId supplier) =>
        ConnectedSupplierRelationship.Request(
            buyer,
            supplier,
            DateTimeOffset.UtcNow,
            buyerDisplayName: "Mica Store",
            buyerPublicOrganizationId: "ORG000001",
            supplierDisplayName: "ABC Wholesale",
            supplierPublicOrganizationId: "ORG123456",
            supplierBranchId: Iloilo,
            supplierBranchName: "Iloilo Branch");

    [Fact]
    public async Task SUPBRREQ01_request_to_Iloilo_persists_Iloilo()
    {
        var supplierOrg = Guid.NewGuid();
        var relationships = new FakeRelationships();
        var useCase = new RequestConnection(
            relationships,
            new FakeSuppliers(),
            new FakeUow(),
            new FakeAccess(),
            new FakeResolve { SupplierOrgId = supplierOrg },
            new FakeLocations
            {
                Locations =
                [
                    new(Manila, "Manila Branch", "BR-MNL", true),
                    new(Iloilo, "Iloilo Branch", "BR-ILO", false)
                ]
            });

        var result = await useCase.ExecuteAsync(
            Guid.NewGuid(),
            new RequestConnectionRequest(
                SupplierPublicOrganizationIdOrQrPayload: "ORG123456",
                SupplierBranchId: Iloilo));

        Assert.True(result.IsSuccess, $"{result.ErrorCode}: {result.ErrorMessage}");
        Assert.Equal(Iloilo, relationships.LastAdded!.SupplierBranchId);
        Assert.Equal(supplierOrg, relationships.LastAdded.SupplierOrganizationId.Value);
    }

    [Fact]
    public async Task SUPBRREQ02_Iloilo_workspace_sees_pending_request()
    {
        var buyer = PosOrganizationId.From(Guid.NewGuid());
        var supplier = PosOrganizationId.From(Guid.NewGuid());
        var repo = new FakeRelationships();
        await repo.AddAsync(PendingIloilo(buyer, supplier));

        var listed = await new ListRelationships(repo, new FakeAccess())
            .ExecuteAsync(supplier.Value, supplierView: true, workspaceBranchId: Iloilo, organizationWideInbox: false);

        Assert.True(listed.IsSuccess);
        Assert.Single(listed.Value!);
        Assert.Equal(Iloilo, listed.Value![0].SupplierBranchId);
    }

    [Fact]
    public async Task SUPBRREQ03_Cebu_workspace_does_not_see_Iloilo_request()
    {
        var buyer = PosOrganizationId.From(Guid.NewGuid());
        var supplier = PosOrganizationId.From(Guid.NewGuid());
        var repo = new FakeRelationships();
        await repo.AddAsync(PendingIloilo(buyer, supplier));

        var listed = await new ListRelationships(repo, new FakeAccess())
            .ExecuteAsync(supplier.Value, supplierView: true, workspaceBranchId: Cebu, organizationWideInbox: false);

        Assert.True(listed.IsSuccess);
        Assert.Empty(listed.Value!);
    }

    [Fact]
    public async Task SUPBRREQ04_Manila_workspace_does_not_see_Iloilo_request()
    {
        var buyer = PosOrganizationId.From(Guid.NewGuid());
        var supplier = PosOrganizationId.From(Guid.NewGuid());
        var repo = new FakeRelationships();
        await repo.AddAsync(PendingIloilo(buyer, supplier));

        var listed = await new ListRelationships(repo, new FakeAccess())
            .ExecuteAsync(supplier.Value, supplierView: true, workspaceBranchId: Manila, organizationWideInbox: false);

        Assert.True(listed.IsSuccess);
        Assert.Empty(listed.Value!);
    }

    [Fact]
    public async Task SUPBRREQ05_Iloilo_authorized_manager_can_accept()
    {
        var buyer = PosOrganizationId.From(Guid.NewGuid());
        var supplier = PosOrganizationId.From(Guid.NewGuid());
        var repo = new FakeRelationships();
        var relationship = PendingIloilo(buyer, supplier);
        await repo.AddAsync(relationship);

        var respond = new RespondConnection(
            repo,
            new FakeUow(),
            new FakeAccess(),
            branchAccess: new FakeBranchAccess
            {
                Scope = new AuthorizedBranchScope(
                    false,
                    [new AuthorizedBranchGrouping(Iloilo, "Iloilo Branch", null, null)])
            });

        var result = await respond.ExecuteAsync(
            supplier.Value,
            relationship.Id.Value,
            approve: true,
            new RespondConnectionRequest(ConfirmCatalogSharing: true, CatalogSharingMode: "SelectedOnly"));

        Assert.True(result.IsSuccess, $"{result.ErrorCode}: {result.ErrorMessage}");
        Assert.Equal("Active", result.Value!.Status);
    }

    [Fact]
    public async Task SUPBRREQ06_Cebu_branch_staff_forged_accept_denied()
    {
        var buyer = PosOrganizationId.From(Guid.NewGuid());
        var supplier = PosOrganizationId.From(Guid.NewGuid());
        var repo = new FakeRelationships();
        var relationship = PendingIloilo(buyer, supplier);
        await repo.AddAsync(relationship);

        var respond = new RespondConnection(
            repo,
            new FakeUow(),
            new FakeAccess(),
            branchAccess: new FakeBranchAccess
            {
                Scope = new AuthorizedBranchScope(
                    false,
                    [new AuthorizedBranchGrouping(Cebu, "Cebu Branch", null, null)])
            });

        var result = await respond.ExecuteAsync(
            supplier.Value,
            relationship.Id.Value,
            approve: true,
            new RespondConnectionRequest(ConfirmCatalogSharing: true, CatalogSharingMode: "SelectedOnly"));

        Assert.False(result.IsSuccess);
        Assert.Equal(ConnectedSupplierErrorCodes.BranchResponseForbidden, result.ErrorCode);
        Assert.Equal(ConnectedSupplierRelationshipStatus.Pending, relationship.Status);
    }

    [Fact]
    public async Task SUPBRREQ07_area_staff_allowed_only_when_target_branch_accessible()
    {
        var buyer = PosOrganizationId.From(Guid.NewGuid());
        var supplier = PosOrganizationId.From(Guid.NewGuid());
        var repo = new FakeRelationships();
        var relationship = PendingIloilo(buyer, supplier);
        await repo.AddAsync(relationship);

        var denied = await new RespondConnection(
            repo,
            new FakeUow(),
            new FakeAccess(),
            branchAccess: new FakeBranchAccess
            {
                Scope = new AuthorizedBranchScope(
                    false,
                    [new AuthorizedBranchGrouping(Manila, "Manila Branch", Guid.NewGuid(), "Visayas")])
            }).ExecuteAsync(
            supplier.Value,
            relationship.Id.Value,
            approve: true,
            new RespondConnectionRequest(ConfirmCatalogSharing: true, CatalogSharingMode: "SelectedOnly"));
        Assert.False(denied.IsSuccess);

        var allowed = await new RespondConnection(
            repo,
            new FakeUow(),
            new FakeAccess(),
            branchAccess: new FakeBranchAccess
            {
                Scope = new AuthorizedBranchScope(
                    false,
                    [
                        new AuthorizedBranchGrouping(Cebu, "Cebu Branch", Guid.NewGuid(), "Visayas"),
                        new AuthorizedBranchGrouping(Iloilo, "Iloilo Branch", Guid.NewGuid(), "Visayas")
                    ])
            }).ExecuteAsync(
            supplier.Value,
            relationship.Id.Value,
            approve: true,
            new RespondConnectionRequest(ConfirmCatalogSharing: true, CatalogSharingMode: "SelectedOnly"));
        Assert.True(allowed.IsSuccess, $"{allowed.ErrorCode}: {allowed.ErrorMessage}");
    }

    [Fact]
    public async Task SUPBRREQ08_owner_admin_may_manage_target_request_globally()
    {
        var buyer = PosOrganizationId.From(Guid.NewGuid());
        var supplier = PosOrganizationId.From(Guid.NewGuid());
        var repo = new FakeRelationships();
        var relationship = PendingIloilo(buyer, supplier);
        await repo.AddAsync(relationship);

        var globalList = await new ListRelationships(repo, new FakeAccess())
            .ExecuteAsync(supplier.Value, supplierView: true, organizationWideInbox: true);
        Assert.Single(globalList.Value!);

        var respond = new RespondConnection(
            repo,
            new FakeUow(),
            new FakeAccess(),
            branchAccess: new FakeBranchAccess
            {
                Scope = new AuthorizedBranchScope(true, [])
            });
        var result = await respond.ExecuteAsync(
            supplier.Value,
            relationship.Id.Value,
            approve: false,
            new RespondConnectionRequest());
        Assert.True(result.IsSuccess, $"{result.ErrorCode}: {result.ErrorMessage}");
        Assert.Equal("Declined", result.Value!.Status);
    }

    [Fact]
    public async Task SUPBRREQ09_notification_carries_target_branch()
    {
        var notifications = new CapturingNotifications();
        var useCase = new RequestConnection(
            new FakeRelationships(),
            new FakeSuppliers(),
            new FakeUow(),
            new FakeAccess(),
            new FakeResolve { SupplierOrgId = Guid.NewGuid() },
            new FakeLocations { Locations = [new(Iloilo, "Iloilo Branch", "BR-ILO", true)] },
            notifications);

        var result = await useCase.ExecuteAsync(
            Guid.NewGuid(),
            new RequestConnectionRequest(SupplierPublicOrganizationIdOrQrPayload: "ORG123456"));

        Assert.True(result.IsSuccess);
        Assert.Equal(SupplierConnectionNotificationTypes.Requested, notifications.LastRelatedType);
        Assert.Equal(Iloilo, notifications.LastTargetBranchId);
        Assert.Contains("Location: Iloilo Branch", notifications.LastPreview, StringComparison.Ordinal);
    }

    [Fact]
    public void SUPBRREQ10_branch_notification_count_excludes_other_branches()
    {
        // Mirrors Platform OrganizationNotificationBranchScope.IsVisible
        static bool IsVisible(Guid? notificationBranchId, Guid? workspaceBranchId) =>
            workspaceBranchId is null
            || notificationBranchId is null
            || notificationBranchId == workspaceBranchId;

        Assert.True(IsVisible(Iloilo, Iloilo));
        Assert.False(IsVisible(Iloilo, Cebu));
        Assert.True(IsVisible(null, Cebu));
        Assert.True(IsVisible(Iloilo, null));
    }

    [Fact]
    public void SUPBRREQ11_existing_org_wide_notification_types_remain_org_wide()
    {
        // Only SupplierConnectionRequested is branch-targetable on Platform.
        Assert.Equal("SupplierConnectionRequested", SupplierConnectionNotificationTypes.Requested);
        Assert.NotEqual(SupplierConnectionNotificationTypes.Requested, SupplierConnectionNotificationTypes.Accepted);
        Assert.NotEqual(SupplierConnectionNotificationTypes.Requested, SupplierConnectionNotificationTypes.Declined);
    }

    [Fact]
    public async Task SUPBRREQ12_legacy_null_branch_request_fails_closed_for_ordinary_branch_staff()
    {
        var buyer = PosOrganizationId.From(Guid.NewGuid());
        var supplier = PosOrganizationId.From(Guid.NewGuid());
        var legacy = ConnectedSupplierRelationship.Request(buyer, supplier, DateTimeOffset.UtcNow);
        Assert.Null(legacy.SupplierBranchId);

        var repo = new FakeRelationships();
        await repo.AddAsync(legacy);

        var listed = await new ListRelationships(repo, new FakeAccess())
            .ExecuteAsync(supplier.Value, supplierView: true, workspaceBranchId: Iloilo, organizationWideInbox: false);
        Assert.Empty(listed.Value!);

        var respond = new RespondConnection(
            repo,
            new FakeUow(),
            new FakeAccess(),
            branchAccess: new FakeBranchAccess
            {
                Scope = new AuthorizedBranchScope(
                    false,
                    [new AuthorizedBranchGrouping(Iloilo, "Iloilo Branch", null, null)])
            });
        var denied = await respond.ExecuteAsync(
            supplier.Value,
            legacy.Id.Value,
            approve: true,
            new RespondConnectionRequest(ConfirmCatalogSharing: true, CatalogSharingMode: "SelectedOnly"));
        Assert.False(denied.IsSuccess);
        Assert.Equal(ConnectedSupplierErrorCodes.BranchResponseForbidden, denied.ErrorCode);

        var ownerList = await new ListRelationships(repo, new FakeAccess())
            .ExecuteAsync(supplier.Value, supplierView: true, organizationWideInbox: true);
        Assert.Single(ownerList.Value!);
    }

    [Fact]
    public async Task SUPBRREQ13_buyer_side_relationship_visibility_unaffected()
    {
        var buyer = PosOrganizationId.From(Guid.NewGuid());
        var supplier = PosOrganizationId.From(Guid.NewGuid());
        var repo = new FakeRelationships();
        await repo.AddAsync(PendingIloilo(buyer, supplier));

        var listed = await new ListRelationships(repo, new FakeAccess())
            .ExecuteAsync(buyer.Value, supplierView: false, workspaceBranchId: Cebu, organizationWideInbox: false);

        Assert.True(listed.IsSuccess);
        Assert.Single(listed.Value!);
        Assert.Equal(Iloilo, listed.Value![0].SupplierBranchId);
    }

    [Fact]
    public async Task SUPBRREQ14_no_branch_duplication_of_canonical_relationship()
    {
        var supplierOrg = Guid.NewGuid();
        var relationships = new FakeRelationships();
        var useCase = new RequestConnection(
            relationships,
            new FakeSuppliers(),
            new FakeUow(),
            new FakeAccess(),
            new FakeResolve { SupplierOrgId = supplierOrg },
            new FakeLocations
            {
                Locations =
                [
                    new(Manila, "Manila Branch", "BR-MNL", true),
                    new(Iloilo, "Iloilo Branch", "BR-ILO", false)
                ]
            });

        var buyer = Guid.NewGuid();
        Assert.True((await useCase.ExecuteAsync(
            buyer,
            new RequestConnectionRequest(
                SupplierPublicOrganizationIdOrQrPayload: "ORG123456",
                SupplierBranchId: Iloilo))).IsSuccess);

        var duplicate = await useCase.ExecuteAsync(
            buyer,
            new RequestConnectionRequest(
                SupplierPublicOrganizationIdOrQrPayload: "ORG123456",
                SupplierBranchId: Manila));

        Assert.False(duplicate.IsSuccess);
        Assert.Equal(ConnectedSupplierErrorCodes.DuplicateRelationship, duplicate.ErrorCode);
        Assert.Single(relationships.Items);
        Assert.Equal(supplierOrg, relationships.Items[0].SupplierOrganizationId.Value);
    }
}
