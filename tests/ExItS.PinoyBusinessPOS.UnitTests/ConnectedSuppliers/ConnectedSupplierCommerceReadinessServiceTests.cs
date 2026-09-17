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
        Assert.Empty(result.Value.BlockerCategories ?? []);
        Assert.Contains(ConnectedSupplierCommerceReadiness.FulfillmentPickup, result.Value.SupportedFulfillmentMethods);
    }

    [Fact]
    public async Task Buyer_projection_includes_blocker_categories_without_requirement_details()
    {
        var relationship = ReadyRelationship(withContact: false);
        var service = CreateService(relationship, readyBranch: true, shared: true);
        var result = await service.GetForBuyerAsync(Buyer.Value, relationship.Id.Value);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.False(result.Value!.IsReady);
        Assert.Null(result.Value.Requirements);
        Assert.Contains(
            ConnectedSupplierCommerceReadiness.BuyerBlockerContact,
            result.Value.BlockerCategories ?? []);
        Assert.DoesNotContain(
            result.Value.BlockerCategories ?? [],
            c => c.Contains("Responsible", StringComparison.OrdinalIgnoreCase));
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

    [Fact]
    public async Task Supplier_pickup_config_complete_when_pickup_ready_even_if_store_closed()
    {
        var relationship = ReadyRelationship();
        var relationships = new FakeRelationships(relationship);
        var shares = new FakeShares(true);
        var branches = new FakeBranchesClosedButSetupReady();
        var payments = new FakePayments();
        var credits = new FakeCredits();
        var service = new ConnectedSupplierCommerceReadinessService(
            relationships,
            shares,
            branches,
            payments,
            credits,
            new FakeAccess());

        var result = await service.GetForSupplierAsync(Supplier.Value, relationship.Id.Value);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        var pickup = Assert.Single(
            result.Value!.Requirements!,
            r => r.Code == ConnectedSupplierCommerceReadiness.PickupConfig);
        Assert.Equal(ConnectedSupplierCommerceReadiness.StatusComplete, pickup.Status);
        Assert.Contains(ConnectedSupplierCommerceReadiness.FulfillmentPickup, result.Value.SupportedFulfillmentMethods);
    }

    [Fact]
    public async Task Delivery_uses_platform_delivery_ready_not_operational_or_policy_heuristic()
    {
        var relationship = ReadyRelationship();
        var service = new ConnectedSupplierCommerceReadinessService(
            new FakeRelationships(relationship),
            new FakeShares(true),
            new FakeBranchesDeliverySetupReadyStoreClosed(),
            new FakePayments(),
            new FakeCredits(),
            new FakeAccess(),
            new FakeOrgOfferDelivery(offer: true));

        var result = await service.GetForBuyerAsync(Buyer.Value, relationship.Id.Value);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.True(result.Value!.IsReady);
        Assert.Contains(ConnectedSupplierCommerceReadiness.FulfillmentDelivery, result.Value.SupportedFulfillmentMethods);
        Assert.Contains(ConnectedSupplierCommerceReadiness.FulfillmentPickup, result.Value.SupportedFulfillmentMethods);
    }

    [Fact]
    public async Task Pickup_ready_keeps_buyer_ready_when_org_delivery_incomplete()
    {
        var relationship = ReadyRelationship();
        var service = new ConnectedSupplierCommerceReadinessService(
            new FakeRelationships(relationship),
            new FakeShares(true),
            new FakeBranches(true),
            new FakePayments(),
            new FakeCredits(),
            new FakeAccess(),
            new FakeOrgOfferDelivery(offer: true));

        // FakeBranches has DeliveryEnabled false → delivery not usable; pickup ready.
        var result = await service.GetForBuyerAsync(Buyer.Value, relationship.Id.Value);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.True(result.Value!.IsReady);
        Assert.Equal(
            [ConnectedSupplierCommerceReadiness.FulfillmentPickup],
            result.Value.SupportedFulfillmentMethods);
        Assert.Empty(result.Value.BlockerCategories ?? []);
    }

    [Fact]
    public async Task Supplier_delivery_only_without_org_offer_marks_fulfillment_complete()
    {
        var relationship = ReadyRelationship();
        var service = new ConnectedSupplierCommerceReadinessService(
            new FakeRelationships(relationship),
            new FakeShares(true),
            new FakeBranchesDeliveryOnlySetupReady(),
            new FakePayments(),
            new FakeCredits(),
            new FakeAccess(),
            new FakeOrgOfferDelivery(offer: false));

        var supplier = await service.GetForSupplierAsync(Supplier.Value, relationship.Id.Value);
        Assert.True(supplier.IsSuccess, supplier.ErrorMessage);
        Assert.True(supplier.Value!.IsReady);
        Assert.Equal(
            ConnectedSupplierCommerceReadiness.StatusComplete,
            supplier.Value.Requirements!
                .Single(r => r.Code == ConnectedSupplierCommerceReadiness.FulfillmentMethod)
                .Status);
        Assert.Equal(
            [ConnectedSupplierCommerceReadiness.FulfillmentDelivery],
            supplier.Value.SupportedFulfillmentMethods);

        // Buyer also gets Delivery from the branch channel (org Offer Delivery not required).
        var buyer = await service.GetForBuyerAsync(Buyer.Value, relationship.Id.Value);
        Assert.True(buyer.IsSuccess, buyer.ErrorMessage);
        Assert.True(buyer.Value!.IsReady);
        Assert.Equal(
            [ConnectedSupplierCommerceReadiness.FulfillmentDelivery],
            buyer.Value.SupportedFulfillmentMethods);
        Assert.Empty(buyer.Value.BlockerCategories ?? []);
    }

    [Fact]
    public async Task Wrong_or_missing_branch_snapshot_does_not_leak_ready_fulfillment()
    {
        var relationship = ReadyRelationship();
        var service = CreateService(relationship, readyBranch: false, shared: true);
        var result = await service.GetForBuyerAsync(Buyer.Value, relationship.Id.Value);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.False(result.Value!.IsReady);
        Assert.Empty(result.Value.SupportedFulfillmentMethods);
        Assert.Contains(
            ConnectedSupplierCommerceReadiness.BuyerBlockerNoUsableMethod,
            result.Value.BlockerCategories ?? []);
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
                        DeliveryPolicy: null,
                        PickupReady: true,
                        DeliveryReady: false)
                    : null);

        public Task<IReadOnlyList<CustomerOrderBranchSnapshot>> ListBranchesAsync(Guid sellerOrganizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CustomerOrderBranchSnapshot>>([]);
    }

    /// <summary>Pickup setup complete but store closed (not operational right now).</summary>
    private sealed class FakeBranchesClosedButSetupReady : ICustomerOrderBranchDirectory
    {
        public Task<CustomerOrderBranchSnapshot?> GetBranchAsync(Guid sellerOrganizationId, Guid branchId, CancellationToken cancellationToken = default) =>
            Task.FromResult<CustomerOrderBranchSnapshot?>(
                new CustomerOrderBranchSnapshot(
                    branchId,
                    "Main",
                    CustomerOrderingEnabled: true,
                    PickupEnabled: true,
                    DeliveryEnabled: false,
                    CustomerOrderingOperational: false,
                    PickupOperational: false,
                    DeliveryOperational: false,
                    OnlineOrdersPaused: false,
                    StoreStatusMessage: "Closed",
                    Latitude: null,
                    Longitude: null,
                    DeliveryPolicy: null,
                    PickupReady: true,
                    DeliveryReady: false));

        public Task<IReadOnlyList<CustomerOrderBranchSnapshot>> ListBranchesAsync(Guid sellerOrganizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CustomerOrderBranchSnapshot>>([]);
    }

    /// <summary>
    /// Both channels setup-ready; delivery operational false (store closed) — must still count.
    /// </summary>
    private sealed class FakeBranchesDeliverySetupReadyStoreClosed : ICustomerOrderBranchDirectory
    {
        public Task<CustomerOrderBranchSnapshot?> GetBranchAsync(Guid sellerOrganizationId, Guid branchId, CancellationToken cancellationToken = default) =>
            Task.FromResult<CustomerOrderBranchSnapshot?>(
                new CustomerOrderBranchSnapshot(
                    branchId,
                    "Main",
                    CustomerOrderingEnabled: true,
                    PickupEnabled: true,
                    DeliveryEnabled: true,
                    CustomerOrderingOperational: false,
                    PickupOperational: false,
                    DeliveryOperational: false,
                    OnlineOrdersPaused: false,
                    StoreStatusMessage: "Closed",
                    Latitude: 14.5m,
                    Longitude: 121.0m,
                    DeliveryPolicy: null,
                    PickupReady: true,
                    DeliveryReady: true));

        public Task<IReadOnlyList<CustomerOrderBranchSnapshot>> ListBranchesAsync(Guid sellerOrganizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CustomerOrderBranchSnapshot>>([]);
    }

    /// <summary>Pickup OFF; Delivery enabled+ready (branch channel only).</summary>
    private sealed class FakeBranchesDeliveryOnlySetupReady : ICustomerOrderBranchDirectory
    {
        public Task<CustomerOrderBranchSnapshot?> GetBranchAsync(Guid sellerOrganizationId, Guid branchId, CancellationToken cancellationToken = default) =>
            Task.FromResult<CustomerOrderBranchSnapshot?>(
                new CustomerOrderBranchSnapshot(
                    branchId,
                    "Main",
                    CustomerOrderingEnabled: true,
                    PickupEnabled: false,
                    DeliveryEnabled: true,
                    CustomerOrderingOperational: true,
                    PickupOperational: false,
                    DeliveryOperational: true,
                    OnlineOrdersPaused: false,
                    StoreStatusMessage: null,
                    Latitude: 14.5m,
                    Longitude: 121.0m,
                    DeliveryPolicy: null,
                    PickupReady: false,
                    DeliveryReady: true));

        public Task<IReadOnlyList<CustomerOrderBranchSnapshot>> ListBranchesAsync(Guid sellerOrganizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CustomerOrderBranchSnapshot>>([]);
    }

    private sealed class FakeOrgOfferDelivery(bool offer) : IOrganizationFulfillmentSettingsRepository
    {
        public Task<OrganizationFulfillmentSettings?> GetAsync(
            PosOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<OrganizationFulfillmentSettings?>(
                OrganizationFulfillmentSettings.Rehydrate(
                    Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                    organizationId,
                    offer,
                    Now,
                    Now));

        public Task AddAsync(OrganizationFulfillmentSettings settings, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task UpdateAsync(OrganizationFulfillmentSettings settings, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
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
