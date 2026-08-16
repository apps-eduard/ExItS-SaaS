using ExItS.Platform.Application.Audit;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Organizations;

public interface IOrganizationComplianceProfileRepository
{
    Task<OrganizationComplianceProfile?> GetByOrganizationIdAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        OrganizationComplianceProfile profile,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        OrganizationComplianceProfile profile,
        CancellationToken cancellationToken = default);
}

public interface IBranchComplianceProfileRepository
{
    Task<BranchComplianceProfile?> GetByBranchIdAsync(
        OrganizationBranchId branchId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BranchComplianceProfile>> ListByOrganizationAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        BranchComplianceProfile profile,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        BranchComplianceProfile profile,
        CancellationToken cancellationToken = default);
}

public interface IComplianceRegistrationRecordRepository
{
    Task<ComplianceRegistrationRecord?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ComplianceRegistrationRecord>> ListByOrganizationAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        ComplianceRegistrationRecord record,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        ComplianceRegistrationRecord record,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Combined Organization compliance view. TIN is always masked — never full TIN on DTOs.
/// </summary>
public sealed record OrganizationComplianceProfileDto(
    Guid OrganizationId,
    bool ProfileInitialized,
    DateTimeOffset? ProfileCreatedAtUtc,
    DateTimeOffset? ProfileUpdatedAtUtc,
    string? LegalName,
    string? RegisteredAddressLine1,
    string? RegisteredCity,
    string? RegisteredRegion,
    string? RegisteredPostalCode,
    string? RegisteredCountryCode,
    string? RegisteredTaxpayerName,
    string? MaskedTin,
    string SetupStatus,
    string ComplianceEligibilityStatus,
    bool TaxDocumentIssuanceEnabled,
    bool TaxConfigurationEnabled,
    bool TaxDocumentImplementationAvailable,
    string DocumentMode,
    string SnapshotGuidance);

public sealed record BranchComplianceProfileDto(
    Guid Id,
    Guid OrganizationId,
    Guid OrganizationBranchId,
    string? BirBranchCode,
    string SetupStatus,
    string? Notes,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    string? UpdatedByActorReference);

public sealed record ComplianceRegistrationRecordDto(
    Guid Id,
    Guid OrganizationId,
    Guid? OrganizationBranchId,
    string RegistrationType,
    string? ReferenceNumber,
    string Status,
    string? EvidenceReference,
    string? DocumentType,
    DateOnly? IssuedAt,
    DateOnly? EffectiveAt,
    DateOnly? ExpiresAt,
    DateTimeOffset RecordedAtUtc,
    string RecordedBy,
    DateTimeOffset? ReviewedAtUtc,
    string? ReviewedBy,
    string? ReviewNotes);

public sealed record ComplianceActivationReadinessDto(
    Guid OrganizationId,
    string OverallStatus,
    bool IsReadyForTaxDocumentActivation,
    IReadOnlyList<string> BlockingReasons,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> CompletedRequirements,
    IReadOnlyList<string> PendingRequirements,
    IReadOnlyList<ComplianceReadinessChecklistItemDto> Checklist);

public sealed record ComplianceReadinessChecklistItemDto(string Code, string Label, bool Done);

public sealed class EnsureOrganizationComplianceProfile(
    IOrganizationComplianceProfileRepository profiles,
    IPlatformUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<OrganizationComplianceProfile> ExecuteAsync(
        PlatformOrganizationId organizationId,
        string? actorReference = null,
        CancellationToken cancellationToken = default)
    {
        var existing = await profiles
            .GetByOrganizationIdAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            return existing;
        }

        var created = OrganizationComplianceProfile.Create(organizationId, clock.UtcNow, actorReference);
        await profiles.AddAsync(created, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return created;
    }
}

public sealed class GetOrganizationComplianceProfile(
    IOrganizationComplianceProfileRepository profiles,
    IPlatformOrganizationRepository organizations,
    IOrganizationSalesDocumentCapabilityRepository capabilities)
{
    public async Task<ApplicationResult<OrganizationComplianceProfileDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var organization = await organizations
            .GetByIdAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);
        if (organization is null)
        {
            return ApplicationResult<OrganizationComplianceProfileDto>.Failure(
                ApplicationErrorCodes.OrganizationNotFound,
                "Organization was not found.");
        }

        var profile = await profiles
            .GetByOrganizationIdAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);
        var capability = await capabilities
            .GetByOrganizationIdAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);
        var orgProfile = organization.Profile;

        return ApplicationResult<OrganizationComplianceProfileDto>.Success(
            MapDto(organizationId, organization, profile, capability));
    }

    internal static OrganizationComplianceProfileDto MapDto(
        PlatformOrganizationId organizationId,
        PlatformOrganization organization,
        OrganizationComplianceProfile? profile,
        OrganizationSalesDocumentCapability? capability)
    {
        var orgProfile = organization.Profile;
        return new(
            organizationId.Value,
            ProfileInitialized: profile is not null,
            profile?.CreatedAtUtc,
            profile?.UpdatedAtUtc,
            orgProfile?.LegalName,
            orgProfile?.AddressLine1,
            orgProfile?.City,
            orgProfile?.Region,
            orgProfile?.PostalCode,
            orgProfile?.CountryCode,
            profile?.RegisteredTaxpayerName,
            profile?.MaskedTin,
            profile?.SetupStatus ?? ComplianceSetupStatuses.NotConfigured,
            capability?.ComplianceEligibilityStatus
                ?? OrganizationComplianceEligibilityStatuses.NotRequested,
            capability?.TaxDocumentIssuanceEnabled == true,
            capability?.TaxConfigurationEnabled == true,
            TaxDocumentIssuanceRuntime.ImplementationAvailable,
            DocumentMode: "TransactionSummary",
            SnapshotGuidance:
                "Future TaxDocument issuance must snapshot seller compliance facts at issuance time; organization profile changes must not rewrite historical documents.");
    }
}

public sealed class UpdateOrganizationRegisteredTaxpayerInfo(
    IOrganizationComplianceProfileRepository profiles,
    IPlatformOrganizationRepository organizations,
    IOrganizationSalesDocumentCapabilityRepository capabilities,
    EnsureOrganizationComplianceProfile ensure,
    IPlatformUnitOfWork unitOfWork,
    IClock clock,
    IAuditWriter audit)
{
    public async Task<ApplicationResult<OrganizationComplianceProfileDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        string? registeredTaxpayerName,
        string? tin,
        string actorReference,
        CancellationToken cancellationToken = default)
    {
        var organization = await organizations
            .GetByIdAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);
        if (organization is null)
        {
            return ApplicationResult<OrganizationComplianceProfileDto>.Failure(
                ApplicationErrorCodes.OrganizationNotFound,
                "Organization was not found.");
        }

        try
        {
            var profile = await ensure
                .ExecuteAsync(organizationId, actorReference, cancellationToken)
                .ConfigureAwait(false);
            // Empty TIN in the request means "keep existing" (UI never re-sends full TIN after mask).
            var tinToApply = string.IsNullOrWhiteSpace(tin) ? profile.TinNormalized : tin;
            profile.UpdateRegisteredTaxpayerInfo(
                registeredTaxpayerName,
                tinToApply,
                actorReference,
                clock.UtcNow);
            await profiles.UpdateAsync(profile, cancellationToken).ConfigureAwait(false);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await audit.WriteAsync(
                actorReference,
                AuditActorType.PlatformUser,
                PlatformAuditActions.OrganizationComplianceProfileUpdated,
                nameof(OrganizationComplianceProfile),
                organizationId.Value.ToString("D"),
                AuditOutcome.Succeeded,
                organizationId,
                summary:
                    $"Updated registered taxpayer info (masked TIN {profile.MaskedTin ?? "(none)"}).",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            var capability = await capabilities
                .GetByOrganizationIdAsync(organizationId, cancellationToken)
                .ConfigureAwait(false);
            return ApplicationResult<OrganizationComplianceProfileDto>.Success(
                GetOrganizationComplianceProfile.MapDto(organizationId, organization, profile, capability));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<OrganizationComplianceProfileDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class UpsertBranchComplianceProfile(
    IBranchComplianceProfileRepository branchProfiles,
    IOrganizationBranchRepository branches,
    IPlatformUnitOfWork unitOfWork,
    IClock clock,
    IAuditWriter audit)
{
    public async Task<ApplicationResult<BranchComplianceProfileDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        OrganizationBranchId branchId,
        string? birBranchCode,
        string? setupStatus,
        string? notes,
        string actorReference,
        CancellationToken cancellationToken = default)
    {
        var branch = await branches.GetByIdAsync(branchId, cancellationToken).ConfigureAwait(false);
        if (branch is null || branch.OrganizationId != organizationId)
        {
            return ApplicationResult<BranchComplianceProfileDto>.Failure(
                ApplicationErrorCodes.BranchComplianceOrganizationMismatch,
                "Branch was not found for this organization.");
        }

        try
        {
            var existing = await branchProfiles
                .GetByBranchIdAsync(branchId, cancellationToken)
                .ConfigureAwait(false);
            BranchComplianceProfile profile;
            if (existing is null)
            {
                profile = BranchComplianceProfile.Create(organizationId, branchId, clock.UtcNow, actorReference);
                profile.Update(birBranchCode, setupStatus, notes, actorReference, clock.UtcNow);
                await branchProfiles.AddAsync(profile, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                profile = existing;
                profile.Update(birBranchCode, setupStatus, notes, actorReference, clock.UtcNow);
                await branchProfiles.UpdateAsync(profile, cancellationToken).ConfigureAwait(false);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await audit.WriteAsync(
                actorReference,
                AuditActorType.PlatformUser,
                PlatformAuditActions.OrganizationBranchComplianceUpdated,
                nameof(BranchComplianceProfile),
                profile.Id.ToString("D"),
                AuditOutcome.Succeeded,
                organizationId,
                summary: $"Updated branch compliance profile for branch {branchId.Value:D}.",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return ApplicationResult<BranchComplianceProfileDto>.Success(Map(profile));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<BranchComplianceProfileDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    internal static BranchComplianceProfileDto Map(BranchComplianceProfile profile) =>
        new(
            profile.Id,
            profile.OrganizationId.Value,
            profile.OrganizationBranchId.Value,
            profile.BirBranchCode,
            profile.SetupStatus,
            profile.Notes,
            profile.CreatedAtUtc,
            profile.UpdatedAtUtc,
            profile.UpdatedByActorReference);
}

public sealed class GetBranchComplianceProfile(IBranchComplianceProfileRepository branchProfiles)
{
    public async Task<ApplicationResult<BranchComplianceProfileDto?>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        OrganizationBranchId branchId,
        CancellationToken cancellationToken = default)
    {
        var profile = await branchProfiles
            .GetByBranchIdAsync(branchId, cancellationToken)
            .ConfigureAwait(false);
        if (profile is null)
        {
            return ApplicationResult<BranchComplianceProfileDto?>.Success(null);
        }

        if (profile.OrganizationId != organizationId)
        {
            return ApplicationResult<BranchComplianceProfileDto?>.Failure(
                ApplicationErrorCodes.BranchComplianceOrganizationMismatch,
                "Branch compliance profile does not belong to this organization.");
        }

        return ApplicationResult<BranchComplianceProfileDto?>.Success(
            UpsertBranchComplianceProfile.Map(profile));
    }
}

public sealed class ListBranchComplianceProfiles(IBranchComplianceProfileRepository branchProfiles)
{
    public async Task<ApplicationResult<IReadOnlyList<BranchComplianceProfileDto>>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var items = await branchProfiles
            .ListByOrganizationAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);
        return ApplicationResult<IReadOnlyList<BranchComplianceProfileDto>>.Success(
            items.Select(UpsertBranchComplianceProfile.Map).ToList());
    }
}

public sealed class ListComplianceRegistrationRecords(IComplianceRegistrationRecordRepository records)
{
    public async Task<ApplicationResult<IReadOnlyList<ComplianceRegistrationRecordDto>>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var items = await records
            .ListByOrganizationAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);
        return ApplicationResult<IReadOnlyList<ComplianceRegistrationRecordDto>>.Success(
            items.Select(Map).ToList());
    }

    internal static ComplianceRegistrationRecordDto Map(ComplianceRegistrationRecord record) =>
        new(
            record.Id,
            record.OrganizationId.Value,
            record.OrganizationBranchId?.Value,
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
}

public sealed class AddComplianceRegistrationRecord(
    IComplianceRegistrationRecordRepository records,
    IOrganizationBranchRepository branches,
    IPlatformUnitOfWork unitOfWork,
    IClock clock,
    IAuditWriter audit)
{
    public async Task<ApplicationResult<ComplianceRegistrationRecordDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        string registrationType,
        string actorReference,
        Guid? organizationBranchId = null,
        string? referenceNumber = null,
        string status = ComplianceRegistrationStatuses.Provided,
        string? evidenceReference = null,
        string? documentType = null,
        DateOnly? issuedAt = null,
        DateOnly? effectiveAt = null,
        DateOnly? expiresAt = null,
        CancellationToken cancellationToken = default)
    {
        OrganizationBranchId? branchId = null;
        if (organizationBranchId is not null)
        {
            var branch = await branches
                .GetByIdAsync(OrganizationBranchId.From(organizationBranchId.Value), cancellationToken)
                .ConfigureAwait(false);
            if (branch is null || branch.OrganizationId != organizationId)
            {
                return ApplicationResult<ComplianceRegistrationRecordDto>.Failure(
                    ApplicationErrorCodes.BranchComplianceOrganizationMismatch,
                    "Branch was not found for this organization.");
            }

            branchId = branch.Id;
        }

        try
        {
            var record = ComplianceRegistrationRecord.Create(
                organizationId,
                registrationType,
                actorReference,
                clock.UtcNow,
                branchId,
                referenceNumber,
                status,
                evidenceReference,
                documentType,
                issuedAt,
                effectiveAt,
                expiresAt);
            await records.AddAsync(record, cancellationToken).ConfigureAwait(false);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await audit.WriteAsync(
                actorReference,
                AuditActorType.PlatformUser,
                PlatformAuditActions.OrganizationComplianceRegistrationCreated,
                nameof(ComplianceRegistrationRecord),
                record.Id.ToString("D"),
                AuditOutcome.Succeeded,
                organizationId,
                summary: $"Created compliance registration ({record.RegistrationType}, status {record.Status}).",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return ApplicationResult<ComplianceRegistrationRecordDto>.Success(
                ListComplianceRegistrationRecords.Map(record));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<ComplianceRegistrationRecordDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class UpdateComplianceRegistrationRecord(
    IComplianceRegistrationRecordRepository records,
    IOrganizationBranchRepository branches,
    IPlatformUnitOfWork unitOfWork,
    IClock clock,
    IAuditWriter audit)
{
    public async Task<ApplicationResult<ComplianceRegistrationRecordDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        Guid recordId,
        string actorReference,
        string? registrationType = null,
        Guid? organizationBranchId = null,
        bool clearBranch = false,
        string? referenceNumber = null,
        string? status = null,
        string? evidenceReference = null,
        string? documentType = null,
        DateOnly? issuedAt = null,
        DateOnly? effectiveAt = null,
        DateOnly? expiresAt = null,
        CancellationToken cancellationToken = default)
    {
        var record = await records.GetByIdAsync(recordId, cancellationToken).ConfigureAwait(false);
        if (record is null || record.OrganizationId != organizationId)
        {
            return ApplicationResult<ComplianceRegistrationRecordDto>.Failure(
                ApplicationErrorCodes.ComplianceRegistrationNotFound,
                "Compliance registration record was not found.");
        }

        OrganizationBranchId? branchId = record.OrganizationBranchId;
        if (clearBranch)
        {
            branchId = null;
        }
        else if (organizationBranchId is not null)
        {
            var branch = await branches
                .GetByIdAsync(OrganizationBranchId.From(organizationBranchId.Value), cancellationToken)
                .ConfigureAwait(false);
            if (branch is null || branch.OrganizationId != organizationId)
            {
                return ApplicationResult<ComplianceRegistrationRecordDto>.Failure(
                    ApplicationErrorCodes.BranchComplianceOrganizationMismatch,
                    "Branch was not found for this organization.");
            }

            branchId = branch.Id;
        }

        try
        {
            record.UpdateByOwner(
                registrationType,
                branchId,
                referenceNumber,
                status ?? record.Status,
                evidenceReference,
                documentType,
                issuedAt,
                effectiveAt,
                expiresAt,
                actorReference,
                clock.UtcNow);
            await records.UpdateAsync(record, cancellationToken).ConfigureAwait(false);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await audit.WriteAsync(
                actorReference,
                AuditActorType.PlatformUser,
                PlatformAuditActions.OrganizationComplianceRegistrationUpdated,
                nameof(ComplianceRegistrationRecord),
                record.Id.ToString("D"),
                AuditOutcome.Succeeded,
                organizationId,
                summary: $"Updated compliance registration ({record.RegistrationType}, status {record.Status}).",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return ApplicationResult<ComplianceRegistrationRecordDto>.Success(
                ListComplianceRegistrationRecords.Map(record));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<ComplianceRegistrationRecordDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class ReviewComplianceRegistrationRecord(
    IComplianceRegistrationRecordRepository records,
    IPlatformUnitOfWork unitOfWork,
    IClock clock,
    IAuditWriter audit)
{
    public async Task<ApplicationResult<ComplianceRegistrationRecordDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        Guid recordId,
        bool accept,
        string reviewer,
        string? reviewNotes,
        CancellationToken cancellationToken = default)
    {
        var record = await records.GetByIdAsync(recordId, cancellationToken).ConfigureAwait(false);
        if (record is null || record.OrganizationId != organizationId)
        {
            return ApplicationResult<ComplianceRegistrationRecordDto>.Failure(
                ApplicationErrorCodes.ComplianceRegistrationNotFound,
                "Compliance registration record was not found.");
        }

        try
        {
            if (accept)
            {
                record.AcceptForReadiness(reviewer, reviewNotes, clock.UtcNow);
            }
            else
            {
                record.RejectForReadiness(reviewer, reviewNotes, clock.UtcNow);
            }

            await records.UpdateAsync(record, cancellationToken).ConfigureAwait(false);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await audit.WriteAsync(
                reviewer,
                AuditActorType.PlatformUser,
                PlatformAuditActions.OrganizationComplianceRegistrationReviewed,
                nameof(ComplianceRegistrationRecord),
                record.Id.ToString("D"),
                AuditOutcome.Succeeded,
                organizationId,
                summary:
                    $"Platform {(accept ? "accepted" : "rejected")} compliance registration for readiness ({record.RegistrationType}).",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return ApplicationResult<ComplianceRegistrationRecordDto>.Success(
                ListComplianceRegistrationRecords.Map(record));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<ComplianceRegistrationRecordDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class GetComplianceActivationReadiness(
    IOrganizationComplianceProfileRepository profiles,
    IOrganizationSalesDocumentCapabilityRepository capabilities,
    IOrganizationMembershipRepository memberships,
    IOrganizationSalesDocumentAcknowledgmentRepository acknowledgments,
    ISalesDocumentEducationVersionProvider versions,
    IOrganizationBranchRepository branches,
    IBranchComplianceProfileRepository branchProfiles,
    IComplianceRegistrationRecordRepository registrations,
    IComplianceActivationReadinessEvaluator evaluator)
{
    public async Task<ApplicationResult<ComplianceActivationReadinessDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var profile = await profiles
            .GetByOrganizationIdAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);
        var capability = await capabilities
            .GetByOrganizationIdAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);
        var owner = await memberships
            .FindActiveOwnerByOrganizationAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);
        var educationAck = false;
        if (owner is not null)
        {
            var ack = await acknowledgments
                .FindAsync(organizationId, owner.UserId, versions.CurrentVersion, cancellationToken)
                .ConfigureAwait(false);
            educationAck = ack is not null;
        }

        var orgBranches = await branches
            .ListByOrganizationAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);
        var branchProfileList = await branchProfiles
            .ListByOrganizationAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);
        var registrationList = await registrations
            .ListByOrganizationAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);

        var result = evaluator.Evaluate(
            profile,
            capability,
            educationAck,
            orgBranches,
            branchProfileList,
            registrationList);

        return ApplicationResult<ComplianceActivationReadinessDto>.Success(new(
            organizationId.Value,
            result.OverallStatus,
            result.IsReadyForTaxDocumentActivation,
            result.BlockingReasons,
            result.Warnings,
            result.CompletedRequirements,
            result.PendingRequirements,
            result.Checklist
                .Select(c => new ComplianceReadinessChecklistItemDto(c.Code, c.Label, c.Done))
                .ToList()));
    }
}

public sealed class SubmitComplianceReadinessForReview(
    GetComplianceActivationReadiness getReadiness,
    IOrganizationComplianceProfileRepository profiles,
    EnsureOrganizationComplianceProfile ensure,
    IPlatformUnitOfWork unitOfWork,
    IClock clock,
    IAuditWriter audit)
{
    public async Task<ApplicationResult<ComplianceActivationReadinessDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        string actorReference,
        CancellationToken cancellationToken = default)
    {
        var readiness = await getReadiness
            .ExecuteAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);
        if (!readiness.IsSuccess || readiness.Value is null)
        {
            return readiness;
        }

        var dto = readiness.Value;
        // Ready for Platform review when non-runtime checklist items that Owners control are done,
        // except POS acceptance (Platform) and runtime (engineering).
        var blockingExceptPlatformAndRuntime = dto.BlockingReasons
            .Where(r =>
                r != ComplianceActivationReadinessEvaluator.RuntimeUnavailableReason
                && !r.Contains("AcceptedForReadiness", StringComparison.Ordinal)
                && !r.Contains("awaiting Platform", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var hasPosProvided = !dto.BlockingReasons.Any(r =>
            r.Contains("At least one POS Permit to Use registration", StringComparison.Ordinal));

        if (blockingExceptPlatformAndRuntime.Count > 0 || !hasPosProvided)
        {
            // Still allow submit when only POS acceptance + runtime remain, if POS is Provided/UnderReview.
            var onlyRuntimeAndAccept = dto.BlockingReasons.All(r =>
                r == ComplianceActivationReadinessEvaluator.RuntimeUnavailableReason
                || r.Contains("AcceptedForReadiness", StringComparison.Ordinal));

            if (!onlyRuntimeAndAccept)
            {
                return ApplicationResult<ComplianceActivationReadinessDto>.Failure(
                    ApplicationErrorCodes.ComplianceReadinessNotReady,
                    "Compliance readiness checklist is not complete enough to submit for review.");
            }
        }

        var profile = await ensure
            .ExecuteAsync(organizationId, actorReference, cancellationToken)
            .ConfigureAwait(false);
        profile.SetSetupStatus(ComplianceSetupStatuses.UnderReview, actorReference, clock.UtcNow);
        await profiles.UpdateAsync(profile, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await audit.WriteAsync(
            actorReference,
            AuditActorType.PlatformUser,
            PlatformAuditActions.OrganizationComplianceReadinessSubmitted,
            nameof(OrganizationComplianceProfile),
            organizationId.Value.ToString("D"),
            AuditOutcome.Succeeded,
            organizationId,
            summary: "Submitted compliance readiness for Platform review.",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return await getReadiness.ExecuteAsync(organizationId, cancellationToken).ConfigureAwait(false);
    }
}
