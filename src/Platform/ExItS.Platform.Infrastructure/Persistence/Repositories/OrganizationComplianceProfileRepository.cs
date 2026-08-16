using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Organizations;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence.Repositories;

internal sealed class OrganizationComplianceProfileRepository(PlatformDbContext db)
    : IOrganizationComplianceProfileRepository
{
    public async Task<OrganizationComplianceProfile?> GetByOrganizationIdAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var record = await db.OrganizationComplianceProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId.Value, cancellationToken)
            .ConfigureAwait(false);

        return record is null ? null : Map(record);
    }

    public Task AddAsync(
        OrganizationComplianceProfile profile,
        CancellationToken cancellationToken = default)
    {
        db.OrganizationComplianceProfiles.Add(ToRecord(profile));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(
        OrganizationComplianceProfile profile,
        CancellationToken cancellationToken = default)
    {
        var record = await db.OrganizationComplianceProfiles
            .FirstOrDefaultAsync(x => x.OrganizationId == profile.OrganizationId.Value, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            db.OrganizationComplianceProfiles.Add(ToRecord(profile));
            return;
        }

        record.RegisteredTaxpayerName = profile.RegisteredTaxpayerName;
        record.TinNormalized = profile.TinNormalized;
        record.SetupStatus = profile.SetupStatus;
        record.UpdatedAtUtc = profile.UpdatedAtUtc;
        record.UpdatedByActorReference = profile.UpdatedByActorReference;
    }

    private static OrganizationComplianceProfile Map(OrganizationComplianceProfileRecord record) =>
        OrganizationComplianceProfile.Rehydrate(
            PlatformOrganizationId.From(record.OrganizationId),
            record.RegisteredTaxpayerName,
            record.TinNormalized,
            record.SetupStatus,
            record.CreatedAtUtc,
            record.UpdatedAtUtc,
            record.UpdatedByActorReference);

    private static OrganizationComplianceProfileRecord ToRecord(OrganizationComplianceProfile profile) =>
        new()
        {
            OrganizationId = profile.OrganizationId.Value,
            RegisteredTaxpayerName = profile.RegisteredTaxpayerName,
            TinNormalized = profile.TinNormalized,
            SetupStatus = profile.SetupStatus,
            CreatedAtUtc = profile.CreatedAtUtc,
            UpdatedAtUtc = profile.UpdatedAtUtc,
            UpdatedByActorReference = profile.UpdatedByActorReference
        };
}

internal sealed class BranchComplianceProfileRepository(PlatformDbContext db)
    : IBranchComplianceProfileRepository
{
    public async Task<BranchComplianceProfile?> GetByBranchIdAsync(
        OrganizationBranchId branchId,
        CancellationToken cancellationToken = default)
    {
        var record = await db.BranchComplianceProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrganizationBranchId == branchId.Value, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : Map(record);
    }

    public async Task<IReadOnlyList<BranchComplianceProfile>> ListByOrganizationAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var records = await db.BranchComplianceProfiles
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId.Value)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(Map).ToList();
    }

    public Task AddAsync(
        BranchComplianceProfile profile,
        CancellationToken cancellationToken = default)
    {
        db.BranchComplianceProfiles.Add(ToRecord(profile));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(
        BranchComplianceProfile profile,
        CancellationToken cancellationToken = default)
    {
        var record = await db.BranchComplianceProfiles
            .FirstOrDefaultAsync(x => x.Id == profile.Id, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            db.BranchComplianceProfiles.Add(ToRecord(profile));
            return;
        }

        record.BirBranchCode = profile.BirBranchCode;
        record.SetupStatus = profile.SetupStatus;
        record.Notes = profile.Notes;
        record.UpdatedAtUtc = profile.UpdatedAtUtc;
        record.UpdatedByActorReference = profile.UpdatedByActorReference;
    }

    private static BranchComplianceProfile Map(BranchComplianceProfileRecord record) =>
        BranchComplianceProfile.Rehydrate(
            record.Id,
            PlatformOrganizationId.From(record.OrganizationId),
            OrganizationBranchId.From(record.OrganizationBranchId),
            record.BirBranchCode,
            record.SetupStatus,
            record.Notes,
            record.CreatedAtUtc,
            record.UpdatedAtUtc,
            record.UpdatedByActorReference);

    private static BranchComplianceProfileRecord ToRecord(BranchComplianceProfile profile) =>
        new()
        {
            Id = profile.Id,
            OrganizationId = profile.OrganizationId.Value,
            OrganizationBranchId = profile.OrganizationBranchId.Value,
            BirBranchCode = profile.BirBranchCode,
            SetupStatus = profile.SetupStatus,
            Notes = profile.Notes,
            CreatedAtUtc = profile.CreatedAtUtc,
            UpdatedAtUtc = profile.UpdatedAtUtc,
            UpdatedByActorReference = profile.UpdatedByActorReference
        };
}

internal sealed class ComplianceRegistrationRecordRepository(PlatformDbContext db)
    : IComplianceRegistrationRecordRepository
{
    public async Task<ComplianceRegistrationRecord?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var record = await db.ComplianceRegistrationRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : Map(record);
    }

    public async Task<IReadOnlyList<ComplianceRegistrationRecord>> ListByOrganizationAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var records = await db.ComplianceRegistrationRecords
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId.Value)
            .OrderByDescending(x => x.RecordedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(Map).ToList();
    }

    public Task AddAsync(
        ComplianceRegistrationRecord record,
        CancellationToken cancellationToken = default)
    {
        db.ComplianceRegistrationRecords.Add(ToRecord(record));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(
        ComplianceRegistrationRecord domain,
        CancellationToken cancellationToken = default)
    {
        var record = await db.ComplianceRegistrationRecords
            .FirstOrDefaultAsync(x => x.Id == domain.Id, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            db.ComplianceRegistrationRecords.Add(ToRecord(domain));
            return;
        }

        record.OrganizationBranchId = domain.OrganizationBranchId?.Value;
        record.RegistrationType = domain.RegistrationType;
        record.ReferenceNumber = domain.ReferenceNumber;
        record.Status = domain.Status;
        record.EvidenceReference = domain.EvidenceReference;
        record.DocumentType = domain.DocumentType;
        record.IssuedAt = domain.IssuedAt;
        record.EffectiveAt = domain.EffectiveAt;
        record.ExpiresAt = domain.ExpiresAt;
        record.RecordedAtUtc = domain.RecordedAtUtc;
        record.RecordedBy = domain.RecordedBy;
        record.ReviewedAtUtc = domain.ReviewedAtUtc;
        record.ReviewedBy = domain.ReviewedBy;
        record.ReviewNotes = domain.ReviewNotes;
    }

    private static ComplianceRegistrationRecord Map(ComplianceRegistrationRecordEntity record) =>
        ComplianceRegistrationRecord.Rehydrate(
            record.Id,
            PlatformOrganizationId.From(record.OrganizationId),
            record.OrganizationBranchId is null
                ? null
                : OrganizationBranchId.From(record.OrganizationBranchId.Value),
            record.RegistrationType,
            record.ReferenceNumber,
            record.Status,
            record.EvidenceReference,
            record.DocumentType,
            record.IssuedAt,
            record.EffectiveAt,
            record.ExpiresAt,
            record.RecordedAtUtc,
            record.RecordedBy,
            record.ReviewedAtUtc,
            record.ReviewedBy,
            record.ReviewNotes);

    private static ComplianceRegistrationRecordEntity ToRecord(ComplianceRegistrationRecord domain) =>
        new()
        {
            Id = domain.Id,
            OrganizationId = domain.OrganizationId.Value,
            OrganizationBranchId = domain.OrganizationBranchId?.Value,
            RegistrationType = domain.RegistrationType,
            ReferenceNumber = domain.ReferenceNumber,
            Status = domain.Status,
            EvidenceReference = domain.EvidenceReference,
            DocumentType = domain.DocumentType,
            IssuedAt = domain.IssuedAt,
            EffectiveAt = domain.EffectiveAt,
            ExpiresAt = domain.ExpiresAt,
            RecordedAtUtc = domain.RecordedAtUtc,
            RecordedBy = domain.RecordedBy,
            ReviewedAtUtc = domain.ReviewedAtUtc,
            ReviewedBy = domain.ReviewedBy,
            ReviewNotes = domain.ReviewNotes
        };
}
