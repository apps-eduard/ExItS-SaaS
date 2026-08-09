using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.PrivacyCompliance;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.PrivacyCompliance;

namespace ExItS.Platform.UnitTests.PrivacyCompliance;

public sealed class EnsurePrivacyComplianceCatalogTests
{
    [Fact]
    public async Task Ensure_is_idempotent_on_second_run()
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

        var first = await ensure.ExecuteAsync();
        var second = await ensure.ExecuteAsync();

        Assert.True(first.RequirementsAdded > 0);
        Assert.True(first.SystemsAdded > 0);
        Assert.Equal(0, second.RequirementsAdded);
        Assert.Equal(0, second.SystemsAdded);
        Assert.Equal(0, second.EvidenceAdded);
        Assert.Equal(first.RequirementsAdded, requirements.Items.Count);
        Assert.DoesNotContain(requirements.Items, r => r.Status == ComplianceItemStatus.Approved);
        Assert.Contains(requirements.Items, r => r.RequiresDpoLegalVerification);
    }

    private sealed class FakeClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.Parse("2026-08-09T12:00:00Z");
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
