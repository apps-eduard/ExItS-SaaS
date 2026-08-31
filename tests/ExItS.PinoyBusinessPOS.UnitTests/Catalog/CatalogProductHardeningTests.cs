using System.Reflection;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Permissions;

namespace ExItS.PinoyBusinessPOS.UnitTests.Catalog;

public sealed class CatalogProductHardeningFilterTests
{
    [Fact]
    public void PGA_HARD_FILTER_01_canBeSold_without_commerciallyOffered_does_not_require_branch()
    {
        var filter = new CatalogProductFilter(CanBeSold: true);
        Assert.True(filter.CanBeSold);
        Assert.False(filter.CommerciallyOfferedAtBranch);
        Assert.Null(filter.ActingBranchId);
    }

    [Fact]
    public void PGA_HARD_FILTER_03_commerciallyOffered_requires_acting_branch_in_filter_model()
    {
        var branch = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var filter = new CatalogProductFilter(
            CanBeSold: true,
            CommerciallyOfferedAtBranch: true,
            ActingBranchId: branch);
        Assert.True(filter.CommerciallyOfferedAtBranch);
        Assert.Equal(branch, filter.ActingBranchId);
    }

    [Fact]
    public void PGA_HARD_FILTER_connected_buyer_scope_defaults_to_OrganizationStandard()
    {
        var filter = new CatalogProductFilter(
            Status: CatalogProductStatus.Active,
            Scope: CatalogProductScope.OrganizationStandard);
        Assert.Equal(CatalogProductScope.OrganizationStandard, filter.Scope);
    }
}

public sealed class CatalogProductHardeningConnectedBuyerTests
{
    private static readonly Guid OrgGuid = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly PosOrganizationId Org = PosOrganizationId.From(OrgGuid);
    private static readonly PosBranchId BranchA =
        PosBranchId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-09-01T00:00:00Z");

    [Fact]
    public async Task PGA_HARD_CB_01_StoreManager_cannot_mutate_Standard_connected_buyer()
    {
        var product = CatalogProduct.Create(Org, "Coke", UnitOfMeasure.Piece, 50m, Now);
        var useCase = CreateBulk(new MemoryProducts([product]), FixedCatalogGovernanceActorAccessor.StoreManager(BranchA.Value));

        var result = await useCase.ExecuteAsync(
            OrgGuid,
            new BulkConnectedBuyerAvailabilityMutationRequest("enable", [product.Id.Value]));

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.ProductAvailabilityForbidden, result.ErrorCode);
    }

    [Fact]
    public async Task PGA_HARD_CB_05_explicit_BranchLocal_bulk_mutation_rejected()
    {
        var local = CatalogProduct.Create(
            Org, "Bangus", UnitOfMeasure.Kilogram, 180m, Now,
            scope: CatalogProductScope.BranchLocal, originBranchId: BranchA);
        var useCase = CreateBulk(new MemoryProducts([local]), FixedCatalogGovernanceActorAccessor.Owner());

        var result = await useCase.ExecuteAsync(
            OrgGuid,
            new BulkConnectedBuyerAvailabilityMutationRequest("enable", [local.Id.Value]));

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.ProductScopeForbidden, result.ErrorCode);
    }

    [Fact]
    public async Task PGA_HARD_CB_03_Owner_can_mutate_Standard_connected_buyer()
    {
        var product = CatalogProduct.Create(Org, "Coke", UnitOfMeasure.Piece, 50m, Now);
        product.DisableConnectedBuyerAvailability(Now);
        var useCase = CreateBulk(new MemoryProducts([product]), FixedCatalogGovernanceActorAccessor.Owner());

        var result = await useCase.ExecuteAsync(
            OrgGuid,
            new BulkConnectedBuyerAvailabilityMutationRequest("enable", [product.Id.Value]));

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.True(product.CanExposeToConnectedBuyers);
    }

    private static BulkMutateConnectedBuyerAvailability CreateBulk(
        MemoryProducts products,
        ICatalogGovernanceActorAccessor actor) =>
        new(
            products,
            new NoOpExposures(),
            new FakeUow(),
            new FixedClock(Now),
            new FakeAccess(),
            new CatalogProductGovernanceAuthority(),
            actor);

    private sealed class MemoryProducts : ICatalogProductRepository
    {
        private readonly Dictionary<Guid, CatalogProduct> _items;

        public MemoryProducts(IEnumerable<CatalogProduct> items) =>
            _items = items.ToDictionary(p => p.Id.Value);

        public Task AddAsync(CatalogProduct product, CancellationToken cancellationToken = default)
        {
            _items[product.Id.Value] = product;
            return Task.CompletedTask;
        }

        public Task<(int TotalCount, int AvailableCount, int NotAvailableCount)> CountConnectedBuyerAvailabilityAsync(
            PosOrganizationId organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult((0, 0, 0));

        public Task<CatalogProduct?> FindByBarcodeAsync(
            PosOrganizationId organizationId, string barcode, CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogProduct?>(null);

        public Task<CatalogProduct?> FindByNormalizedSkuAsync(
            PosOrganizationId organizationId, string normalizedSku, CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogProduct?>(null);

        public Task<CatalogProduct?> FindByPlatformGlobalProductIdAsync(
            PosOrganizationId organizationId, Guid platformGlobalProductId, CancellationToken cancellationToken = default) =>
            Task.FromResult<CatalogProduct?>(null);

        public Task<CatalogProduct?> GetByIdAsync(
            PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.GetValueOrDefault(productId.Value));

        public Task<(IReadOnlyList<CatalogProduct> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId, CatalogProductFilter filter, int skip, int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<CatalogProduct>, int)>((_items.Values.ToList(), _items.Count));

        public Task<IReadOnlyList<CatalogProduct>> ListByIdsAsync(
            PosOrganizationId organizationId, IReadOnlyCollection<CatalogProductId> productIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CatalogProduct>>(
                productIds.Where(id => _items.ContainsKey(id.Value)).Select(id => _items[id.Value]).ToList());

        public Task<IReadOnlyList<(Guid? CategoryId, int Count)>> ListConnectedBuyerAvailabilityCategoryFacetsAsync(
            PosOrganizationId organizationId, CatalogProductFilter filter, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<(Guid?, int)>>([]);

        public Task<IReadOnlyList<Guid>> ListIdsAsync(
            PosOrganizationId organizationId, CatalogProductFilter filter, int skip, int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Guid>>(
                _items.Values
                    .Where(p => filter.Scope is null || p.Scope == filter.Scope)
                    .Select(p => p.Id.Value)
                    .Skip(skip)
                    .Take(take)
                    .ToList());

        public Task<IReadOnlySet<Guid>> ListPlatformGlobalProductIdsAsync(
            PosOrganizationId organizationId, IReadOnlyCollection<Guid> platformGlobalProductIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlySet<Guid>>(new HashSet<Guid>());

        public Task UpdateAsync(CatalogProduct product, CancellationToken cancellationToken = default)
        {
            _items[product.Id.Value] = product;
            return Task.CompletedTask;
        }
    }

    private sealed class NoOpExposures : ISupplierProductExposureRepository
    {
        public Task AddAsync(SupplierProductExposure exposure, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<SupplierProductExposure?> GetAsync(SupplierProductExposureId id, CancellationToken cancellationToken = default) =>
            Task.FromResult<SupplierProductExposure?>(null);

        public Task<SupplierProductExposure?> GetByProductAsync(
            PosOrganizationId supplier, CatalogProductId productId, CancellationToken cancellationToken = default) =>
            Task.FromResult<SupplierProductExposure?>(null);

        public Task<IReadOnlyList<SupplierProductExposure>> ListAsync(
            PosOrganizationId supplier, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SupplierProductExposure>>([]);

        public Task<(IReadOnlyList<SupplierProductExposure> Items, int Total)> SearchAsync(
            PosOrganizationId supplier, string? query, string? category, int skip, int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<SupplierProductExposure>, int)>(([], 0));

        public Task UpdateAsync(SupplierProductExposure exposure, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FakeUow : IPosUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<T> ExecuteInSerializableTransactionAsync<T>(
            Func<CancellationToken, Task<T>> action,
            CancellationToken cancellationToken = default) =>
            action(cancellationToken);
    }

    private sealed class FixedClock(DateTimeOffset utc) : IClock
    {
        public DateTimeOffset UtcNow => utc;
    }

    private sealed class FakeAccess : IPosCommercialAccessAccessor
    {
        public PosCommercialAccess Current { get; set; } = PosCommercialAccess.DevelopmentDefault;
    }
}

public sealed class CatalogProductHardeningAuthorityArchitectureTests
{
    [Fact]
    public void PGA_HARD_AUTH_no_optional_governance_on_security_sensitive_use_cases()
    {
        var types = new[]
        {
            typeof(CreateCatalogProduct),
            typeof(UpdateCatalogProduct),
            typeof(UpdateCatalogProductPrices),
            typeof(DeactivateCatalogProduct),
            typeof(ReactivateCatalogProduct),
            typeof(SetCatalogProductImage),
            typeof(RemoveCatalogProductImage),
            typeof(ImportTemplateBatch),
            typeof(ImportSelectedProducts),
            typeof(BulkMutateConnectedBuyerAvailability),
            typeof(QueryConnectedBuyerAvailability),
            typeof(PreviewDefaultConnectedPoPricing),
            typeof(ApplyDefaultConnectedPoPricing),
        };

        foreach (var type in types)
        {
            foreach (var ctor in type.GetConstructors())
            {
                var govParam = ctor.GetParameters()
                    .FirstOrDefault(p => p.ParameterType == typeof(CatalogProductGovernanceAuthority)
                                         || Nullable.GetUnderlyingType(p.ParameterType) == typeof(CatalogProductGovernanceAuthority));
                Assert.True(govParam is not null, $"{type.Name} missing governance ctor param");
                Assert.Equal(typeof(CatalogProductGovernanceAuthority), govParam!.ParameterType);
                Assert.False(govParam.HasDefaultValue, $"{type.Name} governance must not be optional");
            }
        }
    }

    [Fact]
    public void PGA_HARD_LOOKUP_management_visibility_foreign_Local_denied()
    {
        var authority = new CatalogProductGovernanceAuthority();
        var actor = new CatalogGovernanceActor(
            PosRole.StoreManager, false, Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"));
        var origin = PosBranchId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
        Assert.False(authority.CanViewBranchLocalInManagement(actor, origin));
        Assert.True(authority.CanViewBranchLocalInManagement(
            new CatalogGovernanceActor(PosRole.Owner, false, null), origin));
    }
}
