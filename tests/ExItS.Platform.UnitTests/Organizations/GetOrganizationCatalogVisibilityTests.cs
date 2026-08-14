using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Integration.Pos;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.UnitTests.Organizations;

public sealed class GetOrganizationCatalogVisibilityTests
{
    private static readonly Guid OrgA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid OrgB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public async Task Execute_passes_requested_organization_id_to_pos_client()
    {
        var org = PlatformOrganization.Create(
            "Org A",
            "org-a",
            DateTimeOffset.Parse("2026-08-10T00:00:00Z"),
            PlatformOrganizationId.From(OrgA));
        var client = new FakePosCatalogClient();
        var useCase = new GetOrganizationCatalogVisibility(new FakeOrgRepo(org), client);

        var result = await useCase.ExecuteAsync(OrgA, page: 1, pageSize: 20);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrgA, client.LastRequestedOrganizationId);
        Assert.DoesNotContain(OrgB, client.RequestedOrganizationIds);
        Assert.Equal("Org A", result.Value!.OrganizationDisplayName);
        Assert.Null(result.Value.BusinessType);
        Assert.All(result.Value.Products, _ => Assert.Equal(OrgA, client.LastRequestedOrganizationId));
    }

    [Fact]
    public async Task Execute_returns_not_found_when_organization_missing()
    {
        var client = new FakePosCatalogClient();
        var useCase = new GetOrganizationCatalogVisibility(new FakeOrgRepo(null), client);

        var result = await useCase.ExecuteAsync(OrgA);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.OrganizationNotFound, result.ErrorCode);
        Assert.Empty(client.RequestedOrganizationIds);
    }

    [Fact]
    public void Read_client_interface_has_no_write_methods()
    {
        var methods = typeof(IPosOrganizationCatalogReadClient).GetMethods();
        Assert.All(methods, m =>
        {
            Assert.StartsWith("Get", m.Name, StringComparison.Ordinal);
            Assert.DoesNotContain("Create", m.Name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Update", m.Name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Delete", m.Name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Post", m.Name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Patch", m.Name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Put", m.Name, StringComparison.OrdinalIgnoreCase);
        });
        Assert.Single(methods);
        Assert.Equal(nameof(IPosOrganizationCatalogReadClient.GetOrganizationCatalogAsync), methods[0].Name);
    }

    private sealed class FakeOrgRepo : IPlatformOrganizationRepository
    {
        private readonly PlatformOrganization? _org;

        public FakeOrgRepo(PlatformOrganization? org) => _org = org;

        public Task<PlatformOrganization?> GetByIdAsync(PlatformOrganizationId id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_org is not null && _org.Id == id ? _org : null);

        public Task<PlatformOrganization?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
            Task.FromResult<PlatformOrganization?>(null);

        public Task<PlatformOrganization?> GetByPublicOrganizationIdAsync(
            string publicOrganizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<PlatformOrganization?>(null);

        public Task<(IReadOnlyList<PlatformOrganization> Items, int TotalCount)> ListAsync(
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<PlatformOrganization>, int)>(([], 0));

        public Task<(IReadOnlyList<PlatformOrganization> Items, int TotalCount)> ListAsync(
            OrganizationStatus? status,
            string? search,
            OrganizationListSortBy sortBy,
            bool sortDescending,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<PlatformOrganization>, int)>(([], 0));

        public Task AddAsync(PlatformOrganization organization, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task UpdateAsync(PlatformOrganization organization, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FakePosCatalogClient : IPosOrganizationCatalogReadClient
    {
        public Guid? LastRequestedOrganizationId { get; private set; }
        public List<Guid> RequestedOrganizationIds { get; } = [];

        public Task<PosOrganizationCatalogSummaryDto> GetOrganizationCatalogAsync(
            Guid organizationId,
            int? page = null,
            int? pageSize = null,
            string? search = null,
            CancellationToken cancellationToken = default)
        {
            LastRequestedOrganizationId = organizationId;
            RequestedOrganizationIds.Add(organizationId);

            var products = organizationId == OrgA
                ? new List<PosOrganizationCatalogProductDto>
                {
                    new(
                        Guid.Parse("11111111-1111-1111-1111-111111111111"),
                        "Org A product",
                        "SKU-A",
                        null,
                        null,
                        null,
                        10m,
                        false,
                        null,
                        "Active",
                        OrganizationCatalogProvenance.MerchantCreated,
                        null,
                        null,
                        null,
                        "Manual")
                }
                : new List<PosOrganizationCatalogProductDto>
                {
                    new(
                        Guid.Parse("22222222-2222-2222-2222-222222222222"),
                        "Org B product",
                        "SKU-B",
                        null,
                        null,
                        null,
                        20m,
                        false,
                        null,
                        "Active",
                        OrganizationCatalogProvenance.MerchantCreated,
                        null,
                        null,
                        null,
                        "Manual")
                };

            return Task.FromResult(new PosOrganizationCatalogSummaryDto(
                organizationId,
                products.Count,
                new Dictionary<string, int> { [OrganizationCatalogProvenance.MerchantCreated] = products.Count },
                products,
                page ?? 1,
                pageSize ?? 20,
                products.Count));
        }
    }
}
