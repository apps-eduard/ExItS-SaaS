using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Domain.Organizations;

/// <summary>
/// Organization membership — Platform-level participation for one user in one organization.
/// Does not grant product-local roles (Doctor, Cashier, etc.).
/// </summary>
public sealed class OrganizationMembership
{
    public OrganizationMembershipId Id { get; }
    public PlatformOrganizationId OrganizationId { get; }
    public PlatformUserId UserId { get; }
    public MembershipStatus Status { get; private set; }
    public OrganizationRole Role { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private OrganizationMembership(
        OrganizationMembershipId id,
        PlatformOrganizationId organizationId,
        PlatformUserId userId,
        MembershipStatus status,
        OrganizationRole role,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        UserId = userId;
        Status = status;
        Role = role;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static OrganizationMembership Create(
        PlatformOrganizationId organizationId,
        PlatformUserId userId,
        OrganizationRole role,
        DateTimeOffset utcNow,
        OrganizationMembershipId? id = null)
    {
        ArgumentNullException.ThrowIfNull(organizationId);
        ArgumentNullException.ThrowIfNull(userId);
        EnsureUtc(utcNow);
        EnsureDefinedRole(role);

        return new OrganizationMembership(
            id ?? OrganizationMembershipId.New(),
            organizationId,
            userId,
            MembershipStatus.Active,
            role,
            utcNow,
            utcNow);
    }

    public void ChangeRole(OrganizationRole role, DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        EnsureDefinedRole(role);
        if (Status == MembershipStatus.Removed)
        {
            throw new DomainException(
                DomainErrorCodes.MembershipNotActive,
                "A removed membership cannot change role.");
        }

        Role = role;
        UpdatedAtUtc = utcNow;
    }

    public void Suspend(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        TransitionTo(MembershipStatus.Suspended, utcNow);
    }

    public void Reactivate(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (Status == MembershipStatus.Removed)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidMembershipStatusTransition,
                "A removed membership cannot be reactivated. Create a new membership explicitly.");
        }

        TransitionTo(MembershipStatus.Active, utcNow);
    }

    public void Remove(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        TransitionTo(MembershipStatus.Removed, utcNow);
    }

    private void TransitionTo(MembershipStatus target, DateTimeOffset utcNow)
    {
        if (Status == target)
        {
            return;
        }

        var allowed = Status switch
        {
            MembershipStatus.Active => target is MembershipStatus.Suspended or MembershipStatus.Removed,
            MembershipStatus.Suspended => target is MembershipStatus.Active or MembershipStatus.Removed,
            MembershipStatus.Removed => false,
            _ => false
        };

        if (!allowed)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidMembershipStatusTransition,
                $"Cannot transition membership from {Status} to {target}.");
        }

        Status = target;
        UpdatedAtUtc = utcNow;
    }

    private static void EnsureDefinedRole(OrganizationRole role)
    {
        if (!Enum.IsDefined(role))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidOrganizationRole,
                "Organization role is not defined.");
        }
    }

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
