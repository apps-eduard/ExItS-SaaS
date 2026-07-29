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
    public DateTimeOffset? SuspendedAtUtc { get; private set; }
    public DateTimeOffset? RemovedAtUtc { get; private set; }
    public string? Reason { get; private set; }
    public string? ActorReference { get; private set; }

    private OrganizationMembership(
        OrganizationMembershipId id,
        PlatformOrganizationId organizationId,
        PlatformUserId userId,
        MembershipStatus status,
        OrganizationRole role,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        DateTimeOffset? suspendedAtUtc,
        DateTimeOffset? removedAtUtc,
        string? reason,
        string? actorReference)
    {
        Id = id;
        OrganizationId = organizationId;
        UserId = userId;
        Status = status;
        Role = role;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        SuspendedAtUtc = suspendedAtUtc;
        RemovedAtUtc = removedAtUtc;
        Reason = reason;
        ActorReference = actorReference;
    }

    public static OrganizationMembership Create(
        PlatformOrganizationId organizationId,
        PlatformUserId userId,
        OrganizationRole role,
        DateTimeOffset utcNow,
        OrganizationMembershipId? id = null,
        string? actorReference = null)
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
            utcNow,
            null,
            null,
            null,
            NormalizeOptional(actorReference));
    }

    public static OrganizationMembership Rehydrate(
        OrganizationMembershipId id,
        PlatformOrganizationId organizationId,
        PlatformUserId userId,
        MembershipStatus status,
        OrganizationRole role,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        DateTimeOffset? suspendedAtUtc,
        DateTimeOffset? removedAtUtc,
        string? reason,
        string? actorReference) =>
        new(
            id,
            organizationId,
            userId,
            status,
            role,
            createdAtUtc,
            updatedAtUtc,
            suspendedAtUtc,
            removedAtUtc,
            reason,
            actorReference);

    public void ChangeRole(OrganizationRole role, DateTimeOffset utcNow, string? actorReference = null)
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
        ActorReference = NormalizeOptional(actorReference) ?? ActorReference;
        UpdatedAtUtc = utcNow;
    }

    public void Suspend(DateTimeOffset utcNow, string? reason = null, string? actorReference = null)
    {
        EnsureUtc(utcNow);
        TransitionTo(MembershipStatus.Suspended, utcNow);
        SuspendedAtUtc = utcNow;
        Reason = NormalizeOptional(reason) ?? Reason;
        ActorReference = NormalizeOptional(actorReference) ?? ActorReference;
    }

    public void Reactivate(DateTimeOffset utcNow, string? actorReference = null)
    {
        EnsureUtc(utcNow);
        if (Status == MembershipStatus.Removed)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidMembershipStatusTransition,
                "A removed membership cannot be reactivated. Create a new membership explicitly.");
        }

        TransitionTo(MembershipStatus.Active, utcNow);
        SuspendedAtUtc = null;
        ActorReference = NormalizeOptional(actorReference) ?? ActorReference;
    }

    public void Remove(DateTimeOffset utcNow, string? reason = null, string? actorReference = null)
    {
        EnsureUtc(utcNow);
        TransitionTo(MembershipStatus.Removed, utcNow);
        RemovedAtUtc = utcNow;
        Reason = NormalizeOptional(reason) ?? Reason;
        ActorReference = NormalizeOptional(actorReference) ?? ActorReference;
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

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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
