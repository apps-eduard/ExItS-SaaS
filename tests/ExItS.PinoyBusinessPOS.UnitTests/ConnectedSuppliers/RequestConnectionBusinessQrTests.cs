using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;

namespace ExItS.PinoyBusinessPOS.UnitTests.ConnectedSuppliers;

public sealed class RequestConnectionBusinessQrTests
{
    private sealed class FakeAccess : IPosCommercialAccessAccessor
    {
        public PosCommercialAccess Current { get; set; } = PosCommercialAccess.DevelopmentDefault;
    }

    private sealed class FakeRelationships : IConnectedSupplierRelationshipRepository
    {
        public ConnectedSupplierRelationship? LastAdded { get; private set; }
        public ConnectedSupplierRelationship? OpenExisting { get; set; }

        public Task AddAsync(ConnectedSupplierRelationship relationship, CancellationToken ct = default)
        {
            LastAdded = relationship;
            return Task.CompletedTask;
        }

        public Task<ConnectedSupplierRelationship?> FindOpenAsync(
            PosOrganizationId buyer,
            PosOrganizationId supplier,
            CancellationToken ct = default) =>
            Task.FromResult(OpenExisting);

        public Task<ConnectedSupplierRelationship?> GetAsync(
            ConnectedSupplierRelationshipId id,
            CancellationToken ct = default) =>
            Task.FromResult<ConnectedSupplierRelationship?>(null);

        public Task<IReadOnlyList<ConnectedSupplierRelationship>> ListAsync(
            PosOrganizationId organizationId,
            bool supplierView,
            CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ConnectedSupplierRelationship>>([]);

        public Task UpdateAsync(ConnectedSupplierRelationship relationship, CancellationToken ct = default) =>
            Task.CompletedTask;
    }

    private sealed class FakeSuppliers : ISupplierRepository
    {
        public Supplier? LastAdded { get; private set; }
        public int AllocateCalls { get; private set; }

        public Task AddAsync(Supplier supplier, CancellationToken cancellationToken = default)
        {
            LastAdded = supplier;
            return Task.CompletedTask;
        }

        public Task<string> AllocateNextSupplierCodeAsync(
            PosOrganizationId organizationId,
            CancellationToken cancellationToken = default)
        {
            AllocateCalls++;
            return Task.FromResult($"SUP-{AllocateCalls:D6}");
        }

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
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<Supplier>, int)>(([], 0));

        public Task UpdateAsync(Supplier supplier, CancellationToken cancellationToken = default) =>
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

    private sealed class FakeResolve : IPlatformOrganizationPublicResolve
    {
        public ApplicationResult<PlatformOrganizationPublicResolveResult>? Result { get; set; }
        public ApplicationResult<PlatformOrganizationPublicResolveResult>? BuyerIdentity { get; set; }

        public Task<ApplicationResult<PlatformOrganizationPublicResolveResult>> ResolveOrganizationForConnectedSupplierAsync(
            string publicOrganizationIdOrQrPayload,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result!);

        public Task<ApplicationResult<PlatformOrganizationPublicResolveResult>> GetOrganizationPublicIdentityAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                BuyerIdentity
                ?? ApplicationResult<PlatformOrganizationPublicResolveResult>.Success(
                    new PlatformOrganizationPublicResolveResult(organizationId, "ORG000001", "Buyer Biz")));
    }

    private static RequestConnection CreateUseCase(
        FakeRelationships relationships,
        FakeResolve resolve,
        FakeSuppliers? suppliers = null) =>
        new(
            relationships,
            suppliers ?? new FakeSuppliers(),
            new FakeUow(),
            new FakeAccess(),
            resolve);

    [Fact]
    public async Task Requires_business_qr_payload()
    {
        var useCase = CreateUseCase(new FakeRelationships(), new FakeResolve());

        var result = await useCase.ExecuteAsync(
            Guid.NewGuid(),
            new RequestConnectionRequest(SupplierOrganizationId: Guid.NewGuid()));

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorCodes.ConnectedSupplierRequiresBusinessQr, result.ErrorCode);
    }

    [Fact]
    public async Task Rejects_personal_qr_before_platform_call()
    {
        var resolve = new FakeResolve
        {
            Result = ApplicationResult<PlatformOrganizationPublicResolveResult>.Success(
                new PlatformOrganizationPublicResolveResult(Guid.NewGuid(), "ORG001234", "ShouldNotUse"))
        };
        var useCase = CreateUseCase(new FakeRelationships(), resolve);

        var result = await useCase.ExecuteAsync(
            Guid.NewGuid(),
            new RequestConnectionRequest(
                SupplierPublicOrganizationIdOrQrPayload: "exits://qr/v1/personal/EX-4827-1936"));

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorCodes.ConnectedSupplierRequiresBusinessQr, result.ErrorCode);
        Assert.Contains("Personal", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Rejects_device_registration_qr()
    {
        var useCase = CreateUseCase(new FakeRelationships(), new FakeResolve());

        var result = await useCase.ExecuteAsync(
            Guid.NewGuid(),
            new RequestConnectionRequest(
                SupplierPublicOrganizationIdOrQrPayload: "exits://qr/v1/pos-device-registration/tok"));

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorCodes.ConnectedSupplierQrPurposeMismatch, result.ErrorCode);
    }

    [Fact]
    public async Task Accepts_resolved_organization_business_qr_and_creates_buyer_supplier()
    {
        var supplierOrg = Guid.NewGuid();
        var buyerOrg = Guid.NewGuid();
        var relationships = new FakeRelationships();
        var suppliers = new FakeSuppliers();
        var resolve = new FakeResolve
        {
            Result = ApplicationResult<PlatformOrganizationPublicResolveResult>.Success(
                new PlatformOrganizationPublicResolveResult(supplierOrg, "ORG001234", "ABC Trading"))
        };
        var useCase = CreateUseCase(relationships, resolve, suppliers);

        var result = await useCase.ExecuteAsync(
            buyerOrg,
            new RequestConnectionRequest(
                SupplierPublicOrganizationIdOrQrPayload: "exits://qr/v1/organization/ORG001234"));

        Assert.True(result.IsSuccess, $"{result.ErrorCode}: {result.ErrorMessage}");
        Assert.NotNull(relationships.LastAdded);
        Assert.Equal(supplierOrg, relationships.LastAdded!.SupplierOrganizationId.Value);
        Assert.Equal(buyerOrg, relationships.LastAdded.BuyerOrganizationId.Value);
        Assert.Equal(ConnectedSupplierRelationshipStatus.Pending, relationships.LastAdded.Status);
        Assert.Equal("ABC Trading", relationships.LastAdded.SupplierDisplayNameSnapshot);
        Assert.Equal("ORG001234", relationships.LastAdded.SupplierPublicOrganizationIdSnapshot);
        Assert.Equal("Buyer Biz", relationships.LastAdded.BuyerDisplayNameSnapshot);
        Assert.NotNull(suppliers.LastAdded);
        Assert.Equal(SupplierConnectionType.ConnectedOrganization, suppliers.LastAdded!.ConnectionType);
        Assert.Equal(relationships.LastAdded.Id, suppliers.LastAdded.ConnectedRelationshipId);
        Assert.Equal("Pending", result.Value!.Status);
        Assert.Equal("ABC Trading", result.Value.CounterpartyDisplayName);
    }

    [Fact]
    public async Task Rejects_self_connection()
    {
        var org = Guid.NewGuid();
        var resolve = new FakeResolve
        {
            Result = ApplicationResult<PlatformOrganizationPublicResolveResult>.Success(
                new PlatformOrganizationPublicResolveResult(org, "ORG001234", "Same Biz"))
        };
        var useCase = CreateUseCase(new FakeRelationships(), resolve);

        var result = await useCase.ExecuteAsync(
            org,
            new RequestConnectionRequest(SupplierPublicOrganizationIdOrQrPayload: "ORG001234"));

        Assert.False(result.IsSuccess);
        Assert.Equal(ConnectedSupplierDomainErrorCodes.SelfConnection, result.ErrorCode);
        Assert.Contains("itself", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Rejects_duplicate_pending_request()
    {
        var supplierOrg = Guid.NewGuid();
        var buyerOrg = Guid.NewGuid();
        var relationships = new FakeRelationships
        {
            OpenExisting = ConnectedSupplierRelationship.Request(
                PosOrganizationId.From(buyerOrg),
                PosOrganizationId.From(supplierOrg),
                DateTimeOffset.UtcNow)
        };
        var resolve = new FakeResolve
        {
            Result = ApplicationResult<PlatformOrganizationPublicResolveResult>.Success(
                new PlatformOrganizationPublicResolveResult(supplierOrg, "ORG001234", "ABC Trading"))
        };
        var useCase = CreateUseCase(relationships, resolve);

        var result = await useCase.ExecuteAsync(
            buyerOrg,
            new RequestConnectionRequest(SupplierPublicOrganizationIdOrQrPayload: "ORG001234"));

        Assert.False(result.IsSuccess);
        Assert.Equal(ConnectedSupplierErrorCodes.DuplicateRelationship, result.ErrorCode);
        Assert.Contains("already been sent", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Accepts_client_resolved_org_when_platform_session_missing_on_pos()
    {
        var supplierOrg = Guid.NewGuid();
        var buyerOrg = Guid.NewGuid();
        var relationships = new FakeRelationships();
        var suppliers = new FakeSuppliers();
        var resolve = new FakeResolve
        {
            Result = ApplicationResult<PlatformOrganizationPublicResolveResult>.Failure(
                ConnectedSupplierErrorCodes.NotFound,
                "Platform organization resolve failed with 401 Unauthorized.")
        };
        var useCase = CreateUseCase(relationships, resolve, suppliers);

        var result = await useCase.ExecuteAsync(
            buyerOrg,
            new RequestConnectionRequest(
                SupplierOrganizationId: supplierOrg,
                SupplierPublicOrganizationIdOrQrPayload: "ORG544183"));

        Assert.True(result.IsSuccess, $"{result.ErrorCode}: {result.ErrorMessage}");
        Assert.NotNull(relationships.LastAdded);
        Assert.Equal(supplierOrg, relationships.LastAdded!.SupplierOrganizationId.Value);
        Assert.Equal("ORG544183", relationships.LastAdded.SupplierPublicOrganizationIdSnapshot);
    }
}
