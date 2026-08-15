using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.PrivacyCompliance;

namespace ExItS.Platform.Application.PrivacyCompliance;

internal static class PrivacyComplianceMaps
{
    public static ComplianceRequirementDto MapRequirement(
        ComplianceRequirement requirement,
        int evidenceCount) =>
        new(
            requirement.Id,
            requirement.Code,
            requirement.Title,
            requirement.Category,
            requirement.Description,
            requirement.RequirementLevel,
            requirement.Status,
            requirement.OwnerRole,
            requirement.Version,
            requirement.EffectiveDate,
            requirement.LastReviewedDate,
            requirement.NextReviewDate,
            requirement.Notes,
            requirement.SourceReference,
            requirement.RequiresDpoLegalVerification,
            requirement.CreatedAtUtc,
            requirement.UpdatedAtUtc,
            evidenceCount);

    public static ComplianceEvidenceDto MapEvidence(ComplianceEvidenceReference evidence) =>
        new(
            evidence.Id,
            evidence.RequirementId,
            evidence.Kind,
            evidence.Label,
            evidence.ReferencePath,
            evidence.Notes,
            evidence.CreatedAtUtc);

    public static ProcessingSystemDto MapSystem(ProcessingSystemRecord system) =>
        new(
            system.Id,
            system.Code,
            system.SystemName,
            system.Purpose,
            system.DataSubjects,
            system.PersonalDataCategories,
            system.SensitiveDataCategories,
            system.StorageLocation,
            system.RecipientsProcessors,
            system.RetentionSummary,
            system.SecurityControls,
            system.Owner,
            system.PiaStatus,
            system.CreatedAtUtc,
            system.UpdatedAtUtc);
}

public sealed class GetPrivacyComplianceOverview
{
    private readonly EnsurePrivacyComplianceCatalog _ensureCatalog;
    private readonly IComplianceRequirementRepository _requirements;
    private readonly IComplianceEvidenceRepository _evidence;
    private readonly IProcessingSystemRepository _systems;

    public GetPrivacyComplianceOverview(
        EnsurePrivacyComplianceCatalog ensureCatalog,
        IComplianceRequirementRepository requirements,
        IComplianceEvidenceRepository evidence,
        IProcessingSystemRepository systems)
    {
        _ensureCatalog = ensureCatalog;
        _requirements = requirements;
        _evidence = evidence;
        _systems = systems;
    }

    public async Task<PrivacyComplianceOverviewDto> ExecuteAsync(CancellationToken ct = default)
    {
        await _ensureCatalog.ExecuteAsync(ct).ConfigureAwait(false);

        var requirements = await _requirements.ListAsync(category: null, ct).ConfigureAwait(false);
        var systems = await _systems.ListAsync(ct).ConfigureAwait(false);

        var byStatus = requirements
            .GroupBy(r => r.Status.ToString())
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
        var byCategory = requirements
            .GroupBy(r => r.Category.ToString())
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);

        var evidenceCounts = new Dictionary<Guid, int>();
        var totalEvidence = 0;
        foreach (var requirement in requirements)
        {
            var items = await _evidence.ListByRequirementIdAsync(requirement.Id, ct).ConfigureAwait(false);
            evidenceCounts[requirement.Id] = items.Count;
            totalEvidence += items.Count;
        }

        var lastUpdated = requirements.Count == 0
            ? (DateTimeOffset?)null
            : requirements.Max(r => r.UpdatedAtUtc);

        var readyCount = requirements.Count(r => PrivacyReadinessDerivation.IsReadyStatus(r.Status));
        var actionNeededCount = requirements.Count(r => PrivacyReadinessDerivation.IsActionNeededStatus(r.Status));
        var externalLegalCount = requirements.Count(PrivacyReadinessDerivation.CountsAsExternalLegalReview);
        var withEvidence = requirements.Count(r => evidenceCounts.GetValueOrDefault(r.Id) > 0);

        var overall = PrivacyReadinessDerivation.DeriveOverall(requirements);

        var security = requirements.FirstOrDefault(r =>
            string.Equals(r.Code, "SECURITY_ACCESS_CONTROL", StringComparison.OrdinalIgnoreCase));
        var technicalSummary = security is null
            ? "Unavailable"
            : security.Status switch
            {
                ComplianceItemStatus.Approved => "Implemented",
                ComplianceItemStatus.ReadyForReview or ComplianceItemStatus.InProgress => "Partial",
                _ => "ActionNeeded"
            };

        static bool IsGovernanceGroup(PrivacyReadinessCategoryGroup group) =>
            group is PrivacyReadinessCategoryGroup.NoticesAndConsent
                or PrivacyReadinessCategoryGroup.Governance
                or PrivacyReadinessCategoryGroup.DataInventory
                or PrivacyReadinessCategoryGroup.RetentionAndDeletion
                or PrivacyReadinessCategoryGroup.DataSubjectRequests
                or PrivacyReadinessCategoryGroup.VendorsAndProcessors;

        var governanceReady = requirements.Count(r =>
            IsGovernanceGroup(PrivacyReadinessDerivation.ResolveCategoryGroup(r.Code, r.Category))
            && PrivacyReadinessDerivation.IsReadyStatus(r.Status));
        var governanceTotal = requirements.Count(r =>
            IsGovernanceGroup(PrivacyReadinessDerivation.ResolveCategoryGroup(r.Code, r.Category)));
        var governanceSummary = governanceTotal == 0
            ? "Unavailable"
            : $"{governanceReady} of {governanceTotal} ready";

        var legalSummary = externalLegalCount > 0 ? "Required" : "No outstanding legal-review flags";
        var npcSummary = requirements.Any(r =>
            r.Category == ComplianceItemCategory.RegulatoryReadiness
            && r.Status == ComplianceItemStatus.Approved)
            ? "InternalRecordApproved"
            : "NotVerified";

        var categorySummaries = requirements
            .GroupBy(r => PrivacyReadinessDerivation.ResolveCategoryGroup(r.Code, r.Category))
            .OrderBy(g => (int)g.Key)
            .Select(g =>
            {
                var items = g.ToArray();
                var ready = items.Count(r => PrivacyReadinessDerivation.IsReadyStatus(r.Status));
                var action = items.Count(r => PrivacyReadinessDerivation.IsActionNeededStatus(r.Status));
                var covered = items.Count(r => evidenceCounts.GetValueOrDefault(r.Id) > 0);
                var lastReview = items
                    .Select(r => r.LastReviewedDate)
                    .Where(d => d is not null)
                    .OrderByDescending(d => d)
                    .FirstOrDefault();
                var status = action > 0
                    ? nameof(PrivacyReadinessOverallStatus.ActionNeeded)
                    : items.Any(r => r.Status == ComplianceItemStatus.InProgress)
                        ? nameof(PrivacyReadinessOverallStatus.InProgress)
                        : ready == items.Length
                            ? nameof(PrivacyReadinessOverallStatus.ReadyForReview)
                            : nameof(PrivacyReadinessOverallStatus.InProgress);
                return new PrivacyReadinessCategorySummaryDto(
                    g.Key.ToString(),
                    PrivacyReadinessDerivation.ResolveDetailRoute(g.Key),
                    items.Length,
                    ready,
                    action,
                    covered,
                    lastReview,
                    status,
                    action > 0);
            })
            .ToArray();

        var privacyImpacts = requirements
            .Where(r =>
                PrivacyReadinessDerivation.ResolveCategoryGroup(r.Code, r.Category)
                == PrivacyReadinessCategoryGroup.PrivacyImpact)
            .Select(r => new PrivacyImpactFollowUpDto(
                r.Code,
                r.Title,
                r.Status.ToString(),
                r.RequiresDpoLegalVerification,
                evidenceCounts.GetValueOrDefault(r.Id),
                r.LastReviewedDate))
            .OrderBy(r => r.Code, StringComparer.Ordinal)
            .ToArray();

        return new PrivacyComplianceOverviewDto(
            requirements.Count,
            systems.Count,
            totalEvidence,
            byStatus,
            byCategory,
            lastUpdated,
            overall,
            readyCount,
            actionNeededCount,
            externalLegalCount,
            withEvidence,
            technicalSummary,
            governanceSummary,
            legalSummary,
            npcSummary,
            categorySummaries,
            privacyImpacts);
    }
}

public sealed class ListComplianceRequirements
{
    private readonly EnsurePrivacyComplianceCatalog _ensureCatalog;
    private readonly IComplianceRequirementRepository _requirements;
    private readonly IComplianceEvidenceRepository _evidence;

    public ListComplianceRequirements(
        EnsurePrivacyComplianceCatalog ensureCatalog,
        IComplianceRequirementRepository requirements,
        IComplianceEvidenceRepository evidence)
    {
        _ensureCatalog = ensureCatalog;
        _requirements = requirements;
        _evidence = evidence;
    }

    public async Task<IReadOnlyList<ComplianceRequirementDto>> ExecuteAsync(
        ComplianceItemCategory? category,
        CancellationToken ct = default)
    {
        await _ensureCatalog.ExecuteAsync(ct).ConfigureAwait(false);

        var requirements = await _requirements.ListAsync(category, ct).ConfigureAwait(false);
        var mapped = new List<ComplianceRequirementDto>(requirements.Count);
        foreach (var requirement in requirements)
        {
            var evidence = await _evidence.ListByRequirementIdAsync(requirement.Id, ct).ConfigureAwait(false);
            mapped.Add(PrivacyComplianceMaps.MapRequirement(requirement, evidence.Count));
        }

        return mapped;
    }
}

public sealed class GetComplianceRequirement
{
    private readonly IComplianceRequirementRepository _requirements;
    private readonly IComplianceEvidenceRepository _evidence;

    public GetComplianceRequirement(
        IComplianceRequirementRepository requirements,
        IComplianceEvidenceRepository evidence)
    {
        _requirements = requirements;
        _evidence = evidence;
    }

    public async Task<ComplianceRequirementDto?> ExecuteAsync(Guid id, CancellationToken ct = default)
    {
        var requirement = await _requirements.GetByIdAsync(id, ct).ConfigureAwait(false);
        if (requirement is null)
        {
            return null;
        }

        var evidence = await _evidence.ListByRequirementIdAsync(requirement.Id, ct).ConfigureAwait(false);
        return PrivacyComplianceMaps.MapRequirement(requirement, evidence.Count);
    }
}

public sealed class UpdateComplianceRequirementStatus
{
    private readonly IComplianceRequirementRepository _requirements;
    private readonly IComplianceEvidenceRepository _evidence;
    private readonly IPlatformUnitOfWork _uow;
    private readonly IClock _clock;

    public UpdateComplianceRequirementStatus(
        IComplianceRequirementRepository requirements,
        IComplianceEvidenceRepository evidence,
        IPlatformUnitOfWork uow,
        IClock clock)
    {
        _requirements = requirements;
        _evidence = evidence;
        _uow = uow;
        _clock = clock;
    }

    public async Task<ApplicationResult<ComplianceRequirementDto>> ExecuteAsync(
        Guid id,
        ComplianceItemStatus status,
        CancellationToken ct = default)
    {
        var requirement = await _requirements.GetByIdAsync(id, ct).ConfigureAwait(false);
        if (requirement is null)
        {
            return ApplicationResult<ComplianceRequirementDto>.Failure(
                ApplicationErrorCodes.ComplianceRequirementNotFound,
                "Compliance requirement was not found.");
        }

        try
        {
            requirement.TransitionStatus(status, _clock.UtcNow);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<ComplianceRequirementDto>.Failure(ex.ErrorCode, ex.Message);
        }

        await _requirements.UpdateAsync(requirement, ct).ConfigureAwait(false);
        await _uow.SaveChangesAsync(ct).ConfigureAwait(false);

        var evidence = await _evidence.ListByRequirementIdAsync(requirement.Id, ct).ConfigureAwait(false);
        return ApplicationResult<ComplianceRequirementDto>.Success(
            PrivacyComplianceMaps.MapRequirement(requirement, evidence.Count));
    }
}

public sealed class UpdateComplianceRequirementDetails
{
    private readonly IComplianceRequirementRepository _requirements;
    private readonly IComplianceEvidenceRepository _evidence;
    private readonly IPlatformUnitOfWork _uow;
    private readonly IClock _clock;

    public UpdateComplianceRequirementDetails(
        IComplianceRequirementRepository requirements,
        IComplianceEvidenceRepository evidence,
        IPlatformUnitOfWork uow,
        IClock clock)
    {
        _requirements = requirements;
        _evidence = evidence;
        _uow = uow;
        _clock = clock;
    }

    public async Task<ApplicationResult<ComplianceRequirementDto>> ExecuteAsync(
        Guid id,
        UpdateComplianceRequirementDetailsRequest request,
        CancellationToken ct = default)
    {
        var requirement = await _requirements.GetByIdAsync(id, ct).ConfigureAwait(false);
        if (requirement is null)
        {
            return ApplicationResult<ComplianceRequirementDto>.Failure(
                ApplicationErrorCodes.ComplianceRequirementNotFound,
                "Compliance requirement was not found.");
        }

        try
        {
            requirement.UpdateDetails(
                request.Notes,
                request.Version,
                request.EffectiveDate,
                request.LastReviewedDate,
                request.NextReviewDate,
                _clock.UtcNow);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<ComplianceRequirementDto>.Failure(ex.ErrorCode, ex.Message);
        }

        await _requirements.UpdateAsync(requirement, ct).ConfigureAwait(false);
        await _uow.SaveChangesAsync(ct).ConfigureAwait(false);

        var evidence = await _evidence.ListByRequirementIdAsync(requirement.Id, ct).ConfigureAwait(false);
        return ApplicationResult<ComplianceRequirementDto>.Success(
            PrivacyComplianceMaps.MapRequirement(requirement, evidence.Count));
    }
}

public sealed class ListComplianceEvidence
{
    private readonly IComplianceRequirementRepository _requirements;
    private readonly IComplianceEvidenceRepository _evidence;

    public ListComplianceEvidence(
        IComplianceRequirementRepository requirements,
        IComplianceEvidenceRepository evidence)
    {
        _requirements = requirements;
        _evidence = evidence;
    }

    public async Task<ApplicationResult<IReadOnlyList<ComplianceEvidenceDto>>> ExecuteAsync(
        Guid requirementId,
        CancellationToken ct = default)
    {
        var requirement = await _requirements.GetByIdAsync(requirementId, ct).ConfigureAwait(false);
        if (requirement is null)
        {
            return ApplicationResult<IReadOnlyList<ComplianceEvidenceDto>>.Failure(
                ApplicationErrorCodes.ComplianceRequirementNotFound,
                "Compliance requirement was not found.");
        }

        var items = await _evidence.ListByRequirementIdAsync(requirementId, ct).ConfigureAwait(false);
        return ApplicationResult<IReadOnlyList<ComplianceEvidenceDto>>.Success(
            items.Select(PrivacyComplianceMaps.MapEvidence).ToList());
    }
}

public sealed class AddComplianceEvidence
{
    private readonly IComplianceRequirementRepository _requirements;
    private readonly IComplianceEvidenceRepository _evidence;
    private readonly IPlatformUnitOfWork _uow;
    private readonly IClock _clock;

    public AddComplianceEvidence(
        IComplianceRequirementRepository requirements,
        IComplianceEvidenceRepository evidence,
        IPlatformUnitOfWork uow,
        IClock clock)
    {
        _requirements = requirements;
        _evidence = evidence;
        _uow = uow;
        _clock = clock;
    }

    public async Task<ApplicationResult<ComplianceEvidenceDto>> ExecuteAsync(
        AddComplianceEvidenceRequest request,
        CancellationToken ct = default)
    {
        var requirement = await _requirements.GetByIdAsync(request.RequirementId, ct).ConfigureAwait(false);
        if (requirement is null)
        {
            return ApplicationResult<ComplianceEvidenceDto>.Failure(
                ApplicationErrorCodes.ComplianceRequirementNotFound,
                "Compliance requirement was not found.");
        }

        try
        {
            var reference = ComplianceEvidenceReference.Create(
                requirement.Id,
                request.Kind,
                request.Label,
                request.ReferencePath,
                _clock.UtcNow,
                request.Notes);
            await _evidence.AddAsync(reference, ct).ConfigureAwait(false);
            await _uow.SaveChangesAsync(ct).ConfigureAwait(false);
            return ApplicationResult<ComplianceEvidenceDto>.Success(PrivacyComplianceMaps.MapEvidence(reference));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<ComplianceEvidenceDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class ListProcessingSystems
{
    private readonly IProcessingSystemRepository _systems;

    public ListProcessingSystems(IProcessingSystemRepository systems) => _systems = systems;

    public async Task<IReadOnlyList<ProcessingSystemDto>> ExecuteAsync(CancellationToken ct = default)
    {
        var systems = await _systems.ListAsync(ct).ConfigureAwait(false);
        return systems.Select(PrivacyComplianceMaps.MapSystem).ToList();
    }
}

public sealed class ExportComplianceRequirementPdf
{
    private readonly IComplianceRequirementRepository _requirements;
    private readonly IPrivacyCompliancePdfExporter _pdfExporter;
    private readonly IClock _clock;

    public ExportComplianceRequirementPdf(
        IComplianceRequirementRepository requirements,
        IPrivacyCompliancePdfExporter pdfExporter,
        IClock clock)
    {
        _requirements = requirements;
        _pdfExporter = pdfExporter;
        _clock = clock;
    }

    public async Task<ApplicationResult<ExportComplianceRequirementPdfResult>> ExecuteAsync(
        Guid id,
        string? companyName,
        CancellationToken ct = default)
    {
        var requirement = await _requirements.GetByIdAsync(id, ct).ConfigureAwait(false);
        if (requirement is null)
        {
            return ApplicationResult<ExportComplianceRequirementPdfResult>.Failure(
                ApplicationErrorCodes.ComplianceRequirementNotFound,
                "Compliance requirement was not found.");
        }

        var content = _pdfExporter.ExportRequirement(requirement, companyName, _clock.UtcNow);
        var fileName = $"{requirement.Code.ToLowerInvariant()}-privacy-compliance.pdf";
        return ApplicationResult<ExportComplianceRequirementPdfResult>.Success(
            new ExportComplianceRequirementPdfResult(content, fileName, "application/pdf"));
    }
}
