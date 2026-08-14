namespace ExItS.Platform.Domain.Organizations;

/// <summary>
/// Organization-scoped compliance profile anchor.
/// Regulatory identity fields are not invented here; confirmed future requirements
/// must be added only when accreditation/registration sources are recorded.
/// Business identity currently continues to live on <see cref="OrganizationProfile"/>.
/// </summary>
public sealed class OrganizationComplianceProfile
{
    public PlatformOrganizationId OrganizationId { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public string? UpdatedByActorReference { get; private set; }

    private OrganizationComplianceProfile(
        PlatformOrganizationId organizationId,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        string? updatedByActorReference)
    {
        OrganizationId = organizationId;
        CreatedAtUtc = EnsureUtc(createdAtUtc);
        UpdatedAtUtc = EnsureUtc(updatedAtUtc);
        UpdatedByActorReference = updatedByActorReference;
    }

    public static OrganizationComplianceProfile Create(
        PlatformOrganizationId organizationId,
        DateTimeOffset utcNow,
        string? actorReference = null) =>
        new(organizationId, utcNow, utcNow, actorReference);

    public static OrganizationComplianceProfile Rehydrate(
        PlatformOrganizationId organizationId,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        string? updatedByActorReference) =>
        new(organizationId, createdAtUtc, updatedAtUtc, updatedByActorReference);

    public void Touch(string actorReference, DateTimeOffset utcNow)
    {
        if (string.IsNullOrWhiteSpace(actorReference))
        {
            throw new ArgumentException("Actor reference is required.", nameof(actorReference));
        }

        UpdatedAtUtc = EnsureUtc(utcNow);
        UpdatedByActorReference = actorReference.Trim();
    }

    private static DateTimeOffset EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Timestamp must be UTC.", nameof(value));
        }

        return value;
    }
}
