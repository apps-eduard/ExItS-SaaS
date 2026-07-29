namespace ExItS.Platform.Application.Common;

public static class ApplicationErrorCodes
{
    public const string UserNotFound = "application.user.not_found";
    public const string OrganizationNotFound = "application.organization.not_found";
    public const string MembershipNotFound = "application.membership.not_found";
    public const string EmailConflict = "application.user.email_conflict";
    public const string SlugConflict = "application.organization.slug_conflict";
    public const string MembershipConflict = "application.membership.conflict";
    public const string DomainViolation = "application.domain_violation";
}
