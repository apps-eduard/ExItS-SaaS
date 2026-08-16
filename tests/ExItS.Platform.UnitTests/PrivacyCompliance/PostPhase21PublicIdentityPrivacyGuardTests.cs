using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Application.PrivacyCompliance;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.PrivacyCompliance;

namespace ExItS.Platform.UnitTests.PrivacyCompliance;

public sealed class PostPhase21PublicIdentityPrivacyGuardTests
{
    [Fact]
    public void Public_organization_dtos_exclude_compliance_and_tax_identity()
    {
        AssertNoSensitive(typeof(OrganizationPublicIdentityDto));
        AssertNoSensitive(typeof(ResolvedPublicOrganizationDto));
        // Compliance status is Platform/org-authorized — not a public resolver contract.
        Assert.DoesNotContain(
            typeof(OrganizationPublicIdentityDto).GetProperties().Select(p => p.Name),
            name => name.Contains("Compliance", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(
            typeof(ResolvedPublicOrganizationDto).GetProperties().Select(p => p.Name),
            name => name.Contains("Compliance", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("Acknowledg", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Public_personal_dtos_remain_minimal()
    {
        AssertNoSensitive(typeof(PublicIdentityDto));
        AssertNoSensitive(typeof(ResolvedPublicUserDto));
        Assert.Contains(
            typeof(ResolvedPublicUserDto).GetProperties().Select(p => p.Name),
            name => name is "PublicUserId" or "DisplayName" or "Status" or "IsSelf" or "MaskedEmail" or "UserIdentityId");
    }

    [Fact]
    public void Education_and_compliance_dtos_do_not_require_device_or_geo_fields()
    {
        Assert.DoesNotContain(
            typeof(OrganizationSalesDocumentEducationStatusDto).GetProperties(),
            p => p.Name.Contains("Ip", StringComparison.OrdinalIgnoreCase)
                 || p.Name.Contains("Fingerprint", StringComparison.OrdinalIgnoreCase)
                 || p.Name.Contains("Gps", StringComparison.OrdinalIgnoreCase)
                 || p.Name.Contains("Latitude", StringComparison.OrdinalIgnoreCase));
        // Authorized org compliance DTO may expose MaskedTin only — never full TIN / TinNormalized.
        Assert.DoesNotContain(
            typeof(OrganizationComplianceProfileDto).GetProperties(),
            p => p.Name is "Tin" or "TinNormalized" or "FullTin"
                 || p.Name.Contains("Evidence", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            typeof(OrganizationComplianceProfileDto).GetProperties(),
            p => p.Name == "MaskedTin");
    }

    [Fact]
    public async Task Catalog_seeds_include_p25_p26_delta_marked_for_dpo_review()
    {
        var requirements = new FakeRequirementRepo();
        var evidence = new FakeEvidenceRepo();
        var systems = new FakeSystemRepo();
        var ensure = new EnsurePrivacyComplianceCatalog(
            requirements,
            evidence,
            systems,
            new FakeUow(),
            new FakeClock());

        var result = await ensure.ExecuteAsync();

        Assert.True(result.RequirementsAdded >= 18);
        Assert.True(result.SystemsAdded >= 7);
        Assert.Contains(requirements.Items, r => r.Code == "PIA_P25_TYPED_QR" && r.RequiresDpoLegalVerification);
        Assert.Contains(requirements.Items, r => r.Code == "DATA_INV_OWNERSHIP_TRANSFER");
        Assert.Contains(requirements.Items, r => r.Code == "RETENTION_P25_P26_DELTA" && r.RequiresDpoLegalVerification);
        Assert.Contains(requirements.Items, r => r.Code == "PIC_PIP_ROLE_CLASSIFICATION");
        Assert.Contains(requirements.Items, r => r.Code == "PRIVACY_NOTICE_P25_P26_DRAFT");
        Assert.Contains(systems.Items, s => s.Code == "SYS_ORG_COMPLIANCE_ELIGIBILITY");
        Assert.Contains(systems.Items, s => s.Code == "SYS_FUTURE_COMPLIANCE_EVIDENCE");
        Assert.DoesNotContain(requirements.Items, r => r.Status == ComplianceItemStatus.Approved);
        Assert.DoesNotContain(systems.Items, s => s.PiaStatus == ProcessingSystemPiaStatus.Approved);
    }

    private static void AssertNoSensitive(Type type) =>
        Assert.DoesNotContain(
            type.GetProperties(),
            property =>
                property.Name.Contains("Tin", StringComparison.OrdinalIgnoreCase)
                || property.Name.Contains("TaxDocument", StringComparison.OrdinalIgnoreCase)
                || property.Name.Contains("Eligibility", StringComparison.OrdinalIgnoreCase)
                || property.Name.Contains("Acknowledg", StringComparison.OrdinalIgnoreCase)
                || property.Name.Contains("Compliance", StringComparison.OrdinalIgnoreCase)
                || property.Name.Contains("Reviewer", StringComparison.OrdinalIgnoreCase)
                || property.Name.Contains("Evidence", StringComparison.OrdinalIgnoreCase));

    private sealed class FakeClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.Parse("2026-08-15T00:00:00Z");
    }

    private sealed class FakeUow : IPlatformUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeRequirementRepo : IComplianceRequirementRepository
    {
        public List<ComplianceRequirement> Items { get; } = [];

        public Task AddAsync(ComplianceRequirement requirement, CancellationToken cancellationToken = default)
        {
            Items.Add(requirement);
            return Task.CompletedTask;
        }

        public Task<ComplianceRequirement?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(i =>
                string.Equals(i.Code, code, StringComparison.OrdinalIgnoreCase)));

        public Task<ComplianceRequirement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(i => i.Id == id));

        public Task<IReadOnlyList<ComplianceRequirement>> ListAsync(
            ComplianceItemCategory? category,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ComplianceRequirement>>(
                category is null ? Items : Items.Where(i => i.Category == category).ToList());

        public Task UpdateAsync(ComplianceRequirement requirement, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FakeEvidenceRepo : IComplianceEvidenceRepository
    {
        public List<ComplianceEvidenceReference> Items { get; } = [];

        public Task AddAsync(ComplianceEvidenceReference evidence, CancellationToken cancellationToken = default)
        {
            Items.Add(evidence);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(
            Guid requirementId,
            string referencePath,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Any(i =>
                i.RequirementId == requirementId
                && string.Equals(i.ReferencePath, referencePath, StringComparison.OrdinalIgnoreCase)));

        public Task<ComplianceEvidenceReference?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(i => i.Id == id));

        public Task<IReadOnlyList<ComplianceEvidenceReference>> ListByRequirementIdAsync(
            Guid requirementId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ComplianceEvidenceReference>>(
                Items.Where(i => i.RequirementId == requirementId).ToList());
    }

    private sealed class FakeSystemRepo : IProcessingSystemRepository
    {
        public List<ProcessingSystemRecord> Items { get; } = [];

        public Task AddAsync(ProcessingSystemRecord system, CancellationToken cancellationToken = default)
        {
            Items.Add(system);
            return Task.CompletedTask;
        }

        public Task<ProcessingSystemRecord?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(i =>
                string.Equals(i.Code, code, StringComparison.OrdinalIgnoreCase)));

        public Task<ProcessingSystemRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(i => i.Id == id));

        public Task<IReadOnlyList<ProcessingSystemRecord>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ProcessingSystemRecord>>(Items);

        public Task UpdateAsync(ProcessingSystemRecord system, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
