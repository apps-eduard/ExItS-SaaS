using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Domain.Organizations;

/// <summary>
/// Organization ownership transfer — handoff of the sole Organization Owner seat from one
/// Personal identity to another. Does not create org-scoped staff identities and must never
/// be confused with <see cref="OrganizationInvitation"/> (staff invites).
/// </summary>
public sealed class OrganizationOwnershipTransfer
{
    public const int DefaultLifetimeDays = 7;

    public OrganizationOwnershipTransferId Id { get; }
    public PlatformOrganizationId OrganizationId { get; }
    public PlatformUserId FromOwnerUserId { get; }
    public PlatformUserId ToUserId { get; }
    public OrganizationOwnershipTransferStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? AcceptedAtUtc { get; private set; }
    public DateTimeOffset? DeclinedAtUtc { get; private set; }
    public DateTimeOffset? CancelledAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private OrganizationOwnershipTransfer(
        OrganizationOwnershipTransferId id,
        PlatformOrganizationId organizationId,
        PlatformUserId fromOwnerUserId,
        PlatformUserId toUserId,
        OrganizationOwnershipTransferStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset? acceptedAtUtc,
        DateTimeOffset? declinedAtUtc,
        DateTimeOffset? cancelledAtUtc,
        DateTimeOffset? completedAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        FromOwnerUserId = fromOwnerUserId;
        ToUserId = toUserId;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        AcceptedAtUtc = acceptedAtUtc;
        DeclinedAtUtc = declinedAtUtc;
        CancelledAtUtc = cancelledAtUtc;
        CompletedAtUtc = completedAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static OrganizationOwnershipTransfer Create(
        PlatformOrganizationId organizationId,
        PlatformUserId fromOwnerUserId,
        PlatformUserId toUserId,
        DateTimeOffset utcNow,
        TimeSpan? lifetime = null,
        OrganizationOwnershipTransferId? id = null)
    {
        ArgumentNullException.ThrowIfNull(organizationId);
        ArgumentNullException.ThrowIfNull(fromOwnerUserId);
        ArgumentNullException.ThrowIfNull(toUserId);
        EnsureUtc(utcNow);

        if (fromOwnerUserId == toUserId)
        {
            throw new DomainException(
                DomainErrorCodes.OwnershipTransferSelfDenied,
                "You already own this business.");
        }

        return new OrganizationOwnershipTransfer(
            id ?? OrganizationOwnershipTransferId.New(),
            organizationId,
            fromOwnerUserId,
            toUserId,
            OrganizationOwnershipTransferStatus.Pending,
            utcNow,
            utcNow.Add(lifetime ?? TimeSpan.FromDays(DefaultLifetimeDays)),
            null,
            null,
            null,
            null,
            utcNow);
    }

    public static OrganizationOwnershipTransfer Rehydrate(
        OrganizationOwnershipTransferId id,
        PlatformOrganizationId organizationId,
        PlatformUserId fromOwnerUserId,
        PlatformUserId toUserId,
        OrganizationOwnershipTransferStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset? acceptedAtUtc,
        DateTimeOffset? declinedAtUtc,
        DateTimeOffset? cancelledAtUtc,
        DateTimeOffset? completedAtUtc,
        DateTimeOffset updatedAtUtc) =>
        new(
            id,
            organizationId,
            fromOwnerUserId,
            toUserId,
            status,
            createdAtUtc,
            expiresAtUtc,
            acceptedAtUtc,
            declinedAtUtc,
            cancelledAtUtc,
            completedAtUtc,
            updatedAtUtc);

    public void Accept(PlatformUserId actorUserId, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(actorUserId);
        EnsureUtc(utcNow);
        EnsurePendingUsable(utcNow);

        if (actorUserId != ToUserId)
        {
            throw new DomainException(
                DomainErrorCodes.OwnershipTransferActorMismatch,
                "Only the transfer recipient can accept ownership.");
        }

        Status = OrganizationOwnershipTransferStatus.Accepted;
        AcceptedAtUtc = utcNow;
        CompletedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public void Decline(PlatformUserId actorUserId, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(actorUserId);
        EnsureUtc(utcNow);
        EnsurePendingUsable(utcNow);

        if (actorUserId != ToUserId)
        {
            throw new DomainException(
                DomainErrorCodes.OwnershipTransferActorMismatch,
                "Only the transfer recipient can decline ownership.");
        }

        Status = OrganizationOwnershipTransferStatus.Declined;
        DeclinedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public void Cancel(PlatformUserId actorUserId, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(actorUserId);
        EnsureUtc(utcNow);
        EnsurePendingUsable(utcNow);

        if (actorUserId != FromOwnerUserId)
        {
            throw new DomainException(
                DomainErrorCodes.OwnershipTransferActorMismatch,
                "Only the current owner who requested the transfer can cancel it.");
        }

        Status = OrganizationOwnershipTransferStatus.Cancelled;
        CancelledAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public void MarkExpired(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (Status != OrganizationOwnershipTransferStatus.Pending)
        {
            return;
        }

        if (utcNow < ExpiresAtUtc)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidOwnershipTransferStatusTransition,
                "Ownership transfer has not expired yet.");
        }

        Status = OrganizationOwnershipTransferStatus.Expired;
        UpdatedAtUtc = utcNow;
    }

    public bool IsExpired(DateTimeOffset utcNow) =>
        Status == OrganizationOwnershipTransferStatus.Pending && utcNow >= ExpiresAtUtc;

    private void EnsurePendingUsable(DateTimeOffset utcNow)
    {
        if (Status != OrganizationOwnershipTransferStatus.Pending)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidOwnershipTransferStatusTransition,
                $"Ownership transfer is not pending (status {Status}).");
        }

        if (utcNow >= ExpiresAtUtc)
        {
            Status = OrganizationOwnershipTransferStatus.Expired;
            UpdatedAtUtc = utcNow;
            throw new DomainException(
                DomainErrorCodes.OwnershipTransferExpired,
                "Ownership transfer has expired.");
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
