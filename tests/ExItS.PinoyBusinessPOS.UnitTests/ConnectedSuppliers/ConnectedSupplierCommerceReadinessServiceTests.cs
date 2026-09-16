using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Payments;

namespace ExItS.PinoyBusinessPOS.UnitTests.ConnectedSuppliers;

public sealed class ConnectedSupplierCommerceReadinessServiceTests
{
    private static readonly PosOrganizationId Buyer = PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly PosOrganizationId Supplier = PosOrganizationId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly Guid BranchId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly DateTimeOffset Now = new(2026, 9, 16, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Buyer_projection_omits_requirement_details()
    {
        var relationship = ReadyRelationship();
        var service = CreateService(relationship, readyBranch: true, shared: true);
        var result = await service.GetForBuyerAsync(Buyer.Value, relationship.Id.Value);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.True(result.Value!.IsReady);
        Assert.Null(result.Value.Requirements);
        Assert.Contains(ConnectedSupplierCommerceReadiness.FulfillmentPickup, result.Value.SupportedFulfillmentMethods);
    }

    [Fact]
    public async Task Supplier_projection_includes_checklist()
    {
        var relationship = ReadyRelationship(withContact: false, withBranch: false);
        var service = CreateService(relationship, readyBranch: false, shared: false);
        var result = await service.GetForSupplierAsync(Supplier.Value, relationship.Id.Value);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.False(result.Value!.IsReady);
        Assert.NotNull(result.Value.Requirements);
        Assert.Contains(result.Value.Requirements!, r =>
            r.Code == ConnectedSupplierCommerceReadiness.ResponsibleContact
            && r.Status == ConnectedSupplierCommerceReadiness.StatusMissing);
    }

    [Fact]
    public async Task EnsureReady_rejects_unready_supplier_with_buyer_generic_message()
    {
        var relationship = ReadyRelationship(withContact: false);
        var service = CreateService(relationship, readyBranch: true, shared: true);
        var gate = await service.EnsureReadyAsync(relationship, forBuyerMessage: true);

        Assert.False(gate.IsSuccess);
        Assert.Equal(ConnectedSupplierErrorCodes.CommerceNotReady, gate.ErrorCode);
        Assert.Equal(ConnectedSupplierCommerceReadinessService.BuyerNotReadyMessage, gate.ErrorMessage);
        Assert.DoesNotContain("Responsible contact", gate.ErrorMessage!, StringComparison.Ordinal);
        Assert.DoesNotContain("SellingBranch", gate.ErrorMessage!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EnsureReady_succeeds_when_ready()
    {
        var relationship = ReadyRelationship();
        var service = CreateService(relationship, readyBranch: true, shared: true);
        var gate = await service.EnsureReadyAsync(relationship, forBuyerMessage: true);
        Assert.True(gate.IsSuccess, gate.ErrorMessage);
    }

    private static ConnectedSupplierRelationship ReadyRelationship(
        bool withContact = true,
        bool withBranch = true) =>
        ConnectedSupplierRelationship.Rehydrate(
            ConnectedSupplierRelationshipId.From(Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd")),
            Buyer,
            Supplier,
            ConnectedSupplierRelationshipStatus.Active,
            Now,
            null,
            Now,
            null,
            null,
            Now,
            Now,
            "Buyer Co",
            "ORG1",
            "Supplier Co",
            "ORG2",
            CatalogSharingMode.SelectedOnly,
            supplierBranchId: withBranch ? BranchId : null,
            supplierBranchNameSnapshot: withBranch ? "Main" : null,
            contactPersonName: withContact ? "Ana" : null,
            contactPhone: withContact ? "0917" : null);

    private static ConnectedSupplierCommerceReadinessService CreateService(
        ConnectedSupplierRelationship relationship,
        bool readyBranch,
        bool shared)
    {
        var relationships = new FakeRelationships(relationship);
        var shares = new FakeShares(shared);
        var branches = new FakeBranches(readyBranch);
        var payments = new FakePayments();
        var credits = new FakeCredits();
        return new ConnectedSupplierCommerceReadinessService(
            relationships,
            shares,
            branches,
            payments,
            credits,
            new FakeAccess());
    }

    private sealed class FakeAccess : IPosCommercialAccessAccessor
    {
        public PosCommercialAccess Current { get; set; } = PosCommercialAccess.DevelopmentDefault;
    }

    private sealed class FakeRelationships(ConnectedSupplierRelationship relationship)
        : IConnectedSupplierRelationshipRepository
    {
        public Task AddAsync(ConnectedSupplierRelationship r, CancellationToken ct = default) => Task.CompletedTask;
        public Task<ConnectedSupplierRelationship?> FindOpenAsync(PosOrganizationId buyer, PosOrganizationId supplier, CancellationToken ct = default) =>
            Task.FromResult<ConnectedSupplierRelationship?>(relationship);
        public Task<ConnectedSupplierRelationship?> GetAsync(ConnectedSupplierRelationshipId id, CancellationToken ct = default) =>
            Task.FromResult<ConnectedSupplierRelationship?>(id == relationship.Id ? relationship : null);
        public Task<IReadOnlyList<ConnectedSupplierRelationship>> ListAsync(PosOrganizationId organizationId, bool supplierView, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ConnectedSupplierRelationship>>([relationship]);
        public Task UpdateAsync(ConnectedSupplierRelationship r, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakeShares(bool shared) : IConnectedBuyerProductShareRepository
    {
        public Task AddAsync(ConnectedBuyerProductShare share, CancellationToken ct = default) => Task.CompletedTask;
        public Task<int> CountEligibleSupplierProductsAsync(PosOrganizationId supplier, CancellationToken ct = default) =>
            Task.FromResult(shared ? 3 : 0);
        public Task<ConnectedBuyerProductShare?> FindAsync(ConnectedSupplierRelationshipId relationshipId, CatalogProductId supplierProductId, CancellationToken ct = default) =>
            Task.FromResult<ConnectedBuyerProductShare?>(null);
        public Task<ConnectedBuyerProductShare?> GetAsync(ConnectedBuyerProductShareId id, CancellationToken ct = default) =>
            Task.FromResult<ConnectedBuyerProductShare?>(null);
        public Task<IReadOnlyList<ConnectedBuyerProductShare>> ListAsync(ConnectedSupplierRelationshipId relationshipId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ConnectedBuyerProductShare>>([]);
        public Task<IReadOnlyDictionary<Guid, BuyerRelationshipShareStats>> ListShareStatsByRelationshipsAsync(
            IReadOnlyList<Guid> relationshipIds, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, BuyerRelationshipShareStats>>(
                relationshipIds.ToDictionary(
                    id => id,
                    _ => new BuyerRelationshipShareStats(shared ? 2 : 0, 0, 0)));
        public Task<(IReadOnlyList<SupplierProductExposure> Exposures, IReadOnlyList<ConnectedBuyerProductShare> Shares, int Total)> SearchSharedCatalogAsync(
            ConnectedSupplierRelationshipId relationshipId, PosOrganizationId supplier, string? query, string? category, int skip, int take, CancellationToken ct = default, CatalogSharingMode catalogSharingMode = CatalogSharingMode.SelectedOnly) =>
            Task.FromResult<(IReadOnlyList<SupplierProductExposure>, IReadOnlyList<ConnectedBuyerProductShare>, int)>(([], [], 0));
        public Task<BuyerProductShareSearchPage> SearchForSupplierManagementAsync(
            ConnectedSupplierRelationshipId relationshipId, PosOrganizationId supplier, string? query, string? category, string? shareFilter, int skip, int take, bool idsOnly, CancellationToken ct = default, CatalogSharingMode catalogSharingMode = CatalogSharingMode.SelectedOnly) =>
            Task.FromResult(new BuyerProductShareSearchPage([], [], 0, 0, 0, []));
        public Task UpdateAsync(ConnectedBuyerProductShare share, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakeBranches(bool ready) : ICustomerOrderBranchDirectory
    {
        public Task<CustomerOrderBranchSnapshot?> GetBranchAsync(Guid sellerOrganizationId, Guid branchId, CancellationToken cancellationToken = default) =>
            Task.FromResult<CustomerOrderBranchSnapshot?>(
                ready
                    ? new CustomerOrderBranchSnapshot(
                        branchId,
                        "Main",
                        CustomerOrderingEnabled: true,
                        PickupEnabled: true,
                        DeliveryEnabled: false,
                        CustomerOrderingOperational: true,
                        PickupOperational: true,
                        DeliveryOperational: false,
                        OnlineOrdersPaused: false,
                        StoreStatusMessage: null,
                        Latitude: null,
                        Longitude: null,
                        DeliveryPolicy: null)
                    : null);

        public Task<IReadOnlyList<CustomerOrderBranchSnapshot>> ListBranchesAsync(Guid sellerOrganizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CustomerOrderBranchSnapshot>>([]);
    }

    private sealed class FakePayments : IOrganizationPaymentMethodSettingRepository
    {
        public Task AddAsync(OrganizationPaymentMethodSetting setting, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<OrganizationPaymentMethodSetting?> GetAsync(PosOrganizationId organizationId, string methodCode, CancellationToken cancellationToken = default) =>
            Task.FromResult<OrganizationPaymentMethodSetting?>(null);
        public Task<IReadOnlyList<OrganizationPaymentMethodSetting>> ListByOrganizationAsync(PosOrganizationId organizationId, CancellationToken cancellationToken = default)
        {
            // Cash on, Utang off — credit policy not required for readiness.
            var cash = OrganizationPaymentMethodSetting.Create(
                organizationId,
                PaymentMethodCatalog.Cash,
                isEnabled: true,
                displayName: null,
                requireReference: false,
                PaymentMethodBranchScope.AllBranches,
                selectedBranchIds: null,
                instructions: null,
                accountHint: null,
                Now);
            var utang = OrganizationPaymentMethodSetting.Create(
                organizationId,
                PaymentMethodCatalog.Utang,
                isEnabled: false,
                displayName: null,
                requireReference: false,
                PaymentMethodBranchScope.AllBranches,
                selectedBranchIds: null,
                instructions: null,
                accountHint: null,
                Now);
            return Task.FromResult<IReadOnlyList<OrganizationPaymentMethodSetting>>([cash, utang]);
        }
        public Task UpdateAsync(OrganizationPaymentMethodSetting setting, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeCredits : IBusinessCustomerCreditPolicyRepository
    {
        public Task AcquireBusinessCustomerCreditLockAsync(PosOrganizationId sellerOrganizationId, PosOrganizationId buyerOrganizationId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddAsync(BusinessCustomerCreditPolicy policy, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddChangeAsync(BusinessCustomerCreditPolicyChange change, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<BusinessCustomerCreditPolicy?> GetBySellerAndBuyerAsync(PosOrganizationId sellerOrganizationId, PosOrganizationId buyerOrganizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult<BusinessCustomerCreditPolicy?>(null);
        public Task<(IReadOnlyList<BusinessCustomerCreditPolicyChange> Items, int TotalCount)> ListChangesAsync(PosOrganizationId sellerOrganizationId, PosOrganizationId buyerOrganizationId, int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<BusinessCustomerCreditPolicyChange>, int)>(([], 0));
        public Task<IReadOnlyList<BusinessCustomerCreditPolicy>> ListBySellerAndBuyerIdsAsync(PosOrganizationId sellerOrganizationId, IReadOnlyCollection<Guid> buyerOrganizationIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BusinessCustomerCreditPolicy>>([]);
        public Task UpdateAsync(BusinessCustomerCreditPolicy policy, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
