using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.PrivacyCompliance;
using ExItS.Platform.Domain.PrivacyCompliance;
using ExItS.Platform.Infrastructure.Persistence.PrivacyCompliance;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence.Repositories;

internal sealed class ComplianceRequirementRepository : IComplianceRequirementRepository
{
    private readonly PlatformDbContext _db;

    public ComplianceRequirementRepository(PlatformDbContext db) => _db = db;

    public async Task<ComplianceRequirement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await _db.ComplianceRequirements.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : PrivacyComplianceEntityMapper.ToDomain(record);
    }

    public async Task<ComplianceRequirement?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalized = code.Trim().ToUpperInvariant();
        var record = await _db.ComplianceRequirements.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Code == normalized, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : PrivacyComplianceEntityMapper.ToDomain(record);
    }

    public async Task<IReadOnlyList<ComplianceRequirement>> ListAsync(
        ComplianceItemCategory? category,
        CancellationToken cancellationToken = default)
    {
        var query = _db.ComplianceRequirements.AsNoTracking().AsQueryable();
        if (category is not null)
        {
            var categoryText = category.Value.ToString();
            query = query.Where(r => r.Category == categoryText);
        }

        var records = await query
            .OrderBy(r => r.Category)
            .ThenBy(r => r.Code)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(PrivacyComplianceEntityMapper.ToDomain).ToList();
    }

    public Task AddAsync(ComplianceRequirement requirement, CancellationToken cancellationToken = default)
    {
        _db.ComplianceRequirements.Add(PrivacyComplianceEntityMapper.ToRecord(requirement));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(ComplianceRequirement requirement, CancellationToken cancellationToken = default)
    {
        var record = await _db.ComplianceRequirements
            .FirstOrDefaultAsync(r => r.Id == requirement.Id, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.ComplianceRequirementNotFound,
                "Compliance requirement was not found.");
        }

        PrivacyComplianceEntityMapper.ApplyToRecord(requirement, record);
    }
}

internal sealed class ComplianceEvidenceRepository : IComplianceEvidenceRepository
{
    private readonly PlatformDbContext _db;

    public ComplianceEvidenceRepository(PlatformDbContext db) => _db = db;

    public async Task<ComplianceEvidenceReference?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await _db.ComplianceEvidence.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : PrivacyComplianceEntityMapper.ToDomain(record);
    }

    public async Task<IReadOnlyList<ComplianceEvidenceReference>> ListByRequirementIdAsync(
        Guid requirementId,
        CancellationToken cancellationToken = default)
    {
        var records = await _db.ComplianceEvidence.AsNoTracking()
            .Where(e => e.RequirementId == requirementId)
            .OrderBy(e => e.CreatedAtUtc)
            .ThenBy(e => e.Label)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(PrivacyComplianceEntityMapper.ToDomain).ToList();
    }

    public async Task<bool> ExistsAsync(
        Guid requirementId,
        string referencePath,
        CancellationToken cancellationToken = default)
    {
        var normalizedPath = referencePath.Trim();
        return await _db.ComplianceEvidence.AsNoTracking()
            .AnyAsync(
                e => e.RequirementId == requirementId && e.ReferencePath == normalizedPath,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public Task AddAsync(ComplianceEvidenceReference evidence, CancellationToken cancellationToken = default)
    {
        _db.ComplianceEvidence.Add(PrivacyComplianceEntityMapper.ToRecord(evidence));
        return Task.CompletedTask;
    }
}

internal sealed class ProcessingSystemRepository : IProcessingSystemRepository
{
    private readonly PlatformDbContext _db;

    public ProcessingSystemRepository(PlatformDbContext db) => _db = db;

    public async Task<ProcessingSystemRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await _db.ProcessingSystems.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : PrivacyComplianceEntityMapper.ToDomain(record);
    }

    public async Task<ProcessingSystemRecord?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalized = code.Trim().ToUpperInvariant();
        var record = await _db.ProcessingSystems.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Code == normalized, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : PrivacyComplianceEntityMapper.ToDomain(record);
    }

    public async Task<IReadOnlyList<ProcessingSystemRecord>> ListAsync(CancellationToken cancellationToken = default)
    {
        var records = await _db.ProcessingSystems.AsNoTracking()
            .OrderBy(s => s.Code)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(PrivacyComplianceEntityMapper.ToDomain).ToList();
    }

    public Task AddAsync(ProcessingSystemRecord system, CancellationToken cancellationToken = default)
    {
        _db.ProcessingSystems.Add(PrivacyComplianceEntityMapper.ToRecord(system));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(ProcessingSystemRecord system, CancellationToken cancellationToken = default)
    {
        var record = await _db.ProcessingSystems
            .FirstOrDefaultAsync(s => s.Id == system.Id, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.ProcessingSystemNotFound,
                "Processing system was not found.");
        }

        PrivacyComplianceEntityMapper.ApplyToRecord(system, record);
    }
}
