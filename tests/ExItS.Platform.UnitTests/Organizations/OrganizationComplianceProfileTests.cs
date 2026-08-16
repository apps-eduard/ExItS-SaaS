using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.UnitTests.Support;

namespace ExItS.Platform.UnitTests.Organizations;

public sealed class OrganizationComplianceProfileTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 14, 22, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Profiles_are_organization_scoped_and_do_not_expose_full_tin()
    {
        var orgA = PlatformOrganizationId.New();
        var orgB = PlatformOrganizationId.New();
        var orgs = new InMemoryPlatformOrganizationRepository();
        await orgs.AddAsync(CreateOrg(orgA, "Shop A"));
        await orgs.AddAsync(CreateOrg(orgB, "Shop B"));
        var profiles = new InMemoryComplianceProfileRepository();
        var capabilities = new InMemoryCapabilityRepository();
        await capabilities.AddAsync(OrganizationSalesDocumentCapability.CreateDefault(orgA, Now));
        var capA = await capabilities.GetByOrganizationIdAsync(orgA);
        capA!.TransitionEligibility(
            OrganizationComplianceEligibilityStatuses.Requested,
            "owner",
            Now);
        await capabilities.UpdateAsync(capA);

        var get = new GetOrganizationComplianceProfile(profiles, orgs, capabilities);
        var a = await get.ExecuteAsync(orgA);
        var b = await get.ExecuteAsync(orgB);

        Assert.True(a.IsSuccess);
        Assert.True(b.IsSuccess);
        Assert.Equal("Shop A", a.Value!.LegalName);
        Assert.Equal(OrganizationComplianceEligibilityStatuses.Requested, a.Value.ComplianceEligibilityStatus);
        Assert.Equal(OrganizationComplianceEligibilityStatuses.NotRequested, b.Value!.ComplianceEligibilityStatus);
        Assert.False(a.Value.TaxDocumentIssuanceEnabled);
        Assert.False(a.Value.TaxConfigurationEnabled);
        Assert.Equal("TransactionSummary", a.Value.DocumentMode);
        Assert.Equal(ComplianceSetupStatuses.NotConfigured, a.Value.SetupStatus);
        Assert.Contains(
            a.Value.GetType().GetProperties(),
            p => p.Name == "SnapshotGuidance");
        Assert.DoesNotContain(
            typeof(OrganizationComplianceProfileDto).GetProperties(),
            p => p.Name is "Tin" or "TinNormalized" or "FullTin");
        Assert.Contains(
            typeof(OrganizationComplianceProfileDto).GetProperties(),
            p => p.Name == "MaskedTin");
        AssertNoTaxIdentityProperties(typeof(OrganizationPublicIdentityDto));
        AssertNoTaxIdentityProperties(typeof(ResolvedPublicOrganizationDto));
    }

    [Fact]
    public async Task Ensure_initializes_profile_without_enabling_tax_document()
    {
        var orgId = PlatformOrganizationId.New();
        var orgs = new InMemoryPlatformOrganizationRepository();
        await orgs.AddAsync(CreateOrg(orgId, "Corner Store"));
        var profiles = new InMemoryComplianceProfileRepository();
        var capabilities = new InMemoryCapabilityRepository();
        var ensure = new EnsureOrganizationComplianceProfile(profiles, new NoOpUnitOfWork(), new FixedClock(Now));

        var created = await ensure.ExecuteAsync(orgId, "platform-admin");
        var again = await ensure.ExecuteAsync(orgId, "platform-admin");
        var dto = await new GetOrganizationComplianceProfile(profiles, orgs, capabilities)
            .ExecuteAsync(orgId);

        Assert.Equal(created.OrganizationId, again.OrganizationId);
        Assert.True(dto.Value!.ProfileInitialized);
        Assert.False(dto.Value.TaxDocumentIssuanceEnabled);
        Assert.False(dto.Value.TaxConfigurationEnabled);
        Assert.False(dto.Value.TaxDocumentImplementationAvailable);
        Assert.Equal(ComplianceSetupStatuses.NotConfigured, created.SetupStatus);
        Assert.Single(profiles.Items);
    }

    [Fact]
    public async Task Ownership_change_does_not_move_or_clear_org_profile_anchor()
    {
        var orgId = PlatformOrganizationId.New();
        var profiles = new InMemoryComplianceProfileRepository();
        var ensure = new EnsureOrganizationComplianceProfile(profiles, new NoOpUnitOfWork(), new FixedClock(Now));
        await ensure.ExecuteAsync(orgId, "eduard");

        // Ownership transfer changes membership only; profile remains keyed by OrganizationId.
        Assert.NotNull(await profiles.GetByOrganizationIdAsync(orgId));
        Assert.DoesNotContain(
            typeof(OrganizationComplianceProfile).GetProperties(),
            p => p.Name.Contains("Owner", StringComparison.OrdinalIgnoreCase)
                 || p.Name.Contains("User", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Rehydrate_preserves_registered_taxpayer_fields()
    {
        var orgId = PlatformOrganizationId.New();
        var rehydrated = OrganizationComplianceProfile.Rehydrate(
            orgId,
            "Acme Corp",
            "123456789",
            ComplianceSetupStatuses.SetupInProgress,
            Now,
            Now,
            "actor");

        Assert.Equal("Acme Corp", rehydrated.RegisteredTaxpayerName);
        Assert.Equal("123456789", rehydrated.TinNormalized);
        Assert.Equal("***-***-789", rehydrated.MaskedTin);
        Assert.Equal(ComplianceSetupStatuses.SetupInProgress, rehydrated.SetupStatus);
    }

    private static PlatformOrganization CreateOrg(PlatformOrganizationId id, string legalName) =>
        PlatformOrganization.Rehydrate(
            id,
            legalName,
            "org-" + id.Value.ToString("N")[..8],
            publicOrganizationId: null,
            primaryBusinessTypeId: null,
            OrganizationStatus.Active,
            OrganizationProfile.Create(
                legalName: legalName,
                contactEmail: null,
                contactPhone: null,
                addressLine1: "1 Main St",
                addressLine2: null,
                city: "Manila",
                region: "NCR",
                postalCode: "1000",
                countryCode: "PH",
                timeZoneId: null,
                locale: null,
                currencyCode: null),
            OrganizationBranding.Empty,
            Now,
            Now);

    private static void AssertNoTaxIdentityProperties(Type type) =>
        Assert.DoesNotContain(
            type.GetProperties(),
            property =>
                property.Name.Contains("Tin", StringComparison.OrdinalIgnoreCase)
                || property.Name.Contains("Tax", StringComparison.OrdinalIgnoreCase)
                || property.Name.Contains("Compliance", StringComparison.OrdinalIgnoreCase));

    private sealed class InMemoryComplianceProfileRepository : IOrganizationComplianceProfileRepository
    {
        public Dictionary<Guid, OrganizationComplianceProfile> Items { get; } = [];

        public Task<OrganizationComplianceProfile?> GetByOrganizationIdAsync(
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default)
        {
            Items.TryGetValue(organizationId.Value, out var value);
            return Task.FromResult(value);
        }

        public Task AddAsync(
            OrganizationComplianceProfile profile,
            CancellationToken cancellationToken = default)
        {
            Items[profile.OrganizationId.Value] = profile;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(
            OrganizationComplianceProfile profile,
            CancellationToken cancellationToken = default)
        {
            Items[profile.OrganizationId.Value] = profile;
            return Task.CompletedTask;
        }
    }

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
