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

/// <summary>B2B-01..10 focused coverage for supplier operating-branch connection.</summary>
public sealed class ConnectedSupplierBranchLocationTests
{
    private static readonly Guid Manila = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Cebu = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid Iloilo = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid Foreign = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");

    private sealed class FakeAccess : IPosCommercialAccessAccessor
    {
        public PosCommercialAccess Current { get; set; } = PosCommercialAccess.DevelopmentDefault;
    }

    private sealed class FakeRelationships : IConnectedSupplierRelationshipRepository
    {
        public ConnectedSupplierRelationship? LastAdded { get; private set; }
        public ConnectedSupplierRelationship? Stored { get; set; }

        public Task AddAsync(ConnectedSupplierRelationship relationship, CancellationToken ct = default)
        {
            LastAdded = relationship;
            Stored = relationship;
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
            Task.FromResult(Stored is not null && Stored.Id == id ? Stored : null);

        public Task<IReadOnlyList<ConnectedSupplierRelationship>> ListAsync(
            PosOrganizationId organizationId,
            bool supplierView,
            CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ConnectedSupplierRelationship>>(
                Stored is null ? [] : [Stored]);

        public Task UpdateAsync(ConnectedSupplierRelationship relationship, CancellationToken ct = default)
        {
            Stored = relationship;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeSuppliers : ISupplierRepository
    {
        public int AllocateCalls { get; private set; }

        public Task AddAsync(Supplier supplier, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

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

        public Task<Supplier?> FindActiveByNormalizedTaxOrRegistrationAsync(
            PosOrganizationId organizationId,
            string normalizedTaxOrRegistration,
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

    private static RequestConnection CreateRequest(
        FakeRelationships relationships,
        FakeResolve resolve,
        FakeLocations locations) =>
        new(relationships, new FakeSuppliers(), new FakeUow(), new FakeAccess(), resolve, locations);

    [Fact]
    public async Task B2B01_org_qr_chooses_supplier_branch()
    {
        var supplierOrg = Guid.NewGuid();
        var relationships = new FakeRelationships();
        var useCase = CreateRequest(
            relationships,
            new FakeResolve { SupplierOrgId = supplierOrg },
            new FakeLocations
            {
                Locations =
                [
                    new(Manila, "Manila Branch", "BR-MNL", true),
                    new(Cebu, "Cebu Branch", "BR-CEB", false),
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
        Assert.Equal("Iloilo Branch", relationships.LastAdded.SupplierBranchNameSnapshot);
        Assert.Equal(supplierOrg, relationships.LastAdded.SupplierOrganizationId.Value);
    }

    [Fact]
    public async Task B2B02_single_branch_auto_selected()
    {
        var relationships = new FakeRelationships();
        var useCase = CreateRequest(
            relationships,
            new FakeResolve { SupplierOrgId = Guid.NewGuid() },
            new FakeLocations { Locations = [new(Iloilo, "Iloilo Branch", "BR-ILO", true)] });

        var result = await useCase.ExecuteAsync(
            Guid.NewGuid(),
            new RequestConnectionRequest(SupplierPublicOrganizationIdOrQrPayload: "ORG123456"));

        Assert.True(result.IsSuccess, $"{result.ErrorCode}: {result.ErrorMessage}");
        Assert.Equal(Iloilo, relationships.LastAdded!.SupplierBranchId);
        Assert.Equal("Iloilo Branch", result.Value!.SupplierBranchName);
    }

    [Fact]
    public async Task B2B03_branch_qr_preselect_persists_exact_branch()
    {
        var relationships = new FakeRelationships();
        var useCase = CreateRequest(
            relationships,
            new FakeResolve { SupplierOrgId = Guid.NewGuid() },
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

        Assert.True(result.IsSuccess);
        Assert.Equal(Iloilo, result.Value!.SupplierBranchId);
        Assert.Equal("Iloilo Branch", result.Value.SupplierBranchName);
    }

    [Fact]
    public async Task B2B04_supplier_remains_one_organization_relationship()
    {
        var supplierOrg = Guid.NewGuid();
        var relationships = new FakeRelationships();
        var useCase = CreateRequest(
            relationships,
            new FakeResolve { SupplierOrgId = supplierOrg },
            new FakeLocations { Locations = [new(Iloilo, "Iloilo Branch", "BR-ILO", true)] });

        var result = await useCase.ExecuteAsync(
            Guid.NewGuid(),
            new RequestConnectionRequest(SupplierPublicOrganizationIdOrQrPayload: "ORG123456"));

        Assert.True(result.IsSuccess);
        Assert.Equal(supplierOrg, result.Value!.SupplierOrganizationId);
        Assert.Equal(Iloilo, result.Value.SupplierBranchId);
    }

    [Fact]
    public async Task B2B05_wrong_org_branch_id_rejected()
    {
        var useCase = CreateRequest(
            new FakeRelationships(),
            new FakeResolve { SupplierOrgId = Guid.NewGuid() },
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
                SupplierBranchId: Foreign));

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorCodes.ConnectedSupplierBranchInvalid, result.ErrorCode);
    }

    [Fact]
    public async Task B2B06_inactive_branch_not_listed_is_rejected()
    {
        var useCase = CreateRequest(
            new FakeRelationships(),
            new FakeResolve { SupplierOrgId = Guid.NewGuid() },
            new FakeLocations { Locations = [new(Manila, "Manila Branch", "BR-MNL", true)] });

        var result = await useCase.ExecuteAsync(
            Guid.NewGuid(),
            new RequestConnectionRequest(
                SupplierPublicOrganizationIdOrQrPayload: "ORG123456",
                SupplierBranchId: Cebu));

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorCodes.ConnectedSupplierBranchInvalid, result.ErrorCode);
    }

    [Fact]
    public async Task B2B07_dto_exposes_supplier_source_branch_name_not_required_uuid_in_name()
    {
        var relationships = new FakeRelationships();
        var useCase = CreateRequest(
            relationships,
            new FakeResolve { SupplierOrgId = Guid.NewGuid() },
            new FakeLocations { Locations = [new(Iloilo, "Iloilo Branch", "BR-ILO", true)] });

        var result = await useCase.ExecuteAsync(
            Guid.NewGuid(),
            new RequestConnectionRequest(SupplierPublicOrganizationIdOrQrPayload: "ORG123456"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Iloilo Branch", result.Value!.SupplierBranchName);
        Assert.DoesNotContain(Iloilo.ToString("D"), result.Value.SupplierBranchName!, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("ABC Wholesale", result.Value.CounterpartyDisplayName);
    }

    [Fact]
    public async Task B2B_change_supplier_location_updates_branch_only()
    {
        var buyer = Guid.NewGuid();
        var supplierOrg = Guid.NewGuid();
        var relationships = new FakeRelationships
        {
            Stored = ConnectedSupplierRelationship.Request(
                PosOrganizationId.From(buyer),
                PosOrganizationId.From(supplierOrg),
                DateTimeOffset.UtcNow,
                supplierDisplayName: "ABC Wholesale",
                supplierPublicOrganizationId: "ORG123456",
                supplierBranchId: Manila,
                supplierBranchName: "Manila Branch")
        };
        relationships.Stored.Approve(DateTimeOffset.UtcNow);
        var locations = new FakeLocations
        {
            Locations =
            [
                new(Manila, "Manila Branch", "BR-MNL", true),
                new(Iloilo, "Iloilo Branch", "BR-ILO", false)
            ]
        };
        var useCase = new UpdateSupplierLocation(
            relationships,
            new FakeUow(),
            new FakeAccess(),
            locations);

        var result = await useCase.ExecuteAsync(
            buyer,
            relationships.Stored.Id.Value,
            new UpdateSupplierLocationRequest(Iloilo));

        Assert.True(result.IsSuccess, $"{result.ErrorCode}: {result.ErrorMessage}");
        Assert.Equal(Iloilo, relationships.Stored.SupplierBranchId);
        Assert.Equal("Iloilo Branch", relationships.Stored.SupplierBranchNameSnapshot);
        Assert.Equal(supplierOrg, relationships.Stored.SupplierOrganizationId.Value);
    }

    [Fact]
    public async Task B2B_multi_branch_without_selection_requires_choice()
    {
        var useCase = CreateRequest(
            new FakeRelationships(),
            new FakeResolve { SupplierOrgId = Guid.NewGuid() },
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
            new RequestConnectionRequest(SupplierPublicOrganizationIdOrQrPayload: "ORG123456"));

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorCodes.ConnectedSupplierBranchRequired, result.ErrorCode);
    }
}
