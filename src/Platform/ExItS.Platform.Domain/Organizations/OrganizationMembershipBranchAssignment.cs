using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Organizations;

/// <summary>
/// Explicit branch workspace authorization for organization staff (OrganizationMember).
/// Owner/Administrator roles do not require rows — they inherit all Active branches.
/// </summary>
public sealed class OrganizationMembershipBranchAssignment
{
    public OrganizationMembershipBranchAssignmentId Id { get; }
    public PlatformOrganizationId OrganizationId { get; }
    public OrganizationMembershipId MembershipId { get; }
    public OrganizationBranchId BranchId { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public string? ActorReference { get; }

    private OrganizationMembershipBranchAssignment(
        OrganizationMembershipBranchAssignmentId id,
        PlatformOrganizationId organizationId,
        OrganizationMembershipId membershipId,
        OrganizationBranchId branchId,
        DateTimeOffset createdAtUtc,
        string? actorReference)
    {
        Id = id;
        OrganizationId = organizationId;
        MembershipId = membershipId;
        BranchId = branchId;
        CreatedAtUtc = createdAtUtc;
        ActorReference = actorReference;
    }

    public static OrganizationMembershipBranchAssignment Create(
        PlatformOrganizationId organizationId,
        OrganizationMembershipId membershipId,
        OrganizationBranchId branchId,
        DateTimeOffset utcNow,
        OrganizationMembershipBranchAssignmentId? id = null,
        string? actorReference = null)
    {
        ArgumentNullException.ThrowIfNull(organizationId);
        ArgumentNullException.ThrowIfNull(membershipId);
        ArgumentNullException.ThrowIfNull(branchId);
        EnsureUtc(utcNow);

        return new OrganizationMembershipBranchAssignment(
            id ?? OrganizationMembershipBranchAssignmentId.New(),
            organizationId,
            membershipId,
            branchId,
            utcNow,
            NormalizeOptional(actorReference));
    }

    public static OrganizationMembershipBranchAssignment Rehydrate(
        OrganizationMembershipBranchAssignmentId id,
        PlatformOrganizationId organizationId,
        OrganizationMembershipId membershipId,
        OrganizationBranchId branchId,
        DateTimeOffset createdAtUtc,
        string? actorReference) =>
        new(id, organizationId, membershipId, branchId, createdAtUtc, actorReference);

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
