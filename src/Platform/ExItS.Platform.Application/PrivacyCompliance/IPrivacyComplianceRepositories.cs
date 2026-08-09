using ExItS.Platform.Domain.PrivacyCompliance;

namespace ExItS.Platform.Application.PrivacyCompliance;

public interface IComplianceRequirementRepository
{
    Task<ComplianceRequirement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ComplianceRequirement?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ComplianceRequirement>> ListAsync(
        ComplianceItemCategory? category,
        CancellationToken cancellationToken = default);

    Task AddAsync(ComplianceRequirement requirement, CancellationToken cancellationToken = default);

    Task UpdateAsync(ComplianceRequirement requirement, CancellationToken cancellationToken = default);
}

public interface IComplianceEvidenceRepository
{
    Task<ComplianceEvidenceReference?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ComplianceEvidenceReference>> ListByRequirementIdAsync(
        Guid requirementId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        Guid requirementId,
        string referencePath,
        CancellationToken cancellationToken = default);

    Task AddAsync(ComplianceEvidenceReference evidence, CancellationToken cancellationToken = default);
}

public interface IProcessingSystemRepository
{
    Task<ProcessingSystemRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ProcessingSystemRecord?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProcessingSystemRecord>> ListAsync(CancellationToken cancellationToken = default);

    Task AddAsync(ProcessingSystemRecord system, CancellationToken cancellationToken = default);

    Task UpdateAsync(ProcessingSystemRecord system, CancellationToken cancellationToken = default);
}
