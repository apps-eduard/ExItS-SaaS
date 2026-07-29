using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Domain.Catalog;

/// <summary>
/// Reusable trial definition. Not an organization's active trial subscription.
/// Duration is supplied by approved configuration or application input (positive TimeSpan).
/// Product-specific calendar policies (e.g. PinoyBusinessPOS three calendar months) are not
/// hard-coded here and must be applied by a later configuration work package.
/// </summary>
public sealed class TrialDefinition
{
    private readonly List<FeatureGrantSpec> _featureGrants;
    private readonly List<FeatureGrantSpec> _postExpiryFeatureGrants;

    public TrialDefinitionId Id { get; }
    public ProductCode ProductCode { get; }
    public PlanId? PlanId { get; }
    public string DisplayName { get; private set; }
    public TimeSpan Duration { get; }
    public TrialDefinitionStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public IReadOnlyList<FeatureGrantSpec> FeatureGrants => _featureGrants;
    public IReadOnlyList<FeatureGrantSpec> PostExpiryFeatureGrants => _postExpiryFeatureGrants;

    private TrialDefinition(
        TrialDefinitionId id,
        ProductCode productCode,
        PlanId? planId,
        string displayName,
        TimeSpan duration,
        TrialDefinitionStatus status,
        IEnumerable<FeatureGrantSpec> featureGrants,
        IEnumerable<FeatureGrantSpec> postExpiryFeatureGrants,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        ProductCode = productCode;
        PlanId = planId;
        DisplayName = displayName;
        Duration = duration;
        Status = status;
        _featureGrants = featureGrants.ToList();
        _postExpiryFeatureGrants = postExpiryFeatureGrants.ToList();
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static TrialDefinition Create(
        ProductCode productCode,
        string displayName,
        TimeSpan duration,
        IReadOnlyList<FeatureGrantSpec> featureGrants,
        IReadOnlyList<FeatureGrantSpec> postExpiryFeatureGrants,
        DateTimeOffset utcNow,
        PlanId? planId = null,
        TrialDefinitionId? id = null)
    {
        ArgumentNullException.ThrowIfNull(productCode);
        ArgumentNullException.ThrowIfNull(featureGrants);
        ArgumentNullException.ThrowIfNull(postExpiryFeatureGrants);
        DomainTime.EnsureUtc(utcNow);

        if (duration <= TimeSpan.Zero)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidTrialDuration,
                "Trial duration must be positive.");
        }

        return new TrialDefinition(
            id ?? TrialDefinitionId.New(),
            productCode,
            planId,
            DomainTime.NormalizeDisplayName(displayName),
            duration,
            TrialDefinitionStatus.Active,
            featureGrants,
            postExpiryFeatureGrants,
            utcNow,
            utcNow);
    }

    public void Retire(DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);
        if (Status == TrialDefinitionStatus.Retired)
        {
            return;
        }

        Status = TrialDefinitionStatus.Retired;
        UpdatedAtUtc = utcNow;
    }
}
