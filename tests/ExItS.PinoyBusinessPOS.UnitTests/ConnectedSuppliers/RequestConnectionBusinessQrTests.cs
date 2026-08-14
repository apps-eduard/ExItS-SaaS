using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;

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

        public Task AddAsync(ConnectedSupplierRelationship relationship, CancellationToken ct = default)
        {
            LastAdded = relationship;
            return Task.CompletedTask;
        }

        public Task<ConnectedSupplierRelationship?> FindOpenAsync(
            PosOrganizationId buyer,
            PosOrganizationId supplier,
            CancellationToken ct = default) =>
            Task.FromResult<ConnectedSupplierRelationship?>(null);

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

        public Task<ApplicationResult<PlatformOrganizationPublicResolveResult>> ResolveOrganizationForConnectedSupplierAsync(
            string publicOrganizationIdOrQrPayload,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result!);
    }

    [Fact]
    public async Task Requires_business_qr_payload()
    {
        var useCase = new RequestConnection(
            new FakeRelationships(),
            new FakeUow(),
            new FakeAccess(),
            new FakeResolve());

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
        var useCase = new RequestConnection(
            new FakeRelationships(),
            new FakeUow(),
            new FakeAccess(),
            resolve);

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
        var useCase = new RequestConnection(
            new FakeRelationships(),
            new FakeUow(),
            new FakeAccess(),
            new FakeResolve());

        var result = await useCase.ExecuteAsync(
            Guid.NewGuid(),
            new RequestConnectionRequest(
                SupplierPublicOrganizationIdOrQrPayload: "exits://qr/v1/pos-device-registration/tok"));

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorCodes.ConnectedSupplierQrPurposeMismatch, result.ErrorCode);
    }

    [Fact]
    public async Task Accepts_resolved_organization_business_qr()
    {
        var supplierOrg = Guid.NewGuid();
        var buyerOrg = Guid.NewGuid();
        var relationships = new FakeRelationships();
        var resolve = new FakeResolve
        {
            Result = ApplicationResult<PlatformOrganizationPublicResolveResult>.Success(
                new PlatformOrganizationPublicResolveResult(supplierOrg, "ORG001234", "ABC Trading"))
        };
        var useCase = new RequestConnection(
            relationships,
            new FakeUow(),
            new FakeAccess(),
            resolve);

        var result = await useCase.ExecuteAsync(
            buyerOrg,
            new RequestConnectionRequest(
                SupplierPublicOrganizationIdOrQrPayload: "exits://qr/v1/organization/ORG001234"));

        Assert.True(result.IsSuccess);
        Assert.NotNull(relationships.LastAdded);
        Assert.Equal(supplierOrg, relationships.LastAdded!.SupplierOrganizationId.Value);
        Assert.Equal(buyerOrg, relationships.LastAdded.BuyerOrganizationId.Value);
    }
}
