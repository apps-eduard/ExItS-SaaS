namespace ExItS.Platform.Domain.Organizations;

public sealed record ComplianceReadinessChecklistItem(string Code, string Label, bool Done);

public sealed record ComplianceActivationReadinessResult(
    string OverallStatus,
    bool IsReadyForTaxDocumentActivation,
    IReadOnlyList<string> BlockingReasons,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> CompletedRequirements,
    IReadOnlyList<string> PendingRequirements,
    IReadOnlyList<ComplianceReadinessChecklistItem> Checklist);

public interface IComplianceActivationReadinessEvaluator
{
    ComplianceActivationReadinessResult Evaluate(
        OrganizationComplianceProfile? profile,
        OrganizationSalesDocumentCapability? capability,
        bool currentOwnerEducationAcknowledged,
        IReadOnlyList<OrganizationBranch> branches,
        IReadOnlyList<BranchComplianceProfile> branchProfiles,
        IReadOnlyList<ComplianceRegistrationRecord> registrationRecords);
}

/// <summary>
/// Evaluates BIR registration readiness for ExItS activation.
/// Eligibility alone or TaxConfiguration alone never makes an organization ready.
/// While <see cref="TaxDocumentIssuanceRuntime.ImplementationAvailable"/> is false,
/// <see cref="ComplianceActivationReadinessResult.IsReadyForTaxDocumentActivation"/> stays false
/// and overall status becomes <see cref="ComplianceSetupStatuses.ActivationBlocked"/> when other items complete.
/// Machine/MIN association is intentionally omitted (FUTURE residual).
/// </summary>
public sealed class ComplianceActivationReadinessEvaluator : IComplianceActivationReadinessEvaluator
{
    public const string RuntimeUnavailableReason = "Tax-document issuance runtime is not implemented";

    public ComplianceActivationReadinessResult Evaluate(
        OrganizationComplianceProfile? profile,
        OrganizationSalesDocumentCapability? capability,
        bool currentOwnerEducationAcknowledged,
        IReadOnlyList<OrganizationBranch> branches,
        IReadOnlyList<BranchComplianceProfile> branchProfiles,
        IReadOnlyList<ComplianceRegistrationRecord> registrationRecords)
    {
        var blocking = new List<string>();
        var warnings = new List<string>();
        var completed = new List<string>();
        var pending = new List<string>();
        var checklist = new List<ComplianceReadinessChecklistItem>();

        var nameDone = !string.IsNullOrWhiteSpace(profile?.RegisteredTaxpayerName);
        AddRequirement(
            checklist, completed, pending, blocking,
            "registered_taxpayer_name",
            "Registered taxpayer name",
            nameDone,
            "Registered taxpayer name is required.");

        var tinDone = !string.IsNullOrWhiteSpace(profile?.TinNormalized)
                      && profile!.TinNormalized.Length == TinMask.RequiredLength;
        AddRequirement(
            checklist, completed, pending, blocking,
            "tin",
            "Taxpayer Identification Number (TIN)",
            tinDone,
            "A valid 9-digit TIN is required.");

        var activeBranches = branches
            .Where(b => b.Status == OrganizationBranchStatus.Active)
            .ToList();
        var profileByBranch = branchProfiles
            .ToDictionary(p => p.OrganizationBranchId.Value);

        var hasActiveBranch = activeBranches.Count > 0;
        var allActiveHaveCode = hasActiveBranch
            && activeBranches.All(b =>
                profileByBranch.TryGetValue(b.Id.Value, out var bp)
                && !string.IsNullOrWhiteSpace(bp.BirBranchCode));

        AddRequirement(
            checklist, completed, pending, blocking,
            "branch_codes",
            "BIR branch code on every active branch",
            allActiveHaveCode,
            hasActiveBranch
                ? "Every active branch must have a BIR branch code."
                : "At least one active branch with a BIR branch code is required.");

        if (hasActiveBranch && !allActiveHaveCode)
        {
            var missing = activeBranches.Count(b =>
                !profileByBranch.TryGetValue(b.Id.Value, out var bp)
                || string.IsNullOrWhiteSpace(bp.BirBranchCode));
            warnings.Add($"{missing} active branch(es) still need a BIR branch code.");
        }

        var posPermits = registrationRecords
            .Where(r => r.RegistrationType == ComplianceRegistrationTypes.PosPermitToUse)
            .ToList();
        var posAccepted = posPermits.Any(r =>
            r.Status == ComplianceRegistrationStatuses.AcceptedForReadiness);
        var posPendingReview = !posAccepted && posPermits.Any(r =>
            r.Status is ComplianceRegistrationStatuses.Provided
                or ComplianceRegistrationStatuses.UnderReview);

        checklist.Add(new("pos_permit_to_use", "POS Permit to Use accepted for readiness", posAccepted));
        if (posAccepted)
        {
            completed.Add("POS Permit to Use accepted for readiness");
        }
        else if (posPendingReview)
        {
            pending.Add("POS Permit to Use awaiting Platform readiness acceptance");
            blocking.Add("POS Permit to Use must be AcceptedForReadiness.");
        }
        else
        {
            pending.Add("POS Permit to Use registration");
            blocking.Add("At least one POS Permit to Use registration AcceptedForReadiness is required.");
        }

        var eligibilityApproved =
            capability?.ComplianceEligibilityStatus == OrganizationComplianceEligibilityStatuses.Approved;
        AddRequirement(
            checklist, completed, pending, blocking,
            "eligibility_approved",
            "Compliance eligibility Approved",
            eligibilityApproved,
            "Compliance eligibility must be Approved.");

        AddRequirement(
            checklist, completed, pending, blocking,
            "owner_education",
            "Current Owner education acknowledged",
            currentOwnerEducationAcknowledged,
            "Current Organization Owner must acknowledge sales-document education.");

        var runtimeAvailable = TaxDocumentIssuanceRuntime.ImplementationAvailable;
        checklist.Add(new(
            "tax_document_runtime",
            "Tax-document issuance runtime available",
            runtimeAvailable));
        if (runtimeAvailable)
        {
            completed.Add("Tax-document issuance runtime available");
        }
        else
        {
            pending.Add("Tax-document issuance runtime");
            blocking.Add(RuntimeUnavailableReason);
        }

        warnings.Add("Machine Identification Number (MIN) association is deferred (FUTURE).");
        warnings.Add("Eligibility alone or Tax Configuration alone does not authorize tax-document activation.");

        var otherComplete = nameDone && tinDone && allActiveHaveCode && posAccepted
                            && eligibilityApproved && currentOwnerEducationAcknowledged;
        var overall = DeriveOverallStatus(
            profile?.SetupStatus,
            otherComplete,
            runtimeAvailable,
            blocking.Count,
            pending.Count);

        return new(
            overall,
            IsReadyForTaxDocumentActivation: otherComplete && runtimeAvailable,
            blocking,
            warnings,
            completed,
            pending,
            checklist);
    }

    private static void AddRequirement(
        List<ComplianceReadinessChecklistItem> checklist,
        List<string> completed,
        List<string> pending,
        List<string> blocking,
        string code,
        string label,
        bool done,
        string blockingReason)
    {
        checklist.Add(new(code, label, done));
        if (done)
        {
            completed.Add(label);
        }
        else
        {
            pending.Add(label);
            blocking.Add(blockingReason);
        }
    }

    private static string DeriveOverallStatus(
        string? storedStatus,
        bool otherComplete,
        bool runtimeAvailable,
        int blockingCount,
        int pendingCount)
    {
        if (otherComplete && !runtimeAvailable)
        {
            return ComplianceSetupStatuses.ActivationBlocked;
        }

        if (otherComplete && runtimeAvailable)
        {
            return ComplianceSetupStatuses.Activated;
        }

        if (storedStatus is ComplianceSetupStatuses.UnderReview
            or ComplianceSetupStatuses.ApprovedForExItsActivation
            or ComplianceSetupStatuses.NeedsAttention)
        {
            return storedStatus;
        }

        if (blockingCount == 0 && pendingCount == 0)
        {
            return ComplianceSetupStatuses.ReadyForReview;
        }

        // Nearly complete but still missing Platform acceptance, etc.
        if (pendingCount > 0 && blockingCount > 0)
        {
            return storedStatus is ComplianceSetupStatuses.NotConfigured
                ? ComplianceSetupStatuses.NotConfigured
                : ComplianceSetupStatuses.SetupInProgress;
        }

        return string.IsNullOrWhiteSpace(storedStatus)
            ? ComplianceSetupStatuses.NotConfigured
            : storedStatus;
    }
}
