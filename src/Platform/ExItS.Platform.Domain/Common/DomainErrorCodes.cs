namespace ExItS.Platform.Domain.Common;

/// <summary>Stable domain error codes for identity and organization invariants.</summary>
public static class DomainErrorCodes
{
    public const string InvalidPlatformUserId = "platform.user.id.invalid";
    public const string InvalidPlatformOrganizationId = "platform.organization.id.invalid";
    public const string InvalidOrganizationMembershipId = "platform.membership.id.invalid";

    public const string InvalidDisplayName = "platform.display_name.invalid";
    public const string InvalidEmail = "platform.email.invalid";
    public const string InvalidOrganizationSlug = "platform.organization.slug.invalid";
    public const string InvalidProductCode = "platform.product_code.invalid";
    public const string InvalidUtcTimestamp = "platform.timestamp.invalid";

    public const string InvalidAccountStatusTransition = "platform.user.status.invalid_transition";
    public const string InvalidOrganizationStatusTransition = "platform.organization.status.invalid_transition";
    public const string InvalidMembershipStatusTransition = "platform.membership.status.invalid_transition";

    public const string UserNotActive = "platform.user.not_active";
    public const string OrganizationNotActive = "platform.organization.not_active";
    public const string MembershipNotActive = "platform.membership.not_active";
    public const string InvalidOrganizationRole = "platform.membership.role.invalid";
}
