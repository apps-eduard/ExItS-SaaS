using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Organizations;

/// <summary>
/// Area grant for organization staff (<see cref="BranchAccessScope.Areas"/>).
/// Accessible branches are derived from the Active branches inside the granted Active Areas;
/// the row itself confers no operational authority.
/// </summary>
public sealed class OrganizationMembershipAreaAssignment
{
    public OrganizationMembershipAreaAssignmentId Id { get; }
    public PlatformOrganizationId OrganizationId { get; }
    public OrganizationMembershipId MembershipId { get; }
    public OrganizationAreaId AreaId { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public string? ActorReference { get; }

    private OrganizationMembershipAreaAssignment(
        OrganizationMembershipAreaAssignmentId id,
        PlatformOrganizationId organizationId,
        OrganizationMembershipId membershipId,
        OrganizationAreaId areaId,
        DateTimeOffset createdAtUtc,
        string? actorReference)
    {
        Id = id;
        OrganizationId = organizationId;
        MembershipId = membershipId;
        AreaId = areaId;
        CreatedAtUtc = createdAtUtc;
        ActorReference = actorReference;
    }

    public static OrganizationMembershipAreaAssignment Create(
        PlatformOrganizationId organizationId,
        OrganizationMembershipId membershipId,
        OrganizationAreaId areaId,
        DateTimeOffset utcNow,
        OrganizationMembershipAreaAssignmentId? id = null,
        string? actorReference = null)
    {
        ArgumentNullException.ThrowIfNull(organizationId);
        ArgumentNullException.ThrowIfNull(membershipId);
        ArgumentNullException.ThrowIfNull(areaId);
        EnsureUtc(utcNow);

        return new OrganizationMembershipAreaAssignment(
            id ?? OrganizationMembershipAreaAssignmentId.New(),
            organizationId,
            membershipId,
            areaId,
            utcNow,
            NormalizeOptional(actorReference));
    }

    public static OrganizationMembershipAreaAssignment Rehydrate(
        OrganizationMembershipAreaAssignmentId id,
        PlatformOrganizationId organizationId,
        OrganizationMembershipId membershipId,
        OrganizationAreaId areaId,
        DateTimeOffset createdAtUtc,
        string? actorReference) =>
        new(id, organizationId, membershipId, areaId, createdAtUtc, actorReference);

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidUtcTimestamp,
                "Timestamps must be UTC (offset zero).");
        }
    }
}
