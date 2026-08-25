using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Domain.Onboarding;

/// <summary>
/// POS-owned post-subscription onboarding progress for an organization.
/// Rows are created only via ensure for NEW orgs — never backfilled for existing orgs.
/// </summary>
public sealed class OrganizationOnboardingProgress
{
    public const int StatusMaxLength = 32;

    public PosOrganizationId OrganizationId { get; }
    public OnboardingStepStatus OrganizationSetupStatus { get; private set; }
    public OnboardingStepStatus BusinessSetupStatus { get; private set; }
    public OnboardingStepStatus ProductTemplateStatus { get; private set; }
    public OnboardingOverallStatus OverallStatus { get; private set; }
    public Guid? PrimaryBusinessTypeId { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }

    private OrganizationOnboardingProgress(
        PosOrganizationId organizationId,
        OnboardingStepStatus organizationSetupStatus,
        OnboardingStepStatus businessSetupStatus,
        OnboardingStepStatus productTemplateStatus,
        OnboardingOverallStatus overallStatus,
        Guid? primaryBusinessTypeId,
        DateTimeOffset updatedAtUtc,
        DateTimeOffset createdAtUtc)
    {
        OrganizationId = organizationId;
        OrganizationSetupStatus = organizationSetupStatus;
        BusinessSetupStatus = businessSetupStatus;
        ProductTemplateStatus = productTemplateStatus;
        OverallStatus = overallStatus;
        PrimaryBusinessTypeId = primaryBusinessTypeId;
        UpdatedAtUtc = updatedAtUtc;
        CreatedAtUtc = createdAtUtc;
    }

    public static OrganizationOnboardingProgress Create(
        PosOrganizationId organizationId,
        Guid? primaryBusinessTypeId = null,
        DateTimeOffset? utcNow = null)
    {
        var now = utcNow ?? DateTimeOffset.UtcNow;
        EnsureUtc(now);
        if (primaryBusinessTypeId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidOnboardingPrimaryBusinessTypeId,
                "Primary business type id must be a non-empty GUID when provided.");
        }

        return new OrganizationOnboardingProgress(
            organizationId,
            OnboardingStepStatus.NotStarted,
            OnboardingStepStatus.NotStarted,
            OnboardingStepStatus.NotStarted,
            OnboardingOverallStatus.InProgress,
            primaryBusinessTypeId,
            now,
            now);
    }

    public static OrganizationOnboardingProgress Rehydrate(
        PosOrganizationId organizationId,
        OnboardingStepStatus organizationSetupStatus,
        OnboardingStepStatus businessSetupStatus,
        OnboardingStepStatus productTemplateStatus,
        OnboardingOverallStatus overallStatus,
        Guid? primaryBusinessTypeId,
        DateTimeOffset updatedAtUtc,
        DateTimeOffset createdAtUtc) =>
        new(
            organizationId,
            organizationSetupStatus,
            businessSetupStatus,
            productTemplateStatus,
            overallStatus,
            primaryBusinessTypeId,
            updatedAtUtc,
            createdAtUtc);

    public void EnsureInProgress(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (OverallStatus == OnboardingOverallStatus.Completed)
        {
            return;
        }

        if (OverallStatus == OnboardingOverallStatus.InProgress)
        {
            return;
        }

        OverallStatus = OnboardingOverallStatus.InProgress;
        UpdatedAtUtc = utcNow;
    }

    public void MarkOrganizationSetup(OnboardingStepStatus status, DateTimeOffset utcNow)
    {
        ApplyStepStatus(status, s => OrganizationSetupStatus = s, utcNow);
    }

    public void MarkBusinessSetup(OnboardingStepStatus status, DateTimeOffset utcNow)
    {
        ApplyStepStatus(status, s => BusinessSetupStatus = s, utcNow);
    }

    public void MarkProductTemplate(OnboardingStepStatus status, DateTimeOffset utcNow)
    {
        ApplyStepStatus(status, s => ProductTemplateStatus = s, utcNow);
    }

    public void MarkFinishedLater(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (OverallStatus == OnboardingOverallStatus.Completed)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidOnboardingOverallStatusTransition,
                "Completed onboarding cannot be marked FinishedLater.");
        }

        OverallStatus = OnboardingOverallStatus.FinishedLater;
        UpdatedAtUtc = utcNow;
    }

    public void MarkCompleted(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (OverallStatus == OnboardingOverallStatus.Completed)
        {
            UpdatedAtUtc = utcNow;
            return;
        }

        if (!AllStepsResolved())
        {
            throw new DomainException(
                DomainErrorCodes.InvalidOnboardingCompletion,
                "All onboarding steps must be Completed or Skipped before marking overall Completed.");
        }

        OverallStatus = OnboardingOverallStatus.Completed;
        UpdatedAtUtc = utcNow;
    }

    public void SetPrimaryBusinessTypeId(Guid? primaryBusinessTypeId, DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (primaryBusinessTypeId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidOnboardingPrimaryBusinessTypeId,
                "Primary business type id must be a non-empty GUID when provided.");
        }

        PrimaryBusinessTypeId = primaryBusinessTypeId;
        UpdatedAtUtc = utcNow;
    }

    private void ApplyStepStatus(
        OnboardingStepStatus status,
        Action<OnboardingStepStatus> assign,
        DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (status is not (OnboardingStepStatus.Completed or OnboardingStepStatus.Skipped))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidOnboardingStepStatus,
                "Step status must be Completed or Skipped.");
        }

        assign(status);
        UpdatedAtUtc = utcNow;

        if (OverallStatus == OnboardingOverallStatus.FinishedLater)
        {
            OverallStatus = OnboardingOverallStatus.InProgress;
        }

        // Do not auto-complete overall here. Ready screen / Start Selling / Finish Later
        // owns overall completion so interrupt-before-Ready can still resume.
        if (!AllStepsResolved() && OverallStatus == OnboardingOverallStatus.Completed)
        {
            OverallStatus = OnboardingOverallStatus.InProgress;
        }
    }

    private bool AllStepsResolved() =>
        IsResolved(OrganizationSetupStatus)
        && IsResolved(BusinessSetupStatus)
        && IsResolved(ProductTemplateStatus);

    private static bool IsResolved(OnboardingStepStatus status) =>
        status is OnboardingStepStatus.Completed or OnboardingStepStatus.Skipped;

    private static void EnsureUtc(DateTimeOffset utcNow)
    {
        if (utcNow.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Timestamp must be UTC.");
        }
    }
}
