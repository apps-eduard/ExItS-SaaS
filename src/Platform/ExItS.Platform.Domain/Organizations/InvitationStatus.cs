namespace ExItS.Platform.Domain.Organizations;

/// <summary>Lifecycle status for an organization invitation (single-use when Accepted).</summary>
public enum InvitationStatus
{
    Pending = 0,
    Accepted = 1,
    Revoked = 2,
    Expired = 3,
    Declined = 4
}
