using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Domain.Organizations;

/// <summary>
/// Organization approval before a staff workplace password can change.
/// The password itself is never stored on this record.
/// </summary>
public sealed class StaffPasswordResetRequest
{
    public static readonly TimeSpan PendingLifetime = TimeSpan.FromDays(3);
    public static readonly TimeSpan ApprovedLifetime = TimeSpan.FromHours(24);

    public StaffPasswordResetRequestId Id { get; }
    public PlatformOrganizationId OrganizationId { get; }
    public PlatformUserId StaffUserId { get; }
    public OrganizationMembershipId MembershipId { get; }
    public PlatformUserId RequestedByUserId { get; }
    public StaffPasswordResetRequestStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? DecidedAtUtc { get; private set; }
    public PlatformUserId? DecidedByUserId { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private StaffPasswordResetRequest(
        StaffPasswordResetRequestId id,
        PlatformOrganizationId organizationId,
        PlatformUserId staffUserId,
        OrganizationMembershipId membershipId,
        PlatformUserId requestedByUserId,
        StaffPasswordResetRequestStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset? decidedAtUtc,
        PlatformUserId? decidedByUserId,
        DateTimeOffset? completedAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        StaffUserId = staffUserId;
        MembershipId = membershipId;
        RequestedByUserId = requestedByUserId;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        DecidedAtUtc = decidedAtUtc;
        DecidedByUserId = decidedByUserId;
        CompletedAtUtc = completedAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static StaffPasswordResetRequest Create(
        PlatformOrganizationId organizationId,
        PlatformUserId staffUserId,
        OrganizationMembershipId membershipId,
        PlatformUserId requestedByUserId,
        DateTimeOffset utcNow,
        StaffPasswordResetRequestId? id = null)
    {
        ArgumentNullException.ThrowIfNull(organizationId);
        ArgumentNullException.ThrowIfNull(staffUserId);
        ArgumentNullException.ThrowIfNull(membershipId);
        ArgumentNullException.ThrowIfNull(requestedByUserId);
        EnsureUtc(utcNow);

        return new StaffPasswordResetRequest(
            id ?? StaffPasswordResetRequestId.New(),
            organizationId,
            staffUserId,
            membershipId,
            requestedByUserId,
            StaffPasswordResetRequestStatus.Pending,
            utcNow,
            utcNow.Add(PendingLifetime),
            decidedAtUtc: null,
            decidedByUserId: null,
            completedAtUtc: null,
            utcNow);
    }

    public static StaffPasswordResetRequest Rehydrate(
        StaffPasswordResetRequestId id,
        PlatformOrganizationId organizationId,
        PlatformUserId staffUserId,
        OrganizationMembershipId membershipId,
        PlatformUserId requestedByUserId,
        StaffPasswordResetRequestStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset? decidedAtUtc,
        PlatformUserId? decidedByUserId,
        DateTimeOffset? completedAtUtc,
        DateTimeOffset updatedAtUtc) =>
        new(
            id,
            organizationId,
            staffUserId,
            membershipId,
            requestedByUserId,
            status,
            createdAtUtc,
            expiresAtUtc,
            decidedAtUtc,
            decidedByUserId,
            completedAtUtc,
            updatedAtUtc);

    public bool IsOpen =>
        Status is StaffPasswordResetRequestStatus.Pending or StaffPasswordResetRequestStatus.Approved;

    public bool IsPastExpiry(DateTimeOffset utcNow) => IsOpen && utcNow >= ExpiresAtUtc;

    public void Approve(PlatformUserId actorUserId, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(actorUserId);
        EnsureUtc(utcNow);
        EnsureStatus(StaffPasswordResetRequestStatus.Pending, utcNow);
        RejectSelfDecision(actorUserId);
        Status = StaffPasswordResetRequestStatus.Approved;
        DecidedAtUtc = utcNow;
        DecidedByUserId = actorUserId;
        ExpiresAtUtc = utcNow.Add(ApprovedLifetime);
        UpdatedAtUtc = utcNow;
    }

    public void Deny(PlatformUserId actorUserId, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(actorUserId);
        EnsureUtc(utcNow);
        EnsureStatus(StaffPasswordResetRequestStatus.Pending, utcNow);
        RejectSelfDecision(actorUserId);
        Status = StaffPasswordResetRequestStatus.Denied;
        DecidedAtUtc = utcNow;
        DecidedByUserId = actorUserId;
        UpdatedAtUtc = utcNow;
    }

    public void Complete(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        EnsureStatus(StaffPasswordResetRequestStatus.Approved, utcNow);
        Status = StaffPasswordResetRequestStatus.Completed;
        CompletedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public void MarkExpired(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (!IsOpen)
        {
            return;
        }

        if (utcNow < ExpiresAtUtc)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStaffPasswordResetStatusTransition,
                "Password reset request has not expired yet.");
        }

        Status = StaffPasswordResetRequestStatus.Expired;
        UpdatedAtUtc = utcNow;
    }

    private void RejectSelfDecision(PlatformUserId actorUserId)
    {
        if (actorUserId.Equals(StaffUserId))
        {
            throw new DomainException(
                DomainErrorCodes.StaffPasswordResetSelfDenied,
                "The staff member cannot decide their own password reset.");
        }
    }

    private void EnsureStatus(StaffPasswordResetRequestStatus expected, DateTimeOffset utcNow)
    {
        if (Status != expected)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStaffPasswordResetStatusTransition,
                "Password reset request cannot change from its current status.");
        }

        if (utcNow >= ExpiresAtUtc)
        {
            throw new DomainException(
                DomainErrorCodes.StaffPasswordResetExpired,
                "Password reset request has expired.");
        }
    }

    private static void EnsureUtc(DateTimeOffset utcNow)
    {
        if (utcNow.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Timestamp must be UTC.");
        }
    }
}
