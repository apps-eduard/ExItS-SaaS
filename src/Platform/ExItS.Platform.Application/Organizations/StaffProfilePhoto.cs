namespace ExItS.Platform.Application.Organizations;

public static class StaffProfilePhoto
{
    public static string StorageKey(Guid membershipId) => "staff-" + membershipId.ToString("N");

    public static string PublicPath(Guid organizationId, Guid membershipId, long version) =>
        $"/api/v1/platform/organizations/{organizationId:D}/members/{membershipId:D}/staff-photo?v={version}";

    public static string LinkedPersonalPublicPath(Guid organizationId, Guid membershipId, long version) =>
        $"/api/v1/platform/organizations/{organizationId:D}/members/{membershipId:D}/personal-photo?v={version}";

    public static bool IsStaffPhotoPath(string? url) =>
        url?.Contains("/staff-photo", StringComparison.OrdinalIgnoreCase) == true;

    public static bool IsPersonalPhotoPath(string? url) =>
        url?.Contains("/personal/profile/photo", StringComparison.OrdinalIgnoreCase) == true
        || url?.Contains("/personal-photo", StringComparison.OrdinalIgnoreCase) == true;
}
