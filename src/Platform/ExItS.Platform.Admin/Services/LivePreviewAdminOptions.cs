namespace ExItS.Platform.Admin.Services;

public sealed class LivePreviewAdminOptions
{
    public const string SectionName = "LivePreview";
    public bool Enabled { get; set; }
}

public sealed record LivePreviewIdentityOptionDto(
    string Key,
    string Username,
    string DisplayName,
    string Email,
    Guid UserId,
    Guid? OrganizationId,
    string Summary,
    string? PosLocalRoleCode);
