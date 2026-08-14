namespace ExItS.Platform.Domain.Organizations;

/// <summary>Lifecycle status for an organization ownership transfer.</summary>
public enum OrganizationOwnershipTransferStatus
{
    Pending = 0,
    Accepted = 1,
    Declined = 2,
    Cancelled = 3,
    Expired = 4
}
