namespace ExItS.Platform.Domain.Organizations;

/// <summary>
/// Stable invitation type discriminators. Accepting one kind must never create another relationship type.
/// </summary>
public static class InvitationKinds
{
    public const string OrganizationStaffInvitation = "OrganizationStaffInvitation";
    public const string CustomerLinkRequest = "CustomerLinkRequest";
}
