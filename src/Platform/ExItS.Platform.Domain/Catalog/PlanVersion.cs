using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Domain.Catalog;

/// <summary>
/// Historical commercial plan version. Published versions are immutable.
/// </summary>
public sealed class PlanVersion
{
    private readonly List<FeatureGrantSpec> _grants;

    public PlanVersionId Id { get; }
    public PlanId PlanId { get; }
    public ProductCode ProductCode { get; }
    public int VersionNumber { get; }
    public DateTimeOffset EffectiveFromUtc { get; }
    public DateTimeOffset? EffectiveToUtc { get; private set; }
    public BillingPeriod BillingPeriod { get; }
    public bool TrialEligible { get; }
    public PlanVersionStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public IReadOnlyList<FeatureGrantSpec> Grants => _grants;

    private PlanVersion(
        PlanVersionId id,
        PlanId planId,
        ProductCode productCode,
        int versionNumber,
        DateTimeOffset effectiveFromUtc,
        DateTimeOffset? effectiveToUtc,
        BillingPeriod billingPeriod,
        bool trialEligible,
        PlanVersionStatus status,
        IEnumerable<FeatureGrantSpec> grants,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        PlanId = planId;
        ProductCode = productCode;
        VersionNumber = versionNumber;
        EffectiveFromUtc = effectiveFromUtc;
        EffectiveToUtc = effectiveToUtc;
        BillingPeriod = billingPeriod;
        TrialEligible = trialEligible;
        Status = status;
        _grants = grants.ToList();
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static PlanVersion CreateDraft(
        Plan plan,
        int versionNumber,
        DateTimeOffset effectiveFromUtc,
        BillingPeriod billingPeriod,
        bool trialEligible,
        IReadOnlyList<FeatureGrantSpec> grants,
        DateTimeOffset utcNow,
        DateTimeOffset? effectiveToUtc = null,
        PlanVersionId? id = null)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(grants);
        DomainTime.EnsureUtc(utcNow);
        DomainTime.EnsureUtc(effectiveFromUtc);
        if (effectiveToUtc is not null)
        {
            DomainTime.EnsureUtc(effectiveToUtc.Value);
            if (effectiveToUtc.Value <= effectiveFromUtc)
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidEffectiveRange,
                    "Plan version effective end must be after start.");
            }
        }

        if (versionNumber < 1)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPlanVersionNumber,
                "Plan version number must be positive.");
        }

        if (!Enum.IsDefined(billingPeriod))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPlanVersionNumber,
                "Billing period is not defined.");
        }

        EnsureUniqueGrants(grants);

        return new PlanVersion(
            id ?? PlanVersionId.New(),
            plan.Id,
            plan.ProductCode,
            versionNumber,
            effectiveFromUtc,
            effectiveToUtc,
            billingPeriod,
            trialEligible,
            PlanVersionStatus.Draft,
            grants,
            utcNow,
            utcNow);
    }

    internal static PlanVersion Rehydrate(
        PlanVersionId id,
        PlanId planId,
        ProductCode productCode,
        int versionNumber,
        DateTimeOffset effectiveFromUtc,
        DateTimeOffset? effectiveToUtc,
        BillingPeriod billingPeriod,
        bool trialEligible,
        PlanVersionStatus status,
        IEnumerable<FeatureGrantSpec> grants,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc) =>
        new(
            id,
            planId,
            productCode,
            versionNumber,
            effectiveFromUtc,
            effectiveToUtc,
            billingPeriod,
            trialEligible,
            status,
            grants,
            createdAtUtc,
            updatedAtUtc);

    public void ReplaceDraftGrants(IReadOnlyList<FeatureGrantSpec> grants, DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);
        EnsureDraft();
        ArgumentNullException.ThrowIfNull(grants);
        EnsureUniqueGrants(grants);
        _grants.Clear();
        _grants.AddRange(grants);
        UpdatedAtUtc = utcNow;
    }

    public void Publish(DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);
        if (Status == PlanVersionStatus.Published)
        {
            return;
        }

        if (Status != PlanVersionStatus.Draft)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPlanVersionTransition,
                $"Cannot publish PlanVersion from {Status}.");
        }

        Status = PlanVersionStatus.Published;
        UpdatedAtUtc = utcNow;
    }

    public void Retire(DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);
        if (Status == PlanVersionStatus.Retired)
        {
            return;
        }

        if (Status == PlanVersionStatus.Draft)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPlanVersionTransition,
                "Draft plan versions should be discarded, not retired. Publish first or abandon.");
        }

        Status = PlanVersionStatus.Retired;
        UpdatedAtUtc = utcNow;
    }

    private void EnsureDraft()
    {
        if (Status != PlanVersionStatus.Draft)
        {
            throw new DomainException(
                DomainErrorCodes.PlanVersionImmutable,
                "Published or retired plan versions are immutable.");
        }
    }

    private static void EnsureUniqueGrants(IReadOnlyList<FeatureGrantSpec> grants)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var grant in grants)
        {
            if (!seen.Add(grant.FeatureCode.Value))
            {
                throw new DomainException(
                    DomainErrorCodes.DuplicateFeatureCode,
                    $"Duplicate feature grant '{grant.FeatureCode}'.");
            }
        }
    }
}
