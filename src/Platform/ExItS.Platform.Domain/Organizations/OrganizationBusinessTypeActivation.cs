using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.GlobalCatalog;

namespace ExItS.Platform.Domain.Organizations;

/// <summary>
/// Organization-activated additional Business Type (add-on).
/// PrimaryBusinessTypeId remains on <see cref="PlatformOrganization"/> and is never managed here.
/// Subscription-grant authorization is enforced in application/WP03 — not on this aggregate.
/// </summary>
public sealed class OrganizationBusinessTypeActivation
{
    public PlatformOrganizationId OrganizationId { get; }
    public BusinessTypeId BusinessTypeId { get; }
    public DateTimeOffset ActivatedAtUtc { get; }

    private OrganizationBusinessTypeActivation(
        PlatformOrganizationId organizationId,
        BusinessTypeId businessTypeId,
        DateTimeOffset activatedAtUtc)
    {
        OrganizationId = organizationId;
        BusinessTypeId = businessTypeId;
        ActivatedAtUtc = activatedAtUtc;
    }

    /// <summary>
    /// Creates an add-on activation. Rejects activating the org primary type (primary is authoritative elsewhere).
    /// </summary>
    public static OrganizationBusinessTypeActivation Activate(
        PlatformOrganizationId organizationId,
        BusinessTypeId businessTypeId,
        DateTimeOffset utcNow,
        BusinessTypeId? primaryBusinessTypeId = null)
    {
        ArgumentNullException.ThrowIfNull(organizationId);
        ArgumentNullException.ThrowIfNull(businessTypeId);
        DomainTime.EnsureUtc(utcNow);

        if (businessTypeId.Value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGlobalCatalogBusinessType,
                "Business type id cannot be empty.");
        }

        if (primaryBusinessTypeId is not null && primaryBusinessTypeId == businessTypeId)
        {
            throw new DomainException(
                DomainErrorCodes.PrimaryBusinessTypeActivationForbidden,
                "Primary business type is not managed through additional-type activations.");
        }

        return new OrganizationBusinessTypeActivation(organizationId, businessTypeId, utcNow);
    }

    internal static OrganizationBusinessTypeActivation Rehydrate(
        PlatformOrganizationId organizationId,
        BusinessTypeId businessTypeId,
        DateTimeOffset activatedAtUtc) =>
        new(organizationId, businessTypeId, activatedAtUtc);

    /// <summary>Ensures a set of activations has no duplicate BusinessTypeId for one organization.</summary>
    public static void EnsureUnique(IEnumerable<OrganizationBusinessTypeActivation> activations)
    {
        ArgumentNullException.ThrowIfNull(activations);
        var seen = new HashSet<Guid>();
        foreach (var activation in activations)
        {
            ArgumentNullException.ThrowIfNull(activation);
            if (!seen.Add(activation.BusinessTypeId.Value))
            {
                throw new DomainException(
                    DomainErrorCodes.DuplicateBusinessTypeActivation,
                    $"Duplicate business type activation '{activation.BusinessTypeId}'.");
            }
        }
    }
}
