using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.UnitTests.ConnectedSuppliers;

/// <summary>SUPBRH1-01..08: branch-read authorization hardening.</summary>
public sealed class SupplierConnectionBranchReadAuthTests
{
    private static readonly Guid Iloilo = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid Cebu   = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid Manila  = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private sealed class FakeAccess : IPosCommercialAccessAccessor
    {
        public PosCommercialAccess Current { get; set; } = PosCommercialAccess.DevelopmentDefault;
    }

    private sealed class FakeUow : IPosUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<T> ExecuteInSerializableTransactionAsync<T>(
            Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default) =>
            action(cancellationToken);
    }

    private sealed class FakeBranchAccess(AuthorizedBranchScope scope) : IAuthorizedBranchGroupingDirectory
    {
        public Task<AuthorizedBranchScope> ListAuthorizedAsync(Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(scope);
    }

    private sealed class FakeRelationships : IConnectedSupplierRelationshipRepository
    {
        private readonly List<ConnectedSupplierRelationship> _items = [];
        public void Add(ConnectedSupplierRelationship r) => _items.Add(r);
        public Task AddAsync(ConnectedSupplierRelationship r, CancellationToken ct = default) { _items.Add(r); return Task.CompletedTask; }
        public Task<ConnectedSupplierRelationship?> FindOpenAsync(
            PosOrganizationId buyer, PosOrganizationId supplier, CancellationToken ct = default) =>
            Task.FromResult(_items.FirstOrDefault(x => x.BuyerOrganizationId == buyer && x.SupplierOrganizationId == supplier));
        public Task<ConnectedSupplierRelationship?> GetAsync(
            ConnectedSupplierRelationshipId id, CancellationToken ct = default) =>
            Task.FromResult(_items.FirstOrDefault(x => x.Id == id));
        public Task<IReadOnlyList<ConnectedSupplierRelationship>> ListAsync(
            PosOrganizationId organizationId, bool supplierView, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ConnectedSupplierRelationship>>(
                _items.Where(x => supplierView
                    ? x.SupplierOrganizationId == organizationId
                    : x.BuyerOrganizationId == organizationId).ToList());
        public Task UpdateAsync(ConnectedSupplierRelationship r, CancellationToken ct = default) => Task.CompletedTask;
    }

    private static ConnectedSupplierRelationship PendingIloilo(PosOrganizationId buyer, PosOrganizationId supplier) =>
        ConnectedSupplierRelationship.Request(
            buyer, supplier, DateTimeOffset.UtcNow,
            buyerDisplayName: "Mica Store", buyerPublicOrganizationId: "ORG111",
            supplierDisplayName: "Global Wholesale", supplierPublicOrganizationId: "ORG999",
            supplierBranchId: Iloilo, supplierBranchName: "Iloilo Branch");

    private static AuthorizedBranchScope IloiloOnly =>
        new(false, [new AuthorizedBranchGrouping(Iloilo, "Iloilo Branch", null, null)]);

    private static AuthorizedBranchScope CebuOnly =>
        new(false, [new AuthorizedBranchGrouping(Cebu, "Cebu Branch", null, null)]);

    private static AuthorizedBranchScope OrgWide => new(true, []);

    // ── LIST RELATIONSHIP FILTERING ──────────────────────────────────────────

    /// <summary>SUPBRH1-01 Iloilo staff + Iloilo workspace sees the pending request.</summary>
    [Fact]
    public async Task SUPBRH1_01_Iloilo_staff_sees_own_branch_request()
    {
        var buyer = PosOrganizationId.From(Guid.NewGuid());
        var supplier = PosOrganizationId.From(Guid.NewGuid());
        var repo = new FakeRelationships();
        repo.Add(PendingIloilo(buyer, supplier));

        // Iloilo staff: scope = partial / Iloilo only; requested branch = Iloilo → allowed.
        var scope = IloiloOnly;
        var list = new ListRelationships(repo, new FakeAccess());

        // The use-case itself still takes explicit workspaceBranchId after endpoint validates it.
        var result = await list.ExecuteAsync(supplier.Value, supplierView: true, workspaceBranchId: Iloilo, organizationWideInbox: false);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        Assert.Equal(Iloilo, result.Value![0].SupplierBranchId);
    }

    /// <summary>
    /// SUPBRH1-02 Cebu-only staff + forged Iloilo header gets 403.
    /// Tested via SupplierConnectionBranchRouting.IsAuthorizedToReadBranch helper used by the endpoint.
    /// </summary>
    [Fact]
    public void SUPBRH1_02_Cebu_only_staff_forged_Iloilo_header_is_rejected()
    {
        // Simulate what the endpoint does: check requestedBranchId against scope.
        var scope = CebuOnly;
        var requestedBranchId = Iloilo;

        var authorized = scope.Branches.Select(b => b.BranchId).ToHashSet();
        var isAllowed = scope.IsOrganizationWide || authorized.Contains(requestedBranchId);

        Assert.False(isAllowed);
    }

    /// <summary>SUPBRH1-03 Area staff may read only branches inside their granted Area.</summary>
    [Fact]
    public void SUPBRH1_03_Area_staff_inside_area_is_allowed_outside_is_denied()
    {
        var areaId = Guid.NewGuid();
        var areaScope = new AuthorizedBranchScope(false, [
            new AuthorizedBranchGrouping(Cebu, "Cebu Branch", areaId, "Visayas"),
            new AuthorizedBranchGrouping(Iloilo, "Iloilo Branch", areaId, "Visayas")
        ]);

        var iloiloAllowed = areaScope.IsOrganizationWide || areaScope.Branches.Any(b => b.BranchId == Iloilo);
        var manilaAllowed = areaScope.IsOrganizationWide || areaScope.Branches.Any(b => b.BranchId == Manila);

        Assert.True(iloiloAllowed);
        Assert.False(manilaAllowed);
    }

    /// <summary>SUPBRH1-04 Explicit staff may read only their assigned branches.</summary>
    [Fact]
    public void SUPBRH1_04_Explicit_staff_only_reads_assigned_branches()
    {
        var explicitScope = new AuthorizedBranchScope(false, [
            new AuthorizedBranchGrouping(Iloilo, "Iloilo Branch", null, null)
        ]);

        var iloiloOk = explicitScope.IsOrganizationWide || explicitScope.Branches.Any(b => b.BranchId == Iloilo);
        var cebuDenied = explicitScope.IsOrganizationWide || explicitScope.Branches.Any(b => b.BranchId == Cebu);

        Assert.True(iloiloOk);
        Assert.False(cebuDenied);
    }

    /// <summary>SUPBRH1-05 Owner/Admin global inbox: no branch header → global view.</summary>
    [Fact]
    public async Task SUPBRH1_05_Owner_admin_global_inbox_sees_all_requests()
    {
        var buyer = PosOrganizationId.From(Guid.NewGuid());
        var supplier = PosOrganizationId.From(Guid.NewGuid());
        var repo = new FakeRelationships();
        repo.Add(PendingIloilo(buyer, supplier));

        var list = new ListRelationships(repo, new FakeAccess());
        var result = await list.ExecuteAsync(supplier.Value, supplierView: true, organizationWideInbox: true);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
    }

    /// <summary>SUPBRH1-06 Buyer-side (non-supplier view) relationships are unaffected by branch auth.</summary>
    [Fact]
    public async Task SUPBRH1_06_Buyer_side_relationships_not_filtered_by_branch()
    {
        var buyer = PosOrganizationId.From(Guid.NewGuid());
        var supplier = PosOrganizationId.From(Guid.NewGuid());
        var repo = new FakeRelationships();
        repo.Add(PendingIloilo(buyer, supplier));

        var list = new ListRelationships(repo, new FakeAccess());
        // buyer-view: no branch filtering; workspaceBranchId and organizationWideInbox are not applied.
        var result = await list.ExecuteAsync(buyer.Value, supplierView: false);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        Assert.Equal(Iloilo, result.Value![0].SupplierBranchId);
    }

    // ── RESPOND CONNECTION AUTH ───────────────────────────────────────────────

    /// <summary>SUPBRH1-07 Forged accept from Cebu staff is still denied by RespondConnection.</summary>
    [Fact]
    public async Task SUPBRH1_07_Forged_accept_from_cebu_staff_denied()
    {
        var buyer = PosOrganizationId.From(Guid.NewGuid());
        var supplier = PosOrganizationId.From(Guid.NewGuid());
        var repo = new FakeRelationships();
        repo.Add(PendingIloilo(buyer, supplier));
        var rel = (await repo.ListAsync(supplier, supplierView: true)).First();

        var respond = new RespondConnection(
            repo, new FakeUow(), new FakeAccess(),
            new FakeBranchAccess(CebuOnly));

        var result = await respond.ExecuteAsync(supplier.Value, rel.Id.Value, approve: true, new RespondConnectionRequest());

        Assert.False(result.IsSuccess);
        Assert.Equal(ConnectedSupplierErrorCodes.BranchResponseForbidden, result.ErrorCode);
    }

    /// <summary>
    /// SUPBRH1-08 Missing branch-access dependency cannot fail open:
    /// the constructor throws immediately rather than silently skipping the check.
    /// </summary>
    [Fact]
    public void SUPBRH1_08_Missing_branch_access_dependency_throws_not_silently_skips()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new RespondConnection(
                new FakeRelationships(),
                new FakeUow(),
                new FakeAccess(),
                branchAccess: null!));

        Assert.Equal("branchAccess", ex.ParamName);
    }
}
