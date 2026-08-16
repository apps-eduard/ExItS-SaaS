using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.UnitTests.Organizations;

public sealed class OrganizationSalesDocumentCapabilityTests
{
    [Fact]
    public async Task Missing_capability_is_not_enabled_even_when_product_tax_is_configured()
    {
        var repository = new InMemoryCapabilityRepository();
        var organizationId = PlatformOrganizationId.New();
        const decimal configuredTaxRatePercent = 12m;

        var result = await new GetOrganizationSalesDocumentCapability(repository)
            .ExecuteAsync(organizationId);

        Assert.Equal(12m, configuredTaxRatePercent);
        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.TransactionSummaryAvailable);
        Assert.False(result.Value.TaxDocumentIssuanceEnabled);
        Assert.Equal(SalesDocumentCapabilityStatuses.NotEnabled, result.Value.TaxDocumentIssuanceStatus);
        Assert.False(result.Value.TaxConfigurationEnabled);
        Assert.Equal(SalesDocumentCapabilityStatuses.NotEnabled, result.Value.TaxConfigurationStatus);
        Assert.Equal(OrganizationComplianceEligibilityStatuses.NotRequested, result.Value.ComplianceEligibilityStatus);
        Assert.False(result.Value.TaxDocumentImplementationAvailable);
    }

    [Fact]
    public void CreateDefault_tax_configuration_enabled_is_false()
    {
        var capability = OrganizationSalesDocumentCapability.CreateDefault(
            PlatformOrganizationId.New(),
            new DateTimeOffset(2026, 8, 16, 12, 0, 0, TimeSpan.Zero));

        Assert.False(capability.TaxConfigurationEnabled);
        Assert.False(capability.TaxDocumentIssuanceEnabled);
        Assert.Equal(OrganizationComplianceEligibilityStatuses.NotRequested, capability.ComplianceEligibilityStatus);
    }

    [Fact]
    public async Task Ensure_and_request_tax_document_fail_when_implementation_unavailable()
    {
        var repository = new InMemoryCapabilityRepository();
        var organizationId = PlatformOrganizationId.New();
        var ensure = new EnsureTaxDocumentIssuanceAllowed(repository);

        var ensureResult = await ensure.ExecuteAsync(organizationId);
        var requestResult = await new RequestTaxDocumentIssuance(ensure).ExecuteAsync(organizationId);

        Assert.False(ensureResult.IsSuccess);
        Assert.False(requestResult.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.TaxDocumentIssuanceNotImplemented, ensureResult.ErrorCode);
        Assert.Equal(ApplicationErrorCodes.TaxDocumentIssuanceNotImplemented, requestResult.ErrorCode);
    }

    [Fact]
    public async Task Capabilities_are_independent_between_organizations_and_survive_owner_concept_changes()
    {
        var repository = new InMemoryCapabilityRepository();
        var orgA = PlatformOrganizationId.New();
        var orgB = PlatformOrganizationId.New();
        await repository.AddAsync(OrganizationSalesDocumentCapability.CreateDefault(
            orgA,
            new DateTimeOffset(2026, 8, 14, 12, 0, 0, TimeSpan.Zero)));

        Assert.NotNull(await repository.GetByOrganizationIdAsync(orgA));
        Assert.Null(await repository.GetByOrganizationIdAsync(orgB));
        Assert.DoesNotContain(
            typeof(OrganizationSalesDocumentCapability).GetProperties(),
            property => property.Name.Contains("Owner", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Public_organization_contracts_do_not_expose_tax_or_tin_fields()
    {
        AssertNoTaxIdentityProperties(typeof(OrganizationPublicIdentityDto));
        AssertNoTaxIdentityProperties(typeof(ResolvedPublicOrganizationDto));
    }

    private static void AssertNoTaxIdentityProperties(Type type) =>
        Assert.DoesNotContain(
            type.GetProperties(),
            property =>
                property.Name.Contains("Tin", StringComparison.OrdinalIgnoreCase)
                || property.Name.Contains("Tax", StringComparison.OrdinalIgnoreCase));

    private sealed class InMemoryCapabilityRepository : IOrganizationSalesDocumentCapabilityRepository
    {
        private readonly Dictionary<Guid, OrganizationSalesDocumentCapability> _items = [];

        public Task<OrganizationSalesDocumentCapability?> GetByOrganizationIdAsync(
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default)
        {
            _items.TryGetValue(organizationId.Value, out var value);
            return Task.FromResult(value);
        }

        public Task AddAsync(
            OrganizationSalesDocumentCapability capability,
            CancellationToken cancellationToken = default)
        {
            _items[capability.OrganizationId.Value] = capability;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(
            OrganizationSalesDocumentCapability capability,
            CancellationToken cancellationToken = default)
        {
            _items[capability.OrganizationId.Value] = capability;
            return Task.CompletedTask;
        }
    }
}
